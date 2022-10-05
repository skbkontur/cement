using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Common.Exceptions;

namespace Common;

public sealed class VisualStudioProjectParser
{
    private readonly string solutionPath;
    private readonly string[] solutionContent;
    private readonly List<string> modulesList;
    private readonly string cwd;

    public VisualStudioProjectParser(string solutionPath, IEnumerable<Module> modules)
        : this(solutionPath, modules.Select(m => m.Name))
    {
    }

    public VisualStudioProjectParser(string solutionPath, IEnumerable<string> modules)
    {
        this.solutionPath = solutionPath;
        cwd = Path.GetDirectoryName(solutionPath);
        modulesList = modules.ToList();
        solutionContent = File.ReadAllText(solutionPath).Split('\n');
    }

    public List<string> GetReferences(BuildData buildData)
    {
        var configCsprojList = GetCsprojList(buildData).Select(csproj => Path.Combine(cwd, csproj)).ToList();
        return GetReferencesFromCsprojList(configCsprojList, buildData.Configuration);
    }

    public List<string> GetSolutionConfigsByCsproj(string csprojFullPath)
    {
        var guidToCsprojDict = GetGuidToCsprojDict();
        if (guidToCsprojDict.All(kv => kv.Value.ToLower() != csprojFullPath.ToLower()))
            return new List<string>();
        var guid = guidToCsprojDict.First(kv => kv.Value.ToLower() == csprojFullPath.ToLower()).Key;
        return GetConfigSetByGuid(guid);
    }

    public IEnumerable<string> GetReferencesFromCsproj(string csprojPath, string configuration, bool allReferences)
    {
        if (!File.Exists(csprojPath))
        {
            ConsoleWriter.Shared.WriteError(csprojPath + " not found");
            return new List<string>();
        }

        var result = new List<string>();
        var xml = new XmlDocument();
        xml.Load(csprojPath);

        var csprojDir = Path.GetDirectoryName(csprojPath);

        if (allReferences)
            return GetAllReferencesFromCsproj(xml, csprojPath, configuration);

        var refs = xml.GetElementsByTagName("HintPath");

        foreach (XmlNode reference in refs)
        {
            var path = reference.ChildNodes[0].Value;
            path = path.Replace("$(ProjectDir)", "");
            if (IsReferenceToCementModule(path, csprojDir))
                result.Add(GetRelaxedPath(path, csprojPath));
        }

        return result;
    }

    public string GetOutputPathFromCsproj(XmlDocument xml, string csprojPath, string configuration)
    {
        var refs = xml.GetElementsByTagName("PropertyGroup");
        foreach (XmlNode reference in refs)
        {
            var condition = reference?.Attributes?.GetNamedItem("Condition");
            if (condition == null || !condition.InnerText.ToLower().Contains(configuration.ToLower()))
                continue;

            foreach (XmlNode child in reference.ChildNodes)
            {
                if (child.Name == "OutputPath")
                    return child.InnerText.Trim('\\', '/');
            }
        }

        return "";
    }

    public IEnumerable<string> GetCsprojList()
    {
        var guidToCsprojDict = GetGuidToCsprojDict();
        return guidToCsprojDict.Select(kvp => kvp.Value).ToList();
    }

    public IEnumerable<string> GetCsprojList(BuildData buildData)
    {
        if (buildData.Target.EndsWith(".csproj"))
            return new List<string> {Path.Combine(cwd, buildData.Target)};

        var guidToCsprojDict = GetGuidToCsprojDict();
        var guidSetForConfig = GetGuidSetForConfig(buildData.Configuration);
        return guidSetForConfig.Where(guid => guidToCsprojDict.ContainsKey(guid)).Select(guid => guidToCsprojDict[guid]).ToList();
    }

    private List<string> GetConfigSetByGuid(string guid)
    {
        var result = new List<string>();
        foreach (
            var line in
            solutionContent
                .Where(l => !string.IsNullOrEmpty(l) && l.Contains(guid + ".") && l.ToLower().Contains(".build.0"))
        )
            result.Add(line.Split('|').First().Split(new[] {"}."}, StringSplitOptions.None).Last().Trim());
        return result;
    }

    private List<string> GetReferencesFromCsprojList(IEnumerable<string> csprojList, string configuration)
    {
        var result = new List<string>();
        foreach (var csproj in csprojList)
        {
            result.AddRange(GetReferencesFromCsproj(csproj, configuration, false));
        }

        return result;
    }

    private List<string> GetAllReferencesFromCsproj(XmlDocument xml, string csprojPath, string configuration)
    {
        var result = new List<string>();
        var outputPath = GetOutputPathFromCsproj(xml, csprojPath, configuration);

        var references = xml.GetElementsByTagName("Reference");
        foreach (XmlNode reference in references)
        {
            string path = null;

            foreach (XmlNode child in reference.ChildNodes)
            {
                if (child.Name == "HintPath")
                    path = child.InnerText;
            }

            if (path == null && reference.Attributes != null)
            {
                var includeAttribute = reference.Attributes?.GetNamedItem("Include");
                if (includeAttribute != null)
                {
                    var value = includeAttribute.Value ?? "";
                    value = value.Split(',').First();
                    path = value + ".dll";
                    path = Path.Combine(outputPath, path);
                }
            }

            path = path?.Replace("$(ProjectDir)", "");
            if (path != null)
                result.Add(GetRelaxedPath(path, csprojPath));
        }

        return result;
    }

    private List<string> GetSplitedReference(string reference)
    {
        return reference.Split('/', '\\').ToList();
    }

    private string GetRelaxedPath(string reference, string csprojPath)
    {
        csprojPath = Directory.GetParent(csprojPath).FullName;
        var splitedRef = GetSplitedReference(reference);

        while (splitedRef[0].Equals(".."))
        {
            splitedRef.RemoveAt(0);
            csprojPath = Directory.GetParent(csprojPath).FullName;
        }

        while (Helper.CurrentWorkspace.Length < csprojPath.Length)
        {
            splitedRef.Insert(0, Path.GetFileName(csprojPath));
            csprojPath = Directory.GetParent(csprojPath).FullName;
        }

        if (splitedRef.Any())
            splitedRef[0] = TryFixRegister(splitedRef[0]);

        return string.Join(Path.DirectorySeparatorChar.ToString(), splitedRef);
    }

    private string TryFixRegister(string module)
    {
        foreach (var m in modulesList)
        {
            if (string.Equals(m, module, StringComparison.CurrentCultureIgnoreCase))
                return m;
        }

        return module;
    }

    private bool IsReferenceToCementModule(string reference, string csprojDir)
    {
        try
        {
            var splitedRef = GetSplitedReference(reference);
            var currentCwd = csprojDir;
            var idx = 0;
            while (idx < splitedRef.Count() && splitedRef[idx].Equals(".."))
            {
                currentCwd = Directory.GetParent(currentCwd).FullName;
                idx++;
            }

            if (idx == splitedRef.Count())
                throw new CementException("Something went wrong. Really bad HintPath " + reference + " in " + csprojDir);

            if (!Helper.CurrentWorkspace.Equals(currentCwd))
                return false;
            return modulesList.Any(name => name.ToLower() == splitedRef[idx].ToLower());
        }
        catch (Exception)
        {
            ConsoleWriter.Shared.WriteError($"Failed to find reference {reference}");
            return false;
        }
    }

    private Dictionary<string, string> GetGuidToCsprojDict()
    {
        var result = new Dictionary<string, string>();
        foreach (var line in solutionContent.Where(l => l.StartsWith("Project")))
        {
            var splited = line.Split(',').Select(token => token.Trim()).ToList();
            if (splited.Count == 3 && splited[1].EndsWith(".csproj\""))
            {
                var file = splited[1].Substring(1, splited[1].Length - 2);
                file = Path.Combine(Directory.GetParent(solutionPath).FullName, file);
                result[splited[2].Substring(1, splited[2].Length - 2)] = file;
            }
        }

        return result;
    }

    private IEnumerable<string> GetGuidSetForConfig(string configName)
    {
        var result = new HashSet<string>();
        foreach (
            var line in
            solutionContent
                .Where(l => !string.IsNullOrEmpty(l) && l.ToLower().Contains($".{configName.ToLower()}|") && l.ToLower().Contains(".build.0"))
        )
            result.Add(line.Split('.').First().Trim());
        return result;
    }
}

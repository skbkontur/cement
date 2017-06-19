using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Common
{
    public class VisualStudioProjectParser
    {
        private readonly string solutionPath;
        private readonly string[] solutionContent;
        private readonly List<string> modulesList;
        private readonly string cwd;

        public VisualStudioProjectParser(string solutionPath, IEnumerable<Module> modules)
            :this(solutionPath, modules.Select(m => m.Name))
        {
            
        } 

        public VisualStudioProjectParser(string solutionPath, IEnumerable<string> modules) 
        {
            this.solutionPath = solutionPath;
            cwd = Path.GetDirectoryName(solutionPath);
            modulesList = modules.ToList();
            solutionContent = File.ReadAllText(solutionPath).Split('\n');
        }
		
		public List<string> GetReferences(string configName)
        {
            var configCsprojList = GetCsprojList(configName).Select(csproj => Path.Combine(cwd, csproj)).ToList();
            return GetReferencesFromCsprojList(configCsprojList);
        }

	    public List<string> GetSolutionConfigsByCsproj(string csprojFullPath)
	    {
		    var guidToCsprojDict = GetGuidToCsprojDict();
            if (guidToCsprojDict.All(kv => kv.Value.ToLower() != csprojFullPath.ToLower()))
                return new List<string>();
		    var guid = guidToCsprojDict.First(kv => kv.Value.ToLower() == csprojFullPath.ToLower()).Key;
		    return GetConfigSetByGuid(guid);
	    }

	    private List<string> GetConfigSetByGuid(string guid)
		{
			var result = new List<string>();
			foreach (
				var line in
					solutionContent
						.Where(l => !string.IsNullOrEmpty(l) && l.Contains(guid + ".") && l.ToLower().Contains(".build.0"))
				)
				result.Add(line.Split('|').First().Split(new []{"}."}, StringSplitOptions.None).Last().Trim());
			return result;
		}

	    private List<string> GetReferencesFromCsprojList(IEnumerable<string> csprojList)
        {
            var result = new List<string>();
            foreach (var csproj in csprojList)
            {
                result.AddRange(GetReferencesFromCsproj(csproj));
            }
            return result;
        }

		public IEnumerable<string> GetReferencesFromCsproj(string csprojPath, bool notOnlyCement = false)
        {
			if (!File.Exists(csprojPath))
			{
				ConsoleWriter.WriteError(csprojPath + " not found");
				return new List<string>();
			}
            var result = new List<string>();
            var xml = new XmlDocument();
            xml.Load(csprojPath);

            var csprojDir = Path.GetDirectoryName(csprojPath);

            var refs = xml.GetElementsByTagName("HintPath");
            foreach (XmlNode reference in refs)
            {
                var data = reference.ChildNodes[0].Value;
                data = data.Replace("$(ProjectDir)", "");
                if (notOnlyCement || IsReferenceToCementModule(data, csprojDir))
                    result.Add(GetRelaxedPath(data, csprojPath));
            }
            return result;
        }

		public static List<string> GetOutputPathFromCsproj(string csprojPath)
		{
			var result = new List<string>();
			var xml = new XmlDocument();
			xml.Load(csprojPath);

			var csprojDir = Path.GetDirectoryName(csprojPath);
			var refs = xml.GetElementsByTagName("OutputPath");
			foreach (XmlNode reference in refs)
			{
				var data = reference.ChildNodes[0].Value;
				result.Add(Path.Combine(csprojDir, data).TrimEnd('\\'));
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
	            int idx = 0;
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
                ConsoleWriter.WriteError($"Failed to find reference {reference}");
	            return false;
	        }

        }

        public IEnumerable<string> GetCsprojList()
        {
            var guidToCsprojDict = GetGuidToCsprojDict();
            return guidToCsprojDict.Select(kvp => kvp.Value).ToList();
        }

        public IEnumerable<string> GetCsprojList(string configName)
        {
            var guidToCsprojDict = GetGuidToCsprojDict();
            var guidSetForConfig = GetGuidSetForConfig(configName);
            return guidSetForConfig.Where(guid => guidToCsprojDict.ContainsKey(guid)).Select(guid => guidToCsprojDict[guid]).ToList();
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
}
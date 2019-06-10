using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Common;
using Common.Extensions;
using Common.YamlParsers;

namespace Commands
{
    public class RefFix : Command
    {
        private bool hasFixedReferences;
        private bool fixExternal;
        private readonly FixReferenceResult fixReferenceResult = new FixReferenceResult();
        private string rootModuleName;
        private string oldYamlContent;
        private readonly HashSet<string> missingModules = new HashSet<string>();

        public RefFix()
            : base(new CommandSettings
            {
                LogPerfix = "REF-FIX",
                LogFileName = "fixing-refs.net.log",
                MeasureElapsedTime = false,
                RequireModuleYaml = true,
                Location = CommandSettings.CommandLocation.RootModuleDirectory
            })
        {
        }

        protected override void ParseArgs(string[] args)
        {
            if (args.Length < 2 || args[0] != "ref" || args[1] != "fix")
                throw new BadArgumentException("Wrong usage of command.\nUsage: cm ref fix [-e]");

            var parsedArgs = ArgumentParser.ParseFixRefs(args);
            fixExternal = (bool) parsedArgs["external"];
        }

        protected override int Execute()
        {
            rootModuleName = Path.GetFileName(Directory.GetCurrentDirectory());
            Fix();
            fixReferenceResult.Print();

            if (!Yaml.ReadAllText(rootModuleName).Equals(oldYamlContent))
                ConsoleWriter.WriteOk("Check and commit modified module.yaml.");

            if (!hasFixedReferences)
                ConsoleWriter.WriteInfo("No fixed references.");
            else
                ConsoleWriter.WriteOk("Check and commit new references.");

            ConsoleWriter.WriteInfo("See also 'check-deps' command.");

            return 0;
        }

        private void Fix()
        {
            oldYamlContent = Yaml.ReadAllText(rootModuleName);

            var modules = Helper.GetModules();

            var configs = Yaml.ConfigurationParser(rootModuleName).GetConfigurations();
            var buildsInfo = configs.SelectMany(config => Yaml.BuildParser(rootModuleName).Get(config));
            var processedFiles = new HashSet<string>();

            foreach (var buildInfo in buildsInfo)
                Fix(buildInfo, modules, processedFiles);
        }

        private void Fix(BuildData buildInfo, List<Module> modules, HashSet<string> processedFiles)
        {
            if (buildInfo.Target.IsFakeTarget())
                throw new TargetNotFoundException(rootModuleName);

            var vsParser = new VisualStudioProjectParser(buildInfo.Target, modules);
            foreach (var file in vsParser.GetCsprojList(buildInfo))
            {
                if (processedFiles.Contains(file))
                    continue;
                processedFiles.Add(file);
                fixReferenceResult.NotFound[file] = new List<string>();
                fixReferenceResult.Replaced[file] = new List<string>();
                var refs = vsParser.GetReferencesFromCsproj(file, buildInfo.Configuration, fixExternal);
                foreach (var r in refs)
                    Fix(file, r);
            }
        }

        private void Fix(string project, string reference)
        {
            var moduleName = Helper.GetRootFolder(reference);
            if (moduleName == rootModuleName && reference.ToLower().Contains("\\packages\\"))
                return;

            if (!Directory.Exists(Path.Combine(Helper.CurrentWorkspace, moduleName)))
            {
                if (!missingModules.Contains(moduleName))
                {
                    ConsoleWriter.WriteError($"Can't find module '{moduleName}'");
                    missingModules.Add(moduleName);
                }
                return;
            }
            if (!File.Exists(Path.Combine(Helper.CurrentWorkspace, moduleName, Helper.YamlSpecFile)))
            {
                fixReferenceResult.NoYamlModules.Add(moduleName);
                return;
            }

            var installFiles = InstallHelper.GetAllInstallFiles(moduleName).ToList();
            var withSameName = installFiles.Where(file => Path.GetFileName(file).Equals(Path.GetFileName(reference))).ToList();
            if (!withSameName.Any())
                withSameName = InstallHelper.GetAllInstallFiles().Where(file => Path.GetFileName(file).Equals(Path.GetFileName(reference))).ToList();
            if (withSameName.Any(r => Path.GetFullPath(r) == Path.GetFullPath(reference)))
            {
                TryAddToDeps(reference, project);
                return;
            }

            if (!withSameName.Any())
            {
                if (Helper.GetRootFolder(reference) != rootModuleName)
                    fixReferenceResult.NotFound[project].Add(reference);
                return;
            }

            var newReference = withSameName.Count == 1 && moduleName != rootModuleName
                ? withSameName.First()
                : UserChoseReplace(project, reference, withSameName);

            if (newReference != null)
            {
                UpdateReference(newReference, project);
                TryAddToDeps(newReference, project);
                hasFixedReferences = true;
            }
        }

        private string UserChoseReplace(string project, string oldReference, List<string> withSameName)
        {
            ConsoleWriter.WriteWarning($"{project}\n\tMultiple choise for replace '{oldReference}':");
            withSameName = new[] {"don't replace"}.Concat(withSameName).ToList();
            for (int i = 0; i < withSameName.Count; i++)
            {
                ConsoleWriter.WriteLine($"\t{i}. {withSameName[i].Replace("/", "\\")}");
            }
            ConsoleWriter.WriteLine($"Print 0-{withSameName.Count - 1} for choose");

            var answer = Console.ReadLine();
            int index;
            if (int.TryParse(answer, out index))
                answer = index <= 0 || index >= withSameName.Count() ? null : withSameName[index];
            else
                answer = null;
            return answer;
        }



        private void UpdateReference(string reference, string project)
        {
            var projectPath = Path.GetFullPath(project);
            var csproj = new ProjectFile(projectPath);

            var refName = Path.GetFileNameWithoutExtension(reference);
            var hintPath = Helper.GetRelativePath(Path.Combine(Helper.CurrentWorkspace, reference),
                Directory.GetParent(projectPath).FullName);
            XmlNode refXml;
            if (csproj.ContainsRef(refName, out refXml))
            {
                csproj.ReplaceRef(refName, hintPath);
                Log.Info($"'{refName}' ref replaced to {hintPath}");
                fixReferenceResult.Replaced[project].Add(
                    $"{refName}\n\t\t{GetHintPath(refXml)} ->\n\t\t{hintPath}");
            }
            csproj.Save();
        }

        private void TryAddToDeps(string reference, string project)
        {
            var moduleDep = Helper.GetRootFolder(reference);
            if (moduleDep == rootModuleName)
                return;

            var configs = Yaml.ConfigurationParser(moduleDep).GetConfigurations();
            var configsWithArtifact =
                configs.Where(c =>
                    Yaml.InstallParser(moduleDep).Get(c).Artifacts
                        .Select(file => Path.Combine(moduleDep, file)).Any(file => Path.GetFullPath(file) == Path.GetFullPath(reference))).ToList();

            var toAdd = DepsPatcherProject.GetSmallerCementConfigs(Path.Combine(Helper.CurrentWorkspace, moduleDep), configsWithArtifact);
            foreach (var configDep in toAdd)
            {
                DepsPatcherProject.PatchDepsForProject(Directory.GetCurrentDirectory(), new Dep(moduleDep, null, configDep), project);
            }
        }


        private string GetHintPath(XmlNode xmlNode)
        {
            foreach (var child in xmlNode.ChildNodes)
            {
                var xmlChild = child as XmlElement;
                if (xmlChild.Name == "HintPath")
                    return xmlChild.InnerText;
            }
            return "";
        }

        public override string HelpMessage => @"";
    }

    class FixReferenceResult
    {
        public readonly Dictionary<string, List<string>> NotFound = new Dictionary<string, List<string>>();
        public readonly Dictionary<string, List<string>> Replaced = new Dictionary<string, List<string>>();
        public readonly HashSet<string> NoYamlModules = new HashSet<string>();

        public void Print()
        {
            if (NoYamlModules.Any())
            {
                ConsoleWriter.WriteWarning("No 'install' section in modules:");
                foreach (var m in NoYamlModules)
                    ConsoleWriter.WriteBuildWarning("\t- " + m);
            }

            foreach (var key in Replaced.Keys)
            {
                if (!Replaced[key].Any())
                    continue;
                ConsoleWriter.WriteOk(key + " replaces:");
                foreach (var value in Replaced[key])
                    ConsoleWriter.WriteLine("\t" + value);
            }

            foreach (var key in NotFound.Keys)
            {
                if (!NotFound[key].Any())
                    continue;
                ConsoleWriter.WriteError(key + "\n\tnot found references in install/artifacts section of any module:");
                foreach (var value in NotFound[key])
                    ConsoleWriter.WriteLine("\t" + value);
            }
        }
    }
}
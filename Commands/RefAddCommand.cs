using System;
using System.IO;
using System.Linq;
using System.Xml;
using Common;
using Common.YamlParsers;
using Microsoft.Extensions.Logging;

namespace Commands
{
    public sealed class RefAddCommand : Command
    {
        private static readonly CommandSettings Settings = new()
        {
            LogFileName = "ref-add",
            MeasureElapsedTime = false,
            Location = CommandSettings.CommandLocation.InsideModuleDirectory
        };
        private readonly ConsoleWriter consoleWriter;
        private readonly GetCommand getCommand;

        private string project;
        private Dep dep;
        private bool testReplaces;
        private bool hasReplaces;
        private bool force;

        public RefAddCommand(ConsoleWriter consoleWriter, GetCommand getCommand)
            : base(Settings)
        {
            this.consoleWriter = consoleWriter;
            this.getCommand = getCommand;
        }

        public override string HelpMessage => @"";

        protected override void ParseArgs(string[] args)
        {
            var parsedArgs = ArgumentParser.ParseRefAdd(args);

            testReplaces = (bool)parsedArgs["testReplaces"];
            dep = new Dep((string)parsedArgs["module"]);
            if (parsedArgs["configuration"] != null)
                dep.Configuration = (string)parsedArgs["configuration"];

            project = (string)parsedArgs["project"];
            force = (bool)parsedArgs["force"];
            if (!project.EndsWith(".csproj"))
                throw new BadArgumentException(project + " is not csproj file");
        }

        protected override int Execute()
        {
            var currentModuleDirectory = Helper.GetModuleDirectory(Directory.GetCurrentDirectory());
            var currentModule = Path.GetFileName(currentModuleDirectory);

            PackageUpdater.UpdatePackages();
            project = Yaml.GetProjectFileName(project, currentModule);

            var moduleToInsert = Helper.TryFixModuleCase(dep.Name);
            dep = new Dep(moduleToInsert, dep.Treeish, dep.Configuration);
            var configuration = dep.Configuration;

            if (!Helper.HasModule(moduleToInsert))
            {
                consoleWriter.WriteError($"Can't find module '{moduleToInsert}'");
                return -1;
            }

            if (!Directory.Exists(Path.Combine(Helper.CurrentWorkspace, moduleToInsert)))
                GetAndBuild(dep);

            Log.LogDebug(
                $"{moduleToInsert + (configuration == null ? "" : Helper.ConfigurationDelimiter + configuration)} -> {project}");

            CheckBranch();

            Log.LogInformation("Getting install data for " + moduleToInsert + Helper.ConfigurationDelimiter + configuration);
            var installData = InstallParser.Get(moduleToInsert, configuration);
            if (!installData.InstallFiles.Any())
            {
                consoleWriter.WriteWarning($"No install files found in '{moduleToInsert}'");
                return 0;
            }

            AddModuleToCsproj(installData);
            if (testReplaces)
                return hasReplaces ? -1 : 0;

            if (!File.Exists(Path.Combine(currentModuleDirectory, Helper.YamlSpecFile)))
                throw new CementException(
                    "No module.yaml file. You should patch deps file manually or convert old spec to module.yaml (cm convert-spec)");
            DepsPatcherProject.PatchDepsForProject(currentModuleDirectory, dep, project);
            return 0;
        }

        private void SafeAddRef(ProjectFile csproj, string refName, string hintPath)
        {
            try
            {
                csproj.AddRef(refName, hintPath);
            }
            catch (Exception e)
            {
                consoleWriter.WriteLine(e.ToString());
                Log.LogError("Fail to add reference", e);
            }
        }

        private void GetAndBuild(Dep module)
        {
            using (new DirectoryJumper(Helper.CurrentWorkspace))
            {
                consoleWriter.WriteInfo("cm get " + module);
                if (getCommand.Run(new[] {"get", module.ToYamlString()}) != 0)
                    throw new CementException("Failed get module " + module);
                consoleWriter.ResetProgress();
            }

            module.Configuration = module.Configuration ?? Yaml.ConfigurationParser(module.Name).GetDefaultConfigurationName();

            using (new DirectoryJumper(Path.Combine(Helper.CurrentWorkspace, module.Name)))
            {
                consoleWriter.WriteInfo("cm build-deps " + module);
                if (new BuildDepsCommand(consoleWriter).Run(new[] {"build-deps", "-c", module.Configuration}) != 0)
                    throw new CementException("Failed to build deps for " + dep);
                consoleWriter.ResetProgress();
                consoleWriter.WriteInfo("cm build " + module);
                if (new BuildCommand(consoleWriter).Run(new[] {"build", "-c", module.Configuration}) != 0)
                    throw new CementException("Failed to build " + dep);
                consoleWriter.ResetProgress();
            }

            consoleWriter.WriteLine();
        }

        private void CheckBranch()
        {
            if (string.IsNullOrEmpty(dep.Treeish))
                return;

            try
            {
                var repo = new GitRepository(dep.Name, Helper.CurrentWorkspace, Log);
                var current = repo.CurrentLocalTreeish().Value;
                if (current != dep.Treeish)
                    consoleWriter.WriteWarning($"{dep.Name} on @{current} but adding @{dep.Treeish}");
            }
            catch (Exception e)
            {
                Log.LogError($"FAILED-TO-CHECK-BRANCH {dep}", e);
            }
        }

        private void AddModuleToCsproj(InstallData installData)
        {
            var projectPath = Path.GetFullPath(project);
            var csproj = new ProjectFile(projectPath);

            try
            {
                csproj.InstallNuGetPackages(installData.NuGetPackages);
            }
            catch (Exception e)
            {
                consoleWriter.WriteWarning($"Installation of NuGet packages failed: {e.InnerException?.Message ?? e.Message}");
                Log.LogError("Installation of NuGet packages failed:", e);
            }

            foreach (var buildItem in installData.InstallFiles)
            {
                var buildItemPath = Platform.IsUnix() ? Helper.WindowsPathSlashesToUnix(buildItem) : buildItem;
                var refName = Path.GetFileNameWithoutExtension(buildItemPath);

                var hintPath = Helper.GetRelativePath(
                    Path.Combine(Helper.CurrentWorkspace, buildItemPath),
                    Directory.GetParent(projectPath).FullName);

                if (Platform.IsUnix())
                {
                    hintPath = Helper.UnixPathSlashesToWindows(hintPath);
                }

                AddRef(csproj, refName, hintPath);
                CheckExistBuildFile(Path.Combine(Helper.CurrentWorkspace, buildItemPath));
            }

            if (!testReplaces)
                csproj.Save();
        }

        private void CheckExistBuildFile(string file)
        {
            if (File.Exists(file))
                return;
            consoleWriter.WriteWarning($"File {file} does not exist. Probably you need to build {dep.Name}.");
        }

        private void AddRef(ProjectFile csproj, string refName, string hintPath)
        {
            if (testReplaces)
            {
                TestReplaces(csproj, refName);
                return;
            }

            XmlNode refXml;
            if (csproj.ContainsRef(refName, out refXml))
            {
                if (UserChoseReplace(csproj, refXml, refName, hintPath))
                {
                    csproj.ReplaceRef(refName, hintPath);
                    Log.LogDebug($"'{refName}' ref replaced");
                    consoleWriter.WriteOk("Successfully replaced " + refName);
                }
            }
            else
            {
                SafeAddRef(csproj, refName, hintPath);
                Log.LogDebug($"'{refName}' ref added");
                consoleWriter.WriteOk("Successfully installed " + refName);
            }
        }

        private void TestReplaces(ProjectFile csproj, string refName)
        {
            XmlNode refXml;
            if (csproj.ContainsRef(refName, out refXml))
                hasReplaces = true;
        }

        private bool UserChoseReplace(ProjectFile csproj, XmlNode refXml, string refName, string refPath)
        {
            if (force)
                return true;

            var elementToInsert = csproj.CreateReference(refName, refPath);
            var oldRef = refXml.OuterXml;
            var newRef = elementToInsert.OuterXml;

            if (oldRef.Equals(newRef))
            {
                consoleWriter.WriteSkip("Already has same " + refName);
                return false;
            }

            consoleWriter.WriteWarning(
                $"'{project}' already contains ref '{refName}'.\n\n<<<<\n{oldRef}\n\n>>>>\n{newRef}\nDo you want to replace (y/N)?");
            var answer = Console.ReadLine();
            return answer != null && answer.Trim().ToLowerInvariant() == "y";
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Xml;
using Common;
using Common.YamlParsers;

namespace Commands
{
    public class RefAdd : Command
    {
        private string project;
        private Dep dep;
        private bool testReplaces;
        private bool hasReplaces;
        private bool force;

        public RefAdd()
            : base(new CommandSettings
            {
                LogPerfix = "REF-ADD",
                LogFileName = "ref-add.net.log",
                MeasureElapsedTime = false,
                Location = CommandSettings.CommandLocation.InsideModuleDirectory
            })
        {
        }

        protected override void ParseArgs(string[] args)
        {
            var parsedArgs = ArgumentParser.ParseRefAdd(args);

            testReplaces = (bool) parsedArgs["testReplaces"];
            dep = new Dep((string) parsedArgs["module"]);
            if (parsedArgs["configuration"] != null)
                dep.Configuration = (string) parsedArgs["configuration"];

            project = (string) parsedArgs["project"];
            force = (bool) parsedArgs["force"];
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
                ConsoleWriter.WriteError($"Can't find module '{moduleToInsert}'");
                return -1;
            }

            if (!Directory.Exists(Path.Combine(Helper.CurrentWorkspace, moduleToInsert)))
                GetAndBuild(dep);

            Log.Debug(
                $"{moduleToInsert + (configuration == null ? "" : Helper.ConfigurationDelimiter + configuration)} -> {project}");

            CheckBranch();

            Log.Info("Getting install data for " + moduleToInsert + Helper.ConfigurationDelimiter + configuration);
            var installData = InstallParser.Get(moduleToInsert, configuration);
            if (!installData.InstallFiles.Any())
            {
                ConsoleWriter.WriteWarning($"No install files found in '{moduleToInsert}'");
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

        private void GetAndBuild(Dep module)
        {
            using (new DirectoryJumper(Helper.CurrentWorkspace))
            {
                ConsoleWriter.WriteInfo("cm get " + module);
                if (new Get().Run(new[] {"get", module.ToYamlString()}) != 0)
                    throw new CementException("Failed get module " + module);
                ConsoleWriter.ResetProgress();
            }

            module.Configuration = module.Configuration ?? Yaml.ConfigurationParser(module.Name).GetDefaultConfigurationName();

            using (new DirectoryJumper(Path.Combine(Helper.CurrentWorkspace, module.Name)))
            {
                ConsoleWriter.WriteInfo("cm build-deps " + module);
                if (new BuildDeps().Run(new[] { "build-deps", "-c", module.Configuration }) != 0)
                    throw new CementException("Failed to build deps for " + dep);
                ConsoleWriter.ResetProgress();
                ConsoleWriter.WriteInfo("cm build " + module);
                if (new Build().Run(new[] { "build", "-c", module.Configuration }) != 0)
                    throw new CementException("Failed to build " + dep);
                ConsoleWriter.ResetProgress();
            }
            Console.WriteLine();
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
                    ConsoleWriter.WriteWarning($"{dep.Name} on @{current} but adding @{dep.Treeish}");
            }
            catch (Exception e)
            {
                Log.Error($"FAILED-TO-CHECK-BRANCH {dep}", e);
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
                ConsoleWriter.WriteWarning($"Installation of NuGet packages failed: {e.InnerException?.Message ?? e.Message}");
                Log.Error("Installation of NuGet packages failed:", e);
            }

            foreach (var buildItem in installData.InstallFiles)
            {
                var buildItemPath = Helper.OsIsUnix() ? Helper.WindowsPathSlashesToUnix(buildItem) : buildItem;
                var refName = Path.GetFileNameWithoutExtension(buildItemPath);

                var hintPath = Helper.GetRelativePath(Path.Combine(Helper.CurrentWorkspace, buildItemPath),
                    Directory.GetParent(projectPath).FullName);

                if (Helper.OsIsUnix())
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
            ConsoleWriter.WriteWarning($"File {file} does not exist. Probably you need to build {dep.Name}.");
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
                    Log.Debug($"'{refName}' ref replaced");
                    ConsoleWriter.WriteOk("Successfully replaced " + refName);
                }
            }
            else
            {
                SafeAddRef(csproj, refName, hintPath);
                Log.Debug($"'{refName}' ref added");
                ConsoleWriter.WriteOk("Successfully installed " + refName);
            }
        }

        private static void SafeAddRef(ProjectFile csproj, string refName, string hintPath)
        {
            try
            {
                csproj.AddRef(refName, hintPath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Log.Error("Fail to add reference", e);
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
                ConsoleWriter.WriteSkip("Already has same " + refName);
                return false;
            }
            ConsoleWriter.WriteWarning(
                $"'{project}' already contains ref '{refName}'.\n\n<<<<\n{oldRef}\n\n>>>>\n{newRef}\nDo you want to replace (y/N)?");
            var answer = Console.ReadLine();
            return answer != null && answer.Trim().ToLowerInvariant() == "y";
        }

        public override string HelpMessage => @"";
    }
}
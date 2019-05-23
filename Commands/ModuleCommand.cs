using Common;
using log4net;
using System;
using System.IO;
using System.Linq;

namespace Commands
{
    public class ModuleCommand : ICommand
    {
        private static readonly ILog Log = LogManager.GetLogger("moduleCommand");
        private string command;
        private string moduleName;
        private string pushUrl, fetchUrl;
        private Package package;

        public int Run(string[] args)
        {
            try
            {
                ParseArgs(args);
                return Execute();
            }
            catch (CementException e)
            {
                ConsoleWriter.WriteError(e.Message);
                return -1;
            }
            catch (Exception e)
            {
                ConsoleWriter.WriteError(e.Message);
                ConsoleWriter.WriteError(e.StackTrace);
                return -1;
            }
        }

        private int Execute()
        {
            if (package.Type != "git")
            {
                ConsoleWriter.WriteError("You should add/change local modules file manually");
                {
                    return -1;
                }
            }

            switch (command)
            {
                case "add":
                {
                    return AddModule();
                }
                case "change":
                {
                    return ChangeModule();
                }
            }
            return -1;
        }

        private int AddModule()
        {
            return AddModule(package, moduleName, pushUrl, fetchUrl);
        }

        private int ChangeModule()
        {
            return ChangeModule(package, moduleName, pushUrl, fetchUrl);
        }

        public static int AddModule(Package package, string moduleName, string pushUrl, string fetchUrl)
        {
            if (fetchUrl.StartsWith("https://git.skbkontur.ru/"))
                throw new CementException("HTTPS url not allowed for gitlab. You should use SSH url.");
            using (var tempDir = new TempDirectory())
            {
                var repo = new GitRepository("modules_git", tempDir.Path, Log);
                repo.Clone(package.Url);
                if (FindModule(repo, moduleName) != null)
                {
                    ConsoleWriter.WriteError("Module " + moduleName + " already exists in " + package.Name);
                    return -1;
                }
                WriteModuleDescription(moduleName, pushUrl, fetchUrl, repo);

                var message = "(!)cement comment: added module '" + moduleName + "'";
                repo.Commit(new[] {"-am", message});
                repo.Push("master");
            }

            ConsoleWriter.WriteOk($"Successfully added {moduleName} to {package.Name} package.");

            PackageUpdater.UpdatePackages();

            return 0;
        }

        public static int ChangeModule(Package package, string moduleName, string pushUrl, string fetchUrl)
        {
            using (var tempDir = new TempDirectory())
            {
                var repo = new GitRepository("modules_git", tempDir.Path, Log);
                repo.Clone(package.Url);

                var toChange = FindModule(repo, moduleName);
                if (toChange == null)
                {
                    ConsoleWriter.WriteError("Unable to find module " + moduleName + " in package " + package.Name);
                    return -1;
                }
                if (toChange.Url == fetchUrl && toChange.Pushurl == pushUrl)
                {
                    ConsoleWriter.WriteInfo("Your changes were already made");
                    return 0;
                }

                ChangeModuleDescription(repo, toChange, new Module(moduleName, fetchUrl, pushUrl));

                var message = "(!)cement comment: changed module '" + moduleName + "'";
                repo.Commit(new[] {"-am", message});
                repo.Push("master");
            }

            ConsoleWriter.WriteOk("Success changed " + moduleName + " in " + package.Name);
            return 0;
        }

        private static Module FindModule(GitRepository repo, string moduleName)
        {
            var content = File.ReadAllText(Path.Combine(repo.RepoPath, "modules"));
            var modules = ModuleIniParser.Parse(content);
            return modules.FirstOrDefault(m => m.Name == moduleName);
        }

        private static void WriteModuleDescription(string moduleName, string pushUrl, string fetchUrl, GitRepository repo)
        {
            var filePath = Path.Combine(repo.RepoPath, "modules");
            if (!File.Exists(filePath))
                File.Create(filePath).Close();
            File.AppendAllLines(filePath, new[]
            {
                "",
                "[module " + moduleName + "]",
                "url = " + fetchUrl,
                pushUrl != null ? "pushurl = " + pushUrl : ""
            });
        }

        private static void ChangeModuleDescription(GitRepository repo, Module old, Module changed)
        {
            var filePath = Path.Combine(repo.RepoPath, "modules");
            var lines = File.ReadAllLines(filePath);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] != "[module " + old.Name + "]")
                    continue;
                lines[i + 1] = "url = " + changed.Url;
                lines[i + 2] = changed.Pushurl == null ? "" : "pushurl = " + changed.Pushurl;
            }

            File.WriteAllLines(filePath, lines);
        }

        private static Package GetPackage(string packageName)
        {
            PackageUpdater.UpdatePackages();
            var packages = Helper.GetPackages();

            if (packages.Count > 1 && packageName == null)
                throw new CementException($"Specify --package={string.Join("|", packages.Select(p => p.Name))}");

            var package = packageName == null
                ? packages.FirstOrDefault(p => p.Type == "git")
                : packages.FirstOrDefault(p => p.Name == packageName);
            if (package == null)
                throw new CementException("Unable to find " + packageName + " in package list");
            return package;
        }

        private void ParseArgs(string[] args)
        {
            var parsedArgs = ArgumentParser.ParseModuleCommand(args);
            command = (string) parsedArgs["command"];
            moduleName = (string) parsedArgs["module"];
            pushUrl = (string) parsedArgs["pushurl"];
            fetchUrl = (string) parsedArgs["fetchurl"];
            var packageName = (string) parsedArgs["package"];
            package = GetPackage(packageName);
        }

        public string HelpMessage => @"
    Adds new or changes existing cement module
    Don't delete old modules

    Usage:
        cm module <add|change> module_name module_fetch_url [-p|--pushurl=module_push_url] [--package=package_name]
        --pushurl        - module push url
        --package        - name of repository with modules description, specify if multiple
";

        public bool IsHiddenCommand => false;
    }
}
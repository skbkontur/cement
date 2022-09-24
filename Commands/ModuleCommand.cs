using System;
using System.Linq;
using Common;
using Common.Exceptions;

namespace Commands
{
    public sealed class ModuleCommand : ICommand
    {
        private readonly ConsoleWriter consoleWriter;
        private readonly ModuleHelper moduleHelper;

        private string command;
        private string moduleName;
        private string pushUrl, fetchUrl;
        private Package package;

        public ModuleCommand(ConsoleWriter consoleWriter, ModuleHelper moduleHelper)
        {
            this.consoleWriter = consoleWriter;
            this.moduleHelper = moduleHelper;
        }

        public string HelpMessage => @"
    Adds new or changes existing cement module
    Don't delete old modules

    Usage:
        cm module <add|change> module_name module_fetch_url [-p|--pushurl=module_push_url] [--package=package_name]
        --pushurl        - module push url
        --package        - name of repository with modules description, specify if multiple
";

        public string Name => "module";
        public bool IsHiddenCommand => false;

        public int Run(string[] args)
        {
            try
            {
                ParseArgs(args);
                return Execute();
            }
            catch (CementException e)
            {
                consoleWriter.WriteError(e.Message);
                return -1;
            }
            catch (Exception e)
            {
                consoleWriter.WriteError(e.Message);
                consoleWriter.WriteError(e.StackTrace);
                return -1;
            }
        }

        private static Package GetPackage(string packageName)
        {
            PackageUpdater.Shared.UpdatePackages();
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

        private int Execute()
        {
            if (package.Type != "git")
            {
                consoleWriter.WriteError("You should add/change local modules file manually");
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
            return moduleHelper.AddModule(package, moduleName, pushUrl, fetchUrl);
        }

        private int ChangeModule()
        {
            return moduleHelper.ChangeModule(package, moduleName, pushUrl, fetchUrl);
        }

        private void ParseArgs(string[] args)
        {
            var parsedArgs = ArgumentParser.ParseModuleCommand(args);
            command = (string)parsedArgs["command"];
            moduleName = (string)parsedArgs["module"];
            pushUrl = (string)parsedArgs["pushurl"];
            fetchUrl = (string)parsedArgs["fetchurl"];
            var packageName = (string)parsedArgs["package"];
            package = GetPackage(packageName);
        }
    }
}

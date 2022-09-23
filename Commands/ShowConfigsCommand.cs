using System;
using System.IO;
using System.Text;
using Common;
using Common.Exceptions;
using Common.YamlParsers;

namespace Commands
{
    public sealed class ShowConfigsCommand : Command
    {
        private static readonly CommandSettings Settings = new()
        {
            LogFileName = "show-configs",
            MeasureElapsedTime = false,
            Location = CommandSettings.CommandLocation.Any,
            RequireModuleYaml = true
        };
        private readonly ConsoleWriter consoleWriter;
        private string moduleNameArg;

        public ShowConfigsCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags)
            : base(consoleWriter, Settings, featureFlags)
        {
            this.consoleWriter = consoleWriter;
        }

        public override string HelpMessage => @"
    Shows configurations of module

    Usage:
        cm show-configs [<module_name>]
";

        protected override int Execute()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var moduleName = ResolveModuleName(currentDirectory);
            var workspace = Helper.GetWorkspaceDirectory(currentDirectory);

            if (workspace == null)
                throw new CementException("Cement workspace directory not found.");
            if (moduleName == null)
                throw new CementException("Specify module name, or use command inside module directory.");
            if (!Helper.DirectoryContainsModule(workspace, moduleName))
                throw new CementException($"Module {moduleName} not found.");

            Helper.SetWorkspace(workspace);
            if (!Yaml.Exists(moduleName))
                throw new CementException($"No module.yaml in {moduleName}.");
            var configYamlParser = Yaml.ConfigurationParser(moduleName);
            var defaultConfig = configYamlParser.GetDefaultConfigurationName();

            foreach (var config in configYamlParser.GetConfigurations())
            {
                var sb = new StringBuilder(config);
                var parents = configYamlParser.GetParentConfigurations(config);
                if (parents != null && parents.Count > 0)
                {
                    sb.Append(" > ");
                    sb.Append(string.Join(", ", parents));
                }

                if (config == defaultConfig)
                {
                    sb.Append("  *default");
                    consoleWriter.PrintLn(sb.ToString(), ConsoleColor.Green);
                }
                else
                {
                    consoleWriter.WriteLine(sb.ToString());
                }
            }

            return 0;
        }

        protected override void ParseArgs(string[] args)
        {
            var parsedArgs = ArgumentParser.ParseShowConfigs(args);
            moduleNameArg = (string)parsedArgs["module"];
        }

        private string ResolveModuleName(string currentDirectory)
        {
            return moduleNameArg ?? new DirectoryInfo(Helper.GetModuleDirectory(currentDirectory)).Name;
        }
    }
}

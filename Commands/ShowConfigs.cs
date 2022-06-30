using System;
using System.IO;
using System.Text;
using Common;
using Common.YamlParsers;

namespace Commands
{
    public class ShowConfigs : Command
    {
        private string moduleNameArg;

        public ShowConfigs()
            : base(new CommandSettings
            {
                LogPerfix = "SHOW-CONFIGS",
                LogFileName = null,
                MeasureElapsedTime = false,
                Location = CommandSettings.CommandLocation.Any,
                RequireModuleYaml = true
            })
        {
        }

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
                    ConsoleWriter.Shared.PrintLn(sb.ToString(), ConsoleColor.Green);
                }
                else
                {
                    ConsoleWriter.Shared.WriteLine(sb.ToString());
                }
            }
            return 0;
        }

        private string ResolveModuleName(string currentDirectory)
        {
            return moduleNameArg ?? new DirectoryInfo(Helper.GetModuleDirectory(currentDirectory)).Name;
        }

        protected override void ParseArgs(string[] args)
        {
            var parsedArgs = ArgumentParser.ParseShowConfigs(args);
            moduleNameArg = (string) parsedArgs["module"];
        }

        public override string HelpMessage => @"
    Shows configurations of module

    Usage:
        cm show-configs [<module_name>]
";
    }
}

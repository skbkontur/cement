using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;

namespace Commands
{
    public class ConvertSpec : Command
    {
        private StreamWriter writer;

        public ConvertSpec()
            : base(new CommandSettings
            {
                LogPerfix = "CONVERT",
                Location = CommandSettings.CommandLocation.RootModuleDirectory
            })
        {
        }

        protected override int Execute()
        {
            if (File.Exists(Helper.YamlSpecFile))
                throw new CementException("module.yaml already exists");

            var yamlTempName = Guid.NewGuid().ToString();
            writer = File.CreateText(yamlTempName);

            var configurationsParser = new ConfigurationParser(new FileInfo(Directory.GetCurrentDirectory()));
            var defaultConfiguration = configurationsParser.GetDefaultConfigurationName();
            var hierarchy = configurationsParser.GetConfigurationsHierarchy();

            Convert(hierarchy, defaultConfiguration);

            writer.Close();
            File.Move(yamlTempName, Helper.YamlSpecFile);

            ConsoleWriter.WriteOk("Successfully converted info.");
            ConsoleWriter.WriteInfo("Check build section.");
            ConsoleWriter.WriteInfo("Add install section.");
            return 0;
        }

        private void Convert(Dictionary<string, IList<string>> hierarchy, string defaultConfiguration)
        {
            foreach (var configuration in hierarchy.Keys)
            {
                var children = hierarchy.Keys.Where(key => hierarchy[key].Contains(configuration)).ToList();
                var isDefault = (configuration == defaultConfiguration && configuration != "full-build");
                Convert(configuration, children, isDefault);
            }
        }

        private void Convert(string configuration, List<string> children, bool isDefault)
        {
            var childrenStr = children.Count == 0
                ? ""
                : " > " + string.Join(", ", children);
            var defaultStr = isDefault ? " *default" : "";
            writer.WriteLine(configuration + childrenStr + defaultStr + ":");
            ConvertDepsSection(configuration, children);
            ConvertBuildSection(configuration);
        }

        private void ConvertDepsSection(string configuration, List<string> children)
        {
            var parser = new DepsParser(Directory.GetCurrentDirectory());
            var deps = parser.Get(configuration);
            var childrenDeps = children.SelectMany(c => parser.Get(c).Deps).ToList();
            deps.Deps = RelaxDeps(deps.Deps, childrenDeps);

            writer.WriteLine("  deps:");
            if (deps.Force != null)
            {
                deps.Force = deps.Force.Replace("%CURRENT_BRANCH%", "$CURRENT_BRANCH");
                writer.WriteLine("    - force: " + deps.Force);
            }
            if (deps.Deps == null)
                return;
            foreach (var dep in deps.Deps)
                writer.WriteLine("    - " + dep);
            writer.WriteLine();
        }

        private List<Dep> RelaxDeps(List<Dep> deps, List<Dep> childrenDeps)
        {
            if (deps == null)
                return null;
            deps = deps.Where(d => !childrenDeps.Contains(d)).ToList();

            var result = new List<Dep>();
            foreach (var dep in deps)
            {
                var withSameName = childrenDeps.Where(c => c.Name == dep.Name).ToList();
                if (withSameName.Count() > 1)
                    ConsoleWriter.WriteError("Fail to delete dep " + dep.Name + " and add");
                if (withSameName.Count() == 1)
                {
                    var remove = withSameName.First();
                    result.Add(new Dep("-" + remove.Name, remove.Treeish, remove.Configuration));
                }
                result.Add(dep);
            }
            return result;
        }

        private void ConvertBuildSection(string configuration)
        {
            var buildData = GetBuildData(configuration);
            writer.WriteLine("  build:");
            writer.WriteLine("    target: " + (buildData.Target ?? ""));
            writer.WriteLine("    configuration: " + (buildData.Configuration ?? ""));
            writer.WriteLine();
        }

        private BuildData GetBuildData(string configuration)
        {
            var buildFile =
                "build" + (configuration == null || configuration == "full-build" ? "" : "." + configuration) +
                ".cmd";
            if (!File.Exists(buildFile))
                return new BuildData(null, null);

            var script = File.ReadAllLines(buildFile);
            string buildTarget = null, buildConfig = null;
            foreach (var line in script)
            {
                if (line.Contains("target") && buildTarget == null)
                    buildTarget = line.Split('=').Last().Trim();
                if (line.Contains("Configuration="))
                    buildConfig = line.Split(new[] {"Configuration="}, StringSplitOptions.RemoveEmptyEntries)[1]
                        .Split(' ')[0].Trim();
            }

            return new BuildData(buildTarget, buildConfig);
        }

        protected override void ParseArgs(string[] args)
        {
            if (args.Length > 1)
                throw new CementException("Extra aruments. Using: cm convert-spec.");
        }

        public override string HelpMessage => @"
    Converts information about module to new format - module.yaml

    Usage:
        cm convert-spec
";
    }
}

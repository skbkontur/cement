﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;

namespace Commands
{
    public sealed class ConvertSpecCommand : Command
    {
        private StreamWriter writer;
        private static readonly CommandSettings ConvertSpecCommandSettings = new()
        {
            LogPerfix = "CONVERT",
            Location = CommandSettings.CommandLocation.RootModuleDirectory
        };

        private readonly ConsoleWriter consoleWriter;

        public ConvertSpecCommand(ConsoleWriter consoleWriter)
            : base(ConvertSpecCommandSettings)
        {
            this.consoleWriter = consoleWriter;
        }

        public override string HelpMessage => @"
    Converts information about module from old dep format to new format - module.yaml

    Usage:
        cm convert-spec
";

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

            consoleWriter.WriteOk("Successfully converted info.");
            consoleWriter.WriteInfo("Check build section.");
            consoleWriter.WriteInfo("Add install section.");
            return 0;
        }

        protected override void ParseArgs(string[] args)
        {
            if (args.Length > 1)
                throw new CementException("Extra arguments. Using: cm convert-spec.");
        }

        private void Convert(Dictionary<string, IList<string>> hierarchy, string defaultConfiguration)
        {
            foreach (var configuration in hierarchy.Keys)
            {
                var children = hierarchy.Keys.Where(key => hierarchy[key].Contains(configuration)).ToList();
                var isDefault = configuration == defaultConfiguration && configuration != "full-build";
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
                deps.Force = deps.Force.Select(x => x.Replace("%CURRENT_BRANCH%", "$CURRENT_BRANCH")).ToArray();
                writer.WriteLine("    - force: " + string.Join(",", deps.Force));
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
                if (withSameName.Count > 1)
                    consoleWriter.WriteError("Fail to delete dep " + dep.Name + " and add");
                if (withSameName.Count == 1)
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
            var buildFile = "build" + (configuration == null || configuration == "full-build" ? "" : "." + configuration) +
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
                    buildConfig = line.Split(new[] {"Configuration="}, StringSplitOptions.RemoveEmptyEntries)[1].Split(' ')[0].Trim();
            }

            return new BuildData(buildTarget, buildConfig);
        }
    }
}

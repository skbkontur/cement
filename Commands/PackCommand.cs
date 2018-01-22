using System;
using System.IO;
using Common;
using Common.YamlParsers;

namespace Commands
{
    public class PackCommand : Command
    {
        private string project;
        private string configuration;
        private BuildSettings buildSettings;

        public PackCommand() : base(new CommandSettings
        {
            LogPerfix = "PACK",
            LogFileName = null,
            MeasureElapsedTime = false,
            RequireModuleYaml = true,
            Location = CommandSettings.CommandLocation.InsideModuleDirectory
        })
        {
        }

        protected override int Execute()
        {
            var currentModuleDirectory = Helper.GetModuleDirectory(Directory.GetCurrentDirectory());
            var currentModule = Path.GetFileName(currentModuleDirectory);
            project = Yaml.GetProjectFilename(project, currentModule);

            if (project == null)
                return -1;

            var projectPath = Path.GetFullPath(project);
            var csproj = new ProjectFile(projectPath);
            var deps = new DepsParser(currentModuleDirectory).Get(configuration);
            var patchedFileName = csproj.CreateCsProjWithNugetReferences(deps.Deps);
            var tmpFileName = Path.Combine(Path.GetDirectoryName(projectPath) ?? "", "_tmp" + Path.GetFileName(projectPath));
            File.Move(projectPath, tmpFileName);
            try
            {
                File.Move(patchedFileName, projectPath);
                var moduleBuilder = new ModuleBuilder(Log, buildSettings);
                moduleBuilder.DotnetPack(currentModuleDirectory, projectPath);
            }
            finally 
            {
                File.Move(projectPath, patchedFileName);
                File.Move(tmpFileName, projectPath);
            }
            return 0;
        }

        protected override void ParseArgs(string[] args)
        {
            var parsedArgs = ArgumentParser.ParsePack(args);

            //dep = new Dep((string)parsedArgs["module"]);
            if (parsedArgs["configuration"] != null)
                configuration = (string)parsedArgs["configuration"];
            buildSettings = new BuildSettings
            {
                ShowAllWarnings = (bool)parsedArgs["warnings"],
                ShowObsoleteWarnings = (bool)parsedArgs["obsolete"],
                ShowOutput = (bool)parsedArgs["verbose"],
                ShowProgress = (bool)parsedArgs["progress"],
                ShowWarningsSummary = true
            };

            project = (string)parsedArgs["project"];
            if (!project.EndsWith(".csproj"))
                throw new BadArgumentException(project + " is not csproj file");
        }

        public override string HelpMessage => @"
    Pack project to nuget package

    Usage:
        cm pack [-v|--verbose|-w|-W|--warnings] [-p|--progress] [-c configName] <project-file>
        -c/--configuration      - build package for specific configuration

        -v/--verbose            - show full msbuild output
        -w/--warnings           - show warnings
        -W                      - show only obsolete warnings

        -p/--progress           - show msbuild output in one line
";
    }
}
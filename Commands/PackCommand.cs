using System;
using System.IO;
using System.Linq;
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
            var modulePath = Helper.GetModuleDirectory(Directory.GetCurrentDirectory());
            var moduleName = Path.GetFileName(modulePath);
            project = Yaml.GetProjectFilename(project, moduleName);
            configuration = configuration ?? "full-build";

            if (project == null)
                return -1;

            var buildData = Yaml.BuildParser(moduleName).Get(configuration).FirstOrDefault(t => !t.Target.IsFakeTarget());

            var projectPath = Path.GetFullPath(project);
            var csproj = new ProjectFile(projectPath);
            var deps = new DepsParser(modulePath).Get(configuration);
            ConsoleWriter.WriteInfo("patching csproj");
            var patchedDocument = csproj.CreateCsProjWithNugetReferences(deps.Deps, modulePath);
            var backupFileName = Path.Combine(Path.GetDirectoryName(projectPath) ?? "", "backup." + Path.GetFileName(projectPath));
            if (File.Exists(backupFileName))
                File.Delete(backupFileName);
            File.Move(projectPath, backupFileName);
            try
            {
                XmlDocumentHelper.Save(patchedDocument, projectPath, "\n");
                var moduleBuilder = new ModuleBuilder(Log, buildSettings);
                ConsoleWriter.WriteInfo("start pack");
                moduleBuilder.DotnetPack(modulePath, projectPath, buildData?.Configuration ?? "Release");
            }
            finally 
            {
                if (File.Exists(projectPath))
                    File.Delete(projectPath);
                File.Move(backupFileName, projectPath);
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
    Pack project to nuget package. Replace file references to package references in csproj file and run 'dotnet pack' command.
    Allows to publish nuget package to use outside of cement.

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
using System;
using Common;
using Common.YamlParsers;
using System.IO;
using System.Linq;
using Common.Extensions;

namespace Commands
{
    public class PackCommand : Command
    {
        private string project;
        private string configuration;
        private BuildSettings buildSettings;
        private bool preRelease = false;

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
            project = Yaml.GetProjectFileName(project, moduleName);
            configuration = configuration ?? "full-build";

            var buildData = Yaml.BuildParser(moduleName).Get(configuration).FirstOrDefault(t => !t.Target.IsFakeTarget());

            var projectPath = Path.GetFullPath(project);
            var csproj = new ProjectFile(projectPath);
            var deps = new DepsParser(modulePath).Get(configuration);
            ConsoleWriter.WriteInfo("patching csproj");
            var patchedDocument = csproj.CreateCsProjWithNugetReferences(deps.Deps, preRelease);
            var backupFileName = Path.Combine(Path.GetDirectoryName(projectPath) ?? "", "backup." + Path.GetFileName(projectPath));
            if (File.Exists(backupFileName))
                File.Delete(backupFileName);
            File.Move(projectPath, backupFileName);
            try
            {
                XmlDocumentHelper.Save(patchedDocument, projectPath, "\n");
                var moduleBuilder = new ModuleBuilder(Log, buildSettings);
                moduleBuilder.Init();
                ConsoleWriter.WriteInfo("start pack");
                if (!moduleBuilder.DotnetPack(modulePath, projectPath, buildData?.Configuration ?? "Release"))
                    return -1;
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
            preRelease = (bool)parsedArgs["prerelease"];

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
    Packs project to nuget package.
    Replaces file references to package references in csproj file and runs 'dotnet pack' command.
    Allows to publish nuget package to use outside of cement.
    Searches cement deps in nuget by module name.

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
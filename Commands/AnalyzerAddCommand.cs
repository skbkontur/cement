using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Common.Exceptions;
using Common.YamlParsers;
using Microsoft.Extensions.Logging;

namespace Commands
{
    public sealed class AnalyzerAddCommand : Command
    {
        private static readonly CommandSettings Settings = new()
        {
            LogFileName = "analyzer-add",
            MeasureElapsedTime = false,
            Location = CommandLocation.InsideModuleDirectory
        };

        private readonly ConsoleWriter consoleWriter;
        private string moduleSolutionName;
        private Dep analyzerModule;

        public AnalyzerAddCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags)
            : base(consoleWriter, Settings, featureFlags)
        {
            this.consoleWriter = consoleWriter;
        }

        public override string Name => "add";
        public override string HelpMessage => @"";

        protected override void ParseArgs(string[] args)
        {
            var parsedArgs = ArgumentParser.ParseAnalyzerAdd(args);

            analyzerModule = new Dep((string)parsedArgs["module"]);
            if (parsedArgs["configuration"] != null)
                analyzerModule.Configuration = (string)parsedArgs["configuration"];
            moduleSolutionName = (string)parsedArgs["solution"];
        }

        protected override int Execute()
        {
            var moduleDirectory = Helper.GetModuleDirectory(Directory.GetCurrentDirectory());
            if (moduleSolutionName == null)
            {
                var possibleModuleSolutions = Yaml.GetSolutionList(moduleDirectory);
                if (possibleModuleSolutions.Count != 1)
                    throw new BadArgumentException("Unable to resolve sln-file, please specify path to one");
                moduleSolutionName = possibleModuleSolutions[0];
            }

            var moduleSolutionPath = Path.GetFullPath(moduleSolutionName);
            if (!moduleSolutionPath.EndsWith(".sln"))
                throw new BadArgumentException(moduleSolutionPath + " is not sln-file");
            if (!File.Exists(moduleSolutionPath))
                throw new BadArgumentException(moduleSolutionPath + " is not exist");

            var analyzerModuleName = Helper.TryFixModuleCase(analyzerModule.Name);
            analyzerModule = new Dep(analyzerModuleName, analyzerModule.Treeish, analyzerModule.Configuration);
            var configuration = analyzerModule.Configuration;

            if (!Directory.Exists(Path.Combine(Helper.CurrentWorkspace, analyzerModuleName)) || !Helper.HasModule(analyzerModuleName))
                throw new CementException($"Can't find module '{analyzerModuleName}'");

            Log.LogDebug($"{analyzerModuleName + (configuration == null ? "" : Helper.ConfigurationDelimiter + configuration)} -> {moduleSolutionName}");

            CheckBranch();

            Log.LogInformation("Getting install data for " + analyzerModuleName + Helper.ConfigurationDelimiter + configuration);
            var installData = InstallParser.Get(analyzerModuleName, configuration);
            if (!installData.InstallFiles.Any())
            {
                consoleWriter.WriteWarning($"No install files found in '{analyzerModuleName}'");
                return 0;
            }

            var csprojFiles = GetCsprojFiles(moduleSolutionPath);
            var csprojAndRulesetPairs = csprojFiles
                .Select(
                    projectFile => new
                    {
                        Csproj = projectFile,
                        Ruleset = new RulesetFile(Path.ChangeExtension(projectFile.FilePath, "ruleset"))
                    })
                .ToList();

            foreach (var pair in csprojAndRulesetPairs)
            {
                foreach (var installItem in installData.InstallFiles)
                {
                    if (installItem.EndsWith(".ruleset"))
                    {
                        var analyzerModuleRulesetPath = Path.GetFullPath(Path.Combine(Helper.CurrentWorkspace, installItem));
                        pair.Ruleset.Include(analyzerModuleRulesetPath);
                    }
                }

                pair.Csproj.BindRuleset(pair.Ruleset);

                foreach (var installItem in installData.InstallFiles)
                {
                    if (installItem.EndsWith(".dll"))
                    {
                        var analyzerModuleDllPath = Path.GetFullPath(Path.Combine(Helper.CurrentWorkspace, installItem));
                        pair.Csproj.AddAnalyzer(analyzerModuleDllPath);
                    }
                }
            }

            if (!File.Exists(Path.Combine(moduleDirectory, Helper.YamlSpecFile)))
                throw new CementException("No module.yaml file. You should patch deps file manually or convert old spec to module.yaml (cm convert-spec)");
            DepsPatcherProject.PatchDepsForSolution(moduleDirectory, analyzerModule, moduleSolutionPath);

            foreach (var pair in csprojAndRulesetPairs)
            {
                pair.Csproj.Save();
                pair.Ruleset.Save();
            }

            consoleWriter.WriteOk($"Add {analyzerModuleName} to {Path.GetFileName(moduleSolutionPath)} successfully completed");
            return 0;
        }

        private void CheckBranch()
        {
            if (string.IsNullOrEmpty(analyzerModule.Treeish))
                return;

            try
            {
                var repo = new GitRepository(analyzerModule.Name, Helper.CurrentWorkspace, Log);
                var current = repo.CurrentLocalTreeish().Value;
                if (current != analyzerModule.Treeish)
                    consoleWriter.WriteWarning($"{analyzerModule.Name} on @{current} but adding @{analyzerModule.Treeish}");
            }
            catch (Exception e)
            {
                Log.LogError($"FAILED-TO-CHECK-BRANCH {analyzerModule}", e);
            }
        }

        private List<ProjectFile> GetCsprojFiles(string solutionPath)
        {
            var parser = new VisualStudioProjectParser(solutionPath, Helper.GetModules());
            var csprojFiles = parser
                .GetCsprojList()
                .Select(csprojPath => new ProjectFile(csprojPath))
                .ToList();
            return csprojFiles;
        }
    }
}

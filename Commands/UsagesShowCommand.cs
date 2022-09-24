using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Logging;

namespace Commands
{
    public sealed class UsagesShowCommand : Command
    {
        private static readonly CommandSettings Settings = new()
        {
            LogFileName = "usages-show",
            MeasureElapsedTime = false,
            Location = CommandLocation.Any
        };

        private readonly ConsoleWriter consoleWriter;
        private readonly IUsagesProvider usagesProvider;

        private string module, branch, configuration;
        private bool showAll;
        private bool printEdges;

        public UsagesShowCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags)
            : base(consoleWriter, Settings, featureFlags)
        {
            this.consoleWriter = consoleWriter;
            usagesProvider = new UsagesProvider(LogManager.GetLogger<UsagesProvider>(), CementSettingsRepository.Get);
        }

        public override string Name => "show";
        public override string HelpMessage => @"";

        protected override void ParseArgs(string[] args)
        {
            var parsedArgs = ArgumentParser.ParseShowParents(args);
            module = (string)parsedArgs["module"];
            if (Helper.GetModules().All(m => m.Name.ToLower() != module.ToLower()))
                consoleWriter.WriteWarning("Module " + module + " not found");

            branch = (string)parsedArgs["branch"];
            configuration = (string)parsedArgs["configuration"];
            showAll = (bool)parsedArgs["all"];
            printEdges = (bool)parsedArgs["edges"];
        }

        protected override int Execute()
        {
            var response = usagesProvider.GetUsages(module, branch, configuration);

            if (printEdges)
            {
                consoleWriter.WriteLine(";Copy paste this text to http://arborjs.org/halfviz/#");
                foreach (var item in response.Items)
                    PrintArborjsInfo(item);
            }
            else
            {
                foreach (var item in response.Items)
                    PrintInfo(item);
                PrintFooter(response.UpdatedTime);
            }

            return 0;
        }

        private void PrintArborjsInfo(KeyValuePair<Dep, List<Dep>> item)
        {
            consoleWriter.WriteLine("{color:black}");
            consoleWriter.WriteLine(item.Key + " {color:red}");
            var answer = item.Value;
            if (!showAll)
                answer = answer.Select(d => new Dep(d.Name, null, d.Configuration)).Distinct().ToList();

            foreach (var parent in answer)
                consoleWriter.WriteLine(parent + " -> " + item.Key);
        }

        private void PrintInfo(KeyValuePair<Dep, List<Dep>> item)
        {
            var answer = item.Value;
            if (!showAll)
                answer = answer.Select(d => new Dep(d.Name, null, d.Configuration)).Distinct().ToList();

            consoleWriter.WriteLine("{0} usages:", item.Key);

            var modules = answer.GroupBy(dep => dep.Name, dep => dep).OrderBy(kvp => kvp.Key);
            foreach (var kvp in modules)
            {
                var configs = kvp.GroupBy(dep => dep.Configuration).OrderBy(kvp2 => kvp2.Key);
                consoleWriter.WriteLine("  " + kvp.Key);
                foreach (var config in configs)
                {
                    consoleWriter.WriteLine("    " + config.Key);
                    if (config.Any() && showAll)
                        consoleWriter.WriteLine(string.Join("\n", config.Select(c => "      " + c.Treeish).OrderBy(x => x)));
                }
            }

            consoleWriter.WriteLine();
        }

        private void PrintFooter(DateTime updTime)
        {
            consoleWriter.WriteInfo("Data from cache relevant to the " + updTime);
        }
    }
}

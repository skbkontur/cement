using System;
using System.Collections.Generic;
using System.Linq;
using Common;

namespace Commands
{
    public class UsagesShow : Command
    {
        private string module, branch, configuration;
        private bool showAll;
        private bool printEdges;

        public UsagesShow()
            : base(new CommandSettings
            {
                LogPerfix = "USAGES-SHOW",
                LogFileName = null,
                MeasureElapsedTime = false,
                Location = CommandSettings.CommandLocation.Any
            })
        {
        }

        protected override void ParseArgs(string[] args)
        {
            var parsedArgs = ArgumentParser.ParseShowParents(args);
            module = (string) parsedArgs["module"];
            if (Helper.GetModules().All(m => m.Name.ToLower() != module.ToLower()))
                ConsoleWriter.WriteWarning("Module " + module + " not found");
            branch = (string) parsedArgs["branch"];
            configuration = (string) parsedArgs["configuration"];
            showAll = (bool) parsedArgs["all"];
            printEdges = (bool) parsedArgs["edges"];
        }

        protected override int Execute()
        {
            var response = Usages.GetUsagesResponse(module, branch, configuration);

            if (printEdges)
            {
                Console.WriteLine(";Copy paste this text to http://arborjs.org/halfviz/#");
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
            Console.WriteLine("{color:black}");
            Console.WriteLine(item.Key + " {color:red}");
            var answer = item.Value;
            if (!showAll)
                answer = answer.Select(d => new Dep(d.Name, null, d.Configuration)).Distinct().ToList();

            foreach (var parent in answer)
                Console.WriteLine(parent + " -> " + item.Key);
        }

        private void PrintInfo(KeyValuePair<Dep, List<Dep>> item)
        {
            var answer = item.Value;
            if (!showAll)
                answer = answer.Select(d => new Dep(d.Name, null, d.Configuration)).Distinct().ToList();

            Console.WriteLine("{0} usages:", item.Key);

            var modules = answer.GroupBy(dep => dep.Name, dep => dep).OrderBy(kvp => kvp.Key);
            foreach (var kvp in modules)
            {
                var configs = kvp.GroupBy(dep => dep.Configuration).OrderBy(kvp2 => kvp2.Key);
                Console.WriteLine("  " + kvp.Key);
                foreach (var config in configs)
                {
                    Console.WriteLine("    " + config.Key);
                    if (config.Any() && showAll)
                        Console.WriteLine(String.Join("\n", config.Select(c => "      " + c.Treeish).OrderBy(x => x)));
                }
            }

            Console.WriteLine();
        }

        private void PrintFooter(DateTime updTime)
        {
            ConsoleWriter.WriteInfo("Data from cache relevant to the " + updTime);
        }

        public override string HelpMessage => @"";
    }
}
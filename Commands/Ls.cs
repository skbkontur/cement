using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Common.YamlParsers;

namespace Commands
{
    public class Ls : ICommand
    {
        private Dictionary<string, object> parsedArgs;
        private bool simple;

        public int Run(string[] args)
        {
            ParseArgs(args);

            if (simple)
            {
                PrintSimpleLocalWithYaml();
                return 0;
            }

            PackageUpdater.UpdatePackages();
            var packages = Helper.GetPackages();
            foreach (var package in packages)
                PrintPackage(package);
            ConsoleWriter.Shared.ClearLine();
            return 0;
        }

        private static void PrintSimpleLocalWithYaml()
        {
            var modules = Helper.GetModules();
            var workspace = Helper.GetWorkspaceDirectory(Directory.GetCurrentDirectory()) ?? Directory.GetCurrentDirectory();
            Helper.SetWorkspace(workspace);
            var local = modules.Where(m => Yaml.Exists(m.Name));
            Console.WriteLine(string.Join("\n", local.Select(m => m.Name).OrderBy(x => x)));
        }

        private void PrintPackage(Package package)
        {
            Console.WriteLine("[{0}]", package.Name);
            var modules = Helper.GetModulesFromPackage(package).OrderBy(m => m.Name);
            foreach (var module in modules)
                PrintModule(module);
            ConsoleWriter.Shared.ClearLine();
        }

        private void PrintModule(Module module)
        {
            ConsoleWriter.Shared.WriteProgress(module.Name);
            var workspace = Helper.GetWorkspaceDirectory(Directory.GetCurrentDirectory()) ?? Directory.GetCurrentDirectory();

            if ((bool) parsedArgs["all"] || (bool) parsedArgs["local"] &&
                Helper.DirectoryContainsModule(workspace, module.Name))
            {
                if (parsedArgs["branch"] != null && !GitRepository.HasRemoteBranch(module.Url, (string) parsedArgs["branch"]))
                    return;
                ConsoleWriter.Shared.ClearLine();
                Console.Write("{0, -30}", module.Name);
                if ((bool) parsedArgs["url"])
                    Console.Write("{0, -60}", module.Url);
                if ((bool) parsedArgs["pushurl"])
                    Console.Write(module.Url);
                Console.WriteLine();
            }
        }

        private void ParseArgs(string[] args)
        {
            parsedArgs = ArgumentParser.ParseLs(args);
            foreach (var key in new[] {"local", "all", "url", "pushurl"})
            {
                if (!parsedArgs.ContainsKey(key))
                    parsedArgs[key] = false;
            }
            if (!parsedArgs.ContainsKey("branch"))
                parsedArgs["branch"] = null;
            if (!(bool) parsedArgs["local"] && !(bool) parsedArgs["all"])
            {
                if (parsedArgs["branch"] == null)
                {
                    parsedArgs["all"] = true;
                }
                else
                {
                    parsedArgs["local"] = true;
                }
            }

            if (parsedArgs.ContainsKey("simple"))
                simple = true;
        }

        public string HelpMessage => @"
    Lists all available modules

    Usage:
        cm ls [-l|-a] [-b=<branch>] [-u] [-p]

        -l/--local                   lists modules in current directory
        -a/--all                     lists all cement-known modules (default)

        -b/--has-branch<=branch>     lists all modules which have such branch
                                     --local key by default

        -u/--url                     shows module fetch url
        -p/--pushurl                 shows module pushurl

    Example:
        cm ls --all --has-branch=temp --url
";

        public bool IsHiddenCommand => false;
    }
}

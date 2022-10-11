using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Common.YamlParsers;
using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
public sealed class LsCommand : ICommand
{
    private readonly ConsoleWriter consoleWriter;
    private readonly IPackageUpdater packageUpdater;

    private Dictionary<string, object> parsedArgs;
    private bool simple;

    public LsCommand(ConsoleWriter consoleWriter, IPackageUpdater packageUpdater)
    {
        this.consoleWriter = consoleWriter;
        this.packageUpdater = packageUpdater;
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

    public string Name => "ls";

    public int Run(string[] args)
    {
        ParseArgs(args);

        if (simple)
        {
            PrintSimpleLocalWithYaml();
            return 0;
        }

        packageUpdater.UpdatePackages();
        var packages = Helper.GetPackages();
        foreach (var package in packages)
            PrintPackage(package);

        consoleWriter.ClearLine();
        return 0;
    }

    private void PrintSimpleLocalWithYaml()
    {
        var modules = Helper.GetModules();
        var workspace = Helper.GetWorkspaceDirectory(Directory.GetCurrentDirectory()) ?? Directory.GetCurrentDirectory();
        Helper.SetWorkspace(workspace);
        var local = modules.Where(m => Yaml.Exists(m.Name));
        consoleWriter.WriteLine(string.Join("\n", local.Select(m => m.Name).OrderBy(x => x)));
    }

    private void PrintPackage(Package package)
    {
        consoleWriter.WriteLine("[{0}]", package.Name);
        var modules = Helper.GetModulesFromPackage(package).OrderBy(m => m.Name);
        foreach (var module in modules)
            PrintModule(module);
        consoleWriter.ClearLine();
    }

    private void PrintModule(Module module)
    {
        consoleWriter.WriteProgress(module.Name);
        var workspace = Helper.GetWorkspaceDirectory(Directory.GetCurrentDirectory()) ?? Directory.GetCurrentDirectory();

        if ((bool)parsedArgs["all"] || ((bool)parsedArgs["local"] &&
                                        Helper.DirectoryContainsModule(workspace, module.Name)))
        {
            if (parsedArgs["branch"] != null && !GitRepository.HasRemoteBranch(module.Url, (string)parsedArgs["branch"]))
                return;
            consoleWriter.ClearLine();
            consoleWriter.Write("{0, -30}", module.Name);
            if ((bool)parsedArgs["url"])
                consoleWriter.Write("{0, -60}", module.Url);
            if ((bool)parsedArgs["pushurl"])
                consoleWriter.Write(module.Url);
            consoleWriter.WriteLine();
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
        if (!(bool)parsedArgs["local"] && !(bool)parsedArgs["all"])
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
}

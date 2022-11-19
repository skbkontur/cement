using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cement.Cli.Common.YamlParsers;

namespace Cement.Cli.Common;

public sealed class CompleteCommandAutomata
{
    private readonly IGitRepositoryFactory gitRepositoryFactory;
    private readonly Lazy<List<string>> modules = new(() => Helper.GetModules().Select(m => m.Name).ToList());

    private TokensList root;
    private string lastToken;

    public CompleteCommandAutomata(IGitRepositoryFactory gitRepositoryFactory)
    {
        this.gitRepositoryFactory = gitRepositoryFactory;
    }

    public List<string> Complete(string command)
    {
        var newToken = command.EndsWith(" ") || command.EndsWith("\t");
        lastToken = "";
        var parts = command.Split(new[] {' ', '\t'}, StringSplitOptions.RemoveEmptyEntries).ToList();
        if (!newToken)
        {
            lastToken = parts.Last();
            parts = parts.Take(parts.Count - 1).ToList();
        }

        if (parts.Count >= 4 && parts[0] == "cm" && parts[1] == "ref" && parts[2] == "add")
            parts[3] = "*";

        InitAutomata();

        return Complete(parts);
    }

    private static TokensList ModuleConfigs(string moduleName)
    {
        return TokensList.Create(
            Yaml.ConfigurationParser(moduleName).GetConfigurations());
    }

    private static IEnumerable<string> CsprojsToShortFormat(List<string> files)
    {
        files = files.Distinct().ToList();
        var fileNames = files.Select(Path.GetFileName).Distinct().ToList();
        var lowerFileNames = fileNames.Select(n => n.ToLower()).Distinct().ToList();

        if (files.Count == lowerFileNames.Count)
            return fileNames;

        return files
            .Select(f => Helper.GetRelativePath(f, Directory.GetCurrentDirectory()))
            .Select(f => f.Replace('\\', '/'));
    }

    private List<string> Modules => modules.Value;

    private List<string> Complete(List<string> parts)
    {
        var state = root;

        foreach (var part in parts)
        {
            var go = state.FirstOrDefault(s => s.Key == part);
            if (go.Value == null && part == "*")
                go = state.FirstOrDefault();

            if (go.Value == null)
                return new List<string>();

            state = go.Value();
            if (state == null)
                return new List<string>();
        }

        return state.Select(s => s.Key)
            .Where(s => s.ToLower().StartsWith(lastToken.ToLower()))
            .Distinct()
            .ToList();
    }

    private TokensList ConfigKeyWithConfigs()
    {
        var moduleDirectory = Helper.GetModuleDirectory(Directory.GetCurrentDirectory());
        if (moduleDirectory == null)
            return null;

        var moduleName = Path.GetFileName(moduleDirectory);
        Helper.SetWorkspace(Helper.GetWorkspaceDirectory(Directory.GetCurrentDirectory()));
        if (!Yaml.Exists(moduleName))
            return null;

        var configKey = new TokensList
        {
            {"-c", () => ModuleConfigs(moduleName)}
        };
        return configKey;
    }

    private TokensList AllModules()
    {
        return TokensList.Create(Modules);
    }

    private TokensList LocalModules()
    {
        var local = new List<string>();
        var workspace = Helper.GetWorkspaceDirectory(Directory.GetCurrentDirectory());
        if (workspace != null)
        {
            Helper.SetWorkspace(workspace);
            local = Modules.Where(Yaml.Exists).ToList();
        }

        return TokensList.Create(local);
    }

    private TokensList RemoteBranches()
    {
        var moduleDirectory = Helper.GetModuleDirectory(Directory.GetCurrentDirectory());
        if (moduleDirectory == null)
            return null;

        var repo = gitRepositoryFactory.Create(moduleDirectory);
        var branches = repo.GetRemoteBranches().Select(b => b.Name);
        return TokensList.Create(branches);
    }

    private TokensList RefCompleteList()
    {
        return new TokensList
        {
            {"add", RefAddComplete},
            "fix"
        };
    }

    private TokensList RefAddComplete()
    {
        var workspace = Helper.GetWorkspaceDirectory(Directory.GetCurrentDirectory()) ?? Directory.GetCurrentDirectory();
        Helper.SetWorkspace(workspace);

        var local = Modules;
        if (lastToken.Contains("/") && local.Contains(lastToken.Split('/')[0]))
        {
            var name = lastToken.Split('/')[0];
            if (Yaml.Exists(name))
                local.AddRange(
                    Yaml.ConfigurationParser(name).GetConfigurations().Select(c => $"{name}/{c}"));
        }

        return TokensList.Create(local, MoudleCsprojs);
    }

    private TokensList MoudleCsprojs()
    {
        var workspace = Helper.GetWorkspaceDirectory(Directory.GetCurrentDirectory()) ?? Directory.GetCurrentDirectory();
        Helper.SetWorkspace(workspace);

        var moduleDirectory = Helper.GetModuleDirectory(Directory.GetCurrentDirectory());
        if (moduleDirectory == null)
            return null;

        var moduleName = Path.GetFileName(moduleDirectory);
        var files = Yaml.GetCsprojsList(moduleName);

        return TokensList.Create(CsprojsToShortFormat(files));
    }

    private TokensList ModuleKeyWithModules()
    {
        return new TokensList {{"-m", AllModules}};
    }

    private void InitAutomata()
    {
        var commands = new TokensList
        {
            {"build", ConfigKeyWithConfigs},
            {"build-deps", ConfigKeyWithConfigs},
            {"check-deps", ConfigKeyWithConfigs},
            "check-pre-commit",
            {"get", AllModules},
            "help",
            "init",
            "ls",
            {
                "module", () => new TokensList
                {
                    "add",
                    {"change", AllModules}
                }
            },
            {"ref", RefCompleteList},
            {
                "analyzer", () => new TokensList
                {
                    "add"
                }
            },
            "self-update",
            {"show-deps", ConfigKeyWithConfigs},
            {"show-configs", LocalModules},
            "status",
            {"update", RemoteBranches},
            {"update-deps", ConfigKeyWithConfigs},
            {
                "usages", () => new TokensList
                {
                    "build",
                    {"show", ModuleKeyWithModules}
                }
            },
            {"pack", MoudleCsprojs},
            "--version"
        };

        root = new TokensList
        {
            {"cm", () => commands}
        };
    }
}

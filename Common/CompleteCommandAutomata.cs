﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.YamlParsers;
using log4net;

namespace Common
{
    public class CompleteCommandAutomata
    {
        private readonly ILog log;
        private TokensList root;

        private readonly List<string> modules =
            Helper.GetModules().Select(m => m.Name).ToList();

        private string lastToken;

        public CompleteCommandAutomata(ILog log)
        {
            this.log = log;
        }

        public List<string> Complete(string command)
        {
            bool newToken = command.EndsWith(" ") || command.EndsWith("\t");
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

        private static TokensList ModuleConfigs(string moduleName)
        {
            return TokensList.Create(
                Yaml.ConfigurationParser(moduleName).GetConfigurations());
        }

        private TokensList AllModules()
        {
            return TokensList.Create(modules);
        }

        private TokensList LocalModules()
        {
            var local = new List<string>();
            var workspace = Helper.GetWorkspaceDirectory(Directory.GetCurrentDirectory());
            if (workspace != null)
            {
                Helper.SetWorkspace(workspace);
                local = modules.Where(Yaml.Exists).ToList();
            }
            return TokensList.Create(local);
        }

        private TokensList RemoteBranches()
        {
            var moduleDirectory = Helper.GetModuleDirectory(Directory.GetCurrentDirectory());
            if (moduleDirectory == null)
                return null;

            var repo = new GitRepository(moduleDirectory, log);
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

            var local = modules.Where(Yaml.Exists).ToList();
            if (lastToken.Contains("/") && local.Contains(lastToken.Split('/')[0]))
            {
                var name = lastToken.Split('/')[0];
                local.AddRange(
                    Yaml.ConfigurationParser(name).GetConfigurations().Select(c => $"{name}/{c}"));
            }

            return TokensList.Create(local, MoudleCsprojs);
        }

        private TokensList MoudleCsprojs()
        {
            var moduleDirectory = Helper.GetModuleDirectory(Directory.GetCurrentDirectory());
            if (moduleDirectory == null)
                return null;

            var moduleName = Path.GetFileName(moduleDirectory);
            var files = Yaml.GetCsprojsList(moduleName);

            return TokensList.Create(CsprojsToShortFormat(files));
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
                "convert-spec",
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
                        {"add"} //TODO
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
                "--version"
            };

            root = new TokensList
            {
                {"cm", () => commands}
            };
        }
    }

    internal class TokensList : List<KeyValuePair<string, Func<TokensList>>>
    {
        public static TokensList Create(IEnumerable<string> items)
        {
            var result = new TokensList();
            foreach (var item in items)
            {
                result.Add(item);
            }
            return result;
        }

        public static TokensList Create(IEnumerable<string> items, Func<TokensList> next)
        {
            var result = new TokensList();
            foreach (var item in items)
            {
                result.Add(item, next);
            }
            return result;
        }

        public void Add(string key, Func<TokensList> value)
        {
            Add(new KeyValuePair<string, Func<TokensList>>(key, value));
        }

        public void Add(string key)
        {
            Add(new KeyValuePair<string, Func<TokensList>>(key, null));
        }
    }
}

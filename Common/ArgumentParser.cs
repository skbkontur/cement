using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NDesk.Options;

namespace Common
{
    public static class ArgumentParser
    {
        public static Dictionary<string, object> ParseLs(string[] args)
        {
            var parsedArguments = new Dictionary<string, object>();
            var parser = new OptionSet
            {
                {"l|local", l => parsedArguments["local"] = true},
                {"a|all", a => parsedArguments["all"] = true},
                {"b|has-branch=", branch => parsedArguments["branch"] = branch},
                {"u|url", u => parsedArguments["url"] = true},
                {"p|pushurl", p => parsedArguments["pushurl"] = true},
                {"simple", s => parsedArguments["simple"] = true}
            };
            var extraArgs = parser.Parse(args.Skip(1));
            ThrowIfHasExtraArgs(extraArgs);
            var local = parsedArguments.ContainsKey("local") ? 1 : 0;
            var all = parsedArguments.ContainsKey("all") ? 1 : 0;
            if (local + all > 1)
            {
                throw new BadArgumentException("Bad arguments: all and local");
            }

            return parsedArguments;
        }

        public static Dictionary<string, object> ParseUpdateDeps(string[] args)
        {
            var parsedArguments = new Dictionary<string, object>
            {
                {"reset", 0},
                {"force", 0},
                {"pullAnyway", 0},
                {"configuration", null},
                {"merged", null},
                {"localBranchForce", false},
                {"verbose", false},
                {"gitDepth", null}
            };
            var parser = new OptionSet
            {
                {"r|reset", r => parsedArguments["reset"] = 1},
                {"p|pull-anyway", p => parsedArguments["pullAnyway"] = 1},
                {"c|configuration=", conf => parsedArguments["configuration"] = conf},
                {"f|force", f => parsedArguments["force"] = 1},
                {"m|merged:", m => parsedArguments["merged"] = m ?? "master"},
                {"allow-local-branch-force", f => parsedArguments["localBranchForce"] = true},
                {"v|verbose", v => parsedArguments["verbose"] = true},
                {"git-depth=", d => parsedArguments["gitDepth"] = int.Parse(d)},
            };
            var extraArgs = parser.Parse(args.Skip(1));
            ThrowIfHasExtraArgs(extraArgs);
            if ((int)parsedArguments["force"] + (int)parsedArguments["reset"] + (int)parsedArguments["pullAnyway"] > 1)
            {
                throw new BadArgumentException();
            }

            return parsedArguments;
        }

        public static Dictionary<string, object> ParseRefAdd(string[] args)
        {
            var parsedArguments = new Dictionary<string, object>
            {
                {"module", null},
                {"configuration", null},
                {"project", null},
                {"testReplaces", false},
                {"force", false}
            };
            var parser = new OptionSet
            {
                {"c|configuration=", conf => parsedArguments["configuration"] = conf},
                {"testReplaces", t => parsedArguments["testReplaces"] = true},
                {"force", f => parsedArguments["force"] = true}
            };
            args = parser.Parse(args).ToArray();

            if (args.Length != 4 || args[0] != "ref" || args[1] != "add")
                throw new BadArgumentException("Wrong usage of command.\nUsage: cm ref add <module-name>[/configuration] <project-file>");

            parsedArguments["module"] = args[2];
            parsedArguments["project"] = args[3];

            return parsedArguments;
        }

        public static Dictionary<string, object> ParsePack(string[] args)
        {
            var parsedArguments = new Dictionary<string, object>
            {
                {"configuration", null},
                {"project", null},
                {"warnings", false},
                {"obsolete", false},
                {"verbose", false},
                {"progress", false},
                {"prerelease", false}
            };
            var parser = new OptionSet
            {
                {"c|configuration=", conf => parsedArguments["configuration"] = conf},
                {"w|warnings", f => parsedArguments["warnings"] = true},
                {"W", f => parsedArguments["obsolete"] = true},
                {"v|verbose", v => parsedArguments["verbose"] = true},
                {"p|progress", p => parsedArguments["progress"] = true},
                {"prerelease", p => parsedArguments["prerelease"] = true}
            };
            args = parser.Parse(args).ToArray();

            if (args.Length != 2 || args[0] != "pack")
                throw new BadArgumentException("Wrong usage of command.\nUsage: cm pack [-c|--configuration <config-name>] <project-file>");

            parsedArguments["project"] = args[1];

            return parsedArguments;
        }

        public static Dictionary<string, object> ParseAnalyzerAdd(string[] args)
        {
            var parsedArguments = new Dictionary<string, object>
            {
                {"module", null},
                {"configuration", null},
                {"solution", null}
            };

            var parser = new OptionSet
            {
                {"c|configuration=", conf => parsedArguments["configuration"] = conf}
            };

            args = parser.Parse(args).ToArray();

            if (args.Length < 3 || args.Length > 4 || args[0] != "analyzer" || args[1] != "add")
                throw new BadArgumentException($"Command format error: cm {string.Join(" ", args)}\nCommand format: cm analyzer add <analyzer-module-name>[/configuration] [<solution-file>]");

            parsedArguments["module"] = args[2];
            parsedArguments["solution"] = args.Length == 4 ? args[3] : null;
            return parsedArguments;
        }

        public static Dictionary<string, object> ParseGet(string[] args)
        {
            var parsedArguments = new Dictionary<string, object>
            {
                {"module", null},
                {"treeish", null},
                {"reset", 0},
                {"force", 0},
                {"pullAnyway", 0},
                {"configuration", null},
                {"merged", null},
                {"verbose", false},
                {"gitDepth", null}
            };
            var parser = new OptionSet
            {
                {"r|reset", r => parsedArguments["reset"] = 1},
                {"p|pull-anyway", p => parsedArguments["pullAnyway"] = 1},
                {"c|configuration=", conf => parsedArguments["configuration"] = conf},
                {"f|force", f => parsedArguments["force"] = 1},
                {"m|merged:", m => parsedArguments["merged"] = m ?? "master"},
                {"v|verbose", v => parsedArguments["verbose"] = true},
                {"git-depth=", d => parsedArguments["gitDepth"] = int.Parse(d)},
            };
            var extraArgs = parser.Parse(args.Skip(1));
            if (extraArgs.Count > 0)
            {
                var module = new Dep(extraArgs[0]);
                if (module.Configuration != null)
                    parsedArguments["configuration"] = module.Configuration;
                if (module.Treeish != null)
                    parsedArguments["treeish"] = module.Treeish;

                parsedArguments["module"] = module.Name;

                if (extraArgs.Count > 1)
                {
                    parsedArguments["treeish"] = extraArgs[1];
                }

                ThrowIfHasExtraArgs(extraArgs.Skip(2).ToList());
            }

            if ((int)parsedArguments["force"] + (int)parsedArguments["reset"] + (int)parsedArguments["pullAnyway"] > 1)
            {
                throw new BadArgumentException();
            }

            return parsedArguments;
        }

        public static Dictionary<string, object> ParseSelfUpdate(string[] args)
        {
            var parsedArguments = new Dictionary<string, object>
            {
                {"branch", null},
                {"server", null}
            };
            var parser = new OptionSet
            {
                {"b|branch=", b => parsedArguments["branch"] = b},
                {"s|server=", s => parsedArguments["server"] = s}
            };
            var extraArgs = parser.Parse(args.Skip(1));
            ThrowIfHasExtraArgs(extraArgs);
            return parsedArguments;
        }

        public static Dictionary<string, object> ParseBuildDeps(string[] args)
        {
            var parsedArguments = new Dictionary<string, object>
            {
                {"rebuild", false},
                {"configuration", null},
                {"warnings", false},
                {"obsolete", false},
                {"verbose", false},
                {"progress", false},
                {"restore", true},
                {"quickly", false},
                {"cleanBeforeBuild", false}
            };
            var parser = new OptionSet
            {
                {"r|rebuild", f => parsedArguments["rebuild"] = true},
                {"c|configuration=", conf => parsedArguments["configuration"] = conf},
                {"w|warnings", f => parsedArguments["warnings"] = true},
                {"W", f => parsedArguments["obsolete"] = true},
                {"v|verbose", v => parsedArguments["verbose"] = true},
                {"p|progress", p => parsedArguments["progress"] = true},
                {"restore", p => parsedArguments["restore"] = true},
                {"q|quickly", q => parsedArguments["quickly"] = true},
                {"cleanBeforeBuild", q => parsedArguments["cleanBeforeBuild"] = true}
            };
            var extraArgs = parser.Parse(args.Skip(1));
            ThrowIfHasExtraArgs(extraArgs);
            if ((bool)parsedArguments["verbose"] && ((bool)parsedArguments["warnings"] || (bool)parsedArguments["progress"]))
            {
                throw new BadArgumentException();
            }

            return parsedArguments;
        }

        public static Dictionary<string, object> ParseCheckDeps(string[] args)
        {
            var parsedArguments = new Dictionary<string, object>
            {
                {"configuration", null},
                {"all", false},
                {"short", false},
                {"external", false}
            };
            var parser = new OptionSet
            {
                {"c|configuration=", conf => parsedArguments["configuration"] = conf},
                {"a|all", a => parsedArguments["all"] = true},
                {"s|short", s => parsedArguments["short"] = true},
                {"e|external", e => parsedArguments["external"] = true}
            };
            var extraArgs = parser.Parse(args.Skip(1));
            ThrowIfHasExtraArgs(extraArgs);
            return parsedArguments;
        }

        public static Dictionary<string, object> ParseDepsGraph(string[] args)
        {
            var parsedArguments = new Dictionary<string, object>
            {
                {"configuration", null}
            };
            var parser = new OptionSet
            {
                {"c|configuration=", conf => parsedArguments["configuration"] = conf}
            };
            var extraArgs = parser.Parse(args.Skip(1));
            ThrowIfHasExtraArgs(extraArgs);
            return parsedArguments;
        }

        public static Dictionary<string, object> ParseFixRefs(string[] args)
        {
            var parsedArguments = new Dictionary<string, object>
            {
                {"external", false}
            };
            var parser = new OptionSet
            {
                {"e|external", e => parsedArguments["external"] = true}
            };
            var extraArgs = parser.Parse(args.Skip(2));
            ThrowIfHasExtraArgs(extraArgs);
            return parsedArguments;
        }

        public static Dictionary<string, object> ParseShowParents(string[] args)
        {
            var currentDir = Directory.GetCurrentDirectory();
            while (currentDir != Directory.GetDirectoryRoot(currentDir) && !Helper.IsCurrentDirectoryModule(currentDir))
                currentDir = Directory.GetParent(currentDir).FullName;

            var parsedArguments = new Dictionary<string, object>
            {
                {"configuration", "*"},
                {"branch", "*"},
                {"module", null},
                {"all", false},
                {"edges", false}
            };
            if (Helper.IsCurrentDirectoryModule(currentDir))
                parsedArguments["module"] = Path.GetFileName(currentDir);

            var parser = new OptionSet
            {
                {"c|configuration=", conf => parsedArguments["configuration"] = conf},
                {"m|module=", m => parsedArguments["module"] = m},
                {"b|branch=", b => parsedArguments["branch"] = b},
                {"a|all", s => parsedArguments["all"] = true},
                {"e|edges", s => parsedArguments["edges"] = true}
            };
            var extraArgs = parser.Parse(args.Skip(2));
            if (parsedArguments["module"] == null)
            {
                throw new BadArgumentException("Current directory is not cement module directory, use -m to specify module name");
            }

            var module = (string)parsedArguments["module"];
            if (module.Contains("/"))
            {
                parsedArguments["module"] = module.Split('/').First();
                parsedArguments["configuration"] = module.Split('/').Last();
            }

            ThrowIfHasExtraArgs(extraArgs);
            return parsedArguments;
        }

        public static Dictionary<string, object> ParseBuildParents(string[] args)
        {
            var parsedArguments = new Dictionary<string, object>
            {
                {"branch", null},
                {"pause", false}
            };
            var parser = new OptionSet
            {
                {"b|branch=", b => parsedArguments["branch"] = b},
                {"p|pause", b => parsedArguments["pause"] = true}
            };
            var extraArgs = parser.Parse(args.Skip(2));
            ThrowIfHasExtraArgs(extraArgs);
            return parsedArguments;
        }

        public static Dictionary<string, object> ParseGrepParents(string[] args)
        {
            var gitArgs = new List<string>();

            var parsedArguments = new Dictionary<string, object>
            {
                {"branch", null},
                {"skip-get", false}
            };
            var parser = new OptionSet
            {
                {"b|branch=", b => parsedArguments["branch"] = b},
                {"s|skip-get", b => parsedArguments["skip-get"] = true},
                {"<>", b => gitArgs.Add(b)}
            };

            var delimPosition = Array.IndexOf(args, "--");
            if (delimPosition < 0)
                delimPosition = args.Length;

            parser.Parse(args.Take(delimPosition));
            parsedArguments["gitArgs"] = gitArgs.ToArray();
            parsedArguments["fileMaskArgs"] = args.Skip(delimPosition + 1).TakeWhile(_ => true).ToArray();
            return parsedArguments;
        }

        public static Dictionary<string, object> ParseModuleCommand(string[] args)
        {
            var parsedArguments = new Dictionary<string, object>
            {
                {"pushurl", null},
                {"package", null}
            };
            var parser = new OptionSet
            {
                {"p|pushurl=", p => parsedArguments["pushurl"] = p},
                {"package=", p => parsedArguments["package"] = p}
            };

            var extraArgs = parser.Parse(args.Skip(1));
            if (extraArgs.Count < 3)
                throw new BadArgumentException("Too few arguments. \nUsing: cm module <add|change> module_name module_fetch_url [-p|--pushurl=module_push_url] [--package=package_name]");

            parsedArguments["command"] = extraArgs[0];
            parsedArguments["module"] = extraArgs[1];
            parsedArguments["fetchurl"] = extraArgs[2];
            extraArgs = extraArgs.Skip(3).ToList();

            ThrowIfHasExtraArgs(extraArgs);
            return parsedArguments;
        }

        public static Dictionary<string, object> ParseUpdate(string[] args)
        {
            var parsedArguments = new Dictionary<string, object>
            {
                {"treeish", null},
                {"reset", 0},
                {"force", 0},
                {"pullAnyway", 0},
                {"verbose", false},
                {"gitDepth", null}
            };
            var parser = new OptionSet
            {
                {"r|reset", r => parsedArguments["reset"] = 1},
                {"p|pull-anyway", p => parsedArguments["pullAnyway"] = 1},
                {"f|force", f => parsedArguments["force"] = 1},
                {"v|verbose", v => parsedArguments["verbose"] = true},
                {"git-depth=", d => parsedArguments["gitDepth"] = int.Parse(d)},
            };
            var extraArgs = parser.Parse(args.Skip(1));
            if (extraArgs.Count > 0)
            {
                parsedArguments["treeish"] = extraArgs[0];
                ThrowIfHasExtraArgs(extraArgs.Skip(1).ToList());
            }

            if ((int)parsedArguments["force"] + (int)parsedArguments["reset"] + (int)parsedArguments["pullAnyway"] > 1)
            {
                throw new BadArgumentException();
            }

            return parsedArguments;
        }

        public static Dictionary<string, object> ParseShowConfigs(string[] args)
        {
            var parsedArgs = new Dictionary<string, object>
            {
                {"module", null}
            };
            var extraArgs = args.Skip(1).ToList();
            if (extraArgs.Count > 0)
            {
                parsedArgs["module"] = extraArgs[0];
                ThrowIfHasExtraArgs(extraArgs.Skip(1).ToList());
            }

            return parsedArgs;
        }

        private static void ThrowIfHasExtraArgs(List<string> extraArgs)
        {
            if (extraArgs.Count > 0)
                throw new BadArgumentException("Extra arguments: " + string.Join(", ", extraArgs));
        }
    }
}

using System;

namespace Commands
{
    public static class ReadmeGenerator
    {
        public static string Generate()
        {
            var commands = CommandsList.Commands;

            var result = $@"
### cm help
{commands["help"].HelpMessage}

### cm self-update
{commands["self-update"].HelpMessage}
### cm --version
{commands["--version"].HelpMessage}

### cm init
{commands["init"].HelpMessage}
### cm get
{commands["get"].HelpMessage}
### cm update-deps
{commands["update-deps"].HelpMessage}
### cm update
{commands["update"].HelpMessage}

### cm build-deps
{commands["build-deps"].HelpMessage}
### cm build
{commands["build"].HelpMessage}

### cm ls
{commands["ls"].HelpMessage}
### cm module
{commands["module"].HelpMessage}
### cm ref
{commands["ref"].HelpMessage}
### cm show-configs
{commands["show-configs"].HelpMessage}
### cm check-deps
{commands["check-deps"].HelpMessage}
### cm show-deps
{commands["show-deps"].HelpMessage}

### cm status
{commands["status"].HelpMessage}";

            string menu = Environment.NewLine;
            var lines = result.Split(new string[] {"\r\n", "\n"}, StringSplitOptions.None);
            foreach (var line in lines)
            {
                if (line.StartsWith("### "))
                {
                    var name = line.Substring(4);
                    menu += $"[{name}](#{name.Replace(' ', '-')})" + Environment.NewLine + Environment.NewLine;
                }
            }

            result = menu + result;
            return result;
        }
    }
}
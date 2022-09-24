using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Commands
{
    public sealed class ReadmeGenerator
    {
        private readonly IServiceProvider serviceProvider;

        public ReadmeGenerator(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public string Generate()
        {
            var commandsList = serviceProvider.GetServices<ICommand>()
                .ToDictionary(c => c.Name);

            var result = $@"
### cm help
{commandsList["help"].HelpMessage}

### cm self-update
{commandsList["self-update"].HelpMessage}
### cm --version
{commandsList["--version"].HelpMessage}

### cm init
{commandsList["init"].HelpMessage}
### cm get
{commandsList["get"].HelpMessage}
### cm update-deps
{commandsList["update-deps"].HelpMessage}
### cm update
{commandsList["update"].HelpMessage}

### cm build-deps
{commandsList["build-deps"].HelpMessage}
### cm build
{commandsList["build"].HelpMessage}

### cm ls
{commandsList["ls"].HelpMessage}
### cm module
{commandsList["module"].HelpMessage}
### cm ref
{commandsList["ref"].HelpMessage}
### cm analyzer
{commandsList["analyzer"].HelpMessage}
### cm show-configs
{commandsList["show-configs"].HelpMessage}
### cm check-deps
{commandsList["check-deps"].HelpMessage}
### cm show-deps
{commandsList["show-deps"].HelpMessage}
### cm usages
{commandsList["usages"].HelpMessage}
### cm pack
{commandsList["pack"].HelpMessage}

### cm status
{commandsList["status"].HelpMessage}";

            var menu = "# Commands" + Environment.NewLine + Environment.NewLine;

            var lines = result.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None);
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

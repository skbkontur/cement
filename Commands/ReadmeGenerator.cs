namespace Commands
{
    public static class ReadmeGenerator
    {
        public static string Generate()
        {
            var commands = CommandsList.Commands;

            return $@"
[create an anchor](#cm-help)

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
        }
    }
}
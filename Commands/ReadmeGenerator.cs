namespace Commands
{
    public static class ReadmeGenerator
    {
        public static string Generate()
        {
            var commands = CommandsList.Commands;

            return $@"
{commands["help"].HelpMessage}

{commands["self-update"].HelpMessage}
{commands["--version"].HelpMessage}

{commands["init"].HelpMessage}
{commands["get"].HelpMessage}
{commands["update-deps"].HelpMessage}
{commands["update"].HelpMessage}

{commands["build-deps"].HelpMessage}
{commands["build"].HelpMessage}

{commands["ls"].HelpMessage}
{commands["module"].HelpMessage}
{commands["ref"].HelpMessage}
{commands["show-configs"].HelpMessage}
{commands["check-deps"].HelpMessage}
{commands["show-deps"].HelpMessage}

{commands["status"].HelpMessage}";
        }
    }
}
using Common;

namespace Commands
{
    public sealed class ReInstallCommand : Command
    {
        private static readonly CommandSettings Settings = new()
        {
            LogFileName = "reinstall",
            MeasureElapsedTime = false,
            Location = CommandLocation.Any,
            IsHiddenCommand = true
        };
        private readonly ConsoleWriter consoleWriter;
        private readonly FeatureFlags featureFlags;

        public ReInstallCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags)
            : base(consoleWriter, Settings, featureFlags)
        {
            this.consoleWriter = consoleWriter;
            this.featureFlags = featureFlags;
        }

        public override string Name => "reinstall";
        public override string HelpMessage => @"
    Reinstalls cement
	NOTE: Don't use this command from installed cement.

    Usage:
        cm reinstall
";

        protected override int Execute()
        {
            var selfUpdateCommand = new SelfUpdateCommand(consoleWriter, featureFlags)
            {
                IsInstallingCement = true
            };

            return selfUpdateCommand.Run(new[] {"self-update"});
        }

        protected override void ParseArgs(string[] args)
        {
        }
    }
}

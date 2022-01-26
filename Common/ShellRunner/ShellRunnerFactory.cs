using Microsoft.Extensions.Logging;

namespace Common
{
    public static class ShellRunnerFactory
    {
        // public static IShellRunner Create(ILogger logger = null) => new ShellRunner(logger);
        public static IShellRunner Create(ILogger logger = null) => new CliWrapRunner(logger);
    }
}
using Microsoft.Extensions.Logging;

namespace Common
{
    public static class ShellRunnerFactory
    {
        public static IShellRunner Create(ILogger log = null) => new ShellRunner(log);
    }
}
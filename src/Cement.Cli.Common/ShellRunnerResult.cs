using JetBrains.Annotations;

namespace Cement.Cli.Common;

[PublicAPI]
public sealed record ShellRunnerResult(int ExitCode, string Output, string Errors, bool HasTimeout)
{
    public void Deconstruct(out int exitCode, out string output, out string errors, out bool hasTimeout)
    {
        exitCode = ExitCode;
        output = Output;
        errors = Errors;
        hasTimeout = HasTimeout;
    }

    public void Deconstruct(out int exitCode, out string output, out string errors)
    {
        exitCode = ExitCode;
        output = Output;
        errors = Errors;
    }
}

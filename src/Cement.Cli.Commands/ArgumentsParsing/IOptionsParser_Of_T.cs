using System.Collections.Generic;
using Cement.Cli.Common.Exceptions;

namespace Cement.Cli.Commands.ArgumentsParsing;

public abstract class OptionsParser<TOptions> : IOptionsParser<TOptions>
{
    public abstract TOptions Parse(string[] args);

    protected void ThrowIfHasExtraArgs(IReadOnlyCollection<string> extraArgs)
    {
        if (extraArgs.Count > 0)
            throw new BadArgumentException("Extra arguments: " + string.Join(", ", extraArgs));
    }
}

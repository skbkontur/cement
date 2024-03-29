using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public interface ICommand
{
    bool MeasureElapsedTime { get; }

    string Name { get; }

    string HelpMessage { get; }

    int Run(string[] args);
}

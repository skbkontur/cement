using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
public interface ICommand
{
    string Name { get; }

    string HelpMessage { get; }

    int Run(string[] args);
}

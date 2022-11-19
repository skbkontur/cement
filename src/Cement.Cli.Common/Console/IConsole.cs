using System;

namespace Cement.Cli.Common.Console;

public interface IConsole
{
    void Write(string message, ConsoleColor? backgroundColor = default, ConsoleColor? foregroundColor = default);

    void ClearLine();

    int WindowWidth { get; }
}

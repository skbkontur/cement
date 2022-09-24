using System;

namespace Common.Console;

public interface IConsole
{
    void Write(string message, ConsoleColor? backgroundColor = default, ConsoleColor? foregroundColor = default);

    void ClearLine();

    int WindowWidth { get; }
}

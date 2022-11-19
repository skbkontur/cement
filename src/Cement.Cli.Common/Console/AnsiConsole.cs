using System;
using System.IO;

namespace Cement.Cli.Common.Console;

public sealed class AnsiConsole : IConsole
{
    private const string DefaultBackgroundColor = "\x1B[49m";
    private const string DefaultForegroundColor = "\x1B[39m\x1B[22m";

    private readonly TextWriter writer;

    public AnsiConsole(TextWriter writer)
    {
        this.writer = writer;
    }

    public void Write(string message, ConsoleColor? backgroundColor = default, ConsoleColor? foregroundColor = default)
    {
        if (backgroundColor.HasValue)
            writer.Write(GetBackgroundColorEscapeCode(backgroundColor.Value));

        if (foregroundColor.HasValue)
            writer.Write(GetForegroundColorEscapeCode(foregroundColor.Value));

        writer.Write(message);

        if (foregroundColor.HasValue)
            writer.Write(DefaultForegroundColor);

        if (backgroundColor.HasValue)
            writer.Write(DefaultBackgroundColor);
    }

    public void ClearLine()
    {
        const string escapeCode = "\x1B[2K";
        Write(escapeCode);
    }

    public int WindowWidth => CalculateWindowWidth();

    private static string GetForegroundColorEscapeCode(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => "\x1B[30m",
            ConsoleColor.DarkRed => "\x1B[31m",
            ConsoleColor.DarkGreen => "\x1B[32m",
            ConsoleColor.DarkYellow => "\x1B[33m",
            ConsoleColor.DarkBlue => "\x1B[34m",
            ConsoleColor.DarkMagenta => "\x1B[35m",
            ConsoleColor.DarkCyan => "\x1B[36m",
            ConsoleColor.Gray => "\x1B[37m",
            ConsoleColor.Red => "\x1B[1m\x1B[31m",
            ConsoleColor.Green => "\x1B[1m\x1B[32m",
            ConsoleColor.Yellow => "\x1B[1m\x1B[33m",
            ConsoleColor.Blue => "\x1B[1m\x1B[34m",
            ConsoleColor.Magenta => "\x1B[1m\x1B[35m",
            ConsoleColor.Cyan => "\x1B[1m\x1B[36m",
            ConsoleColor.White => "\x1B[1m\x1B[37m",
            _ => DefaultForegroundColor
        };
    }

    private static string GetBackgroundColorEscapeCode(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => "\x1B[40m",
            ConsoleColor.DarkRed => "\x1B[41m",
            ConsoleColor.DarkGreen => "\x1B[42m",
            ConsoleColor.DarkYellow => "\x1B[43m",
            ConsoleColor.DarkBlue => "\x1B[44m",
            ConsoleColor.DarkMagenta => "\x1B[45m",
            ConsoleColor.DarkCyan => "\x1B[46m",
            ConsoleColor.Gray => "\x1B[47m",
            _ => DefaultBackgroundColor
        };
    }

    private static int CalculateWindowWidth()
    {
        var result = 80;
        try
        {
            result = Math.Max(result, System.Console.WindowWidth);
        }
        catch
        {
            // ignored
        }

        return result;
    }
}

using System;
using System.IO;

namespace Common.Console;

public sealed class WindowsConsole : IConsole
{
    private readonly TextWriter writer;

    public WindowsConsole(TextWriter writer)
    {
        this.writer = writer;
    }

    public void Write(string message, ConsoleColor? backgroundColor = default, ConsoleColor? foregroundColor = default)
    {
        var colorChanged = SetColor(backgroundColor, foregroundColor);

        writer.Write(message);

        if (colorChanged)
            ResetColor();
    }

    public void ClearLine()
    {
        Write(string.Format($"\r{{0,-{WindowWidth - 1}}}\r", ""));
    }

    public int WindowWidth => CalculateWindowWidth();

    private static bool SetColor(ConsoleColor? background, ConsoleColor? foreground)
    {
        var backgroundChanged = SetBackgroundColor(background);
        return SetForegroundColor(foreground) || backgroundChanged;
    }

    private static void ResetColor()
    {
        System.Console.ResetColor();
    }

    private static bool SetBackgroundColor(ConsoleColor? background)
    {
        if (!background.HasValue)
            return false;

        System.Console.BackgroundColor = background.Value;
        return true;
    }

    private static bool SetForegroundColor(ConsoleColor? foreground)
    {
        if (!foreground.HasValue)
            return false;

        System.Console.ForegroundColor = foreground.Value;
        return true;
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

namespace Common.Extensions;

public static class CommandLineExtensions
{
    public static string QuoteIfNeeded(this string path)
    {
        if (path is null or {Length: < 2})
            return path;

        if (path.StartsWith('"') && path.EndsWith('"'))
            return path;

        if (path.Contains(' ') && !path.Contains('"'))
            return '"' + path + '"';

        return path;
    }
}

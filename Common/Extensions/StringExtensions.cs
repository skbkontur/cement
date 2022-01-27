using System;

namespace Common.Extensions
{
    public static class StringExtensions
    {
        // todo: that does not apply to all strings and it shouldn't be an extension method
        public static bool IsFakeTarget(this string target)
        {
            return string.IsNullOrEmpty(target) || target == "None";
        }

        private static readonly string[] NewLineStrings = {"\r\n", "\n"};

        public static string[] ToLines(this string src)
        {
            return src.Split(NewLineStrings, StringSplitOptions.None);
        }

        public static string[] ToNonEmptyLines(this string src)
        {
            return src.Split(NewLineStrings, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
namespace Common.Extensions
{
    public static class StringExtensions
    {
        // todo: that does not apply to all strings and it shouldn't be an extension method
        public static bool IsFakeTarget(this string target)
        {
            return string.IsNullOrEmpty(target) || target == "None";
        }
    }
}
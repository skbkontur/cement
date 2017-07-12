using System;
using System.Collections.Generic;

namespace Common
{
    public static class Extensions
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
            (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var keys = new HashSet<TKey>();
            foreach (var element in source)
            {
                if (keys.Add(keySelector(element)))
                    yield return element;
            }
        }

        public static bool IsFakeTarget(this string target)
        {
            return string.IsNullOrEmpty(target) || target == "None";
        }
    }
}
using System;
using System.Collections.Generic;

namespace Common.Extensions
{
    public static class EnumerableExtensions
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
    }
}
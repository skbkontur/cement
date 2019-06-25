using System.Collections.Generic;
using JetBrains.Annotations;

namespace Common.Extensions
{
    public static class DictionaryExtensions
    {
        public static TValue FindValue<TKey, TValue>([NotNull] this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default(TValue))
        {
            return dict.TryGetValue(key, out var value) ? value : defaultValue;
        }
    }
}
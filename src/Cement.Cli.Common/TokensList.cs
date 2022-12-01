using System;
using System.Collections.Generic;

namespace Cement.Cli.Common;

internal sealed class TokensList : List<KeyValuePair<string, Func<TokensList>>>
{
    public static TokensList Create(IEnumerable<string> items)
    {
        var result = new TokensList();
        foreach (var item in items)
        {
            result.Add(item);
        }

        return result;
    }

    public static TokensList Create(IEnumerable<string> items, Func<TokensList> next)
    {
        var result = new TokensList();
        foreach (var item in items)
        {
            result.Add(item, next);
        }

        return result;
    }

    public void Add(string key, Func<TokensList> value)
    {
        Add(new KeyValuePair<string, Func<TokensList>>(key, value));
    }

    public void Add(string key)
    {
        Add(new KeyValuePair<string, Func<TokensList>>(key, null));
    }
}

using System;
using System.Linq;
using Cement.Cli.Common.YamlParsers.Models;

namespace Cement.Cli.Common.YamlParsers.V2;

public sealed class ConfigSectionTitleParser
{
    private const string DefaultKeyword = "*default";
    private static readonly char[] Separators = {'>', ' ', ','};

    public ConfigSectionTitle Parse(string title)
    {
        EnsureValidConfigLine(title);
        title = title.TrimEnd();
        var isDefault = title.EndsWith(DefaultKeyword);
        if (isDefault)
            title = title.Substring(0, title.Length - DefaultKeyword.Length);

        EnsureValidConfigLine(title);

        var parts = title.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        var name = parts[0];
        var parents = parts.Length > 1 ? parts.Skip(1).Distinct().ToArray() : null;

        return new ConfigSectionTitle
        {
            Name = name,
            IsDefault = isDefault,
            Parents = parents
        };
    }

    private void EnsureValidConfigLine(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Got empty configuration line from module.yaml");
    }
}

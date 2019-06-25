using System;
using System.Linq;
using Common.YamlParsers.Models;

namespace Common.YamlParsers.V2
{
    public class ConfigSectionTitleParser
    {
        private const string defaultKeyword = "*default";
        private static readonly char[] separators = {'>', ' ', ','};

        public ConfigSectionTitle Parse(string title)
        {
            EnsureValidConfigLine(title);
            title = title.TrimEnd();
            var isDefault = title.EndsWith(defaultKeyword);
            if (isDefault)
                title = title.Substring(0, title.Length - defaultKeyword.Length);

            EnsureValidConfigLine(title);

            var parts = title.Split(separators, StringSplitOptions.RemoveEmptyEntries);
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
}
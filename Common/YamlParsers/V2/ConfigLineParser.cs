using System;
using System.Linq;
using Common.YamlParsers.Models;

namespace Common.YamlParsers.V2
{
    public class ConfigLineParser
    {
        private const string defaultKeyword = "*default";
        private static readonly char[] separators = {'>', ' ', ','};

        public ConfigurationLine Parse(string configLine)
        {
            EnsureValidConfigLine(configLine);
            configLine = configLine.TrimEnd();
            var isDefault = configLine.EndsWith(defaultKeyword);
            if (isDefault)
                configLine = configLine.Substring(0, configLine.Length - defaultKeyword.Length);

            EnsureValidConfigLine(configLine);

            var parts = configLine.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            var name = parts[0];
            var parents = parts.Length > 1 ? parts.Skip(1).Distinct().ToArray() : null;

            return new ConfigurationLine
            {
                ConfigName = name,
                IsDefault = isDefault,
                ParentConfigs = parents
            };
        }

        private void EnsureValidConfigLine(string configLine)
        {
            if (string.IsNullOrWhiteSpace(configLine))
                throw new ArgumentException("Got empty configuration line from module.yaml");
        }
    }
}
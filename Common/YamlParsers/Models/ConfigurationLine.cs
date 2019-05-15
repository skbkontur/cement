using System;
using JetBrains.Annotations;

namespace Common.YamlParsers.Models
{
    public class ConfigurationLine: IEquatable<ConfigurationLine>
    {
        public string ConfigName { get; set; }

        [CanBeNull]
        public string[] ParentConfigs { get; set; }

        public bool IsDefault { get; set; }

        public override string ToString()
        {
            var result = ConfigName;

            if (ParentConfigs != null)
                result += " > " + string.Join(", ", ParentConfigs);

            if (IsDefault)
                result += " *default";

            return result;
        }

        public bool Equals(ConfigurationLine other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return other.ToString().Equals(ToString());
        }
    }
}
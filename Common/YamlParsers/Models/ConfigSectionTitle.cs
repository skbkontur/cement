using System;
using JetBrains.Annotations;

namespace Common.YamlParsers.Models
{
    public class ConfigSectionTitle: IEquatable<ConfigSectionTitle>
    {
        public string Name { get; set; }

        [CanBeNull]
        public string[] Parents { get; set; }

        public bool IsDefault { get; set; }

        public override string ToString()
        {
            var result = Name;

            if (Parents != null)
                result += " > " + string.Join(", ", Parents);

            if (IsDefault)
                result += " *default";

            return result;
        }

        public bool Equals(ConfigSectionTitle other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return other.ToString().Equals(ToString());
        }
    }
}
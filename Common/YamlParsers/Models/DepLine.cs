using System;

namespace Common.YamlParsers.Models
{
    public class DepLine: IEquatable<DepLine>
    {
        public DepLine(Dep dependency): this(false, dependency)
        {
        }

        public DepLine(bool isRemoved, Dep dependency)
        {
            IsRemoved = isRemoved;
            Dependency = dependency;
        }

        private string DebuggerDisplay => IsRemoved ? "-" + Dependency : Dependency.ToString();

        public bool IsRemoved { get; set; }
        public Dep Dependency { get; set; }

        public bool Equals(DepLine other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return IsRemoved == other.IsRemoved && Dependency.Equals(other.Dependency);
        }

        public override string ToString() => DebuggerDisplay;

    }
}
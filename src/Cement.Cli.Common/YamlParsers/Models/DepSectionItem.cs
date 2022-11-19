using System;

namespace Cement.Cli.Common.YamlParsers.Models;

public sealed class DepSectionItem : IEquatable<DepSectionItem>
{
    public DepSectionItem(Dep dependency)
        : this(false, dependency)
    {
    }

    public DepSectionItem(bool isRemoved, Dep dependency)
    {
        IsRemoved = isRemoved;
        Dependency = dependency;
    }

    public bool IsRemoved { get; set; }
    public Dep Dependency { get; set; }

    public bool Equals(DepSectionItem other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return IsRemoved == other.IsRemoved && Dependency.Equals(other.Dependency);
    }

    public override string ToString() => DebuggerDisplay;

    private string DebuggerDisplay => IsRemoved ? "-" + Dependency : Dependency.ToString();
}

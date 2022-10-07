using System;

namespace Commands.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class HiddenCommandAttribute : Attribute
{
}

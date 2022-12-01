using System;

namespace Cement.Cli.Commands.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class HiddenCommandAttribute : Attribute
{
}

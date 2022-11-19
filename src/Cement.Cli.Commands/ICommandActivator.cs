using System;
using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public interface ICommandActivator
{
    object Create(Type commandType);
}

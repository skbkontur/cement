using System;
using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
public interface ICommandActivator
{
    object Create(Type commandType);
}

using JetBrains.Annotations;

namespace Commands.Extensions;

[PublicAPI]
public static class CommandActivatorExtensions
{
    public static TCommand Create<TCommand>(this ICommandActivator commandActivator)
    {
        return (TCommand)commandActivator.Create(typeof(TCommand));
    }
}

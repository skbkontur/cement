using System;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class DefaultCommandActivator : ICommandActivator
{
    private readonly IServiceProvider serviceProvider;

    public DefaultCommandActivator(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public object Create(Type commandType)
    {
        return serviceProvider.GetRequiredService(commandType);
    }
}

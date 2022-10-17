using Commands;
using Microsoft.Extensions.DependencyInjection;

namespace cm;

internal static class ServiceCollectionExtensions
{
    public static void AddCommand<TCommand>(this IServiceCollection services)
        where TCommand : class, ICommand
    {
        services.AddSingleton<TCommand>();
        services.AddSingleton<ICommand>(sp => sp.GetRequiredService<TCommand>());
    }

    public static void AddSubcommand<TCommand>(this IServiceCollection services)
        where TCommand : class, ICommand
    {
        services.AddSingleton<TCommand>();
    }
}

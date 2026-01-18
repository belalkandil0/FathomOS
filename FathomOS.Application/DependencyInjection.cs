using System.Reflection;
using FathomOS.Application.Common.Behaviors;
using FathomOS.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FathomOS.Application;

/// <summary>
/// Extension methods for configuring application layer services in dependency injection.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds application layer services to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Add pipeline behaviors in order of execution
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        });

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        // Register date/time provider
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }

    /// <summary>
    /// Adds application layer services from a specific assembly.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assembly">The assembly to scan for handlers and validators</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddApplicationServicesFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        // Register MediatR handlers from the specified assembly
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        // Register FluentValidation validators from the specified assembly
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }

    /// <summary>
    /// Adds transaction behavior for commands.
    /// Requires IUnitOfWork to be registered in the container.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddTransactionBehavior(this IServiceCollection services)
    {
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        return services;
    }

    /// <summary>
    /// Adds domain event dispatch behavior for commands.
    /// Requires IDomainEventDispatcher to be registered in the container.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDomainEventDispatchBehavior(this IServiceCollection services)
    {
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(DomainEventDispatchBehavior<,>));
        return services;
    }
}

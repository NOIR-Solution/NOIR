using System.Reflection;

namespace NOIR.Application.Common.Extensions;

/// <summary>
/// Extension methods for Scrutor DI auto-registration using marker interfaces.
/// Centralizes the pattern for scanning and registering services by IScopedService,
/// ITransientService, and ISingletonService marker interfaces.
/// </summary>
public static class ScrutorExtensions
{
    /// <summary>
    /// Scans the specified assembly for services implementing marker interfaces
    /// (IScopedService, ITransientService, ISingletonService) and registers them
    /// with appropriate lifetimes using AsSelfWithInterfaces().
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <param name="assembly">The assembly to scan for services.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Uses AsSelfWithInterfaces() to register both concrete type AND interfaces.
    /// This is required for:
    /// - Hangfire background jobs (inject by concrete type)
    /// - Wolverine handlers with explicit registration
    /// - [FromServices] endpoint injection by concrete type
    /// </remarks>
    public static IServiceCollection ScanMarkerInterfaces(
        this IServiceCollection services,
        Assembly assembly)
    {
        services.Scan(scan => scan
            .FromAssemblies(assembly)

            // Register IScopedService implementations
            .AddClasses(c => c.AssignableTo<IScopedService>(), publicOnly: false)
            .AsSelfWithInterfaces()
            .WithScopedLifetime()

            // Register ITransientService implementations
            .AddClasses(c => c.AssignableTo<ITransientService>(), publicOnly: false)
            .AsSelfWithInterfaces()
            .WithTransientLifetime()

            // Register ISingletonService implementations
            .AddClasses(c => c.AssignableTo<ISingletonService>(), publicOnly: false)
            .AsSelfWithInterfaces()
            .WithSingletonLifetime()
        );

        return services;
    }
}

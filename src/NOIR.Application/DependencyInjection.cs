using NOIR.Application.Common.Extensions;

namespace NOIR.Application;

/// <summary>
/// Extension methods for configuring Application layer services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Note: FluentValidation validators are auto-discovered by Wolverine's UseFluentValidation()
        // from assemblies registered via opts.Discovery.IncludeAssembly(), so we don't need to
        // register them here to avoid duplicate registration.

        // Auto-register Application layer services using shared Scrutor extension
        services.ScanMarkerInterfaces(typeof(DependencyInjection).Assembly);

        return services;
    }
}

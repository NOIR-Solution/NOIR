namespace NOIR.Application;

/// <summary>
/// Extension methods for configuring Application layer services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register FluentValidation validators from this assembly
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}

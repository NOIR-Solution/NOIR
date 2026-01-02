namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Marker interface for services registered with Transient lifetime.
/// Add this interface to any service implementation for automatic registration.
/// </summary>
public interface ITransientService { }

/// <summary>
/// Marker interface for services registered with Scoped lifetime.
/// Add this interface to any service implementation for automatic registration.
/// This is the most common lifetime for services with database access.
/// </summary>
public interface IScopedService { }

/// <summary>
/// Marker interface for services registered with Singleton lifetime.
/// Add this interface to any service implementation for automatic registration.
/// Use for stateless services, caches, and configuration services.
/// </summary>
public interface ISingletonService { }

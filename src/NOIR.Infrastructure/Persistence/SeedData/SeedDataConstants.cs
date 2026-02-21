namespace NOIR.Infrastructure.Persistence.SeedData;

/// <summary>
/// Shared constants and helpers for deterministic seed data generation.
/// All GUIDs are derived from SHA256 hashes to ensure idempotency across runs.
/// </summary>
public static class SeedDataConstants
{
    /// <summary>
    /// Base timestamp for all seed data. UTC+7 (Vietnam timezone).
    /// Seed entities are spread from this date using SpreadDate().
    /// </summary>
    public static readonly DateTimeOffset BaseTimestamp =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(7));

    /// <summary>
    /// Generates a deterministic GUID from a string seed using SHA256.
    /// Same seed always produces the same GUID across runs.
    /// </summary>
    public static Guid DeterministicGuid(string seed)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        return new Guid(hash.AsSpan(0, 16));
    }

    /// <summary>
    /// Generates a tenant-scoped deterministic GUID.
    /// Ensures the same seed produces different GUIDs for different tenants.
    /// </summary>
    public static Guid TenantGuid(string tenantId, string seed) =>
        DeterministicGuid($"{tenantId}:{seed}");

    /// <summary>
    /// Returns BaseTimestamp + dayOffset days for realistic date distribution.
    /// </summary>
    public static DateTimeOffset SpreadDate(int dayOffset) =>
        BaseTimestamp.AddDays(dayOffset);

    /// <summary>
    /// Sets the Id property on an entity using reflection.
    /// Entity.Id has a protected setter, so we walk the type hierarchy.
    /// Shared across all seed modules to avoid duplication.
    /// </summary>
    public static void SetEntityId<T>(T entity, Guid id) where T : class
    {
        var type = entity.GetType();
        while (type != null)
        {
            var idProp = type.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (idProp != null && idProp.CanWrite)
            {
                idProp.SetValue(entity, id);
                return;
            }
            type = type.BaseType;
        }

        // Fallback: try any accessible Id property
        var fallbackProp = entity.GetType().GetProperty("Id");
        if (fallbackProp != null)
        {
            fallbackProp.SetValue(entity, id);
            return;
        }

        throw new InvalidOperationException(
            $"Cannot set Id on entity type {entity.GetType().Name}. No writable Id property found.");
    }
}

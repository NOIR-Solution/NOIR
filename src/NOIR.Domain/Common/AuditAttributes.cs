namespace NOIR.Domain.Common;

/// <summary>
/// Marks an entity class or property as audited.
/// When applied to a class, the entire entity will be audited.
/// When applied to a property, only that property will be tracked.
/// </summary>
/// <remarks>
/// By default, all entities are audited. Use [DisableAuditing] to exclude.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = true)]
public sealed class AuditedAttribute : Attribute
{
    /// <summary>
    /// Whether to include the old value in the audit diff.
    /// Default: true
    /// </summary>
    public bool IncludeOldValue { get; set; } = true;

    /// <summary>
    /// Whether to include the new value in the audit diff.
    /// Default: true
    /// </summary>
    public bool IncludeNewValue { get; set; } = true;

    /// <summary>
    /// Optional friendly name for the property in audit logs.
    /// If not set, the property name is used.
    /// </summary>
    public string? DisplayName { get; set; }
}

/// <summary>
/// Marks an entity class or property as excluded from auditing.
/// When applied to a class, the entire entity will not be audited.
/// When applied to a property, that property will be excluded from the audit diff.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = true)]
public sealed class DisableAuditingAttribute : Attribute
{
    /// <summary>
    /// Optional reason for disabling auditing.
    /// For documentation purposes only.
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Marks a property as containing sensitive data that should be redacted in audit logs.
/// The property will be tracked, but values will show as "[REDACTED]".
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class AuditSensitiveAttribute : Attribute
{
    /// <summary>
    /// The mask to use instead of the actual value.
    /// Default: "[REDACTED]"
    /// </summary>
    public string Mask { get; set; } = "[REDACTED]";
}

/// <summary>
/// Marks a handler/command as excluded from audit logging.
/// Use for high-frequency operations that don't need to be audited.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DisableHandlerAuditingAttribute : Attribute
{
    /// <summary>
    /// Optional reason for disabling auditing.
    /// For documentation purposes only.
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Marks a collection navigation property for tracking add/remove operations.
/// When applied, additions and removals from the collection will be recorded in the audit log.
/// </summary>
/// <example>
/// public class Customer : Entity&lt;Guid&gt;
/// {
///     [AuditCollection]
///     public ICollection&lt;Order&gt; Orders { get; set; }
/// }
/// </example>
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class AuditCollectionAttribute : Attribute
{
    /// <summary>
    /// Whether to include the child entity details in the audit diff.
    /// Default: false (only tracks IDs).
    /// </summary>
    public bool IncludeChildDetails { get; set; }

    /// <summary>
    /// Optional property name to use as the display identifier for child items.
    /// Default: uses the primary key.
    /// </summary>
    public string? ChildDisplayProperty { get; set; }
}

/// <summary>
/// Extension methods for checking audit attributes.
/// </summary>
public static class AuditAttributeExtensions
{
    /// <summary>
    /// Checks if a type has auditing disabled.
    /// </summary>
    public static bool IsAuditingDisabled(this Type type)
    {
        return type.GetCustomAttribute<DisableAuditingAttribute>() is not null;
    }

    /// <summary>
    /// Checks if a type is explicitly marked as audited.
    /// </summary>
    public static bool IsExplicitlyAudited(this Type type)
    {
        return type.GetCustomAttribute<AuditedAttribute>() is not null;
    }

    /// <summary>
    /// Checks if a property has auditing disabled.
    /// </summary>
    public static bool IsAuditingDisabled(this PropertyInfo property)
    {
        return property.GetCustomAttribute<DisableAuditingAttribute>() is not null;
    }

    /// <summary>
    /// Checks if a property is marked as sensitive.
    /// </summary>
    public static bool IsSensitive(this PropertyInfo property)
    {
        return property.GetCustomAttribute<AuditSensitiveAttribute>() is not null;
    }

    /// <summary>
    /// Gets the sensitivity mask for a property.
    /// </summary>
    public static string GetSensitivityMask(this PropertyInfo property)
    {
        return property.GetCustomAttribute<AuditSensitiveAttribute>()?.Mask ?? "[REDACTED]";
    }

    /// <summary>
    /// Gets the display name for a property (from [Audited] attribute or property name).
    /// </summary>
    public static string GetAuditDisplayName(this PropertyInfo property)
    {
        return property.GetCustomAttribute<AuditedAttribute>()?.DisplayName ?? property.Name;
    }

    /// <summary>
    /// Checks if a handler type has auditing disabled.
    /// </summary>
    public static bool IsHandlerAuditingDisabled(this Type handlerType)
    {
        return handlerType.GetCustomAttribute<DisableHandlerAuditingAttribute>() is not null;
    }
}

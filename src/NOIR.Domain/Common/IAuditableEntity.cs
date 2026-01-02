namespace NOIR.Domain.Common;

/// <summary>
/// Interface for entities that track audit information.
/// Uses DateTimeOffset for timezone-aware timestamps.
/// Includes soft delete fields for data safety.
/// </summary>
public interface IAuditableEntity
{
    // Creation audit
    DateTimeOffset CreatedAt { get; }
    string? CreatedBy { get; }

    // Modification audit
    DateTimeOffset? ModifiedAt { get; }
    string? ModifiedBy { get; }

    // Soft delete audit (data safety - never hard delete)
    bool IsDeleted { get; }
    DateTimeOffset? DeletedAt { get; }
    string? DeletedBy { get; }
}

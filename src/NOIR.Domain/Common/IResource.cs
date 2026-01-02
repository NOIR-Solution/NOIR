namespace NOIR.Domain.Common;

/// <summary>
/// Interface for entities that support resource-based authorization.
/// Enables ownership checks, sharing, and permission inheritance.
/// </summary>
public interface IResource
{
    /// <summary>
    /// Unique identifier of the resource.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Type name of the resource (e.g., "document", "folder", "report").
    /// Used for resource share lookups.
    /// </summary>
    string ResourceType { get; }

    /// <summary>
    /// User ID of the resource creator/owner.
    /// Owner has implicit full access.
    /// </summary>
    string? OwnerId { get; }

    /// <summary>
    /// Parent resource ID for permission inheritance.
    /// Null means no parent (root level resource).
    /// </summary>
    Guid? ParentResourceId { get; }

    /// <summary>
    /// Parent resource type for inheritance lookups.
    /// </summary>
    string? ParentResourceType { get; }
}

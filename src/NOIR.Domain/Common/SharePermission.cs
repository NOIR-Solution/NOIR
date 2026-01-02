namespace NOIR.Domain.Common;

/// <summary>
/// Permission levels for resource sharing.
/// Higher levels include all lower level permissions.
/// </summary>
public enum SharePermission
{
    /// <summary>
    /// Read-only access to the resource.
    /// </summary>
    View = 0,

    /// <summary>
    /// Can view and add comments (if supported).
    /// </summary>
    Comment = 1,

    /// <summary>
    /// Can view, comment, and modify the resource.
    /// </summary>
    Edit = 2,

    /// <summary>
    /// Full control including ability to share with others and delete.
    /// </summary>
    Admin = 3
}

/// <summary>
/// Extension methods for SharePermission.
/// </summary>
public static class SharePermissionExtensions
{
    /// <summary>
    /// Checks if this permission level allows the specified action.
    /// </summary>
    public static bool Allows(this SharePermission permission, string action)
    {
        return action.ToLowerInvariant() switch
        {
            "read" or "view" => permission >= SharePermission.View,
            "comment" => permission >= SharePermission.Comment,
            "edit" or "update" or "write" => permission >= SharePermission.Edit,
            "delete" or "admin" or "share" or "manage" => permission >= SharePermission.Admin,
            _ => false
        };
    }

    /// <summary>
    /// Converts action string to minimum required permission.
    /// </summary>
    public static SharePermission? FromAction(string action)
    {
        return action.ToLowerInvariant() switch
        {
            "read" or "view" => SharePermission.View,
            "comment" => SharePermission.Comment,
            "edit" or "update" or "write" => SharePermission.Edit,
            "delete" or "admin" or "share" or "manage" => SharePermission.Admin,
            _ => null
        };
    }
}

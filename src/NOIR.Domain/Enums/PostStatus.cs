namespace NOIR.Domain.Enums;

/// <summary>
/// Status of a blog post.
/// </summary>
public enum PostStatus
{
    /// <summary>
    /// Post is being edited and not visible to public.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Post is published and visible to public.
    /// </summary>
    Published = 1,

    /// <summary>
    /// Post is scheduled for future publication.
    /// </summary>
    Scheduled = 2,

    /// <summary>
    /// Post is archived and no longer actively displayed.
    /// </summary>
    Archived = 3
}

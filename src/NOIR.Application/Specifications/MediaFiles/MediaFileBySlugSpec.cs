namespace NOIR.Application.Specifications.MediaFiles;

/// <summary>
/// Specification to get a media file by its slug.
/// </summary>
public sealed class MediaFileBySlugSpec : Specification<MediaFile>
{
    public MediaFileBySlugSpec(string slug, bool asTracking = false)
    {
        Query.Where(m => m.Slug == slug)
             .TagWith("MediaFileBySlug");

        if (asTracking)
        {
            Query.AsTracking();
        }
    }
}

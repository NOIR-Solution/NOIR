namespace NOIR.Application.Specifications.MediaFiles;

/// <summary>
/// Specification to get a media file by its ID.
/// </summary>
public sealed class MediaFileByIdSpec : Specification<MediaFile>
{
    public MediaFileByIdSpec(Guid id, bool asTracking = false)
    {
        Query.Where(m => m.Id == id)
             .TagWith("MediaFileById");

        if (asTracking)
        {
            Query.AsTracking();
        }
    }
}

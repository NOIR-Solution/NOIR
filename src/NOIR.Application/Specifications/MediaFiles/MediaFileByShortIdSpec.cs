namespace NOIR.Application.Specifications.MediaFiles;

/// <summary>
/// Specification to find a media file by its short ID (8-char unique identifier).
/// </summary>
public sealed class MediaFileByShortIdSpec : Specification<MediaFile>
{
    public MediaFileByShortIdSpec(string shortId, bool asTracking = false)
    {
        Query.Where(m => m.ShortId == shortId)
            .TagWith("MediaFileByShortId");

        if (asTracking)
        {
            Query.AsTracking();
        }
    }
}

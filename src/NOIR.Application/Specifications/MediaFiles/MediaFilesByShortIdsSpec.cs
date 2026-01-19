namespace NOIR.Application.Specifications.MediaFiles;

/// <summary>
/// Specification to find multiple media files by their short IDs.
/// </summary>
public class MediaFilesByShortIdsSpec : Specification<MediaFile>
{
    public MediaFilesByShortIdsSpec(IEnumerable<string> shortIds)
    {
        Query.Where(m => shortIds.Contains(m.ShortId))
            .TagWith("MediaFilesByShortIds");
    }
}

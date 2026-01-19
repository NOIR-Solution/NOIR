namespace NOIR.Application.Specifications.MediaFiles;

/// <summary>
/// Specification to find multiple media files by their IDs.
/// </summary>
public class MediaFilesByIdsSpec : Specification<MediaFile>
{
    public MediaFilesByIdsSpec(IEnumerable<Guid> ids)
    {
        Query.Where(m => ids.Contains(m.Id))
            .TagWith("MediaFilesByIds");
    }
}

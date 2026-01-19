namespace NOIR.Application.Specifications.MediaFiles;

/// <summary>
/// Specification to find multiple media files by their slugs.
/// </summary>
public class MediaFilesBySlugsSpec : Specification<MediaFile>
{
    public MediaFilesBySlugsSpec(IEnumerable<string> slugs)
    {
        Query.Where(m => slugs.Contains(m.Slug))
            .TagWith("MediaFilesBySlugs");
    }
}

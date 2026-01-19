namespace NOIR.Application.Specifications.MediaFiles;

/// <summary>
/// Specification to get media files by folder with pagination.
/// </summary>
public sealed class MediaFilesByFolderSpec : Specification<MediaFile>
{
    public MediaFilesByFolderSpec(string folder, int page = 1, int pageSize = 20)
    {
        Query.Where(m => m.Folder == folder)
             .OrderByDescending(m => m.CreatedAt)
             .Paginate(page - 1, pageSize)
             .TagWith("MediaFilesByFolder");
    }
}

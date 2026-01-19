namespace NOIR.Application.Specifications.MediaFiles;

/// <summary>
/// Specification to get media files uploaded by a specific user.
/// </summary>
public sealed class MediaFilesByUploaderSpec : Specification<MediaFile>
{
    public MediaFilesByUploaderSpec(string userId, int page = 1, int pageSize = 20)
    {
        Query.Where(m => m.UploadedBy == userId)
             .OrderByDescending(m => m.CreatedAt)
             .Paginate(page - 1, pageSize)
             .TagWith("MediaFilesByUploader");
    }
}

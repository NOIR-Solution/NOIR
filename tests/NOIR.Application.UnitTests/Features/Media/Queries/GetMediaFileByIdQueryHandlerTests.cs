using NOIR.Application.Features.Media.Dtos;
using NOIR.Application.Features.Media.Queries.GetMediaFileById;
using NOIR.Application.Specifications.MediaFiles;

namespace NOIR.Application.UnitTests.Features.Media.Queries;

/// <summary>
/// Unit tests for GetMediaFileByIdQueryHandler.
/// Tests retrieval of a single media file by ID.
/// </summary>
public class GetMediaFileByIdQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<MediaFile, Guid>> _repositoryMock;
    private readonly GetMediaFileByIdQueryHandler _handler;

    public GetMediaFileByIdQueryHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<MediaFile, Guid>>();
        _handler = new GetMediaFileByIdQueryHandler(_repositoryMock.Object);
    }

    private static MediaFile CreateTestMediaFile(Guid? id = null)
    {
        var mediaFile = MediaFile.Create(
            shortId: "abc12345",
            slug: "test-image_abc12345",
            originalFileName: "test-image.jpg",
            folder: "blog",
            defaultUrl: "/uploads/blog/test-image.webp",
            thumbHash: "dGVzdA==",
            dominantColor: "#FF5733",
            width: 1920,
            height: 1080,
            format: "jpeg",
            mimeType: "image/jpeg",
            sizeBytes: 250_000,
            hasTransparency: false,
            variantsJson: "[]",
            srcsetsJson: "{}",
            uploadedBy: "user-1",
            tenantId: "test-tenant");

        if (id.HasValue)
        {
            typeof(MediaFile).GetProperty("Id")!.SetValue(mediaFile, id.Value);
        }

        return mediaFile;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithExistingMediaFile_ShouldReturnDto()
    {
        // Arrange
        var mediaFileId = Guid.NewGuid();
        var mediaFile = CreateTestMediaFile(mediaFileId);
        var query = new GetMediaFileByIdQuery(mediaFileId);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<MediaFileByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaFile);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Id.ShouldBe(mediaFileId);
        result.Value.OriginalFileName.ShouldBe("test-image.jpg");
        result.Value.Folder.ShouldBe("blog");
        result.Value.Width.ShouldBe(1920);
        result.Value.Height.ShouldBe(1080);
        result.Value.Format.ShouldBe("jpeg");
        result.Value.MimeType.ShouldBe("image/jpeg");
        result.Value.SizeBytes.ShouldBe(250_000);
    }

    [Fact]
    public async Task Handle_ShouldMapAllDtoFields()
    {
        // Arrange
        var mediaFileId = Guid.NewGuid();
        var mediaFile = CreateTestMediaFile(mediaFileId);
        var query = new GetMediaFileByIdQuery(mediaFileId);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<MediaFileByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaFile);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var dto = result.Value;
        dto.ShortId.ShouldBe("abc12345");
        dto.Slug.ShouldBe("test-image_abc12345");
        dto.DefaultUrl.ShouldBe("/uploads/blog/test-image.webp");
        dto.ThumbHash.ShouldBe("dGVzdA==");
        dto.DominantColor.ShouldBe("#FF5733");
        dto.HasTransparency.ShouldBe(false);
        dto.Variants.ShouldBeEmpty();
        dto.Srcsets.ShouldBeEmpty();
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WithNonExistentMediaFile_ShouldReturnNotFoundError()
    {
        // Arrange
        var query = new GetMediaFileByIdQuery(Guid.NewGuid());

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<MediaFileByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaFile?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        result.Error.Code.ShouldBe("NOIR-MEDIA-001");
    }

    #endregion
}

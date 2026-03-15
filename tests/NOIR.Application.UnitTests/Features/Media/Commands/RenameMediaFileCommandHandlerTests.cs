using NOIR.Application.Features.Media.Commands.RenameMediaFile;
using NOIR.Application.Features.Media.Specifications;

namespace NOIR.Application.UnitTests.Features.Media.Commands;

/// <summary>
/// Unit tests for RenameMediaFileCommandHandler.
/// Tests media file rename scenarios with mocked dependencies.
/// </summary>
public class RenameMediaFileCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<MediaFile, Guid>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly RenameMediaFileCommandHandler _handler;

    public RenameMediaFileCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<MediaFile, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new RenameMediaFileCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object);
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
    public async Task Handle_WithExistingMediaFile_ShouldRenameSuccessfully()
    {
        // Arrange
        var mediaFileId = Guid.NewGuid();
        var mediaFile = CreateTestMediaFile(mediaFileId);
        var command = new RenameMediaFileCommand(mediaFileId, "new-name.jpg");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<MediaFileByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaFile);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldBe(true);
        mediaFile.OriginalFileName.ShouldBe("new-name.jpg");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithDifferentNewName_ShouldUpdateFileName()
    {
        // Arrange
        var mediaFileId = Guid.NewGuid();
        var mediaFile = CreateTestMediaFile(mediaFileId);
        var newFileName = "updated-product-photo.png";
        var command = new RenameMediaFileCommand(mediaFileId, newFileName);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<MediaFileByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaFile);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        mediaFile.OriginalFileName.ShouldBe(newFileName);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WithNonExistentMediaFile_ShouldReturnNotFoundError()
    {
        // Arrange
        var command = new RenameMediaFileCommand(Guid.NewGuid(), "new-name.jpg");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<MediaFileByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaFile?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        result.Error.Code.ShouldBe("NOIR-MEDIA-003");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion
}

using NOIR.Application.Features.Media.Commands.DeleteMediaFile;
using NOIR.Application.Features.Media.Specifications;

namespace NOIR.Application.UnitTests.Features.Media.Commands;

/// <summary>
/// Unit tests for DeleteMediaFileCommandHandler.
/// Tests media file deletion scenarios with mocked dependencies.
/// </summary>
public class DeleteMediaFileCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<MediaFile, Guid>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly DeleteMediaFileCommandHandler _handler;

    public DeleteMediaFileCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<MediaFile, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new DeleteMediaFileCommandHandler(
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
    public async Task Handle_WithExistingMediaFile_ShouldDeleteSuccessfully()
    {
        // Arrange
        var mediaFileId = Guid.NewGuid();
        var mediaFile = CreateTestMediaFile(mediaFileId);
        var command = new DeleteMediaFileCommand(mediaFileId, "test-image.jpg");

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

        _repositoryMock.Verify(
            x => x.Remove(mediaFile),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WithNonExistentMediaFile_ShouldReturnNotFoundError()
    {
        // Arrange
        var command = new DeleteMediaFileCommand(Guid.NewGuid());

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<MediaFileByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaFile?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        result.Error.Code.ShouldBe("NOIR-MEDIA-002");

        _repositoryMock.Verify(
            x => x.Remove(It.IsAny<MediaFile>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCallMarkAsDeletedBeforeRemove()
    {
        // Arrange
        var mediaFileId = Guid.NewGuid();
        var mediaFile = CreateTestMediaFile(mediaFileId);
        var command = new DeleteMediaFileCommand(mediaFileId);

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
        _repositoryMock.Verify(x => x.Remove(mediaFile), Times.Once);
    }

    #endregion
}

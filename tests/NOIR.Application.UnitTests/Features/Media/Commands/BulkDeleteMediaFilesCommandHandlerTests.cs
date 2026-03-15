using NOIR.Application.Features.Media.Commands.BulkDeleteMediaFiles;
using NOIR.Application.Features.Media.Specifications;

namespace NOIR.Application.UnitTests.Features.Media.Commands;

/// <summary>
/// Unit tests for BulkDeleteMediaFilesCommandHandler.
/// Tests bulk media file deletion scenarios with mocked dependencies.
/// </summary>
public class BulkDeleteMediaFilesCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<MediaFile, Guid>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly BulkDeleteMediaFilesCommandHandler _handler;

    public BulkDeleteMediaFilesCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<MediaFile, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new BulkDeleteMediaFilesCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static MediaFile CreateTestMediaFile(Guid id, string fileName = "test-image.jpg")
    {
        var mediaFile = MediaFile.Create(
            shortId: Guid.NewGuid().ToString("N")[..8],
            slug: $"test-image_{Guid.NewGuid():N}"[..20],
            originalFileName: fileName,
            folder: "blog",
            defaultUrl: $"/uploads/blog/{fileName}",
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

        typeof(MediaFile).GetProperty("Id")!.SetValue(mediaFile, id);
        return mediaFile;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithAllExistingFiles_ShouldDeleteAllSuccessfully()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();
        var ids = new List<Guid> { id1, id2, id3 };

        var file1 = CreateTestMediaFile(id1, "photo1.jpg");
        var file2 = CreateTestMediaFile(id2, "photo2.jpg");
        var file3 = CreateTestMediaFile(id3, "photo3.jpg");

        var command = new BulkDeleteMediaFilesCommand(ids);

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<MediaFilesByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MediaFile> { file1, file2, file3 });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(3);
        result.Value.Failed.ShouldBe(0);
        result.Value.Errors.ShouldBeEmpty();

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Partial Success Scenarios

    [Fact]
    public async Task Handle_WithSomeNonExistentIds_ShouldReturnPartialSuccess()
    {
        // Arrange
        var existingId = Guid.NewGuid();
        var nonExistentId = Guid.NewGuid();
        var ids = new List<Guid> { existingId, nonExistentId };

        var existingFile = CreateTestMediaFile(existingId, "existing.jpg");
        var command = new BulkDeleteMediaFilesCommand(ids);

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<MediaFilesByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MediaFile> { existingFile });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(1);
        result.Value.Failed.ShouldBe(1);
        result.Value.Errors.Count().ShouldBe(1);
        result.Value.Errors[0].MediaFileId.ShouldBe(nonExistentId);
        result.Value.Errors[0].Message.ShouldContain("not found");
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WithAllNonExistentIds_ShouldReturnZeroSuccess()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var ids = new List<Guid> { id1, id2 };

        var command = new BulkDeleteMediaFilesCommand(ids);

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<MediaFilesByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MediaFile>());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(0);
        result.Value.Failed.ShouldBe(2);
        result.Value.Errors.Count().ShouldBe(2);
    }

    #endregion
}

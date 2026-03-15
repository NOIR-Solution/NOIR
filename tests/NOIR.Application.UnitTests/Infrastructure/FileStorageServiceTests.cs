namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for FileStorageService.
/// Tests file storage operations with mocked IBlobStorage.
/// </summary>
public class FileStorageServiceTests
{
    private readonly Mock<IBlobStorage> _storageMock;
    private readonly Mock<IOptions<StorageSettings>> _settingsMock;
    private readonly Mock<ILogger<FileStorageService>> _loggerMock;
    private readonly FileStorageService _sut;

    public FileStorageServiceTests()
    {
        _storageMock = new Mock<IBlobStorage>();
        _loggerMock = new Mock<ILogger<FileStorageService>>();

        // Setup default storage settings
        var settings = new StorageSettings { MediaUrlPrefix = "/media" };
        _settingsMock = new Mock<IOptions<StorageSettings>>();
        _settingsMock.Setup(x => x.Value).Returns(settings);

        _sut = new FileStorageService(_storageMock.Object, _settingsMock.Object, _loggerMock.Object);
    }

    #region UploadAsync Tests

    [Fact]
    public async Task UploadAsync_WithValidFile_ShouldReturnPath()
    {
        // Arrange
        var content = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        _storageMock.Setup(x => x.WriteAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UploadAsync("test.txt", content);

        // Assert
        result.ShouldBe("test.txt");
    }

    [Fact]
    public async Task UploadAsync_WithFolder_ShouldReturnFolderPath()
    {
        // Arrange
        var content = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        _storageMock.Setup(x => x.WriteAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UploadAsync("test.txt", content, "uploads");

        // Assert
        result.ShouldBe("uploads/test.txt");
    }

    [Fact]
    public async Task UploadAsync_WhenStorageThrows_ShouldRethrow()
    {
        // Arrange
        var content = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        _storageMock.Setup(x => x.WriteAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Storage error"));

        // Act
        var action = () => _sut.UploadAsync("test.txt", content);

        // Assert
        await Should.ThrowAsync<IOException>(action);
    }

    [Fact]
    public async Task UploadAsync_ShouldCallWriteWithCorrectPath()
    {
        // Arrange
        var content = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        _storageMock.Setup(x => x.WriteAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.UploadAsync("myfile.txt", content, "documents");

        // Assert
        _storageMock.Verify(x => x.WriteAsync("documents/myfile.txt", It.IsAny<Stream>(), false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadAsync_WithEmptyFolder_ShouldUseFileNameOnly()
    {
        // Arrange
        var content = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        _storageMock.Setup(x => x.WriteAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UploadAsync("test.txt", content, "");

        // Assert
        result.ShouldBe("test.txt");
    }

    [Fact]
    public async Task UploadAsync_WithNullFolder_ShouldUseFileNameOnly()
    {
        // Arrange
        var content = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        _storageMock.Setup(x => x.WriteAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UploadAsync("test.txt", content, null);

        // Assert
        result.ShouldBe("test.txt");
    }

    #endregion

    #region DownloadAsync Tests

    [Fact]
    public async Task DownloadAsync_WhenFileExists_ShouldReturnStream()
    {
        // Arrange
        var expectedStream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        // ExistsAsync(string, ct) is an extension method that calls ExistsAsync(string[], ct)
        _storageMock.Setup(x => x.ExistsAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string[] paths, CancellationToken _) =>
                paths.Select(_ => true).ToArray());
        _storageMock.Setup(x => x.OpenReadAsync("test.txt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStream);

        // Act
        var result = await _sut.DownloadAsync("test.txt");

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeSameAs(expectedStream);
    }

    [Fact]
    public async Task DownloadAsync_WhenFileDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        _storageMock.Setup(x => x.ExistsAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string[] paths, CancellationToken _) =>
                paths.Select(_ => false).ToArray());

        // Act
        var result = await _sut.DownloadAsync("missing.txt");

        // Assert
        result.ShouldBeNull();
        _storageMock.Verify(x => x.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DownloadAsync_WhenExistsThrows_ShouldRethrow()
    {
        // Arrange
        _storageMock.Setup(x => x.ExistsAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Storage error"));

        // Act
        var action = () => _sut.DownloadAsync("test.txt");

        // Assert
        await Should.ThrowAsync<IOException>(action);
    }

    [Fact]
    public async Task DownloadAsync_WhenOpenReadThrows_ShouldRethrow()
    {
        // Arrange
        _storageMock.Setup(x => x.ExistsAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string[] paths, CancellationToken _) =>
                paths.Select(_ => true).ToArray());
        _storageMock.Setup(x => x.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Read error"));

        // Act
        var action = () => _sut.DownloadAsync("test.txt");

        // Assert
        await Should.ThrowAsync<IOException>(action);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenFileExists_ShouldDeleteAndReturnTrue()
    {
        // Arrange
        _storageMock.Setup(x => x.ExistsAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string[] paths, CancellationToken _) =>
                paths.Select(_ => true).ToArray());
        _storageMock.Setup(x => x.DeleteAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.DeleteAsync("test.txt");

        // Assert
        result.ShouldBe(true);
        _storageMock.Verify(x => x.DeleteAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenFileDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        _storageMock.Setup(x => x.ExistsAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string[] paths, CancellationToken _) =>
                paths.Select(_ => false).ToArray());

        // Act
        var result = await _sut.DeleteAsync("missing.txt");

        // Assert
        result.ShouldBe(false);
        _storageMock.Verify(x => x.DeleteAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenExistsThrows_ShouldReturnFalse()
    {
        // Arrange
        _storageMock.Setup(x => x.ExistsAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Storage error"));

        // Act
        var result = await _sut.DeleteAsync("test.txt");

        // Assert
        result.ShouldBe(false);
    }

    [Fact]
    public async Task DeleteAsync_WhenDeleteThrows_ShouldReturnFalse()
    {
        // Arrange
        _storageMock.Setup(x => x.ExistsAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string[] paths, CancellationToken _) =>
                paths.Select(_ => true).ToArray());
        _storageMock.Setup(x => x.DeleteAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Delete error"));

        // Act
        var result = await _sut.DeleteAsync("test.txt");

        // Assert
        result.ShouldBe(false);
    }

    #endregion

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_ShouldReturnFilePaths()
    {
        // Arrange
        var blobs = new[]
        {
            new Blob("file1.txt"),
            new Blob("file2.txt")
        };
        _storageMock.Setup(x => x.ListAsync(It.IsAny<ListOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(blobs);

        // Act
        var result = await _sut.ListAsync();

        // Assert
        result.Count().ShouldBe(2);
        result.ShouldContain(p => p.Contains("file1.txt"));
        result.ShouldContain(p => p.Contains("file2.txt"));
    }

    [Fact]
    public async Task ListAsync_WithFolder_ShouldPassFolderToStorage()
    {
        // Arrange
        ListOptions? capturedOptions = null;
        _storageMock.Setup(x => x.ListAsync(It.IsAny<ListOptions>(), It.IsAny<CancellationToken>()))
            .Callback<ListOptions, CancellationToken>((opts, _) => capturedOptions = opts)
            .ReturnsAsync(Array.Empty<Blob>());

        // Act
        await _sut.ListAsync("uploads");

        // Assert
        capturedOptions.ShouldNotBeNull();
        capturedOptions!.FolderPath.ShouldContain("uploads");
    }

    [Fact]
    public async Task ListAsync_WhenStorageThrows_ShouldReturnEmpty()
    {
        // Arrange
        _storageMock.Setup(x => x.ListAsync(It.IsAny<ListOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("List error"));

        // Act
        var result = await _sut.ListAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListAsync_WithEmptyResult_ShouldReturnEmpty()
    {
        // Arrange
        _storageMock.Setup(x => x.ListAsync(It.IsAny<ListOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Blob>());

        // Act
        var result = await _sut.ListAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListAsync_WithNullFolder_ShouldNotSetFolderPath()
    {
        // Arrange
        ListOptions? capturedOptions = null;
        _storageMock.Setup(x => x.ListAsync(It.IsAny<ListOptions>(), It.IsAny<CancellationToken>()))
            .Callback<ListOptions, CancellationToken>((opts, _) => capturedOptions = opts)
            .ReturnsAsync(Array.Empty<Blob>());

        // Act
        await _sut.ListAsync(null);

        // Assert
        capturedOptions.ShouldNotBeNull();
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WhenFileExists_ShouldReturnTrue()
    {
        // Arrange - ExistsAsync(string, ct) is an extension method that calls ExistsAsync(string[], ct)
        _storageMock.Setup(x => x.ExistsAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string[] paths, CancellationToken _) =>
                paths.Select(_ => true).ToArray());

        // Act
        var result = await _sut.ExistsAsync("test.txt");

        // Assert
        result.ShouldBe(true);
    }

    [Fact]
    public async Task ExistsAsync_WhenFileDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        _storageMock.Setup(x => x.ExistsAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string[] paths, CancellationToken _) =>
                paths.Select(_ => false).ToArray());

        // Act
        var result = await _sut.ExistsAsync("missing.txt");

        // Assert
        result.ShouldBe(false);
    }

    [Fact]
    public async Task ExistsAsync_WhenStorageThrows_ShouldReturnFalse()
    {
        // Arrange
        _storageMock.Setup(x => x.ExistsAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Storage error"));

        // Act
        var result = await _sut.ExistsAsync("test.txt");

        // Assert
        result.ShouldBe(false);
    }

    #endregion

    #region GetPublicUrl Tests

    [Fact]
    public void GetPublicUrl_ShouldReturnApiEndpointUrl()
    {
        // Act
        var result = _sut.GetPublicUrl("test.txt");

        // Assert
        result.ShouldBe("/media/test.txt");
    }

    [Fact]
    public void GetPublicUrl_WithNestedPath_ShouldReturnApiEndpointUrl()
    {
        // Act
        var result = _sut.GetPublicUrl("folder/subfolder/file.txt");

        // Assert
        result.ShouldBe("/media/folder/subfolder/file.txt");
    }

    [Fact]
    public void GetPublicUrl_WithEmptyPath_ShouldReturnNull()
    {
        // Act
        var result = _sut.GetPublicUrl("");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetPublicUrl_WithNullPath_ShouldReturnNull()
    {
        // Act
        var result = _sut.GetPublicUrl(null!);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetPublicUrl_WithPublicBaseUrl_ShouldReturnAbsoluteCloudUrl()
    {
        // Arrange
        var settings = new StorageSettings
        {
            MediaUrlPrefix = "/media",
            PublicBaseUrl = "https://mybucket.s3.amazonaws.com"
        };
        var settingsMock = new Mock<IOptions<StorageSettings>>();
        settingsMock.Setup(x => x.Value).Returns(settings);
        var sut = new FileStorageService(_storageMock.Object, settingsMock.Object, _loggerMock.Object);

        // Act
        var result = sut.GetPublicUrl("products/product-123/hero-banner-thumb.webp");

        // Assert
        result.ShouldBe("https://mybucket.s3.amazonaws.com/products/product-123/hero-banner-thumb.webp");
    }

    [Fact]
    public void GetPublicUrl_WithPublicBaseUrlTrailingSlash_ShouldNormalize()
    {
        // Arrange
        var settings = new StorageSettings
        {
            MediaUrlPrefix = "/media",
            PublicBaseUrl = "https://mybucket.s3.amazonaws.com/"
        };
        var settingsMock = new Mock<IOptions<StorageSettings>>();
        settingsMock.Setup(x => x.Value).Returns(settings);
        var sut = new FileStorageService(_storageMock.Object, settingsMock.Object, _loggerMock.Object);

        // Act
        var result = sut.GetPublicUrl("test.txt");

        // Assert
        result.ShouldBe("https://mybucket.s3.amazonaws.com/test.txt");
    }

    [Fact]
    public void GetPublicUrl_WithoutPublicBaseUrl_ShouldReturnRelativeUrl()
    {
        // Act (default _sut has no PublicBaseUrl)
        var result = _sut.GetPublicUrl("products/product-123/hero.webp");

        // Assert
        result.ShouldBe("/media/products/product-123/hero.webp");
    }

    #endregion

    #region GetStoragePath with Cloud URL Tests

    [Fact]
    public void GetStoragePath_WithCloudUrl_ShouldExtractPath()
    {
        // Arrange
        var settings = new StorageSettings
        {
            MediaUrlPrefix = "/media",
            PublicBaseUrl = "https://mybucket.s3.amazonaws.com"
        };
        var settingsMock = new Mock<IOptions<StorageSettings>>();
        settingsMock.Setup(x => x.Value).Returns(settings);
        var sut = new FileStorageService(_storageMock.Object, settingsMock.Object, _loggerMock.Object);

        // Act
        var result = sut.GetStoragePath("https://mybucket.s3.amazonaws.com/products/hero.webp");

        // Assert
        result.ShouldBe("products/hero.webp");
    }

    [Fact]
    public void GetStoragePath_WithCloudUrl_NotMatching_ShouldReturnNull()
    {
        // Arrange
        var settings = new StorageSettings
        {
            MediaUrlPrefix = "/media",
            PublicBaseUrl = "https://mybucket.s3.amazonaws.com"
        };
        var settingsMock = new Mock<IOptions<StorageSettings>>();
        settingsMock.Setup(x => x.Value).Returns(settings);
        var sut = new FileStorageService(_storageMock.Object, settingsMock.Object, _loggerMock.Object);

        // Act
        var result = sut.GetStoragePath("https://other-bucket.s3.amazonaws.com/test.webp");

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldAcceptDependencies()
    {
        // Act
        var service = new FileStorageService(_storageMock.Object, _settingsMock.Object, _loggerMock.Object);

        // Assert
        service.ShouldNotBeNull();
    }

    [Fact]
    public void Service_ShouldImplementIFileStorage()
    {
        // Assert
        _sut.ShouldBeAssignableTo<IFileStorage>();
    }

    [Fact]
    public void Service_ShouldImplementIScopedService()
    {
        // Assert
        _sut.ShouldBeAssignableTo<IScopedService>();
    }

    #endregion
}

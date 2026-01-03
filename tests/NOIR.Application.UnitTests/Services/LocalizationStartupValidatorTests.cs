namespace NOIR.Application.UnitTests.Services;

/// <summary>
/// Unit tests for LocalizationStartupValidator.
/// Tests startup validation of localization resource files.
/// </summary>
public class LocalizationStartupValidatorTests : IDisposable
{
    private readonly string _tempPath;
    private readonly LocalizationSettings _settings;
    private readonly Mock<IHostEnvironment> _environmentMock;
    private readonly Mock<ILogger<LocalizationStartupValidator>> _loggerMock;

    public LocalizationStartupValidatorTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"loc_validator_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempPath);

        _settings = new LocalizationSettings
        {
            DefaultCulture = "en",
            SupportedCultures = ["en", "vi"],
            ResourcesPath = "Resources/Localization"
        };

        _environmentMock = new Mock<IHostEnvironment>();
        _environmentMock.Setup(e => e.ContentRootPath).Returns(_tempPath);
        _environmentMock.Setup(e => e.EnvironmentName).Returns("Development");

        _loggerMock = new Mock<ILogger<LocalizationStartupValidator>>();
    }

    public void Dispose()
    {
        // Clean up temp directory
        if (Directory.Exists(_tempPath))
        {
            Directory.Delete(_tempPath, recursive: true);
        }
    }

    private LocalizationStartupValidator CreateValidator()
    {
        var options = Options.Create(_settings);
        return new LocalizationStartupValidator(options, _environmentMock.Object, _loggerMock.Object);
    }

    private void CreateResourceDirectory(string culture)
    {
        var path = Path.Combine(_tempPath, _settings.ResourcesPath, culture);
        Directory.CreateDirectory(path);
    }

    private void CreateResourceFile(string culture, string fileName, string content)
    {
        var dirPath = Path.Combine(_tempPath, _settings.ResourcesPath, culture);
        Directory.CreateDirectory(dirPath);
        File.WriteAllText(Path.Combine(dirPath, fileName), content);
    }

    [Fact]
    public async Task StartAsync_WithValidResources_CompletesSuccessfully()
    {
        // Arrange
        CreateResourceFile("en", "validation.json", """
            {
                "email": { "required": "Email is required." },
                "password": { "required": "Password is required." }
            }
            """);
        CreateResourceFile("en", "auth.json", """
            {
                "login": { "invalidCredentials": "Invalid email or password." },
                "user": { "notFound": "User not found." },
                "role": { "notFound": "Role not found." }
            }
            """);
        CreateResourceFile("vi", "validation.json", """
            {
                "email": { "required": "Email la bat buoc." }
            }
            """);
        CreateResourceFile("vi", "auth.json", """
            {
                "login": { "invalidCredentials": "Email hoac mat khau khong dung." }
            }
            """);

        var validator = CreateValidator();

        // Act
        await validator.StartAsync(CancellationToken.None);

        // Assert - no exception means success
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validating localization resources")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_WithMissingResourcesDirectory_LogsWarningInDevelopment()
    {
        // Arrange - no resources directory created
        var validator = CreateValidator();

        // Act
        await validator.StartAsync(CancellationToken.None);

        // Assert - should log warning but not throw in development
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Localization validation failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_WithMissingResourcesDirectory_ThrowsInProduction()
    {
        // Arrange - no resources directory created
        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var validator = CreateValidator();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            validator.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_WithInvalidJson_LogsWarning()
    {
        // Arrange
        CreateResourceFile("en", "validation.json", "{ invalid json }");
        CreateResourceFile("en", "auth.json", """
            {
                "login": { "invalidCredentials": "Invalid email or password." },
                "user": { "notFound": "User not found." },
                "role": { "notFound": "Role not found." }
            }
            """);
        CreateResourceDirectory("vi");

        var validator = CreateValidator();

        // Act
        await validator.StartAsync(CancellationToken.None);

        // Assert - should log error for invalid JSON
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to parse JSON")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_WithEmptyJsonFile_LogsWarning()
    {
        // Arrange
        CreateResourceFile("en", "validation.json", "");
        CreateResourceFile("en", "auth.json", """
            {
                "login": { "invalidCredentials": "Invalid email or password." },
                "user": { "notFound": "User not found." },
                "role": { "notFound": "Role not found." }
            }
            """);
        CreateResourceDirectory("vi");

        var validator = CreateValidator();

        // Act
        await validator.StartAsync(CancellationToken.None);

        // Assert - should log warning for empty file
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Empty JSON file")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_WithMissingCultureDirectory_LogsWarning()
    {
        // Arrange - only create en, not vi
        CreateResourceFile("en", "validation.json", """
            {
                "email": { "required": "Email is required." },
                "password": { "required": "Password is required." }
            }
            """);
        CreateResourceFile("en", "auth.json", """
            {
                "login": { "invalidCredentials": "Invalid email or password." },
                "user": { "notFound": "User not found." },
                "role": { "notFound": "Role not found." }
            }
            """);

        var validator = CreateValidator();

        // Act
        await validator.StartAsync(CancellationToken.None);

        // Assert - should log warning for missing vi directory
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Resource directory not found for culture 'vi'")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_WithMissingCriticalKeys_LogsWarning()
    {
        // Arrange - create files without critical keys
        CreateResourceFile("en", "validation.json", """
            {
                "someOtherKey": "some value"
            }
            """);
        CreateResourceFile("en", "auth.json", """
            {
                "someOtherKey": "some value"
            }
            """);
        CreateResourceDirectory("vi");

        var validator = CreateValidator();

        // Act
        await validator.StartAsync(CancellationToken.None);

        // Assert - should log warnings for missing critical keys
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Critical translation key")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartAsync_WithJsonComments_ParsesSuccessfully()
    {
        // Arrange - JSON with comments (allowed by JsonCommentHandling.Skip)
        CreateResourceFile("en", "validation.json", """
            {
                // This is a comment
                "email": { "required": "Email is required." },
                "password": { "required": "Password is required." }
            }
            """);
        CreateResourceFile("en", "auth.json", """
            {
                "login": { "invalidCredentials": "Invalid email or password." },
                "user": { "notFound": "User not found." },
                "role": { "notFound": "Role not found." }
            }
            """);
        CreateResourceDirectory("vi");

        var validator = CreateValidator();

        // Act
        await validator.StartAsync(CancellationToken.None);

        // Assert - no parse errors
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to parse JSON")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task StartAsync_WithTrailingCommas_ParsesSuccessfully()
    {
        // Arrange - JSON with trailing commas (allowed by AllowTrailingCommas)
        CreateResourceFile("en", "validation.json", """
            {
                "email": { "required": "Email is required.", },
                "password": { "required": "Password is required.", },
            }
            """);
        CreateResourceFile("en", "auth.json", """
            {
                "login": { "invalidCredentials": "Invalid email or password." },
                "user": { "notFound": "User not found." },
                "role": { "notFound": "Role not found." }
            }
            """);
        CreateResourceDirectory("vi");

        var validator = CreateValidator();

        // Act
        await validator.StartAsync(CancellationToken.None);

        // Assert - no parse errors
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to parse JSON")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task StopAsync_CompletesImmediately()
    {
        // Arrange
        var validator = CreateValidator();

        // Act
        var task = validator.StopAsync(CancellationToken.None);

        // Assert
        task.IsCompleted.Should().BeTrue();
        await task;
    }

    [Fact]
    public async Task StartAsync_WithJsonArrayRoot_LogsWarning()
    {
        // Arrange - JSON file with array instead of object
        CreateResourceFile("en", "validation.json", """
            [
                "this is an array, not an object"
            ]
            """);
        CreateResourceFile("en", "auth.json", """
            {
                "login": { "invalidCredentials": "Invalid email or password." },
                "user": { "notFound": "User not found." },
                "role": { "notFound": "Role not found." }
            }
            """);
        CreateResourceDirectory("vi");

        var validator = CreateValidator();

        // Act
        await validator.StartAsync(CancellationToken.None);

        // Assert - should log warning for invalid structure
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid JSON structure")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

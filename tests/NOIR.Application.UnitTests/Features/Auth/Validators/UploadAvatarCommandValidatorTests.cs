namespace NOIR.Application.UnitTests.Features.Auth.Validators;

/// <summary>
/// Unit tests for UploadAvatarCommandValidator.
/// Tests validation rules for avatar upload.
/// </summary>
public class UploadAvatarCommandValidatorTests
{
    private readonly UploadAvatarCommandValidator _validator;
    private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2MB

    public UploadAvatarCommandValidatorTests()
    {
        _validator = new UploadAvatarCommandValidator(CreateLocalizationMock());
    }

    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected validation messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        // Generic validations
        mock.Setup(x => x["validation.required"]).Returns("This field is required.");
        mock.Setup(x => x["profile.avatar.maxSize"]).Returns("File size must not exceed 2MB.");
        mock.Setup(x => x["profile.avatar.invalidFormat"]).Returns("Only JPEG, PNG, GIF, and WebP formats are allowed.");

        return mock.Object;
    }

    /// <summary>
    /// Creates a valid UploadAvatarCommand with the given overrides.
    /// </summary>
    private static UploadAvatarCommand CreateValidCommand(
        string? fileName = null,
        Stream? fileStream = null,
        string? contentType = null,
        long? fileSize = null)
    {
        return new UploadAvatarCommand(
            FileName: fileName ?? "avatar.png",
            FileStream: fileStream ?? new MemoryStream(new byte[1024]),
            ContentType: contentType ?? "image/png",
            FileSize: fileSize ?? 1024);
    }

    #region FileName Validation

    [Fact]
    public async Task Validate_WhenFileNameIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(fileName: "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileName)
            .WithErrorMessage("This field is required.");
    }

    [Fact]
    public async Task Validate_WhenFileNameIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new UploadAvatarCommand(
            FileName: null!,
            FileStream: new MemoryStream(new byte[1024]),
            ContentType: "image/png",
            FileSize: 1024);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public async Task Validate_WhenFileNameIsWhitespace_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(fileName: "   ");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Theory]
    [InlineData("avatar.png")]
    [InlineData("my-photo.jpg")]
    [InlineData("profile_pic.jpeg")]
    [InlineData("image.gif")]
    [InlineData("photo.webp")]
    public async Task Validate_WhenFileNameIsValid_ShouldNotHaveError(string fileName)
    {
        // Arrange
        var command = CreateValidCommand(fileName: fileName);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FileName);
    }

    #endregion

    #region FileSize Validation

    [Fact]
    public async Task Validate_WhenFileSizeExceedsMaximum_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(fileSize: MaxFileSizeBytes + 1);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileSize)
            .WithErrorMessage("File size must not exceed 2MB.");
    }

    [Fact]
    public async Task Validate_WhenFileSizeIsExactlyMaximum_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(fileSize: MaxFileSizeBytes);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FileSize);
    }

    [Fact]
    public async Task Validate_WhenFileSizeIsSmall_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(fileSize: 1024); // 1KB

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FileSize);
    }

    [Fact]
    public async Task Validate_WhenFileSizeIsZero_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(fileSize: 0);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FileSize);
    }

    [Fact]
    public async Task Validate_WhenFileSizeIsLargelyOverMaximum_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(fileSize: 10 * 1024 * 1024); // 10MB

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileSize)
            .WithErrorMessage("File size must not exceed 2MB.");
    }

    #endregion

    #region ContentType Validation

    [Fact]
    public async Task Validate_WhenContentTypeIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(contentType: "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ContentType);
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("text/plain")]
    [InlineData("video/mp4")]
    [InlineData("image/svg+xml")]
    [InlineData("image/bmp")]
    [InlineData("application/octet-stream")]
    public async Task Validate_WhenContentTypeIsNotAllowed_ShouldHaveError(string contentType)
    {
        // Arrange
        var command = CreateValidCommand(contentType: contentType);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ContentType)
            .WithErrorMessage("Only JPEG, PNG, GIF, and WebP formats are allowed.");
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/jpg")]
    [InlineData("image/png")]
    [InlineData("image/gif")]
    [InlineData("image/webp")]
    public async Task Validate_WhenContentTypeIsAllowed_ShouldNotHaveError(string contentType)
    {
        // Arrange
        var command = CreateValidCommand(contentType: contentType);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ContentType);
    }

    [Theory]
    [InlineData("IMAGE/JPEG")]
    [InlineData("Image/Png")]
    [InlineData("IMAGE/GIF")]
    [InlineData("Image/WebP")]
    public async Task Validate_WhenContentTypeIsAllowedWithDifferentCase_ShouldNotHaveError(string contentType)
    {
        // Arrange
        var command = CreateValidCommand(contentType: contentType);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ContentType);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Multiple Errors Tests

    [Fact]
    public async Task Validate_WhenMultipleFieldsAreInvalid_ShouldReturnAllErrors()
    {
        // Arrange
        var command = new UploadAvatarCommand(
            FileName: "",
            FileStream: new MemoryStream(),
            ContentType: "application/pdf",
            FileSize: MaxFileSizeBytes + 1);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileName);
        result.ShouldHaveValidationErrorFor(x => x.ContentType);
        result.ShouldHaveValidationErrorFor(x => x.FileSize);
    }

    #endregion
}

using NOIR.Application.Features.Media.Commands.BulkDeleteMediaFiles;

namespace NOIR.Application.UnitTests.Features.Media.Commands;

/// <summary>
/// Unit tests for BulkDeleteMediaFilesCommandValidator.
/// Tests all validation rules for bulk deleting media files.
/// </summary>
public class BulkDeleteMediaFilesCommandValidatorTests
{
    private readonly BulkDeleteMediaFilesCommandValidator _validator = new();

    #region Ids Validation

    [Fact]
    public async Task Validate_WhenIdsIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new BulkDeleteMediaFilesCommand(new List<Guid>());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Ids)
            .WithErrorMessage("At least one media file ID is required.");
    }

    [Fact]
    public async Task Validate_WhenIdsExceedsMaximum_ShouldHaveError()
    {
        // Arrange
        var ids = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();
        var command = new BulkDeleteMediaFilesCommand(ids);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Ids.Count)
            .WithErrorMessage("Maximum 100 media files per operation.");
    }

    [Fact]
    public async Task Validate_WhenIdContainsEmptyGuid_ShouldHaveError()
    {
        // Arrange
        var command = new BulkDeleteMediaFilesCommand(new List<Guid> { Guid.NewGuid(), Guid.Empty });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.ShouldBe(false);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenSingleValidId_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new BulkDeleteMediaFilesCommand(new List<Guid> { Guid.NewGuid() });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenExactly100Ids_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var ids = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
        var command = new BulkDeleteMediaFilesCommand(ids);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMultipleValidIds_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new BulkDeleteMediaFilesCommand(
            new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

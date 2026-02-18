namespace NOIR.Application.UnitTests.Features.Customers.Validators;

/// <summary>
/// Unit tests for UpdateCustomerSegmentCommandValidator.
/// Tests all validation rules for updating a customer segment.
/// </summary>
public class UpdateCustomerSegmentCommandValidatorTests
{
    private readonly UpdateCustomerSegmentCommandValidator _validator = new();

    #region Id Validation

    [Fact]
    public async Task Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateCustomerSegmentCommand(Guid.Empty, CustomerSegment.VIP);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Customer ID is required.");
    }

    [Fact]
    public async Task Validate_WhenIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateCustomerSegmentCommand(Guid.NewGuid(), CustomerSegment.VIP);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    #endregion

    #region Segment Validation

    [Fact]
    public async Task Validate_WhenSegmentIsInvalid_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateCustomerSegmentCommand(Guid.NewGuid(), (CustomerSegment)999);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Segment)
            .WithErrorMessage("Invalid customer segment.");
    }

    [Theory]
    [InlineData(CustomerSegment.New)]
    [InlineData(CustomerSegment.Active)]
    [InlineData(CustomerSegment.VIP)]
    [InlineData(CustomerSegment.AtRisk)]
    [InlineData(CustomerSegment.Dormant)]
    [InlineData(CustomerSegment.Lost)]
    public async Task Validate_WhenSegmentIsValid_ShouldNotHaveError(CustomerSegment segment)
    {
        // Arrange
        var command = new UpdateCustomerSegmentCommand(Guid.NewGuid(), segment);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Segment);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateCustomerSegmentCommand(Guid.NewGuid(), CustomerSegment.VIP);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Multiple Errors

    [Fact]
    public async Task Validate_WhenIdIsEmptyAndSegmentIsInvalid_ShouldHaveMultipleErrors()
    {
        // Arrange
        var command = new UpdateCustomerSegmentCommand(Guid.Empty, (CustomerSegment)999);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
        result.ShouldHaveValidationErrorFor(x => x.Segment);
    }

    #endregion
}

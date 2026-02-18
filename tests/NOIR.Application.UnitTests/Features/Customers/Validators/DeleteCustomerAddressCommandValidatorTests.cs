namespace NOIR.Application.UnitTests.Features.Customers.Validators;

/// <summary>
/// Unit tests for DeleteCustomerAddressCommandValidator.
/// Tests all validation rules for deleting a customer address.
/// </summary>
public class DeleteCustomerAddressCommandValidatorTests
{
    private readonly DeleteCustomerAddressCommandValidator _validator = new();

    #region CustomerId Validation

    [Fact]
    public async Task Validate_WhenCustomerIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new DeleteCustomerAddressCommand(Guid.Empty, Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId)
            .WithErrorMessage("Customer ID is required.");
    }

    #endregion

    #region AddressId Validation

    [Fact]
    public async Task Validate_WhenAddressIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new DeleteCustomerAddressCommand(Guid.NewGuid(), Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AddressId)
            .WithErrorMessage("Address ID is required.");
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new DeleteCustomerAddressCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Multiple Errors

    [Fact]
    public async Task Validate_WhenBothIdsAreEmpty_ShouldHaveMultipleErrors()
    {
        // Arrange
        var command = new DeleteCustomerAddressCommand(Guid.Empty, Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
        result.ShouldHaveValidationErrorFor(x => x.AddressId);
    }

    #endregion
}

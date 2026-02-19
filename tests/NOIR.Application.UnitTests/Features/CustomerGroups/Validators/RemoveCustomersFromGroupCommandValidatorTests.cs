using NOIR.Application.Features.CustomerGroups.Commands.RemoveCustomersFromGroup;

namespace NOIR.Application.UnitTests.Features.CustomerGroups.Validators;

/// <summary>
/// Unit tests for RemoveCustomersFromGroupCommandValidator.
/// Tests all validation rules for removing customers from a group.
/// </summary>
public class RemoveCustomersFromGroupCommandValidatorTests
{
    private readonly RemoveCustomersFromGroupCommandValidator _validator = new();

    #region CustomerGroupId Validation

    [Fact]
    public async Task Validate_WhenCustomerGroupIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new RemoveCustomersFromGroupCommand(
            CustomerGroupId: Guid.Empty,
            CustomerIds: new List<Guid> { Guid.NewGuid() });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerGroupId)
            .WithErrorMessage("Customer group ID is required.");
    }

    [Fact]
    public async Task Validate_WhenCustomerGroupIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new RemoveCustomersFromGroupCommand(
            CustomerGroupId: Guid.NewGuid(),
            CustomerIds: new List<Guid> { Guid.NewGuid() });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CustomerGroupId);
    }

    #endregion

    #region CustomerIds Validation

    [Fact]
    public async Task Validate_WhenCustomerIdsIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new RemoveCustomersFromGroupCommand(
            CustomerGroupId: Guid.NewGuid(),
            CustomerIds: new List<Guid>());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerIds)
            .WithErrorMessage("At least one customer ID is required.");
    }

    [Fact]
    public async Task Validate_WhenCustomerIdsIsNull_ShouldThrowOrFail()
    {
        // Arrange
        // Note: The validator's .Must(ids => ids.Count <= 100) does not guard against null,
        // so passing null throws NullReferenceException. This documents the actual behavior.
        var command = new RemoveCustomersFromGroupCommand(
            CustomerGroupId: Guid.NewGuid(),
            CustomerIds: null!);

        // Act & Assert
        var act = () => _validator.TestValidateAsync(command);
        await act.Should().ThrowAsync<NullReferenceException>();
    }

    [Fact]
    public async Task Validate_WhenCustomerIdsExceeds100_ShouldHaveError()
    {
        // Arrange
        var customerIds = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();
        var command = new RemoveCustomersFromGroupCommand(
            CustomerGroupId: Guid.NewGuid(),
            CustomerIds: customerIds);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerIds)
            .WithErrorMessage("Cannot remove more than 100 customers at once.");
    }

    [Fact]
    public async Task Validate_WhenCustomerIdsHasExactly100_ShouldNotHaveError()
    {
        // Arrange
        var customerIds = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
        var command = new RemoveCustomersFromGroupCommand(
            CustomerGroupId: Guid.NewGuid(),
            CustomerIds: customerIds);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CustomerIds);
    }

    [Fact]
    public async Task Validate_WhenCustomerIdsHasOneEntry_ShouldNotHaveError()
    {
        // Arrange
        var command = new RemoveCustomersFromGroupCommand(
            CustomerGroupId: Guid.NewGuid(),
            CustomerIds: new List<Guid> { Guid.NewGuid() });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CustomerIds);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new RemoveCustomersFromGroupCommand(
            CustomerGroupId: Guid.NewGuid(),
            CustomerIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMinimalValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new RemoveCustomersFromGroupCommand(
            CustomerGroupId: Guid.NewGuid(),
            CustomerIds: new List<Guid> { Guid.NewGuid() });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

using FluentAssertions;
using FluentValidation.TestHelper;
using NOIR.Application.Features.CustomerGroups.Commands.UpdateCustomerGroup;

namespace NOIR.Application.UnitTests.Features.CustomerGroups.Validators;

/// <summary>
/// Unit tests for UpdateCustomerGroupCommandValidator.
/// </summary>
public class UpdateCustomerGroupCommandValidatorTests
{
    private readonly UpdateCustomerGroupCommandValidator _validator;

    public UpdateCustomerGroupCommandValidatorTests()
    {
        _validator = new UpdateCustomerGroupCommandValidator();
    }

    private static UpdateCustomerGroupCommand CreateValidCommand(
        Guid? id = null,
        string name = "Updated Group",
        string? description = "Updated description",
        bool isActive = true) =>
        new(id ?? Guid.NewGuid(), name, description, isActive);

    #region Id Validation

    [Fact]
    public async Task Id_Empty_ShouldHaveError()
    {
        var command = CreateValidCommand(id: Guid.Empty);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Customer group ID is required.");
    }

    [Fact]
    public async Task Id_Valid_ShouldNotHaveError()
    {
        var command = CreateValidCommand();
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    #endregion

    #region Name Validation

    [Fact]
    public async Task Name_Empty_ShouldHaveError()
    {
        var command = CreateValidCommand(name: "");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Group name is required.");
    }

    [Fact]
    public async Task Name_ExceedsMaxLength_ShouldHaveError()
    {
        var command = CreateValidCommand(name: new string('a', 201));
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Group name must not exceed 200 characters.");
    }

    [Fact]
    public async Task Name_ValidLength_ShouldNotHaveError()
    {
        var command = CreateValidCommand(name: "VIP Customers");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region Description Validation

    [Fact]
    public async Task Description_Null_ShouldNotHaveError()
    {
        var command = CreateValidCommand(description: null);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public async Task Description_ExceedsMaxLength_ShouldHaveError()
    {
        var command = CreateValidCommand(description: new string('a', 1001));
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description must not exceed 1000 characters.");
    }

    #endregion

    #region Combined Validation

    [Fact]
    public async Task ValidCommand_ShouldNotHaveAnyErrors()
    {
        var command = CreateValidCommand();
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

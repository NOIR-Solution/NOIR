using FluentAssertions;
using FluentValidation.TestHelper;
using NOIR.Application.Features.CustomerGroups.Commands.CreateCustomerGroup;

namespace NOIR.Application.UnitTests.Features.CustomerGroups.Validators;

/// <summary>
/// Unit tests for CreateCustomerGroupCommandValidator.
/// </summary>
public class CreateCustomerGroupCommandValidatorTests
{
    private readonly CreateCustomerGroupCommandValidator _validator;

    public CreateCustomerGroupCommandValidatorTests()
    {
        _validator = new CreateCustomerGroupCommandValidator();
    }

    private static CreateCustomerGroupCommand CreateValidCommand(
        string name = "VIP Customers",
        string? description = "Top-tier customers") =>
        new(name, description);

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

    [Fact]
    public async Task Name_MaxLength_ShouldNotHaveError()
    {
        var command = CreateValidCommand(name: new string('a', 200));
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

    [Fact]
    public async Task Description_ValidLength_ShouldNotHaveError()
    {
        var command = CreateValidCommand(description: "A valid description");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
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

    [Fact]
    public async Task MinimalValidCommand_ShouldNotHaveAnyErrors()
    {
        var command = CreateValidCommand(name: "A", description: null);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

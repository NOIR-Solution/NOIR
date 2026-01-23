namespace NOIR.Application.UnitTests.Features.Notifications.Validators;

using NOIR.Application.Features.Notifications.Commands.UpdatePreferences;
using NOIR.Application.Features.Notifications.DTOs;
using NOIR.Domain.Enums;

/// <summary>
/// Unit tests for UpdatePreferencesCommandValidator.
/// Tests validation rules for updating notification preferences.
/// </summary>
public class UpdatePreferencesCommandValidatorTests
{
    private readonly UpdatePreferencesCommandValidator _validator;

    public UpdatePreferencesCommandValidatorTests()
    {
        _validator = new UpdatePreferencesCommandValidator();
    }

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdatePreferencesCommand(
            Preferences: new List<UpdatePreferenceRequest>
            {
                new(NotificationCategory.System, true, EmailFrequency.Immediate),
                new(NotificationCategory.Security, false, EmailFrequency.Daily)
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenSinglePreferenceIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdatePreferencesCommand(
            Preferences: new List<UpdatePreferenceRequest>
            {
                new(NotificationCategory.UserAction, true, EmailFrequency.Weekly)
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(NotificationCategory.System, EmailFrequency.None)]
    [InlineData(NotificationCategory.UserAction, EmailFrequency.Immediate)]
    [InlineData(NotificationCategory.Workflow, EmailFrequency.Daily)]
    [InlineData(NotificationCategory.Security, EmailFrequency.Weekly)]
    [InlineData(NotificationCategory.Integration, EmailFrequency.None)]
    public async Task Validate_WhenAllCategoryAndFrequencyCombinationsAreValid_ShouldNotHaveErrors(
        NotificationCategory category, EmailFrequency frequency)
    {
        // Arrange
        var command = new UpdatePreferencesCommand(
            Preferences: new List<UpdatePreferenceRequest>
            {
                new(category, true, frequency)
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Preferences List Validation

    [Fact]
    public async Task Validate_WhenPreferencesIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new UpdatePreferencesCommand(Preferences: null!);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Preferences)
            .WithErrorMessage("Preferences list is required.");
    }

    [Fact]
    public async Task Validate_WhenPreferencesIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new UpdatePreferencesCommand(
            Preferences: new List<UpdatePreferenceRequest>());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Preferences)
            .WithErrorMessage("At least one preference update is required.");
    }

    #endregion

    #region Category Validation

    [Fact]
    public async Task Validate_WhenCategoryIsInvalid_ShouldHaveError()
    {
        // Arrange
        var command = new UpdatePreferencesCommand(
            Preferences: new List<UpdatePreferenceRequest>
            {
                new((NotificationCategory)999, true, EmailFrequency.Immediate)
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Invalid notification category.");
    }

    [Fact]
    public async Task Validate_WhenCategoryIsValidEnum_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdatePreferencesCommand(
            Preferences: new List<UpdatePreferenceRequest>
            {
                new(NotificationCategory.System, true, EmailFrequency.Immediate)
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region EmailFrequency Validation

    [Fact]
    public async Task Validate_WhenEmailFrequencyIsInvalid_ShouldHaveError()
    {
        // Arrange
        var command = new UpdatePreferencesCommand(
            Preferences: new List<UpdatePreferenceRequest>
            {
                new(NotificationCategory.System, true, (EmailFrequency)999)
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Invalid email frequency.");
    }

    [Fact]
    public async Task Validate_WhenEmailFrequencyIsValidEnum_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdatePreferencesCommand(
            Preferences: new List<UpdatePreferenceRequest>
            {
                new(NotificationCategory.System, true, EmailFrequency.Daily)
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Multiple Preferences Validation

    [Fact]
    public async Task Validate_WhenMultiplePreferencesHaveInvalidCategory_ShouldHaveErrors()
    {
        // Arrange
        var command = new UpdatePreferencesCommand(
            Preferences: new List<UpdatePreferenceRequest>
            {
                new((NotificationCategory)100, true, EmailFrequency.Immediate),
                new((NotificationCategory)200, false, EmailFrequency.Daily)
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Invalid notification category.");
    }

    [Fact]
    public async Task Validate_WhenOnePreferenceIsInvalidAndOneIsValid_ShouldHaveError()
    {
        // Arrange
        var command = new UpdatePreferencesCommand(
            Preferences: new List<UpdatePreferenceRequest>
            {
                new(NotificationCategory.System, true, EmailFrequency.Immediate),
                new((NotificationCategory)999, false, EmailFrequency.Daily)
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Invalid notification category.");
    }

    #endregion
}

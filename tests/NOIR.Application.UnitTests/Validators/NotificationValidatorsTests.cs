namespace NOIR.Application.UnitTests.Validators;

using NOIR.Application.Features.Notifications.Commands.UpdatePreferences;
using NOIR.Application.Features.Notifications.DTOs;

/// <summary>
/// Unit tests for notification command validators.
/// Tests all validation rules using FluentValidation.TestHelper.
/// </summary>
public class NotificationValidatorsTests
{
    #region UpdatePreferencesCommandValidator Tests

    public class UpdatePreferencesCommandValidatorTests
    {
        private readonly UpdatePreferencesCommandValidator _validator;

        public UpdatePreferencesCommandValidatorTests()
        {
            _validator = new UpdatePreferencesCommandValidator();
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new UpdatePreferencesCommand(
                Preferences: new[]
                {
                    new UpdatePreferenceRequest(
                        Category: NotificationCategory.System,
                        InAppEnabled: true,
                        EmailFrequency: EmailFrequency.Immediate),
                    new UpdatePreferenceRequest(
                        Category: NotificationCategory.UserAction,
                        InAppEnabled: false,
                        EmailFrequency: EmailFrequency.Daily)
                });

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_SingleValidPreference_ShouldPass()
        {
            // Arrange
            var command = new UpdatePreferencesCommand(
                Preferences: new[]
                {
                    new UpdatePreferenceRequest(
                        Category: NotificationCategory.Security,
                        InAppEnabled: true,
                        EmailFrequency: EmailFrequency.None)
                });

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_NullPreferences_ShouldFail()
        {
            // Arrange
            var command = new UpdatePreferencesCommand(Preferences: null!);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Preferences)
                .WithErrorMessage("Preferences list is required.");
        }

        [Fact]
        public void Validate_EmptyPreferences_ShouldFail()
        {
            // Arrange
            var command = new UpdatePreferencesCommand(
                Preferences: Array.Empty<UpdatePreferenceRequest>());

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Preferences)
                .WithErrorMessage("At least one preference update is required.");
        }

        [Fact]
        public void Validate_InvalidCategory_ShouldFail()
        {
            // Arrange
            var command = new UpdatePreferencesCommand(
                Preferences: new[]
                {
                    new UpdatePreferenceRequest(
                        Category: (NotificationCategory)999, // Invalid enum value
                        InAppEnabled: true,
                        EmailFrequency: EmailFrequency.Immediate)
                });

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("Preferences[0].Category")
                .WithErrorMessage("Invalid notification category.");
        }

        [Fact]
        public void Validate_InvalidEmailFrequency_ShouldFail()
        {
            // Arrange
            var command = new UpdatePreferencesCommand(
                Preferences: new[]
                {
                    new UpdatePreferenceRequest(
                        Category: NotificationCategory.System,
                        InAppEnabled: true,
                        EmailFrequency: (EmailFrequency)999) // Invalid enum value
                });

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("Preferences[0].EmailFrequency")
                .WithErrorMessage("Invalid email frequency.");
        }

        [Fact]
        public void Validate_MultiplePreferencesWithOneInvalid_ShouldFail()
        {
            // Arrange
            var command = new UpdatePreferencesCommand(
                Preferences: new[]
                {
                    new UpdatePreferenceRequest(
                        Category: NotificationCategory.System,
                        InAppEnabled: true,
                        EmailFrequency: EmailFrequency.Immediate),
                    new UpdatePreferenceRequest(
                        Category: (NotificationCategory)999, // Invalid
                        InAppEnabled: false,
                        EmailFrequency: EmailFrequency.Daily)
                });

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("Preferences[1].Category")
                .WithErrorMessage("Invalid notification category.");
        }

        [Theory]
        [InlineData(NotificationCategory.System)]
        [InlineData(NotificationCategory.UserAction)]
        [InlineData(NotificationCategory.Security)]
        public void Validate_AllValidCategories_ShouldPass(NotificationCategory category)
        {
            // Arrange
            var command = new UpdatePreferencesCommand(
                Preferences: new[]
                {
                    new UpdatePreferenceRequest(
                        Category: category,
                        InAppEnabled: true,
                        EmailFrequency: EmailFrequency.Immediate)
                });

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor("Preferences[0].Category");
        }

        [Theory]
        [InlineData(EmailFrequency.None)]
        [InlineData(EmailFrequency.Immediate)]
        [InlineData(EmailFrequency.Daily)]
        [InlineData(EmailFrequency.Weekly)]
        public void Validate_AllValidEmailFrequencies_ShouldPass(EmailFrequency frequency)
        {
            // Arrange
            var command = new UpdatePreferencesCommand(
                Preferences: new[]
                {
                    new UpdatePreferenceRequest(
                        Category: NotificationCategory.System,
                        InAppEnabled: true,
                        EmailFrequency: frequency)
                });

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor("Preferences[0].EmailFrequency");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Validate_InAppEnabledBothValues_ShouldPass(bool inAppEnabled)
        {
            // Arrange
            var command = new UpdatePreferencesCommand(
                Preferences: new[]
                {
                    new UpdatePreferenceRequest(
                        Category: NotificationCategory.System,
                        InAppEnabled: inAppEnabled,
                        EmailFrequency: EmailFrequency.Immediate)
                });

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_AllCategoriesWithDifferentSettings_ShouldPass()
        {
            // Arrange
            var command = new UpdatePreferencesCommand(
                Preferences: new[]
                {
                    new UpdatePreferenceRequest(
                        Category: NotificationCategory.System,
                        InAppEnabled: true,
                        EmailFrequency: EmailFrequency.Immediate),
                    new UpdatePreferenceRequest(
                        Category: NotificationCategory.UserAction,
                        InAppEnabled: false,
                        EmailFrequency: EmailFrequency.Daily),
                    new UpdatePreferenceRequest(
                        Category: NotificationCategory.Security,
                        InAppEnabled: true,
                        EmailFrequency: EmailFrequency.None)
                });

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_BothCategoryAndEmailFrequencyInvalid_ShouldFailWithMultipleErrors()
        {
            // Arrange
            var command = new UpdatePreferencesCommand(
                Preferences: new[]
                {
                    new UpdatePreferenceRequest(
                        Category: (NotificationCategory)999,
                        InAppEnabled: true,
                        EmailFrequency: (EmailFrequency)999)
                });

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("Preferences[0].Category");
            result.ShouldHaveValidationErrorFor("Preferences[0].EmailFrequency");
        }
    }

    #endregion
}

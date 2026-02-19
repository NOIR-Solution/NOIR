using NOIR.Application.Features.FilterAnalytics.Commands.CreateFilterEvent;

namespace NOIR.Application.UnitTests.Features.FilterAnalytics.Validators;

/// <summary>
/// Unit tests for CreateFilterEventCommandValidator.
/// Tests all validation rules for creating a filter analytics event.
/// </summary>
public class CreateFilterEventCommandValidatorTests
{
    private readonly CreateFilterEventCommandValidator _validator = new();

    #region SessionId Validation

    [Fact]
    public async Task Validate_WhenSessionIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: "",
            EventType: FilterEventType.FilterApplied,
            ProductCount: 10);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SessionId)
            .WithErrorMessage("Session ID is required.");
    }

    [Fact]
    public async Task Validate_WhenSessionIdIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: null!,
            EventType: FilterEventType.FilterApplied,
            ProductCount: 10);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SessionId);
    }

    [Fact]
    public async Task Validate_WhenSessionIdExceeds100Characters_ShouldHaveError()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: new string('a', 101),
            EventType: FilterEventType.FilterApplied,
            ProductCount: 10);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SessionId)
            .WithErrorMessage("Session ID must not exceed 100 characters.");
    }

    [Fact]
    public async Task Validate_WhenSessionIdIs100Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: new string('a', 100),
            EventType: FilterEventType.FilterApplied,
            ProductCount: 10);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SessionId);
    }

    [Fact]
    public async Task Validate_WhenSessionIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: "session-abc-123",
            EventType: FilterEventType.FilterApplied,
            ProductCount: 10);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SessionId);
    }

    #endregion

    #region EventType Validation

    [Fact]
    public async Task Validate_WhenEventTypeIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: "session-123",
            EventType: FilterEventType.FilterApplied,
            ProductCount: 10);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.EventType);
    }

    [Fact]
    public async Task Validate_WhenEventTypeIsInvalid_ShouldHaveError()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: "session-123",
            EventType: (FilterEventType)999,
            ProductCount: 10);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EventType)
            .WithErrorMessage("Invalid event type.");
    }

    #endregion

    #region ProductCount Validation

    [Fact]
    public async Task Validate_WhenProductCountIsNegative_ShouldHaveError()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: "session-123",
            EventType: FilterEventType.FilterApplied,
            ProductCount: -1);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductCount)
            .WithErrorMessage("Product count must be non-negative.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public async Task Validate_WhenProductCountIsNonNegative_ShouldNotHaveError(int productCount)
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: "session-123",
            EventType: FilterEventType.FilterApplied,
            ProductCount: productCount);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProductCount);
    }

    #endregion

    #region CategorySlug Validation

    [Fact]
    public async Task Validate_WhenCategorySlugExceeds200Characters_ShouldHaveError()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: "session-123",
            EventType: FilterEventType.FilterApplied,
            ProductCount: 10,
            CategorySlug: new string('a', 201));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CategorySlug)
            .WithErrorMessage("Category slug must not exceed 200 characters.");
    }

    [Fact]
    public async Task Validate_WhenCategorySlugIs200Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: "session-123",
            EventType: FilterEventType.FilterApplied,
            ProductCount: 10,
            CategorySlug: new string('a', 200));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CategorySlug);
    }

    [Fact]
    public async Task Validate_WhenCategorySlugIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: "session-123",
            EventType: FilterEventType.FilterApplied,
            ProductCount: 10,
            CategorySlug: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CategorySlug);
    }

    #endregion

    #region FilterCode Validation

    [Fact]
    public async Task Validate_WhenFilterCodeExceeds100Characters_ShouldHaveError()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: "session-123",
            EventType: FilterEventType.FilterApplied,
            ProductCount: 10,
            FilterCode: new string('a', 101));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FilterCode)
            .WithErrorMessage("Filter code must not exceed 100 characters.");
    }

    [Fact]
    public async Task Validate_WhenFilterCodeIs100Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: "session-123",
            EventType: FilterEventType.FilterApplied,
            ProductCount: 10,
            FilterCode: new string('a', 100));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FilterCode);
    }

    [Fact]
    public async Task Validate_WhenFilterCodeIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: "session-123",
            EventType: FilterEventType.FilterApplied,
            ProductCount: 10,
            FilterCode: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FilterCode);
    }

    #endregion

    #region FilterValue Validation

    [Fact]
    public async Task Validate_WhenFilterValueExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: "session-123",
            EventType: FilterEventType.FilterApplied,
            ProductCount: 10,
            FilterValue: new string('a', 501));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FilterValue)
            .WithErrorMessage("Filter value must not exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenFilterValueIs500Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: "session-123",
            EventType: FilterEventType.FilterApplied,
            ProductCount: 10,
            FilterValue: new string('a', 500));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FilterValue);
    }

    [Fact]
    public async Task Validate_WhenFilterValueIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: "session-123",
            EventType: FilterEventType.FilterApplied,
            ProductCount: 10,
            FilterValue: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FilterValue);
    }

    #endregion

    #region SearchQuery Validation

    [Fact]
    public async Task Validate_WhenSearchQueryExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: "session-123",
            EventType: FilterEventType.FilterApplied,
            ProductCount: 10,
            SearchQuery: new string('a', 501));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SearchQuery)
            .WithErrorMessage("Search query must not exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenSearchQueryIs500Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: "session-123",
            EventType: FilterEventType.FilterApplied,
            ProductCount: 10,
            SearchQuery: new string('a', 500));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SearchQuery);
    }

    [Fact]
    public async Task Validate_WhenSearchQueryIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: "session-123",
            EventType: FilterEventType.FilterApplied,
            ProductCount: 10,
            SearchQuery: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SearchQuery);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: "session-abc-123",
            EventType: FilterEventType.FilterApplied,
            ProductCount: 42,
            CategorySlug: "electronics",
            FilterCode: "brand",
            FilterValue: "Nike",
            SearchQuery: "running shoes",
            ClickedProductId: Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMinimalValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: "session-123",
            EventType: FilterEventType.FilterApplied,
            ProductCount: 0);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

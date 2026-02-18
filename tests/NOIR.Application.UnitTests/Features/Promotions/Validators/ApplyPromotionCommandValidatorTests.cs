namespace NOIR.Application.UnitTests.Features.Promotions.Validators;

/// <summary>
/// Unit tests for ApplyPromotionCommandValidator.
/// Tests all validation rules for applying a promotion.
/// </summary>
public class ApplyPromotionCommandValidatorTests
{
    private readonly ApplyPromotionCommandValidator _validator = new();

    private static ApplyPromotionCommand CreateValidCommand(
        string code = "SUMMER2026",
        Guid? orderId = null,
        decimal orderTotal = 500000m)
    {
        return new ApplyPromotionCommand(
            code,
            orderId ?? Guid.NewGuid(),
            orderTotal);
    }

    #region Code Validation

    [Fact]
    public async Task Validate_WhenCodeIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(code: "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Promotion code is required.");
    }

    [Fact]
    public async Task Validate_WhenCodeExceeds50Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(code: new string('A', 51));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Promotion code cannot exceed 50 characters.");
    }

    [Fact]
    public async Task Validate_WhenCodeIs50Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(code: new string('A', 50));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Code);
    }

    #endregion

    #region OrderId Validation

    [Fact]
    public async Task Validate_WhenOrderIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(orderId: Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrderId)
            .WithErrorMessage("Order ID is required.");
    }

    [Fact]
    public async Task Validate_WhenOrderIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(orderId: Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.OrderId);
    }

    #endregion

    #region OrderTotal Validation

    [Fact]
    public async Task Validate_WhenOrderTotalIsNegative_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(orderTotal: -1m);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrderTotal)
            .WithErrorMessage("Order total must be non-negative.");
    }

    [Fact]
    public async Task Validate_WhenOrderTotalIsZero_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(orderTotal: 0m);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.OrderTotal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100000)]
    [InlineData(999999.99)]
    public async Task Validate_WhenOrderTotalIsNonNegative_ShouldNotHaveError(double orderTotal)
    {
        // Arrange
        var command = CreateValidCommand(orderTotal: (decimal)orderTotal);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.OrderTotal);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new ApplyPromotionCommand(
            "SUMMER2026",
            Guid.NewGuid(),
            500000m);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMinimalValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new ApplyPromotionCommand(
            "A",
            Guid.NewGuid(),
            0m);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

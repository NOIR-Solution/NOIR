using NOIR.Application.Features.Cart.Commands.MergeCart;

namespace NOIR.Application.UnitTests.Features.Cart.Commands.MergeCart;

/// <summary>
/// Unit tests for MergeCartCommandValidator.
/// </summary>
public class MergeCartCommandValidatorTests
{
    private readonly MergeCartCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var command = new MergeCartCommand("session-abc-123", "user-456");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptySessionId_ShouldFail()
    {
        var command = new MergeCartCommand("", "user-456");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SessionId)
            .WithErrorMessage("Session ID is required");
    }

    [Fact]
    public void Validate_WithNullSessionId_ShouldFail()
    {
        var command = new MergeCartCommand(null!, "user-456");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SessionId)
            .WithErrorMessage("Session ID is required");
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldFail()
    {
        var command = new MergeCartCommand("session-abc-123", "");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User ID is required");
    }

    [Fact]
    public void Validate_WithNullUserId_ShouldFail()
    {
        var command = new MergeCartCommand("session-abc-123", null!);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User ID is required");
    }

    [Fact]
    public void Validate_WithBothEmpty_ShouldFail()
    {
        var command = new MergeCartCommand("", "");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SessionId);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}

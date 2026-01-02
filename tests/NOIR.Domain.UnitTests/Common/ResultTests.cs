namespace NOIR.Domain.UnitTests.Common;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_ShouldCreateFailedResult()
    {
        // Arrange
        var error = Error.NotFound("User", "123");

        // Act
        var result = Result.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Success_WithValue_ShouldReturnValue()
    {
        // Arrange
        var value = "test-value";

        // Act
        var result = Result.Success(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void Failure_AccessingValue_ShouldThrow()
    {
        // Arrange
        var result = Result.Failure<string>(Error.NotFound("User", "123"));

        // Act
        var act = () => result.Value;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot access value of a failed result.");
    }

    [Fact]
    public void ImplicitConversion_ShouldCreateSuccessResult()
    {
        // Arrange
        string value = "test";

        // Act
        Result<string> result = value;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }
}

public class ErrorTests
{
    [Fact]
    public void NotFound_ShouldCreateNotFoundError()
    {
        // Act
        var error = Error.NotFound("User", "123");

        // Assert
        error.Code.Should().Be("User.NotFound");
        error.Message.Should().Be("User with id '123' was not found.");
        error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void Unauthorized_ShouldCreateUnauthorizedError()
    {
        // Act
        var error = Error.Unauthorized("Invalid credentials.");

        // Assert
        error.Code.Should().Be("Error.Unauthorized");
        error.Message.Should().Be("Invalid credentials.");
        error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public void Forbidden_ShouldCreateForbiddenError()
    {
        // Act
        var error = Error.Forbidden("Access denied.");

        // Assert
        error.Code.Should().Be("Error.Forbidden");
        error.Message.Should().Be("Access denied.");
        error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public void Validation_ShouldCreateValidationError()
    {
        // Act
        var error = Error.Validation("Email", "Email is required.");

        // Assert
        error.Code.Should().Be("Validation.Email");
        error.Message.Should().Be("Email is required.");
        error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Conflict_ShouldCreateConflictError()
    {
        // Act
        var error = Error.Conflict("Email already exists.");

        // Assert
        error.Code.Should().Be("Error.Conflict");
        error.Message.Should().Be("Email already exists.");
        error.Type.Should().Be(ErrorType.Conflict);
    }
}

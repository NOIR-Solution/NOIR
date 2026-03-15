namespace NOIR.Domain.UnitTests.Common;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Error.ShouldBe(Error.None);
    }

    [Fact]
    public void Failure_ShouldCreateFailedResult()
    {
        // Arrange
        var error = Error.NotFound("User", "123");

        // Act
        var result = Result.Failure(error);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
    }

    [Fact]
    public void Success_WithValue_ShouldReturnValue()
    {
        // Arrange
        var value = "test-value";

        // Act
        var result = Result.Success(value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(value);
    }

    [Fact]
    public void Failure_AccessingValue_ShouldThrow()
    {
        // Arrange
        var result = Result.Failure<string>(Error.NotFound("User", "123"));

        // Act
        var act = () => result.Value;

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Cannot access value of a failed result.");
    }

    [Fact]
    public void ImplicitConversion_ShouldCreateSuccessResult()
    {
        // Arrange
        string value = "test";

        // Act
        Result<string> result = value;

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(value);
    }
}

public class ErrorTests
{
    [Fact]
    public void NotFound_ShouldCreateNotFoundError()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act - Use Guid to ensure (entity, id) overload is selected, not (message, code)
        var error = Error.NotFound("User", id);

        // Assert
        error.Code.ShouldBe(ErrorCodes.Business.NotFound);
        error.Message.ShouldBe($"User with id '{id}' was not found.");
        error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public void Unauthorized_ShouldCreateUnauthorizedError()
    {
        // Act
        var error = Error.Unauthorized("Invalid credentials.");

        // Assert
        error.Code.ShouldBe(ErrorCodes.Auth.Unauthorized);
        error.Message.ShouldBe("Invalid credentials.");
        error.Type.ShouldBe(ErrorType.Unauthorized);
    }

    [Fact]
    public void Forbidden_ShouldCreateForbiddenError()
    {
        // Act
        var error = Error.Forbidden("Access denied.");

        // Assert
        error.Code.ShouldBe(ErrorCodes.Auth.Forbidden);
        error.Message.ShouldBe("Access denied.");
        error.Type.ShouldBe(ErrorType.Forbidden);
    }

    [Fact]
    public void Validation_ShouldCreateValidationError()
    {
        // Act
        var error = Error.Validation("Email", "Email is required.");

        // Assert
        error.Code.ShouldBe(ErrorCodes.Validation.General);
        error.Message.ShouldBe("Email is required.");
        error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public void Conflict_ShouldCreateConflictError()
    {
        // Act
        var error = Error.Conflict("Email already exists.");

        // Assert
        error.Code.ShouldBe(ErrorCodes.Business.Conflict);
        error.Message.ShouldBe("Email already exists.");
        error.Type.ShouldBe(ErrorType.Conflict);
    }
}

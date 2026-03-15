namespace NOIR.Domain.UnitTests.Common;

/// <summary>
/// Extended tests for Result and Error types to improve code coverage.
/// </summary>
public class ResultExtendedTests
{
    [Fact]
    public void ValidationFailure_WithDictionary_ShouldCreateValidationError()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            ["Email"] = ["Email is required", "Email is invalid"],
            ["Password"] = ["Password is too short"]
        };

        // Act
        var result = Result.ValidationFailure(errors);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe(ErrorCodes.Validation.General);
        result.Error.Message.ShouldContain("Email is required");
        result.Error.Message.ShouldContain("Password is too short");
    }

    [Fact]
    public void ValidationFailure_WithEmptyDictionary_ShouldCreateValidationError()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>();

        // Act
        var result = Result.ValidationFailure(errors);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public void Constructor_SuccessWithError_ShouldThrow()
    {
        // Act
        var act = () => Result.Success().GetType()
            .GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                [typeof(bool), typeof(Error)],
                null)!
            .Invoke([true, Error.NotFound("test", "1")]);

        // Assert
        var __ex = Should.Throw<System.Reflection.TargetInvocationException>(act);

        __ex.InnerException.ShouldBeOfType<InvalidOperationException>();

        __ex.InnerException!.Message.ShouldContain("Success result cannot have an error.");
    }

    [Fact]
    public void Constructor_FailureWithoutError_ShouldThrow()
    {
        // Act
        var act = () => Result.Success().GetType()
            .GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                [typeof(bool), typeof(Error)],
                null)!
            .Invoke([false, Error.None]);

        // Assert
        var __ex = Should.Throw<System.Reflection.TargetInvocationException>(act);

        __ex.InnerException.ShouldBeOfType<InvalidOperationException>();

        __ex.InnerException!.Message.ShouldContain("Failure result must have an error.");
    }
}

/// <summary>
/// Extended tests for Error type to improve code coverage.
/// </summary>
public class ErrorExtendedTests
{
    [Fact]
    public void NotFound_WithMessageOnly_ShouldCreateNotFoundError()
    {
        // Act
        var error = Error.NotFound("Custom not found message");

        // Assert
        error.Code.ShouldBe(ErrorCodes.Business.NotFound);
        error.Message.ShouldBe("Custom not found message");
        error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public void Unauthorized_WithDefaultMessage_ShouldUseDefaultMessage()
    {
        // Act
        var error = Error.Unauthorized();

        // Assert
        error.Code.ShouldBe(ErrorCodes.Auth.Unauthorized);
        error.Message.ShouldBe("Unauthorized access.");
        error.Type.ShouldBe(ErrorType.Unauthorized);
    }

    [Fact]
    public void Forbidden_WithDefaultMessage_ShouldUseDefaultMessage()
    {
        // Act
        var error = Error.Forbidden();

        // Assert
        error.Code.ShouldBe(ErrorCodes.Auth.Forbidden);
        error.Message.ShouldBe("Access forbidden.");
        error.Type.ShouldBe(ErrorType.Forbidden);
    }

    [Fact]
    public void ValidationErrors_WithDictionary_ShouldCombineMessages()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            ["Email"] = ["Email is required", "Email is invalid"],
            ["Name"] = ["Name is required"]
        };

        // Act
        var error = Error.ValidationErrors(errors);

        // Assert
        error.Code.ShouldBe(ErrorCodes.Validation.General);
        error.Type.ShouldBe(ErrorType.Validation);
        error.Message.ShouldContain("Email is required");
        error.Message.ShouldContain("Email is invalid");
        error.Message.ShouldContain("Name is required");
    }

    [Fact]
    public void ValidationErrors_WithEnumerable_ShouldCombineMessages()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2", "Error 3" };

        // Act
        var error = Error.ValidationErrors(errors);

        // Assert
        error.Code.ShouldBe(ErrorCodes.Validation.General);
        error.Type.ShouldBe(ErrorType.Validation);
        error.Message.ShouldBe("Error 1; Error 2; Error 3");
    }

    [Fact]
    public void Failure_WithCodeAndMessage_ShouldCreateFailureError()
    {
        // Act
        var error = Error.Failure("Custom.Code", "Something went wrong");

        // Assert
        error.Code.ShouldBe("Custom.Code");
        error.Message.ShouldBe("Something went wrong");
        error.Type.ShouldBe(ErrorType.Failure);
    }

    [Fact]
    public void None_ShouldHaveEmptyCodeAndMessage()
    {
        // Assert
        Error.None.Code.ShouldBeEmpty();
        Error.None.Message.ShouldBeEmpty();
    }

    [Fact]
    public void NullValue_ShouldHaveCorrectCodeAndMessage()
    {
        // Assert
        Error.NullValue.Code.ShouldBe(ErrorCodes.Validation.Required);
        Error.NullValue.Message.ShouldBe("The specified result value is null.");
    }
}

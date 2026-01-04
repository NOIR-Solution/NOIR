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
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be(ErrorCodes.Validation.General);
        result.Error.Message.Should().Contain("Email is required");
        result.Error.Message.Should().Contain("Password is too short");
    }

    [Fact]
    public void ValidationFailure_WithEmptyDictionary_ShouldCreateValidationError()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>();

        // Act
        var result = Result.ValidationFailure(errors);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
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
        act.Should().Throw<System.Reflection.TargetInvocationException>()
            .WithInnerException<InvalidOperationException>()
            .WithMessage("Success result cannot have an error.");
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
        act.Should().Throw<System.Reflection.TargetInvocationException>()
            .WithInnerException<InvalidOperationException>()
            .WithMessage("Failure result must have an error.");
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
        error.Code.Should().Be(ErrorCodes.Business.NotFound);
        error.Message.Should().Be("Custom not found message");
        error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void Unauthorized_WithDefaultMessage_ShouldUseDefaultMessage()
    {
        // Act
        var error = Error.Unauthorized();

        // Assert
        error.Code.Should().Be(ErrorCodes.Auth.Unauthorized);
        error.Message.Should().Be("Unauthorized access.");
        error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public void Forbidden_WithDefaultMessage_ShouldUseDefaultMessage()
    {
        // Act
        var error = Error.Forbidden();

        // Assert
        error.Code.Should().Be(ErrorCodes.Auth.Forbidden);
        error.Message.Should().Be("Access forbidden.");
        error.Type.Should().Be(ErrorType.Forbidden);
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
        error.Code.Should().Be(ErrorCodes.Validation.General);
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Contain("Email is required");
        error.Message.Should().Contain("Email is invalid");
        error.Message.Should().Contain("Name is required");
    }

    [Fact]
    public void ValidationErrors_WithEnumerable_ShouldCombineMessages()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2", "Error 3" };

        // Act
        var error = Error.ValidationErrors(errors);

        // Assert
        error.Code.Should().Be(ErrorCodes.Validation.General);
        error.Type.Should().Be(ErrorType.Validation);
        error.Message.Should().Be("Error 1; Error 2; Error 3");
    }

    [Fact]
    public void Failure_WithCodeAndMessage_ShouldCreateFailureError()
    {
        // Act
        var error = Error.Failure("Custom.Code", "Something went wrong");

        // Assert
        error.Code.Should().Be("Custom.Code");
        error.Message.Should().Be("Something went wrong");
        error.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void None_ShouldHaveEmptyCodeAndMessage()
    {
        // Assert
        Error.None.Code.Should().BeEmpty();
        Error.None.Message.Should().BeEmpty();
    }

    [Fact]
    public void NullValue_ShouldHaveCorrectCodeAndMessage()
    {
        // Assert
        Error.NullValue.Code.Should().Be(ErrorCodes.Validation.Required);
        Error.NullValue.Message.Should().Be("The specified result value is null.");
    }
}

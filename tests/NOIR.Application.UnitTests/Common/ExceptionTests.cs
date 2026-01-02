namespace NOIR.Application.UnitTests.Common;

using ValidationException = NOIR.Application.Common.Exceptions.ValidationException;

/// <summary>
/// Unit tests for application-specific exceptions.
/// </summary>
public class ExceptionTests
{
    #region NotFoundException Tests

    [Fact]
    public void NotFoundException_Default_ShouldHaveEmptyMessage()
    {
        // Act
        var exception = new NotFoundException();

        // Assert
        exception.Message.Should().NotBeNull();
    }

    [Fact]
    public void NotFoundException_WithMessage_ShouldSetMessage()
    {
        // Arrange
        const string message = "Resource not found";

        // Act
        var exception = new NotFoundException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void NotFoundException_WithEntityNameAndKey_ShouldFormatMessage()
    {
        // Act
        var exception = new NotFoundException("User", "123");

        // Assert
        exception.Message.Should().Contain("User");
        exception.Message.Should().Contain("123");
    }

    [Fact]
    public void NotFoundException_WithInnerException_ShouldSetInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new NotFoundException("Outer error", innerException);

        // Assert
        exception.Message.Should().Be("Outer error");
        exception.InnerException.Should().Be(innerException);
    }

    #endregion

    #region ForbiddenAccessException Tests

    [Fact]
    public void ForbiddenAccessException_Default_ShouldHaveDefaultMessage()
    {
        // Act
        var exception = new ForbiddenAccessException();

        // Assert
        exception.Message.Should().NotBeNull();
    }

    [Fact]
    public void ForbiddenAccessException_WithMessage_ShouldSetMessage()
    {
        // Arrange
        const string message = "Access denied to resource";

        // Act
        var exception = new ForbiddenAccessException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    #endregion

    #region ValidationException Tests

    [Fact]
    public void ValidationException_Default_ShouldHaveEmptyErrors()
    {
        // Act
        var exception = new ValidationException();

        // Assert
        exception.Errors.Should().NotBeNull();
        exception.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidationException_WithValidationFailures_ShouldSetErrors()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new ValidationFailure("Email", "Email is required"),
            new ValidationFailure("Password", "Password must be at least 6 characters"),
            new ValidationFailure("Password", "Password must contain a number")
        };

        // Act
        var exception = new ValidationException(failures);

        // Assert
        exception.Errors.Should().ContainKey("Email");
        exception.Errors.Should().ContainKey("Password");
        exception.Errors["Email"].Should().Contain("Email is required");
        exception.Errors["Password"].Should().HaveCount(2);
    }

    [Fact]
    public void ValidationException_WithPropertyNameAndErrorMessage_ShouldSetSingleError()
    {
        // Act
        var exception = new ValidationException("Email", "Email is invalid");

        // Assert
        exception.Errors.Should().ContainKey("Email");
        exception.Errors["Email"].Should().Contain("Email is invalid");
    }

    #endregion
}

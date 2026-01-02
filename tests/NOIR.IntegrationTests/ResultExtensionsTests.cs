namespace NOIR.IntegrationTests;

/// <summary>
/// Tests for ResultExtensions that map domain Results to HTTP responses.
/// </summary>
public class ResultExtensionsTests
{
    #region Result.ToHttpResult Tests

    [Fact]
    public void ToHttpResult_WithSuccessResult_ShouldReturnOk()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().BeOfType<Ok>();
    }

    [Fact]
    public void ToHttpResult_WithValidationError_ShouldReturn400()
    {
        // Arrange
        var result = Result.Failure(Error.Validation("field", "Validation error"));

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().BeAssignableTo<ProblemHttpResult>();
    }

    [Fact]
    public void ToHttpResult_WithNotFoundError_ShouldReturn404()
    {
        // Arrange
        var result = Result.Failure(Error.NotFound("entity", "Not found error"));

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().BeAssignableTo<ProblemHttpResult>();
    }

    [Fact]
    public void ToHttpResult_WithUnauthorizedError_ShouldReturn401()
    {
        // Arrange
        var result = Result.Failure(Error.Unauthorized("Unauthorized error"));

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().BeAssignableTo<ProblemHttpResult>();
    }

    [Fact]
    public void ToHttpResult_WithForbiddenError_ShouldReturn403()
    {
        // Arrange
        var result = Result.Failure(Error.Forbidden("Forbidden error"));

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().BeAssignableTo<ProblemHttpResult>();
    }

    [Fact]
    public void ToHttpResult_WithConflictError_ShouldReturn409()
    {
        // Arrange
        var result = Result.Failure(Error.Conflict("Conflict error"));

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().BeAssignableTo<ProblemHttpResult>();
    }

    #endregion

    #region Result<T>.ToHttpResult Tests

    [Fact]
    public void ToHttpResultT_WithSuccessResult_ShouldReturnOkWithValue()
    {
        // Arrange
        var result = Result.Success("test-value");

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().BeOfType<Ok<string>>();
        var okResult = (Ok<string>)httpResult;
        okResult.Value.Should().Be("test-value");
    }

    [Fact]
    public void ToHttpResultT_WithFailureResult_ShouldReturnProblem()
    {
        // Arrange
        var result = Result.Failure<string>(Error.Validation("field", "error"));

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().BeAssignableTo<ProblemHttpResult>();
    }

    [Fact]
    public void ToHttpResultT_WithCustomSuccess_ShouldInvokeCustomHandler()
    {
        // Arrange
        var result = Result.Success("created-id");

        // Act
        var httpResult = result.ToHttpResult(value => Results.Created($"/api/items/{value}", value));

        // Assert
        httpResult.Should().BeOfType<Created<string>>();
    }

    [Fact]
    public void ToHttpResultT_WithCustomSuccess_FailedResult_ShouldReturnProblem()
    {
        // Arrange
        var result = Result.Failure<string>(Error.NotFound("item", "Item not found"));

        // Act
        var httpResult = result.ToHttpResult(value => Results.Created($"/api/items/{value}", value));

        // Assert
        httpResult.Should().BeAssignableTo<ProblemHttpResult>();
    }

    #endregion

    #region Result<T>.Match Tests

    [Fact]
    public void Match_WithSuccessResult_ShouldInvokeSuccessHandler()
    {
        // Arrange
        var result = Result.Success(42);

        // Act
        var httpResult = result.Match(
            value => Results.Ok(value * 2),
            error => Results.Problem(error.Message));

        // Assert
        httpResult.Should().BeOfType<Ok<int>>();
        var okResult = (Ok<int>)httpResult;
        okResult.Value.Should().Be(84);
    }

    [Fact]
    public void Match_WithFailureResult_ShouldInvokeFailureHandler()
    {
        // Arrange
        var error = Error.NotFound("item", "Item not found");
        var result = Result.Failure<int>(error);

        // Act
        var httpResult = result.Match(
            value => Results.Ok(value),
            err => Results.Problem(err.Message));

        // Assert
        httpResult.Should().BeAssignableTo<ProblemHttpResult>();
    }

    #endregion

    #region Complex Object Tests

    [Fact]
    public void ToHttpResult_WithComplexObject_ShouldReturnOkWithObject()
    {
        // Arrange
        var authResponse = new AuthResponse(
            "user-id",
            "test@example.com",
            "access-token",
            "refresh-token",
            DateTimeOffset.UtcNow.AddHours(1));
        var result = Result.Success(authResponse);

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().BeOfType<Ok<AuthResponse>>();
        var okResult = (Ok<AuthResponse>)httpResult;
        okResult.Value.Should().Be(authResponse);
    }

    #endregion
}

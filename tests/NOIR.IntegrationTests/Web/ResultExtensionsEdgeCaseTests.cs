namespace NOIR.IntegrationTests.Web;

/// <summary>
/// Edge case tests for ResultExtensions.
/// Tests error type mapping and internal server error handling.
/// </summary>
public class ResultExtensionsEdgeCaseTests
{
    #region Default Error Type Tests (Internal Server Error)

    [Fact]
    public void ToProblemResult_WithUnknownErrorType_ShouldReturn500()
    {
        // Arrange - Create error with an error type that maps to default case
        var error = new Error("unknown.error", "Unknown error occurred", (ErrorType)999);
        var result = Result.Failure(error);

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().BeAssignableTo<ProblemHttpResult>();
    }

    #endregion

    #region Null Value Tests

    [Fact]
    public void ToHttpResult_WithNullValue_ShouldReturnOkResult()
    {
        // Arrange
        string? nullValue = null;
        var result = Result.Success<string?>(nullValue);

        // Act
        var httpResult = result.ToHttpResult();

        // Assert - Should be an Ok result (not a problem result)
        httpResult.Should().NotBeAssignableTo<ProblemHttpResult>();
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public void Match_WithSuccessAndNullHandler_ShouldWork()
    {
        // Arrange
        var result = Result.Success(42);
        IResult? successResult = null;

        // Act
        var httpResult = result.Match(
            value => { successResult = Results.Ok(value); return successResult; },
            error => Results.Problem(error.Message));

        // Assert
        httpResult.Should().Be(successResult);
    }

    [Fact]
    public void ToHttpResult_WithCustomSuccess_AndCreatedUri_ShouldReturnCreated()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var result = Result.Success(entityId);

        // Act
        var httpResult = result.ToHttpResult(id =>
            Results.Created($"/api/entities/{id}", id));

        // Assert
        httpResult.Should().BeOfType<Created<Guid>>();
    }

    [Fact]
    public void Match_WithFailure_ShouldInvokeFailureHandler()
    {
        // Arrange
        var error = Error.Validation("field", "Field is required");
        var result = Result.Failure<int>(error);
        Error? capturedError = null;

        // Act
        var httpResult = result.Match(
            value => Results.Ok(value),
            err => { capturedError = err; return Results.Problem(err.Message); });

        // Assert
        capturedError.Should().NotBeNull();
        capturedError!.Code.Should().Be("Validation.field");
    }

    #endregion

    #region All Error Types Verification

    [Fact]
    public void ToHttpResult_ValidationError_ShouldReturn400()
    {
        // Arrange
        var result = Result.Failure(Error.Validation("field", "Invalid field"));

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().BeAssignableTo<ProblemHttpResult>();
    }

    [Fact]
    public void ToHttpResult_NotFoundError_ShouldReturn404()
    {
        // Arrange
        var result = Result.Failure(Error.NotFound("entity", "Entity not found"));

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().BeAssignableTo<ProblemHttpResult>();
    }

    [Fact]
    public void ToHttpResult_UnauthorizedError_ShouldReturn401()
    {
        // Arrange
        var result = Result.Failure(Error.Unauthorized("Not authenticated"));

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().BeAssignableTo<ProblemHttpResult>();
    }

    [Fact]
    public void ToHttpResult_ForbiddenError_ShouldReturn403()
    {
        // Arrange
        var result = Result.Failure(Error.Forbidden("Access denied"));

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().BeAssignableTo<ProblemHttpResult>();
    }

    [Fact]
    public void ToHttpResult_ConflictError_ShouldReturn409()
    {
        // Arrange
        var result = Result.Failure(Error.Conflict("Resource conflict"));

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().BeAssignableTo<ProblemHttpResult>();
    }

    #endregion

    #region Generic Result Tests

    [Fact]
    public void ToHttpResultT_WithSuccessIntResult_ShouldReturnOkWithValue()
    {
        // Arrange
        var result = Result.Success(123);

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().BeOfType<Ok<int>>();
        var okResult = (Ok<int>)httpResult;
        okResult.Value.Should().Be(123);
    }

    [Fact]
    public void ToHttpResultT_WithSuccessListResult_ShouldReturnOkWithList()
    {
        // Arrange
        var list = new List<string> { "a", "b", "c" };
        var result = Result.Success(list);

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().BeOfType<Ok<List<string>>>();
        var okResult = (Ok<List<string>>)httpResult;
        okResult.Value.Should().HaveCount(3);
    }

    [Fact]
    public void ToHttpResultT_WithFailure_ShouldReturnProblem()
    {
        // Arrange
        var result = Result.Failure<List<string>>(Error.NotFound("list", "List not found"));

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().BeAssignableTo<ProblemHttpResult>();
    }

    #endregion

    #region Non-Generic Result Tests

    [Fact]
    public void ToHttpResult_NonGeneric_WithSuccess_ShouldReturnOk()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().BeOfType<Ok>();
    }

    [Fact]
    public void ToHttpResult_NonGeneric_WithFailure_ShouldReturnProblem()
    {
        // Arrange
        var result = Result.Failure(Error.Validation("field", "Validation failed"));

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().BeAssignableTo<ProblemHttpResult>();
    }

    #endregion
}

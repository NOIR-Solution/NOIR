namespace NOIR.Application.UnitTests.Web;

/// <summary>
/// Unit tests for ExceptionHandlingMiddleware.
/// Tests exception handling and HTTP response generation.
/// </summary>
public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock;
    private readonly Mock<IHostEnvironment> _environmentMock;

    public ExceptionHandlingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        _environmentMock = new Mock<IHostEnvironment>();
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Production");
    }

    private ExceptionHandlingMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.TraceIdentifier = "test-trace-id";
        return context;
    }

    private static async Task<string> ReadResponseBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    #region Success Path Tests

    [Fact]
    public async Task InvokeAsync_WhenNoException_ShouldCallNext()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
    }

    #endregion

    #region FluentValidation Exception Tests

    [Fact]
    public async Task InvokeAsync_WithFluentValidationException_ShouldReturn400()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Email", "Email is required"),
            new("Password", "Password is required")
        };
        var exception = new FluentValidation.ValidationException(failures);

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task InvokeAsync_WithFluentValidationException_ShouldReturnProblemJson()
    {
        // Arrange
        var failures = new List<ValidationFailure> { new("Field", "Error") };
        var exception = new FluentValidation.ValidationException(failures);

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.ContentType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task InvokeAsync_WithFluentValidationException_ShouldIncludeTraceId()
    {
        // Arrange
        var failures = new List<ValidationFailure> { new("Field", "Error") };
        var exception = new FluentValidation.ValidationException(failures);

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);
        var body = await ReadResponseBody(context);

        // Assert
        body.Should().Contain("test-trace-id");
    }

    #endregion

    #region NotFoundException Tests

    [Fact]
    public async Task InvokeAsync_WithNotFoundException_ShouldReturn404()
    {
        // Arrange
        var exception = new NotFoundException("User", "123");

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task InvokeAsync_WithNotFoundException_ShouldIncludeNotFoundTitle()
    {
        // Arrange
        var exception = new NotFoundException("User", "123");

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);
        var body = await ReadResponseBody(context);

        // Assert
        body.Should().Contain("Not Found");
    }

    #endregion

    #region ForbiddenAccessException Tests

    [Fact]
    public async Task InvokeAsync_WithForbiddenAccessException_ShouldReturn403()
    {
        // Arrange
        var exception = new ForbiddenAccessException("Access denied");

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task InvokeAsync_WithForbiddenAccessException_ShouldIncludeForbiddenTitle()
    {
        // Arrange
        var exception = new ForbiddenAccessException("Access denied");

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);
        var body = await ReadResponseBody(context);

        // Assert
        body.Should().Contain("Forbidden");
    }

    #endregion

    #region UnauthorizedAccessException Tests

    [Fact]
    public async Task InvokeAsync_WithUnauthorizedAccessException_ShouldReturn401()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("Unauthorized");

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthorizedAccessException_ShouldIncludeUnauthorizedTitle()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("Unauthorized");

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);
        var body = await ReadResponseBody(context);

        // Assert
        body.Should().Contain("Unauthorized");
    }

    #endregion

    #region OperationCanceledException Tests

    [Fact]
    public async Task InvokeAsync_WithOperationCanceledException_ShouldReturn499()
    {
        // Arrange
        var exception = new OperationCanceledException();

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(499); // Client Closed Request
    }

    [Fact]
    public async Task InvokeAsync_WithOperationCanceledException_ShouldIncludeClientClosedTitle()
    {
        // Arrange
        var exception = new OperationCanceledException();

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);
        var body = await ReadResponseBody(context);

        // Assert
        body.Should().Contain("Client Closed Request");
    }

    #endregion

    #region Unknown Exception Tests

    [Fact]
    public async Task InvokeAsync_WithUnknownException_ShouldReturn500()
    {
        // Arrange
        var exception = new InvalidOperationException("Something went wrong");

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task InvokeAsync_WithUnknownException_ShouldIncludeInternalServerErrorTitle()
    {
        // Arrange
        var exception = new InvalidOperationException("Something went wrong");

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);
        var body = await ReadResponseBody(context);

        // Assert
        body.Should().Contain("Internal Server Error");
    }

    [Fact]
    public async Task InvokeAsync_WithUnknownException_ShouldLogError()
    {
        // Arrange
        var exception = new InvalidOperationException("Something went wrong");

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithUnknownExceptionInDevelopment_ShouldIncludeStackTrace()
    {
        // Arrange
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Development");
        var exception = new InvalidOperationException("Something went wrong");

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);
        var body = await ReadResponseBody(context);

        // Assert
        body.Should().Contain("stackTrace");
    }

    [Fact]
    public async Task InvokeAsync_WithUnknownExceptionInProduction_ShouldNotIncludeStackTrace()
    {
        // Arrange
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Production");
        var exception = new InvalidOperationException("Something went wrong");

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);
        var body = await ReadResponseBody(context);

        // Assert
        body.Should().NotContain("stackTrace");
    }

    #endregion

    #region Application ValidationException Tests

    [Fact]
    public async Task InvokeAsync_WithApplicationValidationException_ShouldReturn400()
    {
        // Arrange - Application.Common.Exceptions.ValidationException uses property name + message constructor
        var exception = new Application.Common.Exceptions.ValidationException("Email", "Email is required");

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task InvokeAsync_WithApplicationValidationException_ShouldIncludeValidationTitle()
    {
        // Arrange
        var exception = new Application.Common.Exceptions.ValidationException("Field", "Error message");

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);
        var body = await ReadResponseBody(context);

        // Assert
        body.Should().Contain("Validation Error");
    }

    [Fact]
    public async Task InvokeAsync_WithApplicationValidationException_ShouldLogInformation()
    {
        // Arrange
        var exception = new Application.Common.Exceptions.ValidationException("Field", "Error");

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert - Validation failures are user errors (400), log at Information level
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Logging Level Tests

    [Fact]
    public async Task InvokeAsync_WithNotFoundException_ShouldLogInformation()
    {
        // Arrange
        var exception = new NotFoundException("User", "123");

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert - 404 should log at Information level
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithForbiddenException_ShouldLogInformation()
    {
        // Arrange
        var exception = new ForbiddenAccessException("Access denied");

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert - 403 should log at Information level
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthorizedException_ShouldLogInformation()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("Not authorized");

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert - 401 should log at Information level
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithOperationCanceledException_ShouldLogInformation()
    {
        // Arrange
        var exception = new OperationCanceledException();

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert - 499 should log at Information level
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Development Environment Tests

    [Fact]
    public async Task InvokeAsync_WithUnknownExceptionInDevelopment_ShouldIncludeExceptionType()
    {
        // Arrange
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Development");
        var exception = new InvalidOperationException("Something went wrong");

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);
        var body = await ReadResponseBody(context);

        // Assert
        body.Should().Contain("exceptionType");
        body.Should().Contain("InvalidOperationException");
    }

    [Fact]
    public async Task InvokeAsync_WithClientErrorInDevelopment_ShouldNotIncludeStackTrace()
    {
        // Arrange - Stack trace only added for 500 errors
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Development");
        var exception = new NotFoundException("User", "123");

        RequestDelegate next = _ => throw exception;
        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);
        var body = await ReadResponseBody(context);

        // Assert - 404 should not include stack trace even in development
        body.Should().NotContain("stackTrace");
    }

    #endregion
}

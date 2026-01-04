namespace NOIR.Web.Middleware;

/// <summary>
/// Middleware that handles exceptions and returns appropriate HTTP responses.
/// Follows RFC 7807 Problem Details specification with NOIR error codes.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, problemDetails, errorCode) = exception switch
        {
            Application.Common.Exceptions.ValidationException validationException =>
                HandleValidationException(validationException),
            FluentValidation.ValidationException fluentValidationException =>
                HandleFluentValidationException(fluentValidationException),
            NotFoundException notFoundException =>
                HandleNotFoundException(notFoundException),
            ForbiddenAccessException forbiddenException =>
                HandleForbiddenException(forbiddenException),
            UnauthorizedAccessException =>
                HandleUnauthorizedException(),
            OperationCanceledException =>
                HandleCancelledException(),
            _ =>
                HandleUnknownException(exception)
        };

        // Log based on severity
        LogException(exception, statusCode, errorCode, context.TraceIdentifier);

        // Add standard error tracking extensions
        problemDetails.Extensions["errorCode"] = errorCode;
        problemDetails.Extensions["correlationId"] = context.TraceIdentifier;
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("O");

        // In development, include exception details
        if (_environment.IsDevelopment() && statusCode == StatusCodes.Status500InternalServerError)
        {
            problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
    }

    private void LogException(Exception exception, int statusCode, string errorCode, string correlationId)
    {
        // Don't log client errors (4xx) at Error level - they're expected
        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "Unhandled exception [{ErrorCode}] CorrelationId={CorrelationId}: {Message}",
                errorCode,
                correlationId,
                exception.Message);
        }
        else if (statusCode == StatusCodes.Status400BadRequest)
        {
            // Validation errors are expected in normal operation - log at Information, not Warning
            _logger.LogInformation(
                "Validation failed [{ErrorCode}] CorrelationId={CorrelationId}: {Message}",
                errorCode,
                correlationId,
                exception.Message);
        }
        else
        {
            _logger.LogInformation(
                "Client error {StatusCode} [{ErrorCode}] CorrelationId={CorrelationId}: {Message}",
                statusCode,
                errorCode,
                correlationId,
                exception.Message);
        }
    }

    private static (int, ProblemDetails, string) HandleValidationException(
        Application.Common.Exceptions.ValidationException exception)
    {
        var errorCode = ErrorCodes.Validation.General;
        return (StatusCodes.Status400BadRequest, new ValidationProblemDetails(exception.Errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Error",
            Detail = "One or more validation errors occurred.",
            Type = $"https://api.noir.local/errors/{errorCode}"
        }, errorCode);
    }

    private static (int, ProblemDetails, string) HandleFluentValidationException(
        FluentValidation.ValidationException exception)
    {
        var errorCode = ErrorCodes.Validation.General;

        // Convert FluentValidation errors to dictionary format
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return (StatusCodes.Status400BadRequest, new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Error",
            Detail = "One or more validation errors occurred.",
            Type = $"https://api.noir.local/errors/{errorCode}"
        }, errorCode);
    }

    private static (int, ProblemDetails, string) HandleNotFoundException(NotFoundException exception)
    {
        var errorCode = ErrorCodes.Business.NotFound;
        return (StatusCodes.Status404NotFound, new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Not Found",
            Detail = exception.Message,
            Type = $"https://api.noir.local/errors/{errorCode}"
        }, errorCode);
    }

    private static (int, ProblemDetails, string) HandleForbiddenException(ForbiddenAccessException exception)
    {
        var errorCode = ErrorCodes.Auth.Forbidden;
        return (StatusCodes.Status403Forbidden, new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Detail = exception.Message,
            Type = $"https://api.noir.local/errors/{errorCode}"
        }, errorCode);
    }

    private static (int, ProblemDetails, string) HandleUnauthorizedException()
    {
        var errorCode = ErrorCodes.Auth.Unauthorized;
        return (StatusCodes.Status401Unauthorized, new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Detail = "You are not authorized to access this resource.",
            Type = $"https://api.noir.local/errors/{errorCode}"
        }, errorCode);
    }

    private static (int, ProblemDetails, string) HandleCancelledException()
    {
        var errorCode = ErrorCodes.System.InternalError;
        return (499, new ProblemDetails
        {
            Status = 499, // Client Closed Request
            Title = "Client Closed Request",
            Detail = "The request was cancelled by the client.",
            Type = $"https://api.noir.local/errors/{errorCode}"
        }, errorCode);
    }

    private static (int, ProblemDetails, string) HandleUnknownException(Exception exception)
    {
        var errorCode = ErrorCodes.System.UnknownError;
        return (StatusCodes.Status500InternalServerError, new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = "An unexpected error occurred. Please try again later.",
            Type = $"https://api.noir.local/errors/{errorCode}"
        }, errorCode);
    }
}

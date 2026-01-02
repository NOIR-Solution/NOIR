namespace NOIR.Web.Middleware;

/// <summary>
/// Middleware that handles exceptions and returns appropriate HTTP responses.
/// Follows RFC 7807 Problem Details specification.
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
        var (statusCode, problemDetails) = exception switch
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
        LogException(exception, statusCode);

        // Add trace ID for correlation
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

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

    private void LogException(Exception exception, int statusCode)
    {
        // Don't log client errors (4xx) at Error level - they're expected
        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "Unhandled exception of type {ExceptionType}: {Message}",
                exception.GetType().Name,
                exception.Message);
        }
        else if (statusCode == StatusCodes.Status400BadRequest)
        {
            // Validation errors are expected in normal operation - log at Information, not Warning
            _logger.LogInformation(
                "Validation failed: {Message}",
                exception.Message);
        }
        else
        {
            _logger.LogInformation(
                "Client error {StatusCode} - {ExceptionType}: {Message}",
                statusCode,
                exception.GetType().Name,
                exception.Message);
        }
    }

    private static (int, ProblemDetails) HandleValidationException(
        Application.Common.Exceptions.ValidationException exception) =>
        (StatusCodes.Status400BadRequest, new ValidationProblemDetails(exception.Errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Error",
            Detail = "One or more validation errors occurred.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        });

    private static (int, ProblemDetails) HandleFluentValidationException(
        FluentValidation.ValidationException exception)
    {
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
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        });
    }

    private static (int, ProblemDetails) HandleNotFoundException(NotFoundException exception) =>
        (StatusCodes.Status404NotFound, new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Not Found",
            Detail = exception.Message,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
        });

    private static (int, ProblemDetails) HandleForbiddenException(ForbiddenAccessException exception) =>
        (StatusCodes.Status403Forbidden, new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Detail = exception.Message,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
        });

    private static (int, ProblemDetails) HandleUnauthorizedException() =>
        (StatusCodes.Status401Unauthorized, new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Detail = "You are not authorized to access this resource.",
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
        });

    private static (int, ProblemDetails) HandleCancelledException() =>
        (499, new ProblemDetails
        {
            Status = 499, // Client Closed Request
            Title = "Client Closed Request",
            Detail = "The request was cancelled by the client.",
            Type = "https://httpstatuses.com/499"
        });

    private static (int, ProblemDetails) HandleUnknownException(Exception exception) =>
        (StatusCodes.Status500InternalServerError, new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = "An unexpected error occurred. Please try again later.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        });
}

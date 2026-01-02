# Implementation Plan: Top 3 Enhancement Opportunities

**Date:** 2025-12-31
**Status:** ✅ COMPLETED
**Estimated Effort:** 2-3 days
**Completion:** All 3 enhancements implemented with 1,739 tests

---

## Overview

This plan addresses the top 3 gaps identified in the best practices research:

1. **Wolverine Pipeline Policies** - Cross-cutting concerns (validation, logging, performance)
2. **Result Pattern Integration** - Replace response wrappers with Result<T>
3. **Unit Test Structure** - Domain and Application layer tests

---

## Enhancement 1: Wolverine Pipeline Policies

### Current State
- Handlers manually call validators
- No centralized logging for handler execution
- No performance tracking

### Target State
- Automatic FluentValidation middleware
- Structured logging middleware
- Performance tracking middleware

### Implementation Steps

#### Step 1.1: Add WolverineFx.FluentValidation Package

```bash
dotnet add src/NOIR.Application/NOIR.Application.csproj package WolverineFx.FluentValidation
```

#### Step 1.2: Create Logging Middleware

**File:** `src/NOIR.Application/Behaviors/LoggingMiddleware.cs`

```csharp
namespace NOIR.Application.Behaviors;

/// <summary>
/// Wolverine middleware for structured logging of all handler executions.
/// Logs before/after handler execution with timing information.
/// </summary>
public class LoggingMiddleware
{
    private readonly Stopwatch _stopwatch = new();

    public void Before(object message, ILogger<LoggingMiddleware> logger, Envelope envelope)
    {
        _stopwatch.Restart();
        logger.LogInformation(
            "Handling {MessageType} | CorrelationId: {CorrelationId}",
            message.GetType().Name,
            envelope.CorrelationId);
    }

    public void After(object message, ILogger<LoggingMiddleware> logger, Envelope envelope)
    {
        logger.LogInformation(
            "Handled {MessageType} successfully in {ElapsedMs}ms | CorrelationId: {CorrelationId}",
            message.GetType().Name,
            _stopwatch.ElapsedMilliseconds,
            envelope.CorrelationId);
    }

    public void Finally(Exception? exception, ILogger<LoggingMiddleware> logger, Envelope envelope)
    {
        _stopwatch.Stop();
        if (exception is not null)
        {
            logger.LogError(
                exception,
                "Handler failed for {CorrelationId} after {ElapsedMs}ms",
                envelope.CorrelationId,
                _stopwatch.ElapsedMilliseconds);
        }
    }
}
```

#### Step 1.3: Create Performance Warning Middleware

**File:** `src/NOIR.Application/Behaviors/PerformanceMiddleware.cs`

```csharp
namespace NOIR.Application.Behaviors;

/// <summary>
/// Middleware that logs warnings for slow handler executions.
/// Threshold is configurable via appsettings.json.
/// </summary>
public class PerformanceMiddleware
{
    private readonly Stopwatch _stopwatch = new();
    private const int DefaultThresholdMs = 500;

    public void Before()
    {
        _stopwatch.Restart();
    }

    public void Finally(
        object message,
        ILogger<PerformanceMiddleware> logger,
        IConfiguration configuration)
    {
        _stopwatch.Stop();
        var threshold = configuration.GetValue("Performance:SlowHandlerThresholdMs", DefaultThresholdMs);

        if (_stopwatch.ElapsedMilliseconds > threshold)
        {
            logger.LogWarning(
                "SLOW HANDLER: {MessageType} took {ElapsedMs}ms (threshold: {Threshold}ms)",
                message.GetType().Name,
                _stopwatch.ElapsedMilliseconds,
                threshold);
        }
    }
}
```

#### Step 1.4: Update Program.cs Wolverine Configuration

**File:** `src/NOIR.Web/Program.cs` (modify existing)

```csharp
// Configure Wolverine for CQRS
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(NOIR.Application.DependencyInjection).Assembly);
    opts.Discovery.IncludeAssembly(typeof(NOIR.Infrastructure.DependencyInjection).Assembly);

    // Enable FluentValidation middleware (auto-validates all commands/queries)
    opts.UseFluentValidation();

    // Add logging middleware globally
    opts.Policies.AddMiddleware<LoggingMiddleware>();

    // Add performance tracking middleware
    opts.Policies.AddMiddleware<PerformanceMiddleware>();

    opts.CodeGeneration.TypeLoadMode = TypeLoadMode.Auto;
});
```

#### Step 1.5: Remove Manual Validation from Handlers

**File:** `src/NOIR.Infrastructure/Identity/Handlers/LoginCommandHandler.cs`

Remove:
- `IValidator<LoginCommand> _validator` dependency
- Manual validation call in Handle method

The FluentValidation middleware will automatically validate before the handler executes.

### Files to Create/Modify

| File | Action |
|------|--------|
| `src/NOIR.Application/Behaviors/LoggingMiddleware.cs` | Create |
| `src/NOIR.Application/Behaviors/PerformanceMiddleware.cs` | Create |
| `src/NOIR.Application/NOIR.Application.csproj` | Add package |
| `src/NOIR.Web/Program.cs` | Modify Wolverine config |
| `src/NOIR.Infrastructure/Identity/Handlers/*.cs` | Remove manual validation |

---

## Enhancement 2: Result Pattern Integration

### Current State
- Handlers return custom response records (e.g., `LoginResponse`)
- Response records have `Succeeded`, `Auth`, `Errors` properties
- Endpoints manually check `Succeeded` and return appropriate HTTP status

### Target State
- Handlers return `Result<T>` with typed errors
- Endpoints use extension methods to map Result to HTTP responses
- Consistent error handling across all endpoints

### Implementation Steps

#### Step 2.1: Enhance Result and Error Classes

**File:** `src/NOIR.Domain/Common/Result.cs` (enhance existing)

```csharp
namespace NOIR.Domain.Common;

/// <summary>
/// Represents the result of an operation that can succeed or fail.
/// Enhanced with HTTP mapping support.
/// </summary>
public class Result
{
    // ... existing code ...

    /// <summary>
    /// Creates a validation failure result with multiple errors.
    /// </summary>
    public static Result ValidationFailure(IDictionary<string, string[]> errors) =>
        new(false, Error.ValidationErrors(errors));
}

/// <summary>
/// Error types for HTTP status code mapping.
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5
}

/// <summary>
/// Represents an error with a code, message, and type.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error NotFound(string entity, object id) =>
        new($"{entity}.NotFound", $"{entity} with id '{id}' was not found.", ErrorType.NotFound);

    public static Error Validation(string propertyName, string message) =>
        new($"Validation.{propertyName}", message, ErrorType.Validation);

    public static Error ValidationErrors(IDictionary<string, string[]> errors) =>
        new("Validation.Multiple", string.Join("; ", errors.SelectMany(e => e.Value)), ErrorType.Validation);

    public static Error Conflict(string message) =>
        new("Error.Conflict", message, ErrorType.Conflict);

    public static Error Unauthorized(string message = "Unauthorized access.") =>
        new("Error.Unauthorized", message, ErrorType.Unauthorized);

    public static Error Forbidden(string message = "Access forbidden.") =>
        new("Error.Forbidden", message, ErrorType.Forbidden);
}
```

#### Step 2.2: Create Result-to-HTTP Extension Methods

**File:** `src/NOIR.Web/Extensions/ResultExtensions.cs`

```csharp
namespace NOIR.Web.Extensions;

/// <summary>
/// Extension methods to convert Result to HTTP responses.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a Result to an IResult for Minimal APIs.
    /// </summary>
    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess
            ? Results.Ok()
            : ToProblemResult(result.Error);

    /// <summary>
    /// Converts a Result<T> to an IResult for Minimal APIs.
    /// </summary>
    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblemResult(result.Error);

    /// <summary>
    /// Converts a Result<T> to an IResult with a custom success status.
    /// </summary>
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess
            ? onSuccess(result.Value)
            : ToProblemResult(result.Error);

    private static IResult ToProblemResult(Error error) =>
        error.Type switch
        {
            ErrorType.Validation => Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error",
                detail: error.Message),

            ErrorType.NotFound => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: error.Message),

            ErrorType.Unauthorized => Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: error.Message),

            ErrorType.Forbidden => Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: error.Message),

            ErrorType.Conflict => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                detail: error.Message),

            _ => Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Error",
                detail: error.Message)
        };
}
```

#### Step 2.3: Update Handler to Return Result<T>

**Example:** `src/NOIR.Infrastructure/Identity/Handlers/LoginCommandHandler.cs`

```csharp
public class LoginCommandHandler
{
    // ... dependencies ...

    public async Task<Result<AuthResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        // Validation handled by FluentValidation middleware

        var normalizedEmail = _userManager.NormalizeEmail(command.Email);
        var user = await _userManager.FindByEmailAsync(normalizedEmail);

        if (user is null)
            return Result.Failure<AuthResponse>(Error.Unauthorized("Invalid email or password."));

        if (!user.IsActive)
            return Result.Failure<AuthResponse>(Error.Forbidden("User account is disabled."));

        var result = await _signInManager.CheckPasswordSignInAsync(user, command.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return Result.Failure<AuthResponse>(Error.Forbidden("Account is locked out. Please try again later."));

        if (!result.Succeeded)
            return Result.Failure<AuthResponse>(Error.Unauthorized("Invalid email or password."));

        // Generate tokens...
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, user.TenantId);
        var refreshToken = await _refreshTokenService.CreateTokenAsync(...);

        return new AuthResponse(user.Id, user.Email!, accessToken, refreshToken.Token, refreshToken.ExpiresAt);
    }
}
```

#### Step 2.4: Update Endpoints to Use Result Extensions

**File:** `src/NOIR.Web/Endpoints/AuthEndpoints.cs`

```csharp
group.MapPost("/login", async (LoginCommand command, IMessageBus bus) =>
{
    var result = await bus.InvokeAsync<Result<AuthResponse>>(command);
    return result.ToHttpResult();
})
.RequireRateLimiting("auth")
.WithName("Login")
.WithSummary("Login with email and password")
.Produces<AuthResponse>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);
```

### Files to Create/Modify

| File | Action |
|------|--------|
| `src/NOIR.Domain/Common/Result.cs` | Enhance |
| `src/NOIR.Web/Extensions/ResultExtensions.cs` | Create |
| `src/NOIR.Web/Endpoints/AuthEndpoints.cs` | Modify |
| `src/NOIR.Infrastructure/Identity/Handlers/*.cs` | Modify return types |
| `src/NOIR.Application/Features/Auth/Commands/*/` | Remove response records |

---

## Enhancement 3: Unit Test Structure

### Current State
- Only `NOIR.IntegrationTests` project exists (empty)
- No unit tests for Domain or Application layers

### Target State
- `NOIR.Domain.UnitTests` - Entity, ValueObject, Specification tests
- `NOIR.Application.UnitTests` - Handler tests with mocked dependencies
- `NOIR.Architecture.Tests` - Layer dependency enforcement

### Implementation Steps

#### Step 3.1: Create Domain Unit Test Project

```bash
dotnet new xunit -n NOIR.Domain.UnitTests -o tests/NOIR.Domain.UnitTests
dotnet add tests/NOIR.Domain.UnitTests reference src/NOIR.Domain
dotnet add tests/NOIR.Domain.UnitTests package FluentAssertions
dotnet add tests/NOIR.Domain.UnitTests package Bogus
dotnet sln src/NOIR.sln add tests/NOIR.Domain.UnitTests
```

**File:** `tests/NOIR.Domain.UnitTests/Common/ResultTests.cs`

```csharp
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
        act.Should().Throw<InvalidOperationException>();
    }
}
```

**File:** `tests/NOIR.Domain.UnitTests/Specifications/SpecificationTests.cs`

```csharp
namespace NOIR.Domain.UnitTests.Specifications;

public class SpecificationTests
{
    [Fact]
    public void IsSatisfiedBy_WithMatchingEntity_ShouldReturnTrue()
    {
        // Arrange
        var spec = new TestActiveSpec();
        var entity = new TestEntity { IsActive = true };

        // Act
        var result = spec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void And_CombinesTwoSpecifications_ShouldMatchBoth()
    {
        // Arrange
        var activeSpec = new TestActiveSpec();
        var namedSpec = new TestNamedSpec("John");
        var combinedSpec = activeSpec.And(namedSpec);

        var entity = new TestEntity { IsActive = true, Name = "John" };

        // Act
        var result = combinedSpec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    private class TestEntity
    {
        public bool IsActive { get; set; }
        public string Name { get; set; } = "";
    }

    private class TestActiveSpec : Specification<TestEntity>
    {
        public TestActiveSpec()
        {
            Query.Where(e => e.IsActive);
        }
    }

    private class TestNamedSpec : Specification<TestEntity>
    {
        public TestNamedSpec(string name)
        {
            Query.Where(e => e.Name == name);
        }
    }
}
```

#### Step 3.2: Create Application Unit Test Project

```bash
dotnet new xunit -n NOIR.Application.UnitTests -o tests/NOIR.Application.UnitTests
dotnet add tests/NOIR.Application.UnitTests reference src/NOIR.Application
dotnet add tests/NOIR.Application.UnitTests reference src/NOIR.Infrastructure
dotnet add tests/NOIR.Application.UnitTests package FluentAssertions
dotnet add tests/NOIR.Application.UnitTests package Moq
dotnet add tests/NOIR.Application.UnitTests package Bogus
dotnet sln src/NOIR.sln add tests/NOIR.Application.UnitTests
```

**File:** `tests/NOIR.Application.UnitTests/Features/Auth/LoginCommandTests.cs`

```csharp
namespace NOIR.Application.UnitTests.Features.Auth;

public class LoginCommandTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<IDeviceFingerprintService> _deviceFingerprintServiceMock;
    private readonly LoginCommandHandler _handler;

    public LoginCommandTests()
    {
        // Setup mocks
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(...);
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(...);
        _tokenServiceMock = new Mock<ITokenService>();
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _deviceFingerprintServiceMock = new Mock<IDeviceFingerprintService>();

        _handler = new LoginCommandHandler(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _tokenServiceMock.Object,
            _refreshTokenServiceMock.Object,
            _deviceFingerprintServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        var command = new LoginCommand("user@test.com", "password123");
        var user = new ApplicationUser { Id = "user-1", Email = "user@test.com", IsActive = true };

        _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, command.Password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("access-token");
        _refreshTokenServiceMock.Setup(x => x.CreateTokenAsync(...))
            .ReturnsAsync(new RefreshToken { Token = "refresh-token", ExpiresAt = DateTimeOffset.UtcNow.AddDays(7) });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
    }

    [Fact]
    public async Task Handle_WithInvalidUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new LoginCommand("nonexistent@test.com", "password");
        _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_WithDisabledUser_ShouldReturnForbidden()
    {
        // Arrange
        var command = new LoginCommand("disabled@test.com", "password");
        var user = new ApplicationUser { IsActive = false };
        _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }
}
```

#### Step 3.3: Create Architecture Test Project

```bash
dotnet new xunit -n NOIR.Architecture.Tests -o tests/NOIR.Architecture.Tests
dotnet add tests/NOIR.Architecture.Tests reference src/NOIR.Domain
dotnet add tests/NOIR.Architecture.Tests reference src/NOIR.Application
dotnet add tests/NOIR.Architecture.Tests reference src/NOIR.Infrastructure
dotnet add tests/NOIR.Architecture.Tests package FluentAssertions
dotnet add tests/NOIR.Architecture.Tests package NetArchTest.Rules
dotnet sln src/NOIR.sln add tests/NOIR.Architecture.Tests
```

**File:** `tests/NOIR.Architecture.Tests/LayerDependencyTests.cs`

```csharp
namespace NOIR.Architecture.Tests;

public class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Common.Entity<>).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void Domain_ShouldNotDependOn_Application()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("NOIR.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("NOIR.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("NOIR.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_ShouldNotDependOn_Web()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("NOIR.Web")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
```

### Final Test Project Structure

```
tests/
├── NOIR.Domain.UnitTests/
│   ├── Common/
│   │   ├── ResultTests.cs
│   │   ├── EntityTests.cs
│   │   └── ValueObjectTests.cs
│   ├── Entities/
│   │   ├── RefreshTokenTests.cs
│   │   └── AuditLogTests.cs
│   └── Specifications/
│       └── SpecificationTests.cs
├── NOIR.Application.UnitTests/
│   ├── Features/
│   │   └── Auth/
│   │       ├── LoginCommandTests.cs
│   │       ├── RegisterCommandTests.cs
│   │       └── RefreshTokenCommandTests.cs
│   └── Behaviors/
│       └── ValidationBehaviorTests.cs
├── NOIR.Architecture.Tests/
│   └── LayerDependencyTests.cs
└── NOIR.IntegrationTests/  (existing)
    ├── Endpoints/
    │   └── AuthEndpointsTests.cs
    └── Infrastructure/
        └── TestWebApplicationFactory.cs
```

---

## Implementation Order

### Phase 1: Wolverine Pipeline Policies (Day 1)

1. Add `WolverineFx.FluentValidation` package
2. Create `LoggingMiddleware.cs`
3. Create `PerformanceMiddleware.cs`
4. Update `Program.cs` Wolverine configuration
5. Remove manual validation from handlers
6. Test that validation still works

### Phase 2: Result Pattern Integration (Day 1-2)

1. Enhance `Result.cs` with ErrorType
2. Create `ResultExtensions.cs`
3. Update `LoginCommandHandler` to return `Result<AuthResponse>`
4. Update `/api/auth/login` endpoint
5. Repeat for Register, RefreshToken, GetCurrentUser
6. Remove old response record types
7. Test all auth endpoints

### Phase 3: Unit Tests (Day 2-3)

1. Create `NOIR.Domain.UnitTests` project
2. Write Result and Specification tests
3. Create `NOIR.Application.UnitTests` project
4. Write handler tests with mocks
5. Create `NOIR.Architecture.Tests` project
6. Write layer dependency tests
7. Run all tests and verify coverage

---

## Verification Checklist

After implementation, verify:

- [x] `dotnet build src/NOIR.sln` succeeds
- [x] `dotnet test tests/` all tests pass (765 tests)
- [x] FluentValidation middleware auto-validates commands
- [x] Logging middleware logs handler executions
- [x] Performance middleware warns on slow handlers
- [x] All auth endpoints return proper HTTP status codes
- [x] Architecture tests enforce layer dependencies (25 tests)

---

## References

- [Wolverine Middleware Documentation](https://wolverine.netlify.app/guide/handlers/middleware.html)
- [Wolverine FluentValidation Integration](https://wolverine.netlify.app/guide/handlers/fluent-validation.html)
- [Result Pattern in .NET](https://www.milanjovanovic.tech/blog/functional-error-handling-in-dotnet-with-the-result-pattern)
- [NetArchTest for Architecture Testing](https://github.com/BenMorris/NetArchTest)

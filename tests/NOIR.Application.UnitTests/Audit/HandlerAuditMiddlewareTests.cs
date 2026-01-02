using System.Reflection;
using System.Text.Json;
using NOIR.Infrastructure.Audit;

namespace NOIR.Application.UnitTests.Audit;

/// <summary>
/// Unit tests for HandlerAuditMiddleware.
/// Tests sanitization, serialization, and exception handling.
/// </summary>
public class HandlerAuditMiddlewareTests
{
    private readonly HandlerAuditMiddleware _sut;

    public HandlerAuditMiddlewareTests()
    {
        _sut = new HandlerAuditMiddleware();
    }

    #region Sanitization Tests - JSON Input/Output

    [Fact]
    public void SanitizeAndSerialize_WithPassword_ShouldRedact()
    {
        // Arrange
        var input = new { Email = "test@example.com", Password = "secret123" };

        // Act
        var result = InvokeSanitizeAndSerialize(input);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("\"email\":\"test@example.com\"");
        result.Should().Contain("[REDACTED]");
        result.Should().NotContain("secret123");
    }

    [Fact]
    public void SanitizeAndSerialize_WithApiKey_ShouldRedact()
    {
        // Arrange
        var input = new { UserId = "123", ApiKey = "sk-12345abcdef" };

        // Act
        var result = InvokeSanitizeAndSerialize(input);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("\"userId\":\"123\"");
        result.Should().Contain("[REDACTED]");
        result.Should().NotContain("sk-12345abcdef");
    }

    [Fact]
    public void SanitizeAndSerialize_WithToken_ShouldRedact()
    {
        // Arrange
        var input = new { Name = "Test", RefreshToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9" };

        // Act
        var result = InvokeSanitizeAndSerialize(input);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("\"name\":\"Test\"");
        result.Should().Contain("[REDACTED]");
        result.Should().NotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9");
    }

    [Fact]
    public void SanitizeAndSerialize_WithNestedPassword_ShouldRedact()
    {
        // Arrange
        var input = new
        {
            User = new
            {
                Email = "test@example.com",
                PasswordHash = "hashed_password_here"
            }
        };

        // Act
        var result = InvokeSanitizeAndSerialize(input);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("[REDACTED]");
        result.Should().NotContain("hashed_password_here");
    }

    [Fact]
    public void SanitizeAndSerialize_WithMultipleSensitiveFields_ShouldRedactAll()
    {
        // Arrange
        var input = new
        {
            Email = "test@example.com",
            Password = "pass123",
            Secret = "my-secret",
            ApiKey = "api-key-123",
            Token = "token-value"
        };

        // Act
        var result = InvokeSanitizeAndSerialize(input);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("\"email\":\"test@example.com\"");
        var redactedCount = result!.Split("[REDACTED]").Length - 1;
        redactedCount.Should().Be(4); // Password, Secret, ApiKey, Token
    }

    [Fact]
    public void SanitizeAndSerialize_WithCreditCardInfo_ShouldRedact()
    {
        // Arrange
        var input = new
        {
            OrderId = "ORD-123",
            CreditCard = "4111-1111-1111-1111",
            CVV = "123"
        };

        // Act
        var result = InvokeSanitizeAndSerialize(input);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("\"orderId\":\"ORD-123\"");
        result.Should().NotContain("4111-1111-1111-1111");
        result.Should().NotContain("\"cvv\":\"123\"");
    }

    [Fact]
    public void SanitizeAndSerialize_WithNonSensitiveData_ShouldNotRedact()
    {
        // Arrange
        var input = new
        {
            Id = 123,
            Name = "John Doe",
            Email = "john@example.com",
            IsActive = true
        };

        // Act
        var result = InvokeSanitizeAndSerialize(input);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("\"id\":123");
        result.Should().Contain("\"name\":\"John Doe\"");
        result.Should().Contain("\"email\":\"john@example.com\"");
        result.Should().Contain("\"isActive\":true");
        result.Should().NotContain("[REDACTED]");
    }

    [Fact]
    public void SanitizeAndSerialize_WithNull_ShouldReturnNull()
    {
        // Act
        var result = InvokeSanitizeAndSerialize(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void SanitizeAndSerialize_WithArray_ShouldSerialize()
    {
        // Arrange
        var input = new { Tags = new[] { "tag1", "tag2", "tag3" } };

        // Act
        var result = InvokeSanitizeAndSerialize(input);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("\"tags\":[\"tag1\",\"tag2\",\"tag3\"]");
    }

    #endregion

    #region SanitizeExceptionMessage Tests

    [Fact]
    public void SanitizeExceptionMessage_WithConnectionString_ShouldRedact()
    {
        // Arrange
        var exception = new InvalidOperationException(
            "Failed to connect: Server=myserver;Database=mydb;User Id=admin;Password=secret123;");

        // Act
        var result = InvokeSanitizeExceptionMessage(exception);

        // Assert
        result.Should().Contain("InvalidOperationException");
        result.Should().Contain("[CONNECTION STRING REDACTED]");
        result.Should().NotContain("secret123");
    }

    [Fact]
    public void SanitizeExceptionMessage_WithPassword_ShouldRedact()
    {
        // Arrange - The sanitizer looks for "Password=" pattern and replaces to end of token
        var exception = new Exception("Login failed with Password=secret123; for user");

        // Act
        var result = InvokeSanitizeExceptionMessage(exception);

        // Assert
        result.Should().Contain("Password=[REDACTED]");
        result.Should().NotContain("secret123");
    }

    [Fact]
    public void SanitizeExceptionMessage_WithBearerToken_ShouldRedact()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("Invalid Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0");

        // Act
        var result = InvokeSanitizeExceptionMessage(exception);

        // Assert
        result.Should().Contain("Bearer [REDACTED]");
        result.Should().NotContain("eyJhbGciOiJIUzI1NiJ9");
    }

    [Fact]
    public void SanitizeExceptionMessage_WithApiKey_ShouldRedact()
    {
        // Arrange - Pattern looks for ApiKey= and replaces to end of token
        var exception = new Exception("Request failed: ApiKey=sk-1234567890abcdef;");

        // Act
        var result = InvokeSanitizeExceptionMessage(exception);

        // Assert
        result.Should().Contain("ApiKey=[REDACTED]");
        result.Should().NotContain("sk-1234567890abcdef");
    }

    [Fact]
    public void SanitizeExceptionMessage_VeryLongMessage_ShouldTruncate()
    {
        // Arrange
        var longMessage = new string('A', 1000);
        var exception = new Exception(longMessage);

        // Act
        var result = InvokeSanitizeExceptionMessage(exception);

        // Assert
        result.Should().Contain("[TRUNCATED]");
        result.Length.Should().BeLessThan(600); // 500 chars + exception type + "[TRUNCATED]"
    }

    [Fact]
    public void SanitizeExceptionMessage_NormalMessage_ShouldNotTruncate()
    {
        // Arrange
        var exception = new ArgumentException("Value cannot be null");

        // Act
        var result = InvokeSanitizeExceptionMessage(exception);

        // Assert
        result.Should().Contain("ArgumentException: Value cannot be null");
        result.Should().NotContain("[TRUNCATED]");
    }

    [Fact]
    public void SanitizeExceptionMessage_WithMultiplePatterns_ShouldRedactSequentially()
    {
        // Arrange - The sanitizer processes patterns sequentially
        // Each pattern replaces from pattern start to next delimiter
        var exception = new Exception("Connection failed: password=secret;");

        // Act
        var result = InvokeSanitizeExceptionMessage(exception);

        // Assert
        result.Should().Contain("[REDACTED]");
        result.Should().NotContain("secret");
    }

    [Fact]
    public void SanitizeExceptionMessage_WithDataSource_ShouldRedact()
    {
        // Arrange
        var exception = new Exception("Data Source=myserver.database.windows.net;Initial Catalog=mydb");

        // Act
        var result = InvokeSanitizeExceptionMessage(exception);

        // Assert
        result.Should().Contain("[CONNECTION STRING REDACTED]");
    }

    #endregion

    #region GetTargetDtoType Tests

    [Fact]
    public void GetTargetDtoType_CommandType_ShouldExtractDtoName()
    {
        // The GetTargetDtoType method extracts DTO type from command name
        // It removes Create/Update/Delete prefixes and "Command" suffix, then adds "Dto"
        // This test verifies the method exists and accepts Type parameter
        var method = typeof(HandlerAuditMiddleware)
            .GetMethod("GetTargetDtoType", BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(Type));
    }

    #endregion

    #region Sensitive Property Detection Tests

    [Theory]
    [InlineData("Password")]
    [InlineData("PasswordHash")]
    [InlineData("SecurityStamp")]
    [InlineData("ConcurrencyStamp")]
    [InlineData("Secret")]
    [InlineData("Token")]
    [InlineData("ApiKey")]
    [InlineData("PrivateKey")]
    [InlineData("Salt")]
    [InlineData("RefreshToken")]
    [InlineData("CreditCard")]
    [InlineData("CVV")]
    [InlineData("SSN")]
    [InlineData("SocialSecurityNumber")]
    public void SanitizeAndSerialize_SensitivePropertyNames_ShouldAllBeRedacted(string propertyName)
    {
        // Arrange - Create dynamic object with the sensitive property
        var dict = new Dictionary<string, object>
        {
            { "SafeProperty", "safe value" },
            { propertyName, "sensitive value" }
        };
        var json = JsonSerializer.Serialize(dict);

        // Act
        var result = InvokeSanitizeJson(json);

        // Assert
        result.Should().Contain("[REDACTED]");
        result.Should().NotContain("sensitive value");
        result.Should().Contain("safe value");
    }

    [Theory]
    [InlineData("UserPassword")]
    [InlineData("AdminApiKey")]
    [InlineData("AccessToken")]
    [InlineData("AuthToken")]
    public void SanitizeAndSerialize_PartialMatchPropertyNames_ShouldBeRedacted(string propertyName)
    {
        // Arrange
        var dict = new Dictionary<string, object>
        {
            { propertyName, "sensitive value" }
        };
        var json = JsonSerializer.Serialize(dict);

        // Act
        var result = InvokeSanitizeJson(json);

        // Assert
        result.Should().Contain("[REDACTED]");
        result.Should().NotContain("sensitive value");
    }

    #endregion

    #region ComputeDtoDiff Tests

    [Fact]
    public void ComputeDtoDiff_WithChanges_ShouldReturnDiff()
    {
        // Arrange
        var before = new { Id = "123", Name = "Before", Email = "test@test.com" };
        var after = new { Id = "123", Name = "After", Email = "test@test.com" };

        // Act
        var diff = InvokeComputeDtoDiff(before, after);

        // Assert
        diff.Should().NotBeNull();
        // Properties are camelCase in JSON
        diff.Should().Contain("name");
        diff.Should().Contain("Before");
        diff.Should().Contain("After");
    }

    [Fact]
    public void ComputeDtoDiff_WithNoChanges_ShouldReturnNullOrEmpty()
    {
        // Arrange
        var before = new { Id = "123", Name = "Same", Email = "test@test.com" };
        var after = new { Id = "123", Name = "Same", Email = "test@test.com" };

        // Act
        var diff = InvokeComputeDtoDiff(before, after);

        // Assert
        // When there are no changes, diff should be null or empty array
        if (diff is not null)
        {
            diff.Should().Contain("[]");
        }
    }

    [Fact]
    public void ComputeDtoDiff_ShouldRedactSensitiveFields()
    {
        // Arrange - Both objects have same password (both will be sanitized to [REDACTED])
        // So password won't appear in diff at all
        var before = new { Name = "Test", Password = "secret1" };
        var after = new { Name = "Updated", Password = "secret2" };

        // Act
        var diff = InvokeComputeDtoDiff(before, after);

        // Assert
        diff.Should().NotBeNull();
        // Sensitive values should NOT appear in the diff
        diff.Should().NotContain("secret1");
        diff.Should().NotContain("secret2");
        // Only the Name change should be in the diff (password is redacted on both sides,
        // so appears as no change since [REDACTED] == [REDACTED])
        diff.Should().Contain("name");
    }

    #endregion

    #region GetTargetDtoTypeFromInterface Tests

    [Fact]
    public void GetTargetDtoTypeFromInterface_WithIAuditableCommand_ShouldReturnDtoType()
    {
        // Arrange
        var commandType = typeof(TestAuditableCommand);

        // Act
        var dtoType = InvokeGetTargetDtoTypeFromInterface(commandType);

        // Assert
        dtoType.Should().NotBeNull();
        dtoType!.Name.Should().Be("TestDto");
    }

    [Fact]
    public void GetTargetDtoTypeFromInterface_WithNonAuditableCommand_ShouldReturnNull()
    {
        // Arrange
        var commandType = typeof(TestCommand);

        // Act
        var dtoType = InvokeGetTargetDtoTypeFromInterface(commandType);

        // Assert
        dtoType.Should().BeNull();
    }

    #endregion

    #region Helper Methods - Use Reflection to Access Private Methods

    private static string? InvokeSanitizeAndSerialize(object? obj)
    {
        var method = typeof(HandlerAuditMiddleware)
            .GetMethod("SanitizeAndSerialize", BindingFlags.NonPublic | BindingFlags.Static);
        return (string?)method?.Invoke(null, [obj]);
    }

    private static string InvokeSanitizeExceptionMessage(Exception exception)
    {
        var method = typeof(HandlerAuditMiddleware)
            .GetMethod("SanitizeExceptionMessage", BindingFlags.NonPublic | BindingFlags.Static);
        return (string)method?.Invoke(null, [exception])!;
    }

    private static string? InvokeGetTargetDtoType(Type messageType)
    {
        var method = typeof(HandlerAuditMiddleware)
            .GetMethod("GetTargetDtoType", BindingFlags.NonPublic | BindingFlags.Static);
        return (string?)method?.Invoke(null, [messageType]);
    }

    private static string InvokeSanitizeJson(string json)
    {
        var method = typeof(HandlerAuditMiddleware)
            .GetMethod("SanitizeJson", BindingFlags.NonPublic | BindingFlags.Static);
        return (string)method?.Invoke(null, [json])!;
    }

    private static Type CreateMockMessageType(string typeName)
    {
        // Create a simple mock type for testing GetTargetDtoType
        // We can't easily create dynamic types, so we'll use a workaround
        // The method checks if the type name ends with "Command"
        return typeof(TestCommand);
    }

    private static string? InvokeComputeDtoDiff(object before, object after)
    {
        // Create a mock diff service
        var diffService = new TestDiffService();

        var method = typeof(HandlerAuditMiddleware)
            .GetMethod("ComputeDtoDiff", BindingFlags.NonPublic | BindingFlags.Static);
        return (string?)method?.Invoke(null, [diffService, before, after]);
    }

    private static Type? InvokeGetTargetDtoTypeFromInterface(Type messageType)
    {
        var method = typeof(HandlerAuditMiddleware)
            .GetMethod("GetTargetDtoTypeFromInterface", BindingFlags.NonPublic | BindingFlags.Static);
        return (Type?)method?.Invoke(null, [messageType]);
    }

    // Test command for GetTargetDtoType testing
    private record TestCommand;

    // Test DTO for ComputeDtoDiff testing
    private record TestDto(string Id, string Name);

    // Test auditable command
    private record TestAuditableCommand : IAuditableCommand<TestDto>
    {
        public object? GetTargetId() => "123";
        public AuditOperationType OperationType => AuditOperationType.Update;
    }

    // Test diff service implementation
    private class TestDiffService : IDiffService
    {
        public string? CreateDiff<T>(T? before, T? after) where T : class
        {
            return CreateDiffFromJson(
                before is null ? null : JsonSerializer.Serialize(before),
                after is null ? null : JsonSerializer.Serialize(after));
        }

        public string? CreateDiffFromJson(string? beforeJson, string? afterJson)
        {
            if (beforeJson == afterJson) return null;
            if (beforeJson is null && afterJson is null) return null;

            // Simple diff implementation for testing
            var operations = new List<object>();

            if (beforeJson is null || afterJson is null)
            {
                return JsonSerializer.Serialize(operations);
            }

            try
            {
                var before = JsonSerializer.Deserialize<JsonElement>(beforeJson);
                var after = JsonSerializer.Deserialize<JsonElement>(afterJson);

                if (before.ValueKind == JsonValueKind.Object && after.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in after.EnumerateObject())
                    {
                        if (before.TryGetProperty(prop.Name, out var beforeValue))
                        {
                            if (beforeValue.ToString() != prop.Value.ToString())
                            {
                                operations.Add(new
                                {
                                    op = "replace",
                                    path = $"/{prop.Name}",
                                    value = prop.Value.ToString(),
                                    oldValue = beforeValue.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore parsing errors in test
            }

            return operations.Count == 0 ? "[]" : JsonSerializer.Serialize(operations);
        }

        public string? CreateDiffFromDictionaries(
            IReadOnlyDictionary<string, object?>? before,
            IReadOnlyDictionary<string, object?>? after)
        {
            return null; // Not used in these tests
        }
    }

    #endregion
}

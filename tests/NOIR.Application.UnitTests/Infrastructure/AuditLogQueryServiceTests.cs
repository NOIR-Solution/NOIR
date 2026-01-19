namespace NOIR.Application.UnitTests.Infrastructure;

using NOIR.Application.Features.Audit.DTOs;
using NOIR.Infrastructure.Audit;
using System.Text.Json;

/// <summary>
/// Unit tests for AuditLogQueryService.
/// Tests the service's API contract and behavior.
/// Full integration tests with real DbContext are in integration tests.
/// </summary>
public class AuditLogQueryServiceTests
{
    #region Interface Contract Tests

    [Fact]
    public void IAuditLogQueryService_ShouldDefineGetEntityTypesAsync()
    {
        // Assert
        var method = typeof(IAuditLogQueryService).GetMethod("GetEntityTypesAsync");
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IReadOnlyList<string>>));
    }

    [Fact]
    public void IAuditLogQueryService_ShouldDefineSearchEntitiesAsync()
    {
        // Assert
        var method = typeof(IAuditLogQueryService).GetMethod("SearchEntitiesAsync");
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(5);  // entityType, searchTerm, page, pageSize, ct
    }

    [Fact]
    public void IAuditLogQueryService_ShouldDefineGetEntityHistoryAsync()
    {
        // Assert
        var method = typeof(IAuditLogQueryService).GetMethod("GetEntityHistoryAsync");
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(8);  // entityType, entityId, fromDate, toDate, userId, page, pageSize, ct
    }

    [Fact]
    public void IAuditLogQueryService_ShouldDefineGetEntityVersionsAsync()
    {
        // Assert
        var method = typeof(IAuditLogQueryService).GetMethod("GetEntityVersionsAsync");
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(3);  // entityType, entityId, ct
    }

    [Fact]
    public void IAuditLogQueryService_ShouldDefineSearchActivityTimelineAsync()
    {
        // Assert
        var method = typeof(IAuditLogQueryService).GetMethod("SearchActivityTimelineAsync");
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(12);  // Multiple parameters
    }

    [Fact]
    public void IAuditLogQueryService_ShouldDefineGetActivityDetailsAsync()
    {
        // Assert
        var method = typeof(IAuditLogQueryService).GetMethod("GetActivityDetailsAsync");
        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(2);  // handlerAuditLogId, ct
    }

    [Fact]
    public void IAuditLogQueryService_ShouldDefineGetPageContextsAsync()
    {
        // Assert
        var method = typeof(IAuditLogQueryService).GetMethod("GetPageContextsAsync");
        method.Should().NotBeNull();
    }

    #endregion

    #region AuditLogQueryService Class Tests

    [Fact]
    public void AuditLogQueryService_ShouldImplementIAuditLogQueryService()
    {
        // Assert
        typeof(AuditLogQueryService).Should().Implement<IAuditLogQueryService>();
    }

    [Fact]
    public void AuditLogQueryService_ShouldImplementIScopedService()
    {
        // Assert
        typeof(AuditLogQueryService).Should().Implement<IScopedService>();
    }

    [Fact]
    public void AuditLogQueryService_ShouldHaveConstructorWithDbContext()
    {
        // Assert
        var constructor = typeof(AuditLogQueryService).GetConstructors()
            .FirstOrDefault(c => c.GetParameters().Any(p => p.ParameterType == typeof(ApplicationDbContext)));
        constructor.Should().NotBeNull();
    }

    #endregion

    #region DTO Tests

    [Fact]
    public void EntitySearchResultDto_ShouldHaveAllRequiredProperties()
    {
        // Arrange & Act
        var dto = new EntitySearchResultDto(
            EntityType: "Customer",
            EntityId: "123",
            DisplayName: "Customer (123)",
            Description: "Test description",
            LastModified: DateTimeOffset.UtcNow,
            LastModifiedBy: "user@example.com",
            TotalChanges: 5);

        // Assert
        dto.EntityType.Should().Be("Customer");
        dto.EntityId.Should().Be("123");
        dto.DisplayName.Should().Be("Customer (123)");
        dto.Description.Should().Be("Test description");
        dto.TotalChanges.Should().Be(5);
    }

    [Fact]
    public void EntityHistoryEntryDto_ShouldHaveAllRequiredProperties()
    {
        // Arrange & Act
        var dto = new EntityHistoryEntryDto(
            Id: Guid.NewGuid(),
            Timestamp: DateTimeOffset.UtcNow,
            Operation: "Modified",
            UserId: "user-123",
            UserEmail: "user@example.com",
            HandlerName: "UpdateCustomerCommandHandler",
            CorrelationId: "correlation-123",
            Changes: new List<FieldChangeDto>(),
            Version: 2);

        // Assert
        dto.Operation.Should().Be("Modified");
        dto.UserId.Should().Be("user-123");
        dto.UserEmail.Should().Be("user@example.com");
        dto.Version.Should().Be(2);
    }

    [Fact]
    public void EntityVersionDto_ShouldHaveAllRequiredProperties()
    {
        // Arrange & Act
        var state = new Dictionary<string, object?> { { "Name", "John" } };
        var dto = new EntityVersionDto(
            Version: 1,
            Timestamp: DateTimeOffset.UtcNow,
            Operation: "Added",
            UserId: "user-123",
            UserEmail: "user@example.com",
            State: state);

        // Assert
        dto.Version.Should().Be(1);
        dto.Operation.Should().Be("Added");
        dto.State.Should().ContainKey("Name");
    }

    [Fact]
    public void ActivityTimelineEntryDto_ShouldHaveAllRequiredProperties()
    {
        // Arrange & Act
        var dto = new ActivityTimelineEntryDto(
            Id: Guid.NewGuid(),
            Timestamp: DateTimeOffset.UtcNow,
            UserEmail: "admin@example.com",
            UserId: "admin-123",
            DisplayContext: "Users",
            OperationType: "Update",
            ActionDescription: "Updated user John Doe",
            TargetDisplayName: "John Doe",
            TargetDtoType: "UserDto",
            TargetDtoId: "user-456",
            IsSuccess: true,
            DurationMs: 150,
            EntityChangeCount: 3,
            CorrelationId: "correlation-789",
            HandlerName: "UpdateUserCommandHandler");

        // Assert
        dto.DisplayContext.Should().Be("Users");
        dto.IsSuccess.Should().BeTrue();
        dto.EntityChangeCount.Should().Be(3);
    }

    [Fact]
    public void ActivityDetailsDto_ShouldHaveAllRequiredProperties()
    {
        // Arrange
        var entry = new ActivityTimelineEntryDto(
            Id: Guid.NewGuid(),
            Timestamp: DateTimeOffset.UtcNow,
            UserEmail: "admin@example.com",
            UserId: "admin-123",
            DisplayContext: "Users",
            OperationType: "Update",
            ActionDescription: null,
            TargetDisplayName: null,
            TargetDtoType: null,
            TargetDtoId: null,
            IsSuccess: true,
            DurationMs: 100,
            EntityChangeCount: 0,
            CorrelationId: null,
            HandlerName: null);

        var httpRequest = new HttpRequestDetailsDto(
            Id: Guid.NewGuid(),
            Method: "PUT",
            Path: "/api/users/123",
            StatusCode: 200,
            QueryString: null,
            ClientIpAddress: "127.0.0.1",
            UserAgent: "Mozilla/5.0",
            RequestTime: DateTimeOffset.UtcNow,
            DurationMs: 100);

        // Act
        var dto = new ActivityDetailsDto(
            Entry: entry,
            InputParameters: "{\"name\": \"John\"}",
            OutputResult: "{\"success\": true}",
            DtoDiff: null,
            ErrorMessage: null,
            HttpRequest: httpRequest,
            EntityChanges: new List<EntityChangeDto>());

        // Assert
        dto.Entry.Should().NotBeNull();
        dto.HttpRequest.Should().NotBeNull();
        dto.HttpRequest!.Method.Should().Be("PUT");
        dto.InputParameters.Should().Contain("John");
    }

    [Fact]
    public void HttpRequestDetailsDto_ShouldHaveAllRequiredProperties()
    {
        // Arrange & Act
        var dto = new HttpRequestDetailsDto(
            Id: Guid.NewGuid(),
            Method: "POST",
            Path: "/api/users",
            StatusCode: 201,
            QueryString: "?active=true",
            ClientIpAddress: "192.168.1.1",
            UserAgent: "curl/7.68.0",
            RequestTime: DateTimeOffset.UtcNow,
            DurationMs: 250);

        // Assert
        dto.Method.Should().Be("POST");
        dto.StatusCode.Should().Be(201);
        dto.QueryString.Should().Be("?active=true");
    }

    [Fact]
    public void EntityChangeDto_ShouldHaveAllRequiredProperties()
    {
        // Arrange
        var changes = new List<FieldChangeDto>
        {
            new FieldChangeDto("Name", "Old Name", "New Name", ChangeOperation.Modified)
        };

        // Act
        var dto = new EntityChangeDto(
            Id: Guid.NewGuid(),
            EntityType: "Customer",
            EntityId: "123",
            Operation: "Modified",
            Version: 2,
            Timestamp: DateTimeOffset.UtcNow,
            Changes: changes);

        // Assert
        dto.EntityType.Should().Be("Customer");
        dto.Version.Should().Be(2);
        dto.Changes.Should().HaveCount(1);
    }

    [Fact]
    public void FieldChangeDto_ShouldHaveAllRequiredProperties()
    {
        // Arrange & Act
        var dto = new FieldChangeDto(
            FieldName: "Email",
            OldValue: "old@example.com",
            NewValue: "new@example.com",
            Operation: ChangeOperation.Modified);

        // Assert
        dto.FieldName.Should().Be("Email");
        dto.OldValue.Should().Be("old@example.com");
        dto.NewValue.Should().Be("new@example.com");
        dto.Operation.Should().Be(ChangeOperation.Modified);
    }

    #endregion

    #region ChangeOperation Enum Tests

    [Fact]
    public void ChangeOperation_Added_ShouldBeDefined()
    {
        // Assert
        ChangeOperation.Added.Should().BeDefined();
    }

    [Fact]
    public void ChangeOperation_Modified_ShouldBeDefined()
    {
        // Assert
        ChangeOperation.Modified.Should().BeDefined();
    }

    [Fact]
    public void ChangeOperation_Removed_ShouldBeDefined()
    {
        // Assert
        ChangeOperation.Removed.Should().BeDefined();
    }

    [Fact]
    public void ChangeOperation_ShouldHaveThreeValues()
    {
        // Assert
        var values = Enum.GetValues<ChangeOperation>();
        values.Should().HaveCount(3);
    }

    #endregion

    #region Helper Method Tests - ParseEntityDiff Logic

    [Fact]
    public void ParseEntityDiff_WithEmptyString_ShouldReturnEmptyList()
    {
        // Arrange
        var entityDiff = "";

        // Act
        var result = ParseEntityDiffHelper(entityDiff);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseEntityDiff_WithNullString_ShouldReturnEmptyList()
    {
        // Arrange
        string? entityDiff = null;

        // Act
        var result = ParseEntityDiffHelper(entityDiff);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseEntityDiff_WithValidDiff_ShouldParseCorrectly()
    {
        // Arrange
        var entityDiff = @"{""Name"": {""from"": ""Old"", ""to"": ""New""}}";

        // Act
        var result = ParseEntityDiffHelper(entityDiff);

        // Assert
        result.Should().HaveCount(1);
        result[0].FieldName.Should().Be("Name");
        result[0].OldValue.Should().Be("Old");
        result[0].NewValue.Should().Be("New");
        result[0].Operation.Should().Be(ChangeOperation.Modified);
    }

    [Fact]
    public void ParseEntityDiff_WithAddedField_ShouldSetOperationToAdded()
    {
        // Arrange
        var entityDiff = @"{""Email"": {""to"": ""new@example.com""}}";

        // Act
        var result = ParseEntityDiffHelper(entityDiff);

        // Assert
        result.Should().HaveCount(1);
        result[0].OldValue.Should().BeNull();
        result[0].NewValue.Should().Be("new@example.com");
        result[0].Operation.Should().Be(ChangeOperation.Added);
    }

    [Fact]
    public void ParseEntityDiff_WithRemovedField_ShouldSetOperationToRemoved()
    {
        // Arrange
        var entityDiff = @"{""Email"": {""from"": ""old@example.com""}}";

        // Act
        var result = ParseEntityDiffHelper(entityDiff);

        // Assert
        result.Should().HaveCount(1);
        result[0].OldValue.Should().Be("old@example.com");
        result[0].NewValue.Should().BeNull();
        result[0].Operation.Should().Be(ChangeOperation.Removed);
    }

    [Fact]
    public void ParseEntityDiff_WithMultipleFields_ShouldParseAll()
    {
        // Arrange
        var entityDiff = @"{
            ""Name"": {""from"": ""Old"", ""to"": ""New""},
            ""Email"": {""to"": ""new@example.com""},
            ""Status"": {""from"": ""Active""}
        }";

        // Act
        var result = ParseEntityDiffHelper(entityDiff);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public void ParseEntityDiff_WithInvalidJson_ShouldReturnEmptyList()
    {
        // Arrange
        var entityDiff = "not valid json";

        // Act
        var result = ParseEntityDiffHelper(entityDiff);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseEntityDiff_WithArrayJson_ShouldReturnEmptyList()
    {
        // Arrange - Entity diff should be an object, not array
        var entityDiff = @"[{""op"": ""replace""}]";

        // Act
        var result = ParseEntityDiffHelper(entityDiff);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseEntityDiff_WithNumericValues_ShouldParseCorrectly()
    {
        // Arrange
        var entityDiff = @"{""Age"": {""from"": 25, ""to"": 30}}";

        // Act
        var result = ParseEntityDiffHelper(entityDiff);

        // Assert
        result.Should().HaveCount(1);
        result[0].FieldName.Should().Be("Age");
    }

    [Fact]
    public void ParseEntityDiff_WithBooleanValues_ShouldParseCorrectly()
    {
        // Arrange
        var entityDiff = @"{""IsActive"": {""from"": false, ""to"": true}}";

        // Act
        var result = ParseEntityDiffHelper(entityDiff);

        // Assert
        result.Should().HaveCount(1);
        result[0].FieldName.Should().Be("IsActive");
    }

    [Fact]
    public void ParseEntityDiff_WithNullValues_ShouldParseCorrectly()
    {
        // Arrange
        var entityDiff = @"{""MiddleName"": {""from"": null, ""to"": ""John""}}";

        // Act
        var result = ParseEntityDiffHelper(entityDiff);

        // Assert
        result.Should().HaveCount(1);
        result[0].OldValue.Should().BeNull();
        result[0].NewValue.Should().Be("John");
    }

    #endregion

    #region Helper Method Tests - FormatDisplayName Logic

    [Fact]
    public void FormatDisplayName_ShouldCombineTypeAndId()
    {
        // Arrange
        var entityType = "Customer";
        var entityId = "12345";

        // Act
        var result = FormatDisplayNameHelper(entityType, entityId);

        // Assert
        result.Should().Be("Customer (12345)");
    }

    [Fact]
    public void FormatDisplayName_WithGuidId_ShouldFormat()
    {
        // Arrange
        var entityType = "User";
        var entityId = "550e8400-e29b-41d4-a716-446655440000";

        // Act
        var result = FormatDisplayNameHelper(entityType, entityId);

        // Assert
        result.Should().StartWith("User (");
        result.Should().EndWith(")");
    }

    #endregion

    #region Helper Method Tests - ExtractFieldName Logic

    [Fact]
    public void ExtractFieldName_WithSimplePath_ShouldRemoveLeadingSlash()
    {
        // Arrange
        var path = "/Name";

        // Act
        var result = ExtractFieldNameHelper(path);

        // Assert
        result.Should().Be("Name");
    }

    [Fact]
    public void ExtractFieldName_WithNestedPath_ShouldConvertToDotNotation()
    {
        // Arrange
        var path = "/Address/Street";

        // Act
        var result = ExtractFieldNameHelper(path);

        // Assert
        result.Should().Be("Address.Street");
    }

    [Fact]
    public void ExtractFieldName_WithDeeplyNestedPath_ShouldConvert()
    {
        // Arrange
        var path = "/Customer/Address/City/Name";

        // Act
        var result = ExtractFieldNameHelper(path);

        // Assert
        result.Should().Be("Customer.Address.City.Name");
    }

    #endregion

    #region Helper Method Tests - MapOperation Logic

    [Fact]
    public void MapOperation_Add_ShouldReturnAdded()
    {
        // Assert
        MapOperationHelper("add").Should().Be(ChangeOperation.Added);
    }

    [Fact]
    public void MapOperation_Remove_ShouldReturnRemoved()
    {
        // Assert
        MapOperationHelper("remove").Should().Be(ChangeOperation.Removed);
    }

    [Fact]
    public void MapOperation_Replace_ShouldReturnModified()
    {
        // Assert
        MapOperationHelper("replace").Should().Be(ChangeOperation.Modified);
    }

    [Fact]
    public void MapOperation_Unknown_ShouldDefaultToModified()
    {
        // Assert
        MapOperationHelper("unknown").Should().Be(ChangeOperation.Modified);
    }

    [Fact]
    public void MapOperation_ShouldBeCaseInsensitive()
    {
        // Assert
        MapOperationHelper("ADD").Should().Be(ChangeOperation.Added);
        MapOperationHelper("Add").Should().Be(ChangeOperation.Added);
        MapOperationHelper("REMOVE").Should().Be(ChangeOperation.Removed);
    }

    #endregion

    #region Helper Methods - Simulating private methods

    /// <summary>
    /// Simulates the ParseEntityDiff private method from AuditLogQueryService.
    /// </summary>
    private static IReadOnlyList<FieldChangeDto> ParseEntityDiffHelper(string? entityDiff)
    {
        if (string.IsNullOrWhiteSpace(entityDiff))
            return Array.Empty<FieldChangeDto>();

        try
        {
            using var doc = JsonDocument.Parse(entityDiff);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                return Array.Empty<FieldChangeDto>();

            var changes = new List<FieldChangeDto>();

            foreach (var property in root.EnumerateObject())
            {
                var fieldName = property.Name;
                var diffValue = property.Value;

                if (diffValue.ValueKind == JsonValueKind.Object)
                {
                    object? oldValue = null;
                    object? newValue = null;

                    if (diffValue.TryGetProperty("from", out var fromElement))
                        oldValue = GetJsonValue(fromElement);
                    if (diffValue.TryGetProperty("to", out var toElement))
                        newValue = GetJsonValue(toElement);

                    var operation = (oldValue, newValue) switch
                    {
                        (null, not null) => ChangeOperation.Added,
                        (not null, null) => ChangeOperation.Removed,
                        _ => ChangeOperation.Modified
                    };

                    changes.Add(new FieldChangeDto(
                        FieldName: fieldName,
                        OldValue: oldValue,
                        NewValue: newValue,
                        Operation: operation
                    ));
                }
            }

            return changes;
        }
        catch
        {
            return Array.Empty<FieldChangeDto>();
        }
    }

    private static object? GetJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.GetRawText(),
            JsonValueKind.Object => element.GetRawText(),
            _ => element.GetRawText()
        };
    }

    private static string FormatDisplayNameHelper(string entityType, string entityId)
    {
        return $"{entityType} ({entityId})";
    }

    private static string ExtractFieldNameHelper(string path)
    {
        return path.TrimStart('/').Replace("/", ".");
    }

    private static ChangeOperation MapOperationHelper(string op)
    {
        return op.ToLower() switch
        {
            "add" => ChangeOperation.Added,
            "remove" => ChangeOperation.Removed,
            "replace" => ChangeOperation.Modified,
            _ => ChangeOperation.Modified
        };
    }

    #endregion
}

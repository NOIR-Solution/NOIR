namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for the HandlerAuditLog entity.
/// Tests factory methods, Complete method, SetTargetDto, and state transitions.
/// </summary>
public class HandlerAuditLogTests
{
    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidLog()
    {
        // Arrange
        var correlationId = "corr-123";
        var handlerName = "CreateCustomerCommand";
        var operationType = AuditOperationType.Create;

        // Act
        var log = HandlerAuditLog.Create(correlationId, handlerName, operationType, null);

        // Assert
        log.Should().NotBeNull();
        log.Id.Should().NotBe(Guid.Empty);
        log.CorrelationId.Should().Be(correlationId);
        log.HandlerName.Should().Be(handlerName);
        log.OperationType.Should().Be("Create");
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Arrange & Act
        var log1 = HandlerAuditLog.Create("corr-1", "Handler1", AuditOperationType.Create, null);
        var log2 = HandlerAuditLog.Create("corr-2", "Handler2", AuditOperationType.Create, null);

        // Assert
        log1.Id.Should().NotBe(log2.Id);
    }

    [Fact]
    public void Create_ShouldSetStartTime()
    {
        // Arrange
        var beforeCreate = DateTimeOffset.UtcNow;

        // Act
        var log = HandlerAuditLog.Create("corr-123", "CreateCustomerCommand", AuditOperationType.Create, null);

        // Assert
        var afterCreate = DateTimeOffset.UtcNow;
        log.StartTime.Should().BeOnOrAfter(beforeCreate).And.BeOnOrBefore(afterCreate);
    }

    [Fact]
    public void Create_WithTenantId_ShouldSetTenantId()
    {
        // Arrange
        var tenantId = "tenant-abc";

        // Act
        var log = HandlerAuditLog.Create("corr-123", "CreateCustomerCommand", AuditOperationType.Create, tenantId);

        // Assert
        log.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Create_WithHttpRequestAuditLogId_ShouldSetParentReference()
    {
        // Arrange
        var httpRequestLogId = Guid.NewGuid();

        // Act
        var log = HandlerAuditLog.Create("corr-123", "CreateCustomerCommand", AuditOperationType.Create, null, httpRequestLogId);

        // Assert
        log.HttpRequestAuditLogId.Should().Be(httpRequestLogId);
    }

    [Fact]
    public void Create_ShouldDefaultIsSuccessToTrue()
    {
        // Act
        var log = HandlerAuditLog.Create("corr-123", "CreateCustomerCommand", AuditOperationType.Create, null);

        // Assert
        log.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldNotBeArchived()
    {
        // Act
        var log = HandlerAuditLog.Create("corr-123", "CreateCustomerCommand", AuditOperationType.Create, null);

        // Assert
        log.IsArchived.Should().BeFalse();
        log.ArchivedAt.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldHaveNullEndTimeAndDuration()
    {
        // Act
        var log = HandlerAuditLog.Create("corr-123", "CreateCustomerCommand", AuditOperationType.Create, null);

        // Assert
        log.EndTime.Should().BeNull();
        log.DurationMs.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldInitializeEntityAuditLogsCollection()
    {
        // Act
        var log = HandlerAuditLog.Create("corr-123", "CreateCustomerCommand", AuditOperationType.Create, null);

        // Assert
        log.EntityAuditLogs.Should().NotBeNull();
        log.EntityAuditLogs.Should().BeEmpty();
    }

    #endregion

    #region Operation Type Tests

    [Theory]
    [InlineData(AuditOperationType.Create, "Create")]
    [InlineData(AuditOperationType.Update, "Update")]
    [InlineData(AuditOperationType.Delete, "Delete")]
    public void Create_AllOperationTypes_ShouldSetCorrectString(AuditOperationType operationType, string expected)
    {
        // Act
        var log = HandlerAuditLog.Create("corr-123", "TestHandler", operationType, null);

        // Assert
        log.OperationType.Should().Be(expected);
    }

    #endregion

    #region Complete Method Tests

    [Fact]
    public void Complete_WithSuccess_ShouldSetProperties()
    {
        // Arrange
        var log = HandlerAuditLog.Create("corr-123", "CreateCustomerCommand", AuditOperationType.Create, null);
        Thread.Sleep(10); // Ensure some time passes

        // Act
        log.Complete(isSuccess: true, outputResult: "{\"id\":\"123\"}");

        // Assert
        log.IsSuccess.Should().BeTrue();
        log.OutputResult.Should().Be("{\"id\":\"123\"}");
        log.EndTime.Should().NotBeNull();
        log.DurationMs.Should().BeGreaterThan(0);
        log.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Complete_WithFailure_ShouldSetErrorMessage()
    {
        // Arrange
        var log = HandlerAuditLog.Create("corr-123", "CreateCustomerCommand", AuditOperationType.Create, null);
        var errorMessage = "Validation failed: Name is required";

        // Act
        log.Complete(isSuccess: false, errorMessage: errorMessage);

        // Assert
        log.IsSuccess.Should().BeFalse();
        log.ErrorMessage.Should().Be(errorMessage);
        log.EndTime.Should().NotBeNull();
    }

    [Fact]
    public void Complete_WithDtoDiff_ShouldSetDiff()
    {
        // Arrange
        var log = HandlerAuditLog.Create("corr-123", "UpdateCustomerCommand", AuditOperationType.Update, null);
        var dtoDiff = "[{\"op\":\"replace\",\"path\":\"/name\",\"value\":\"New Name\"}]";

        // Act
        log.Complete(isSuccess: true, dtoDiff: dtoDiff);

        // Assert
        log.DtoDiff.Should().Be(dtoDiff);
    }

    [Fact]
    public void Complete_ShouldCalculateDurationMs()
    {
        // Arrange
        var log = HandlerAuditLog.Create("corr-123", "CreateCustomerCommand", AuditOperationType.Create, null);
        Thread.Sleep(50); // Wait 50ms

        // Act
        log.Complete(isSuccess: true);

        // Assert
        log.DurationMs.Should().NotBeNull();
        log.DurationMs!.Value.Should().BeGreaterThanOrEqualTo(40); // Allow some tolerance
        log.DurationMs.Should().BeLessThan(500); // Should not be too long
    }

    [Fact]
    public void Complete_MultipleTimes_ShouldUpdateValues()
    {
        // Arrange
        var log = HandlerAuditLog.Create("corr-123", "CreateCustomerCommand", AuditOperationType.Create, null);

        // Act
        log.Complete(isSuccess: true, outputResult: "first");
        var firstEndTime = log.EndTime;

        Thread.Sleep(10);
        log.Complete(isSuccess: false, errorMessage: "second attempt failed");

        // Assert
        log.IsSuccess.Should().BeFalse();
        log.ErrorMessage.Should().Be("second attempt failed");
        log.EndTime.Should().BeOnOrAfter(firstEndTime!.Value);
    }

    #endregion

    #region SetTargetDto Method Tests

    [Fact]
    public void SetTargetDto_ShouldSetBothProperties()
    {
        // Arrange
        var log = HandlerAuditLog.Create("corr-123", "UpdateCustomerCommand", AuditOperationType.Update, null);

        // Act
        log.SetTargetDto("CustomerDto", "cust-123");

        // Assert
        log.TargetDtoType.Should().Be("CustomerDto");
        log.TargetDtoId.Should().Be("cust-123");
    }

    [Fact]
    public void SetTargetDto_WithNullId_ShouldSetNullId()
    {
        // Arrange
        var log = HandlerAuditLog.Create("corr-123", "CreateCustomerCommand", AuditOperationType.Create, null);

        // Act
        log.SetTargetDto("CustomerDto", null);

        // Assert
        log.TargetDtoType.Should().Be("CustomerDto");
        log.TargetDtoId.Should().BeNull();
    }

    [Fact]
    public void SetTargetDto_MultipleTimes_ShouldUpdateValues()
    {
        // Arrange
        var log = HandlerAuditLog.Create("corr-123", "UpdateCustomerCommand", AuditOperationType.Update, null);

        // Act
        log.SetTargetDto("CustomerDto", "cust-1");
        log.SetTargetDto("OrderDto", "order-1");

        // Assert
        log.TargetDtoType.Should().Be("OrderDto");
        log.TargetDtoId.Should().Be("order-1");
    }

    #endregion

    #region Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidCorrelationId_ShouldThrow(string? correlationId)
    {
        // Act
        var act = () => HandlerAuditLog.Create(correlationId!, "Handler", AuditOperationType.Create, null);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidHandlerName_ShouldThrow(string? handlerName)
    {
        // Act
        var act = () => HandlerAuditLog.Create("corr-123", handlerName!, AuditOperationType.Create, null);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithInvalidOperationType_ShouldThrow()
    {
        // Arrange
        var invalidOperation = (AuditOperationType)999;

        // Act
        var act = () => HandlerAuditLog.Create("corr-123", "Handler", invalidOperation, null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid operation type*");
    }

    #endregion

    #region Navigation Property Tests

    [Fact]
    public void HttpRequestAuditLog_DefaultsToNull()
    {
        // Act
        var log = HandlerAuditLog.Create("corr-123", "CreateCustomerCommand", AuditOperationType.Create, null);

        // Assert
        log.HttpRequestAuditLog.Should().BeNull();
    }

    #endregion

    #region Handler Name Variations

    [Theory]
    [InlineData("CreateCustomerCommand")]
    [InlineData("UpdateOrderHandler")]
    [InlineData("DeleteProductCommand")]
    [InlineData("GetCustomerByIdQuery")]
    [InlineData("LoginCommand")]
    public void Create_VariousHandlerNames_ShouldWork(string handlerName)
    {
        // Act
        var log = HandlerAuditLog.Create("corr-123", handlerName, AuditOperationType.Create, null);

        // Assert
        log.HandlerName.Should().Be(handlerName);
    }

    #endregion
}

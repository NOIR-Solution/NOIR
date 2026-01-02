namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for the EntityAuditLog entity.
/// Tests factory methods, validation, and property defaults.
/// </summary>
public class EntityAuditLogTests
{
    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidLog()
    {
        // Arrange
        var correlationId = "corr-123";
        var entityType = "Customer";
        var entityId = "cust-456";
        var operation = EntityAuditOperation.Added;

        // Act
        var log = EntityAuditLog.Create(correlationId, entityType, entityId, operation, null, null);

        // Assert
        log.Should().NotBeNull();
        log.Id.Should().NotBe(Guid.Empty);
        log.CorrelationId.Should().Be(correlationId);
        log.EntityType.Should().Be(entityType);
        log.EntityId.Should().Be(entityId);
        log.Operation.Should().Be("Added");
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Arrange & Act
        var log1 = EntityAuditLog.Create("corr-1", "Customer", "1", EntityAuditOperation.Added, null, null);
        var log2 = EntityAuditLog.Create("corr-2", "Customer", "2", EntityAuditOperation.Added, null, null);

        // Assert
        log1.Id.Should().NotBe(log2.Id);
    }

    [Fact]
    public void Create_ShouldSetTimestamp()
    {
        // Arrange
        var beforeCreate = DateTimeOffset.UtcNow;

        // Act
        var log = EntityAuditLog.Create("corr-123", "Customer", "1", EntityAuditOperation.Added, null, null);

        // Assert
        var afterCreate = DateTimeOffset.UtcNow;
        log.Timestamp.Should().BeOnOrAfter(beforeCreate).And.BeOnOrBefore(afterCreate);
    }

    [Fact]
    public void Create_WithEntityDiff_ShouldSetDiff()
    {
        // Arrange
        var diff = "[{\"op\":\"replace\",\"path\":\"/name\",\"value\":\"New Name\"}]";

        // Act
        var log = EntityAuditLog.Create("corr-123", "Customer", "1", EntityAuditOperation.Modified, diff, null);

        // Assert
        log.EntityDiff.Should().Be(diff);
    }

    [Fact]
    public void Create_WithTenantId_ShouldSetTenantId()
    {
        // Arrange
        var tenantId = "tenant-abc";

        // Act
        var log = EntityAuditLog.Create("corr-123", "Customer", "1", EntityAuditOperation.Added, null, tenantId);

        // Assert
        log.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Create_WithHandlerAuditLogId_ShouldSetParentReference()
    {
        // Arrange
        var handlerLogId = Guid.NewGuid();

        // Act
        var log = EntityAuditLog.Create("corr-123", "Customer", "1", EntityAuditOperation.Added, null, null, handlerLogId);

        // Assert
        log.HandlerAuditLogId.Should().Be(handlerLogId);
    }

    [Fact]
    public void Create_ShouldDefaultVersionToOne()
    {
        // Act
        var log = EntityAuditLog.Create("corr-123", "Customer", "1", EntityAuditOperation.Added, null, null);

        // Assert
        log.Version.Should().Be(1);
    }

    [Fact]
    public void Create_ShouldNotBeArchived()
    {
        // Act
        var log = EntityAuditLog.Create("corr-123", "Customer", "1", EntityAuditOperation.Added, null, null);

        // Assert
        log.IsArchived.Should().BeFalse();
        log.ArchivedAt.Should().BeNull();
    }

    #endregion

    #region Operation Type Tests

    [Theory]
    [InlineData(EntityAuditOperation.Added, "Added")]
    [InlineData(EntityAuditOperation.Modified, "Modified")]
    [InlineData(EntityAuditOperation.Deleted, "Deleted")]
    public void Create_AllOperationTypes_ShouldSetCorrectString(EntityAuditOperation operation, string expected)
    {
        // Act
        var log = EntityAuditLog.Create("corr-123", "Customer", "1", operation, null, null);

        // Assert
        log.Operation.Should().Be(expected);
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
        var act = () => EntityAuditLog.Create(correlationId!, "Customer", "1", EntityAuditOperation.Added, null, null);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidEntityType_ShouldThrow(string? entityType)
    {
        // Act
        var act = () => EntityAuditLog.Create("corr-123", entityType!, "1", EntityAuditOperation.Added, null, null);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidEntityId_ShouldThrow(string? entityId)
    {
        // Act
        var act = () => EntityAuditLog.Create("corr-123", "Customer", entityId!, EntityAuditOperation.Added, null, null);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithInvalidOperationType_ShouldThrow()
    {
        // Arrange
        var invalidOperation = (EntityAuditOperation)999;

        // Act
        var act = () => EntityAuditLog.Create("corr-123", "Customer", "1", invalidOperation, null, null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid operation type*");
    }

    #endregion

    #region Navigation Property Tests

    [Fact]
    public void HandlerAuditLog_DefaultsToNull()
    {
        // Act
        var log = EntityAuditLog.Create("corr-123", "Customer", "1", EntityAuditOperation.Added, null, null);

        // Assert
        log.HandlerAuditLog.Should().BeNull();
    }

    #endregion

    #region Entity Type Variations

    [Theory]
    [InlineData("Customer")]
    [InlineData("Order")]
    [InlineData("Product")]
    [InlineData("RefreshToken")]
    [InlineData("ApplicationUser")]
    public void Create_VariousEntityTypes_ShouldWork(string entityType)
    {
        // Act
        var log = EntityAuditLog.Create("corr-123", entityType, "1", EntityAuditOperation.Added, null, null);

        // Assert
        log.EntityType.Should().Be(entityType);
    }

    #endregion
}

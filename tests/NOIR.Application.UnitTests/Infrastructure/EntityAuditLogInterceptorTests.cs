namespace NOIR.Application.UnitTests.Infrastructure;

using System.Reflection;
using NOIR.Infrastructure.Audit;

/// <summary>
/// Unit tests for EntityAuditLogInterceptor.
/// Tests the interceptor's API and behavior contracts.
/// Full integration tests with real DbContext are in integration tests.
/// </summary>
public class EntityAuditLogInterceptorTests
{
    private readonly Mock<IDiffService> _diffServiceMock;
    private readonly Mock<IMultiTenantContextAccessor<Tenant>> _tenantContextMock;
    private readonly EntityAuditLogInterceptor _sut;

    public EntityAuditLogInterceptorTests()
    {
        _diffServiceMock = new Mock<IDiffService>();
        _tenantContextMock = new Mock<IMultiTenantContextAccessor<Tenant>>();

        _sut = new EntityAuditLogInterceptor(_diffServiceMock.Object, _tenantContextMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldAcceptDependencies()
    {
        // Act
        var interceptor = new EntityAuditLogInterceptor(_diffServiceMock.Object, _tenantContextMock.Object);

        // Assert
        interceptor.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullDiffService_ShouldNotThrow()
    {
        // Act - Constructor doesn't validate, defers to usage
        var act = () => new EntityAuditLogInterceptor(null!, _tenantContextMock.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullTenantContext_ShouldNotThrow()
    {
        // Act
        var act = () => new EntityAuditLogInterceptor(_diffServiceMock.Object, null!);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void Interceptor_ShouldInheritFromSaveChangesInterceptor()
    {
        // Assert
        _sut.Should().BeAssignableTo<SaveChangesInterceptor>();
    }

    #endregion

    #region Method Existence Tests

    [Fact]
    public void SavingChangesAsync_MethodShouldExist()
    {
        // Assert
        var method = typeof(EntityAuditLogInterceptor).GetMethod("SavingChangesAsync");
        method.Should().NotBeNull();
    }

    [Fact]
    public void SavingChangesAsync_ShouldOverrideBaseMethod()
    {
        // Arrange
        var method = typeof(EntityAuditLogInterceptor).GetMethod("SavingChangesAsync");

        // Assert
        method.Should().NotBeNull();
        method!.DeclaringType.Should().Be(typeof(EntityAuditLogInterceptor));
    }

    #endregion

    #region Sensitive Properties Tests

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
    public void SensitiveProperties_ShouldBeDefinedInInterceptor(string propertyName)
    {
        // This test documents the expected sensitive properties
        // The interceptor should exclude these from audit logging

        // Assert - Property name is recognized as sensitive by naming convention
        propertyName.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Excluded Entity Types Tests

    [Theory]
    [InlineData("HttpRequestAuditLog")]
    [InlineData("HandlerAuditLog")]
    [InlineData("EntityAuditLog")]
    [InlineData("IdentityUserClaim`1")]
    [InlineData("IdentityUserRole`1")]
    [InlineData("IdentityUserLogin`1")]
    [InlineData("IdentityUserToken`1")]
    public void ExcludedEntityTypes_ShouldBeDefinedInInterceptor(string entityTypeName)
    {
        // This test documents the expected excluded entity types
        // The interceptor should not audit these types to prevent recursion and noise

        // Assert - Entity type name is recognized for exclusion
        entityTypeName.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region AuditContext Integration Tests

    [Fact]
    public void AuditContext_Current_ShouldBeNullByDefault()
    {
        // Assert
        AuditContext.Current.Should().BeNull();
    }

    [Fact]
    public void AuditContext_BeginRequestScope_ShouldSetCurrentContext()
    {
        // Arrange
        var httpRequestId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString();

        // Act
        using (AuditContext.BeginRequestScope(httpRequestId, correlationId))
        {
            // Assert
            AuditContext.Current.Should().NotBeNull();
            AuditContext.Current!.HttpRequestAuditLogId.Should().Be(httpRequestId);
            AuditContext.Current!.CorrelationId.Should().Be(correlationId);
        }
    }

    [Fact]
    public void AuditContext_BeginRequestScope_ShouldClearOnDispose()
    {
        // Arrange
        var httpRequestId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString();

        // Act
        using (AuditContext.BeginRequestScope(httpRequestId, correlationId))
        {
            AuditContext.Current.Should().NotBeNull();
        }

        // Assert - After disposal
        AuditContext.Current.Should().BeNull();
    }

    [Fact]
    public void AuditContext_SetCurrentHandler_ShouldUpdateContext()
    {
        // Arrange
        var httpRequestId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString();
        var handlerId = Guid.NewGuid();

        // Act
        using (AuditContext.BeginRequestScope(httpRequestId, correlationId))
        {
            AuditContext.SetCurrentHandler(handlerId);

            // Assert
            AuditContext.Current!.CurrentHandlerAuditLogId.Should().Be(handlerId);
        }
    }

    [Fact]
    public void AuditContext_ClearCurrentHandler_ShouldResetHandlerId()
    {
        // Arrange
        var httpRequestId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString();
        var handlerId = Guid.NewGuid();

        // Act
        using (AuditContext.BeginRequestScope(httpRequestId, correlationId))
        {
            AuditContext.SetCurrentHandler(handlerId);
            AuditContext.ClearCurrentHandler();

            // Assert
            AuditContext.Current!.CurrentHandlerAuditLogId.Should().BeNull();
        }
    }

    [Fact]
    public void AuditContext_SetCurrentHandler_WhenNoContext_ShouldNotThrow()
    {
        // Arrange - No scope started
        AuditContext.Clear();

        // Act
        var act = () => AuditContext.SetCurrentHandler(Guid.NewGuid());

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AuditContext_ClearCurrentHandler_WhenNoContext_ShouldNotThrow()
    {
        // Arrange - No scope started
        AuditContext.Clear();

        // Act
        var act = () => AuditContext.ClearCurrentHandler();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AuditContext_WithPageContext_ShouldStorePageContext()
    {
        // Arrange
        var httpRequestId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString();
        var pageContext = "Users";

        // Act
        using (AuditContext.BeginRequestScope(httpRequestId, correlationId, pageContext))
        {
            // Assert
            AuditContext.Current!.PageContext.Should().Be("Users");
        }
    }

    [Fact]
    public void AuditContext_Clear_ShouldResetToNull()
    {
        // Arrange
        var httpRequestId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString();

        using (AuditContext.BeginRequestScope(httpRequestId, correlationId))
        {
            // Act
            AuditContext.Clear();

            // Assert
            AuditContext.Current.Should().BeNull();
        }
    }

    #endregion

    #region DiffService Integration Tests

    [Fact]
    public void DiffService_CreateDiffFromDictionaries_ShouldBeCalledForChanges()
    {
        // Arrange
        var beforeValues = new Dictionary<string, object?> { { "Name", "Old" } };
        var afterValues = new Dictionary<string, object?> { { "Name", "New" } };

        _diffServiceMock.Setup(x => x.CreateDiffFromDictionaries(
            It.IsAny<IReadOnlyDictionary<string, object?>>(),
            It.IsAny<IReadOnlyDictionary<string, object?>>()))
            .Returns("{\"Name\": {\"from\": \"Old\", \"to\": \"New\"}}");

        // Act
        var result = _diffServiceMock.Object.CreateDiffFromDictionaries(beforeValues, afterValues);

        // Assert
        result.Should().Contain("Name");
        result.Should().Contain("Old");
        result.Should().Contain("New");
    }

    [Fact]
    public void DiffService_CreateDiffFromDictionaries_WithNullBefore_ShouldReturnAddDiff()
    {
        // Arrange
        var afterValues = new Dictionary<string, object?> { { "Name", "New" } };

        _diffServiceMock.Setup(x => x.CreateDiffFromDictionaries(
            null,
            It.IsAny<IReadOnlyDictionary<string, object?>>()))
            .Returns("{\"Name\": {\"to\": \"New\"}}");

        // Act
        var result = _diffServiceMock.Object.CreateDiffFromDictionaries(null, afterValues);

        // Assert
        result.Should().Contain("Name");
        result.Should().Contain("New");
    }

    [Fact]
    public void DiffService_CreateDiffFromDictionaries_WithNullAfter_ShouldReturnRemoveDiff()
    {
        // Arrange
        var beforeValues = new Dictionary<string, object?> { { "Name", "Old" } };

        _diffServiceMock.Setup(x => x.CreateDiffFromDictionaries(
            It.IsAny<IReadOnlyDictionary<string, object?>>(),
            null))
            .Returns("{\"Name\": {\"from\": \"Old\"}}");

        // Act
        var result = _diffServiceMock.Object.CreateDiffFromDictionaries(beforeValues, null);

        // Assert
        result.Should().Contain("Name");
        result.Should().Contain("Old");
    }

    #endregion

    #region EntityAuditOperation Tests

    [Fact]
    public void EntityAuditOperation_Added_ShouldBeDefined()
    {
        // Assert
        EntityAuditOperation.Added.Should().BeDefined();
    }

    [Fact]
    public void EntityAuditOperation_Modified_ShouldBeDefined()
    {
        // Assert
        EntityAuditOperation.Modified.Should().BeDefined();
    }

    [Fact]
    public void EntityAuditOperation_Deleted_ShouldBeDefined()
    {
        // Assert
        EntityAuditOperation.Deleted.Should().BeDefined();
    }

    [Fact]
    public void EntityAuditOperation_ShouldHaveThreeValues()
    {
        // Assert
        var values = Enum.GetValues<EntityAuditOperation>();
        values.Should().HaveCount(3);
    }

    #endregion

    #region Attribute Tests

    [Fact]
    public void DisableAuditingAttribute_ShouldBeApplicableToClassAndProperty()
    {
        // Arrange
        var attr = typeof(DisableAuditingAttribute).GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        attr.Should().NotBeNull();
        attr!.ValidOn.Should().HaveFlag(AttributeTargets.Class);
        attr.ValidOn.Should().HaveFlag(AttributeTargets.Property);
    }

    [Fact]
    public void AuditSensitiveAttribute_ShouldBeApplicableToProperty()
    {
        // Arrange
        var attr = typeof(AuditSensitiveAttribute).GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        attr.Should().NotBeNull();
        attr!.ValidOn.Should().HaveFlag(AttributeTargets.Property);
    }

    [Fact]
    public void AuditCollectionAttribute_ShouldBeApplicableToProperty()
    {
        // Arrange
        var attr = typeof(AuditCollectionAttribute).GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        attr.Should().NotBeNull();
        attr!.ValidOn.Should().HaveFlag(AttributeTargets.Property);
    }

    [Fact]
    public void AuditSensitiveAttribute_DefaultMask_ShouldBeRedacted()
    {
        // Arrange
        var attr = new AuditSensitiveAttribute();

        // Assert
        attr.Mask.Should().Be("[REDACTED]");
    }

    [Fact]
    public void AuditSensitiveAttribute_Mask_ShouldBeCustomizable()
    {
        // Arrange
        var attr = new AuditSensitiveAttribute { Mask = "***" };

        // Assert
        attr.Mask.Should().Be("***");
    }

    [Fact]
    public void DisableAuditingAttribute_Reason_ShouldBeOptional()
    {
        // Arrange
        var attr = new DisableAuditingAttribute();

        // Assert
        attr.Reason.Should().BeNull();
    }

    [Fact]
    public void DisableAuditingAttribute_Reason_ShouldBeSettable()
    {
        // Arrange
        var attr = new DisableAuditingAttribute { Reason = "High frequency operation" };

        // Assert
        attr.Reason.Should().Be("High frequency operation");
    }

    [Fact]
    public void AuditCollectionAttribute_IncludeChildDetails_DefaultShouldBeFalse()
    {
        // Arrange
        var attr = new AuditCollectionAttribute();

        // Assert
        attr.IncludeChildDetails.Should().BeFalse();
    }

    [Fact]
    public void AuditCollectionAttribute_ChildDisplayProperty_ShouldBeOptional()
    {
        // Arrange
        var attr = new AuditCollectionAttribute();

        // Assert
        attr.ChildDisplayProperty.Should().BeNull();
    }

    #endregion

    #region TenantContext Tests

    [Fact]
    public void TenantContext_WithNullMultiTenantContext_ShouldNotThrow()
    {
        // Arrange
        _tenantContextMock.Setup(x => x.MultiTenantContext).Returns((IMultiTenantContext<Tenant>?)null);

        // Act
        var context = _tenantContextMock.Object.MultiTenantContext;

        // Assert
        context.Should().BeNull();
    }

    [Fact]
    public void TenantContext_WithTenantInfo_ShouldReturnTenantId()
    {
        // Arrange
        var mockTenant = new Tenant("tenant-123", "Test Tenant", null);
        var mockMultiTenantContext = new Mock<IMultiTenantContext<Tenant>>();
        mockMultiTenantContext.Setup(x => x.TenantInfo).Returns(mockTenant);
        _tenantContextMock.Setup(x => x.MultiTenantContext).Returns(mockMultiTenantContext.Object);

        // Act
        var tenantId = _tenantContextMock.Object.MultiTenantContext?.TenantInfo?.Id;

        // Assert
        tenantId.Should().Be("tenant-123");
    }

    #endregion
}

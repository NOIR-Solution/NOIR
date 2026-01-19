namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for the TenantSetting entity.
/// Tests platform defaults, tenant overrides, value updates, and type-safe accessors.
/// </summary>
public class TenantSettingTests
{
    #region CreatePlatformDefault Tests

    [Fact]
    public void CreatePlatformDefault_ShouldCreateValidSetting()
    {
        // Arrange
        var key = "max_users";
        var value = "100";

        // Act
        var setting = TenantSetting.CreatePlatformDefault(key, value);

        // Assert
        setting.Should().NotBeNull();
        setting.Id.Should().NotBe(Guid.Empty);
        setting.Key.Should().Be("max_users");
        setting.Value.Should().Be(value);
        setting.TenantId.Should().BeNull();
        setting.DataType.Should().Be("string");
    }

    [Fact]
    public void CreatePlatformDefault_ShouldLowercaseAndTrimKey()
    {
        // Act
        var setting = TenantSetting.CreatePlatformDefault("  MAX_USERS  ", "100");

        // Assert
        setting.Key.Should().Be("max_users");
    }

    [Fact]
    public void CreatePlatformDefault_WithDataType_ShouldSetDataType()
    {
        // Act
        var setting = TenantSetting.CreatePlatformDefault("max_users", "100", "int");

        // Assert
        setting.DataType.Should().Be("int");
    }

    [Fact]
    public void CreatePlatformDefault_WithDescriptionAndCategory_ShouldSetThem()
    {
        // Act
        var setting = TenantSetting.CreatePlatformDefault(
            "max_users", "100", "int",
            "Maximum number of users allowed",
            "limits");

        // Assert
        setting.Description.Should().Be("Maximum number of users allowed");
        setting.Category.Should().Be("limits");
    }

    [Fact]
    public void CreatePlatformDefault_ShouldLowercaseCategory()
    {
        // Act
        var setting = TenantSetting.CreatePlatformDefault(
            "setting", "value", category: "  SECURITY  ");

        // Assert
        setting.Category.Should().Be("security");
    }

    [Fact]
    public void CreatePlatformDefault_IsPlatformDefault_ShouldBeTrue()
    {
        // Act
        var setting = TenantSetting.CreatePlatformDefault("key", "value");

        // Assert
        setting.IsPlatformDefault.Should().BeTrue();
        setting.IsTenantOverride.Should().BeFalse();
    }

    [Fact]
    public void CreatePlatformDefault_WithNullKey_ShouldThrowArgumentException()
    {
        // Act
        var act = () => TenantSetting.CreatePlatformDefault(null!, "value");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreatePlatformDefault_WithEmptyKey_ShouldThrowArgumentException()
    {
        // Act
        var act = () => TenantSetting.CreatePlatformDefault("", "value");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreatePlatformDefault_WithNullValue_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => TenantSetting.CreatePlatformDefault("key", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region CreateTenantOverride Tests

    [Fact]
    public void CreateTenantOverride_ShouldCreateValidSetting()
    {
        // Arrange
        var tenantId = "tenant-123";
        var key = "max_users";
        var value = "200";

        // Act
        var setting = TenantSetting.CreateTenantOverride(tenantId, key, value);

        // Assert
        setting.Should().NotBeNull();
        setting.TenantId.Should().Be(tenantId);
        setting.Key.Should().Be("max_users");
        setting.Value.Should().Be(value);
    }

    [Fact]
    public void CreateTenantOverride_IsTenantOverride_ShouldBeTrue()
    {
        // Act
        var setting = TenantSetting.CreateTenantOverride("tenant-123", "key", "value");

        // Assert
        setting.IsTenantOverride.Should().BeTrue();
        setting.IsPlatformDefault.Should().BeFalse();
    }

    [Fact]
    public void CreateTenantOverride_WithNullTenantId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => TenantSetting.CreateTenantOverride(null!, "key", "value");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateTenantOverride_WithEmptyTenantId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => TenantSetting.CreateTenantOverride("", "key", "value");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateTenantOverride_ShouldLowercaseKeyAndCategory()
    {
        // Act
        var setting = TenantSetting.CreateTenantOverride(
            "tenant-123", "MAX_USERS", "100", category: "LIMITS");

        // Assert
        setting.Key.Should().Be("max_users");
        setting.Category.Should().Be("limits");
    }

    #endregion

    #region UpdateValue Tests

    [Fact]
    public void UpdateValue_ShouldUpdateValue()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("max_users", "100");

        // Act
        setting.UpdateValue("200");

        // Assert
        setting.Value.Should().Be("200");
    }

    [Fact]
    public void UpdateValue_WithNullValue_ShouldThrowArgumentNullException()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("key", "value");

        // Act
        var act = () => setting.UpdateValue(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateValue_WithEmptyString_ShouldAccept()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("key", "value");

        // Act
        setting.UpdateValue("");

        // Assert
        setting.Value.Should().BeEmpty();
    }

    #endregion

    #region UpdateMetadata Tests

    [Fact]
    public void UpdateMetadata_ShouldUpdateDescriptionAndCategory()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("key", "value");

        // Act
        setting.UpdateMetadata("New description", "new_category");

        // Assert
        setting.Description.Should().Be("New description");
        setting.Category.Should().Be("new_category");
    }

    [Fact]
    public void UpdateMetadata_ShouldLowercaseCategory()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("key", "value");

        // Act
        setting.UpdateMetadata("Description", "  NEW_CATEGORY  ");

        // Assert
        setting.Category.Should().Be("new_category");
    }

    [Fact]
    public void UpdateMetadata_WithNullValues_ShouldClearThem()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("key", "value", description: "Desc", category: "cat");

        // Act
        setting.UpdateMetadata(null, null);

        // Assert
        setting.Description.Should().BeNull();
        setting.Category.Should().BeNull();
    }

    #endregion

    #region Type-Safe Value Accessor Tests

    [Fact]
    public void GetStringValue_ShouldReturnValue()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("key", "test value");

        // Act
        var result = setting.GetStringValue();

        // Assert
        result.Should().Be("test value");
    }

    [Fact]
    public void GetIntValue_ShouldParseInteger()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("max_users", "100", "int");

        // Act
        var result = setting.GetIntValue();

        // Assert
        result.Should().Be(100);
    }

    [Fact]
    public void GetIntValue_WithInvalidValue_ShouldThrowFormatException()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("key", "not-a-number");

        // Act
        var act = () => setting.GetIntValue();

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void GetBoolValue_WithTrue_ShouldReturnTrue()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("feature_enabled", "true", "bool");

        // Act
        var result = setting.GetBoolValue();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetBoolValue_WithFalse_ShouldReturnFalse()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("feature_enabled", "false", "bool");

        // Act
        var result = setting.GetBoolValue();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetDecimalValue_ShouldParseDecimal()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("price", "99.99", "decimal");

        // Act
        var result = setting.GetDecimalValue();

        // Assert
        result.Should().Be(99.99m);
    }

    [Fact]
    public void TryGetValue_WithValidInt_ShouldReturnTrue()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("count", "42");

        // Act
        var success = setting.TryGetValue<int>(out var result);

        // Assert
        success.Should().BeTrue();
        result.Should().Be(42);
    }

    [Fact]
    public void TryGetValue_WithInvalidInt_ShouldReturnFalse()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("key", "not-a-number");

        // Act
        var success = setting.TryGetValue<int>(out _);

        // Assert
        success.Should().BeFalse();
    }

    #endregion

    #region IAuditableEntity Tests

    [Fact]
    public void CreatePlatformDefault_ShouldInitializeAuditableProperties()
    {
        // Act
        var setting = TenantSetting.CreatePlatformDefault("key", "value");

        // Assert
        setting.IsDeleted.Should().BeFalse();
        setting.DeletedAt.Should().BeNull();
        setting.DeletedBy.Should().BeNull();
    }

    #endregion
}

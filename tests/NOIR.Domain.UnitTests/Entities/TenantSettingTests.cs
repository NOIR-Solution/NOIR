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
        setting.ShouldNotBeNull();
        setting.Id.ShouldNotBe(Guid.Empty);
        setting.Key.ShouldBe("max_users");
        setting.Value.ShouldBe(value);
        setting.TenantId.ShouldBeNull();
        setting.DataType.ShouldBe("string");
    }

    [Fact]
    public void CreatePlatformDefault_ShouldLowercaseAndTrimKey()
    {
        // Act
        var setting = TenantSetting.CreatePlatformDefault("  MAX_USERS  ", "100");

        // Assert
        setting.Key.ShouldBe("max_users");
    }

    [Fact]
    public void CreatePlatformDefault_WithDataType_ShouldSetDataType()
    {
        // Act
        var setting = TenantSetting.CreatePlatformDefault("max_users", "100", "int");

        // Assert
        setting.DataType.ShouldBe("int");
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
        setting.Description.ShouldBe("Maximum number of users allowed");
        setting.Category.ShouldBe("limits");
    }

    [Fact]
    public void CreatePlatformDefault_ShouldLowercaseCategory()
    {
        // Act
        var setting = TenantSetting.CreatePlatformDefault(
            "setting", "value", category: "  SECURITY  ");

        // Assert
        setting.Category.ShouldBe("security");
    }

    [Fact]
    public void CreatePlatformDefault_IsPlatformDefault_ShouldBeTrue()
    {
        // Act
        var setting = TenantSetting.CreatePlatformDefault("key", "value");

        // Assert
        setting.IsPlatformDefault.ShouldBeTrue();
        setting.IsTenantOverride.ShouldBeFalse();
    }

    [Fact]
    public void CreatePlatformDefault_WithNullKey_ShouldThrowArgumentException()
    {
        // Act
        var act = () => TenantSetting.CreatePlatformDefault(null!, "value");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void CreatePlatformDefault_WithEmptyKey_ShouldThrowArgumentException()
    {
        // Act
        var act = () => TenantSetting.CreatePlatformDefault("", "value");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void CreatePlatformDefault_WithNullValue_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => TenantSetting.CreatePlatformDefault("key", null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
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
        setting.ShouldNotBeNull();
        setting.TenantId.ShouldBe(tenantId);
        setting.Key.ShouldBe("max_users");
        setting.Value.ShouldBe(value);
    }

    [Fact]
    public void CreateTenantOverride_IsTenantOverride_ShouldBeTrue()
    {
        // Act
        var setting = TenantSetting.CreateTenantOverride("tenant-123", "key", "value");

        // Assert
        setting.IsTenantOverride.ShouldBeTrue();
        setting.IsPlatformDefault.ShouldBeFalse();
    }

    [Fact]
    public void CreateTenantOverride_WithNullTenantId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => TenantSetting.CreateTenantOverride(null!, "key", "value");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void CreateTenantOverride_WithEmptyTenantId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => TenantSetting.CreateTenantOverride("", "key", "value");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void CreateTenantOverride_ShouldLowercaseKeyAndCategory()
    {
        // Act
        var setting = TenantSetting.CreateTenantOverride(
            "tenant-123", "MAX_USERS", "100", category: "LIMITS");

        // Assert
        setting.Key.ShouldBe("max_users");
        setting.Category.ShouldBe("limits");
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
        setting.Value.ShouldBe("200");
    }

    [Fact]
    public void UpdateValue_WithNullValue_ShouldThrowArgumentNullException()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("key", "value");

        // Act
        var act = () => setting.UpdateValue(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void UpdateValue_WithEmptyString_ShouldAccept()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("key", "value");

        // Act
        setting.UpdateValue("");

        // Assert
        setting.Value.ShouldBeEmpty();
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
        setting.Description.ShouldBe("New description");
        setting.Category.ShouldBe("new_category");
    }

    [Fact]
    public void UpdateMetadata_ShouldLowercaseCategory()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("key", "value");

        // Act
        setting.UpdateMetadata("Description", "  NEW_CATEGORY  ");

        // Assert
        setting.Category.ShouldBe("new_category");
    }

    [Fact]
    public void UpdateMetadata_WithNullValues_ShouldClearThem()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("key", "value", description: "Desc", category: "cat");

        // Act
        setting.UpdateMetadata(null, null);

        // Assert
        setting.Description.ShouldBeNull();
        setting.Category.ShouldBeNull();
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
        result.ShouldBe("test value");
    }

    [Fact]
    public void GetIntValue_ShouldParseInteger()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("max_users", "100", "int");

        // Act
        var result = setting.GetIntValue();

        // Assert
        result.ShouldBe(100);
    }

    [Fact]
    public void GetIntValue_WithInvalidValue_ShouldThrowFormatException()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("key", "not-a-number");

        // Act
        var act = () => { setting.GetIntValue(); };

        // Assert
        Should.Throw<FormatException>(act);
    }

    [Fact]
    public void GetBoolValue_WithTrue_ShouldReturnTrue()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("feature_enabled", "true", "bool");

        // Act
        var result = setting.GetBoolValue();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void GetBoolValue_WithFalse_ShouldReturnFalse()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("feature_enabled", "false", "bool");

        // Act
        var result = setting.GetBoolValue();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void GetDecimalValue_ShouldParseDecimal()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("price", "99.99", "decimal");

        // Act
        var result = setting.GetDecimalValue();

        // Assert
        result.ShouldBe(99.99m);
    }

    [Fact]
    public void TryGetValue_WithValidInt_ShouldReturnTrue()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("count", "42");

        // Act
        var success = setting.TryGetValue<int>(out var result);

        // Assert
        success.ShouldBeTrue();
        result.ShouldBe(42);
    }

    [Fact]
    public void TryGetValue_WithInvalidInt_ShouldReturnFalse()
    {
        // Arrange
        var setting = TenantSetting.CreatePlatformDefault("key", "not-a-number");

        // Act
        var success = setting.TryGetValue<int>(out _);

        // Assert
        success.ShouldBeFalse();
    }

    #endregion

    #region IAuditableEntity Tests

    [Fact]
    public void CreatePlatformDefault_ShouldInitializeAuditableProperties()
    {
        // Act
        var setting = TenantSetting.CreatePlatformDefault("key", "value");

        // Assert
        setting.IsDeleted.ShouldBeFalse();
        setting.DeletedAt.ShouldBeNull();
        setting.DeletedBy.ShouldBeNull();
    }

    #endregion
}

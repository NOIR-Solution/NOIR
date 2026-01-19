namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for the TenantBranding entity.
/// Tests factory methods, logo updates, color updates, and customization detection.
/// </summary>
public class TenantBrandingTests
{
    #region CreateDefault Tests

    [Fact]
    public void CreateDefault_ShouldCreateBrandingWithNullValues()
    {
        // Arrange
        var tenantId = "tenant-123";

        // Act
        var branding = TenantBranding.CreateDefault(tenantId);

        // Assert
        branding.Should().NotBeNull();
        branding.Id.Should().NotBe(Guid.Empty);
        branding.TenantId.Should().Be(tenantId);
        branding.LogoUrl.Should().BeNull();
        branding.LogoDarkUrl.Should().BeNull();
        branding.FaviconUrl.Should().BeNull();
        branding.PrimaryColor.Should().BeNull();
        branding.SecondaryColor.Should().BeNull();
        branding.AccentColor.Should().BeNull();
    }

    [Fact]
    public void CreateDefault_WithNullTenantId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => TenantBranding.CreateDefault(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateDefault_WithEmptyTenantId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => TenantBranding.CreateDefault("");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Create Tests

    [Fact]
    public void Create_WithAllParameters_ShouldSetAllProperties()
    {
        // Arrange
        var tenantId = "tenant-123";
        var logoUrl = "/logos/light.png";
        var logoDarkUrl = "/logos/dark.png";
        var faviconUrl = "/favicon.ico";
        var primaryColor = "#3B82F6";
        var secondaryColor = "#6B7280";
        var accentColor = "#10B981";

        // Act
        var branding = TenantBranding.Create(
            tenantId, logoUrl, logoDarkUrl, faviconUrl,
            primaryColor, secondaryColor, accentColor);

        // Assert
        branding.TenantId.Should().Be(tenantId);
        branding.LogoUrl.Should().Be(logoUrl);
        branding.LogoDarkUrl.Should().Be(logoDarkUrl);
        branding.FaviconUrl.Should().Be(faviconUrl);
        branding.PrimaryColor.Should().Be(primaryColor);
        branding.SecondaryColor.Should().Be(secondaryColor);
        branding.AccentColor.Should().Be(accentColor);
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => TenantBranding.Create(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithOptionalNullValues_ShouldCreateBranding()
    {
        // Act
        var branding = TenantBranding.Create("tenant-123");

        // Assert
        branding.Should().NotBeNull();
        branding.LogoUrl.Should().BeNull();
        branding.PrimaryColor.Should().BeNull();
    }

    #endregion

    #region UpdateLogos Tests

    [Fact]
    public void UpdateLogos_ShouldSetAllLogoProperties()
    {
        // Arrange
        var branding = TenantBranding.CreateDefault("tenant-123");
        var logoUrl = "/logos/new-light.png";
        var logoDarkUrl = "/logos/new-dark.png";
        var faviconUrl = "/new-favicon.ico";

        // Act
        branding.UpdateLogos(logoUrl, logoDarkUrl, faviconUrl);

        // Assert
        branding.LogoUrl.Should().Be(logoUrl);
        branding.LogoDarkUrl.Should().Be(logoDarkUrl);
        branding.FaviconUrl.Should().Be(faviconUrl);
    }

    [Fact]
    public void UpdateLogos_WithNullValues_ShouldClearLogos()
    {
        // Arrange
        var branding = TenantBranding.Create("tenant-123", "/logo.png", "/logo-dark.png", "/favicon.ico");

        // Act
        branding.UpdateLogos(null, null, null);

        // Assert
        branding.LogoUrl.Should().BeNull();
        branding.LogoDarkUrl.Should().BeNull();
        branding.FaviconUrl.Should().BeNull();
    }

    [Fact]
    public void UpdateLogos_PartialUpdate_ShouldOnlyUpdateProvided()
    {
        // Arrange
        var branding = TenantBranding.Create("tenant-123", "/old-logo.png", "/old-dark.png", "/old-favicon.ico");

        // Act
        branding.UpdateLogos("/new-logo.png", null, "/old-favicon.ico");

        // Assert
        branding.LogoUrl.Should().Be("/new-logo.png");
        branding.LogoDarkUrl.Should().BeNull();
        branding.FaviconUrl.Should().Be("/old-favicon.ico");
    }

    #endregion

    #region UpdateColors Tests

    [Fact]
    public void UpdateColors_ShouldSetAllColorProperties()
    {
        // Arrange
        var branding = TenantBranding.CreateDefault("tenant-123");
        var primaryColor = "#FF5733";
        var secondaryColor = "#33FF57";
        var accentColor = "#5733FF";
        var backgroundColor = "#FFFFFF";
        var textColor = "#000000";

        // Act
        branding.UpdateColors(primaryColor, secondaryColor, accentColor, backgroundColor, textColor);

        // Assert
        branding.PrimaryColor.Should().Be(primaryColor);
        branding.SecondaryColor.Should().Be(secondaryColor);
        branding.AccentColor.Should().Be(accentColor);
        branding.BackgroundColor.Should().Be(backgroundColor);
        branding.TextColor.Should().Be(textColor);
    }

    [Fact]
    public void UpdateColors_WithNullValues_ShouldClearColors()
    {
        // Arrange
        var branding = TenantBranding.Create("tenant-123", primaryColor: "#3B82F6");

        // Act
        branding.UpdateColors(null, null, null);

        // Assert
        branding.PrimaryColor.Should().BeNull();
        branding.SecondaryColor.Should().BeNull();
        branding.AccentColor.Should().BeNull();
    }

    [Fact]
    public void UpdateColors_WithOptionalBackgroundAndText_ShouldSetThem()
    {
        // Arrange
        var branding = TenantBranding.CreateDefault("tenant-123");

        // Act
        branding.UpdateColors("#3B82F6", null, null, "#F3F4F6", "#1F2937");

        // Assert
        branding.BackgroundColor.Should().Be("#F3F4F6");
        branding.TextColor.Should().Be("#1F2937");
    }

    #endregion

    #region ResetToDefault Tests

    [Fact]
    public void ResetToDefault_ShouldClearAllBrandingProperties()
    {
        // Arrange
        var branding = TenantBranding.Create(
            "tenant-123",
            "/logo.png", "/logo-dark.png", "/favicon.ico",
            "#3B82F6", "#6B7280", "#10B981");
        branding.UpdateColors("#3B82F6", "#6B7280", "#10B981", "#FFFFFF", "#000000");

        // Act
        branding.ResetToDefault();

        // Assert
        branding.LogoUrl.Should().BeNull();
        branding.LogoDarkUrl.Should().BeNull();
        branding.FaviconUrl.Should().BeNull();
        branding.PrimaryColor.Should().BeNull();
        branding.SecondaryColor.Should().BeNull();
        branding.AccentColor.Should().BeNull();
        branding.BackgroundColor.Should().BeNull();
        branding.TextColor.Should().BeNull();
    }

    [Fact]
    public void ResetToDefault_ShouldPreserveTenantId()
    {
        // Arrange
        var tenantId = "tenant-123";
        var branding = TenantBranding.Create(tenantId, "/logo.png");

        // Act
        branding.ResetToDefault();

        // Assert
        branding.TenantId.Should().Be(tenantId);
    }

    #endregion

    #region HasCustomization Tests

    [Fact]
    public void HasCustomization_WithNoCustomization_ShouldReturnFalse()
    {
        // Arrange
        var branding = TenantBranding.CreateDefault("tenant-123");

        // Assert
        branding.HasCustomization.Should().BeFalse();
    }

    [Fact]
    public void HasCustomization_WithLogoUrl_ShouldReturnTrue()
    {
        // Arrange
        var branding = TenantBranding.CreateDefault("tenant-123");
        branding.UpdateLogos("/logo.png", null, null);

        // Assert
        branding.HasCustomization.Should().BeTrue();
    }

    [Fact]
    public void HasCustomization_WithLogoDarkUrl_ShouldReturnTrue()
    {
        // Arrange
        var branding = TenantBranding.CreateDefault("tenant-123");
        branding.UpdateLogos(null, "/logo-dark.png", null);

        // Assert
        branding.HasCustomization.Should().BeTrue();
    }

    [Fact]
    public void HasCustomization_WithFavicon_ShouldReturnTrue()
    {
        // Arrange
        var branding = TenantBranding.CreateDefault("tenant-123");
        branding.UpdateLogos(null, null, "/favicon.ico");

        // Assert
        branding.HasCustomization.Should().BeTrue();
    }

    [Fact]
    public void HasCustomization_WithPrimaryColor_ShouldReturnTrue()
    {
        // Arrange
        var branding = TenantBranding.CreateDefault("tenant-123");
        branding.UpdateColors("#3B82F6", null, null);

        // Assert
        branding.HasCustomization.Should().BeTrue();
    }

    [Fact]
    public void HasCustomization_WithSecondaryColor_ShouldReturnTrue()
    {
        // Arrange
        var branding = TenantBranding.CreateDefault("tenant-123");
        branding.UpdateColors(null, "#6B7280", null);

        // Assert
        branding.HasCustomization.Should().BeTrue();
    }

    [Fact]
    public void HasCustomization_WithAccentColor_ShouldReturnTrue()
    {
        // Arrange
        var branding = TenantBranding.CreateDefault("tenant-123");
        branding.UpdateColors(null, null, "#10B981");

        // Assert
        branding.HasCustomization.Should().BeTrue();
    }

    [Fact]
    public void HasCustomization_AfterReset_ShouldReturnFalse()
    {
        // Arrange
        var branding = TenantBranding.Create("tenant-123", "/logo.png", primaryColor: "#3B82F6");

        // Act
        branding.ResetToDefault();

        // Assert
        branding.HasCustomization.Should().BeFalse();
    }

    #endregion

    #region IAuditableEntity Tests

    [Fact]
    public void Create_ShouldInitializeAuditableProperties()
    {
        // Act
        var branding = TenantBranding.CreateDefault("tenant-123");

        // Assert
        branding.IsDeleted.Should().BeFalse();
        branding.DeletedAt.Should().BeNull();
        branding.DeletedBy.Should().BeNull();
    }

    #endregion
}

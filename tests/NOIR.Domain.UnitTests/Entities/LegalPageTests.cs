using NOIR.Domain.Events.Platform;

namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for the LegalPage entity.
/// Tests platform default and tenant override factory methods,
/// Update method with version incrementing, Activate/Deactivate workflow,
/// and domain event raising.
/// </summary>
public class LegalPageTests
{
    private const string TestTenantId = "test-tenant";

    #region CreatePlatformDefault Tests

    [Fact]
    public void CreatePlatformDefault_ShouldCreateValidPlatformPage()
    {
        // Act
        var page = LegalPage.CreatePlatformDefault(
            "terms-of-service",
            "Terms of Service",
            "<h1>Terms</h1><p>Content...</p>");

        // Assert
        page.ShouldNotBeNull();
        page.Id.ShouldNotBe(Guid.Empty);
        page.Slug.ShouldBe("terms-of-service");
        page.Title.ShouldBe("Terms of Service");
        page.HtmlContent.ShouldBe("<h1>Terms</h1><p>Content...</p>");
        page.TenantId.ShouldBeNull();
    }

    [Fact]
    public void CreatePlatformDefault_ShouldSetDefaultValues()
    {
        // Act
        var page = LegalPage.CreatePlatformDefault("privacy", "Privacy", "<p>Privacy</p>");

        // Assert
        page.IsActive.ShouldBeTrue();
        page.Version.ShouldBe(1);
        page.AllowIndexing.ShouldBeTrue();
        page.MetaTitle.ShouldBeNull();
        page.MetaDescription.ShouldBeNull();
        page.CanonicalUrl.ShouldBeNull();
    }

    [Fact]
    public void CreatePlatformDefault_WithOptionalSeoParameters_ShouldSetAll()
    {
        // Act
        var page = LegalPage.CreatePlatformDefault(
            "terms",
            "Terms of Service",
            "<p>Terms</p>",
            metaTitle: "Our Terms",
            metaDescription: "Read our terms of service",
            canonicalUrl: "https://example.com/terms",
            allowIndexing: false);

        // Assert
        page.MetaTitle.ShouldBe("Our Terms");
        page.MetaDescription.ShouldBe("Read our terms of service");
        page.CanonicalUrl.ShouldBe("https://example.com/terms");
        page.AllowIndexing.ShouldBeFalse();
    }

    [Fact]
    public void CreatePlatformDefault_ShouldSetLastModified()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var page = LegalPage.CreatePlatformDefault("slug", "Title", "<p>Content</p>");

        // Assert
        page.LastModified.ShouldBeGreaterThanOrEqualTo(before);
    }

    [Fact]
    public void CreatePlatformDefault_ShouldRaiseLegalPageCreatedEvent()
    {
        // Act
        var page = LegalPage.CreatePlatformDefault("terms", "Terms", "<p>Terms</p>");

        // Assert
        var __evt = page.DomainEvents.ShouldHaveSingleItem()

            .ShouldBeOfType<LegalPageCreatedEvent>();

        __evt.PageId.ShouldBe(page.Id);

        __evt.PageType.ShouldBe("terms");

        __evt.TenantId.ShouldBe((string?)null);
    }

    [Fact]
    public void CreatePlatformDefault_ShouldHaveNullTenantId()
    {
        // Act
        var page = LegalPage.CreatePlatformDefault("privacy", "Privacy", "<p>Privacy</p>");

        // Assert
        page.TenantId.ShouldBeNull();
    }

    #endregion

    #region CreateTenantOverride Tests

    [Fact]
    public void CreateTenantOverride_ShouldCreateValidTenantPage()
    {
        // Act
        var page = LegalPage.CreateTenantOverride(
            TestTenantId, "terms-of-service", "Custom Terms", "<p>Custom Terms</p>");

        // Assert
        page.ShouldNotBeNull();
        page.Id.ShouldNotBe(Guid.Empty);
        page.Slug.ShouldBe("terms-of-service");
        page.Title.ShouldBe("Custom Terms");
        page.HtmlContent.ShouldBe("<p>Custom Terms</p>");
        page.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void CreateTenantOverride_ShouldSetSameDefaults()
    {
        // Act
        var page = LegalPage.CreateTenantOverride(TestTenantId, "privacy", "Privacy", "<p>Privacy</p>");

        // Assert
        page.IsActive.ShouldBeTrue();
        page.Version.ShouldBe(1);
        page.AllowIndexing.ShouldBeTrue();
    }

    [Fact]
    public void CreateTenantOverride_WithOptionalSeoParameters_ShouldSetAll()
    {
        // Act
        var page = LegalPage.CreateTenantOverride(
            TestTenantId, "terms", "Terms", "<p>Terms</p>",
            metaTitle: "Tenant Terms",
            metaDescription: "Tenant-specific terms",
            canonicalUrl: "https://tenant.example.com/terms",
            allowIndexing: false);

        // Assert
        page.MetaTitle.ShouldBe("Tenant Terms");
        page.MetaDescription.ShouldBe("Tenant-specific terms");
        page.CanonicalUrl.ShouldBe("https://tenant.example.com/terms");
        page.AllowIndexing.ShouldBeFalse();
    }

    [Fact]
    public void CreateTenantOverride_ShouldRaiseLegalPageCreatedEventWithTenantId()
    {
        // Act
        var page = LegalPage.CreateTenantOverride(TestTenantId, "terms", "Terms", "<p>Terms</p>");

        // Assert
        var __evt = page.DomainEvents.ShouldHaveSingleItem()

            .ShouldBeOfType<LegalPageCreatedEvent>();

        __evt.PageId.ShouldBe(page.Id);

        __evt.PageType.ShouldBe("terms");

        __evt.TenantId.ShouldBe(TestTenantId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateTenantOverride_WithNullOrWhiteSpaceTenantId_ShouldThrow(string? tenantId)
    {
        // Act
        var act = () => LegalPage.CreateTenantOverride(tenantId!, "terms", "Terms", "<p>Terms</p>");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldUpdateContentFields()
    {
        // Arrange
        var page = LegalPage.CreatePlatformDefault("terms", "Terms", "<p>Old</p>");
        page.ClearDomainEvents();

        // Act
        page.Update("Updated Terms", "<p>New content</p>",
            metaTitle: "New Meta", metaDescription: "New Desc",
            canonicalUrl: "https://new.url", allowIndexing: false);

        // Assert
        page.Title.ShouldBe("Updated Terms");
        page.HtmlContent.ShouldBe("<p>New content</p>");
        page.MetaTitle.ShouldBe("New Meta");
        page.MetaDescription.ShouldBe("New Desc");
        page.CanonicalUrl.ShouldBe("https://new.url");
        page.AllowIndexing.ShouldBeFalse();
    }

    [Fact]
    public void Update_ShouldIncrementVersion()
    {
        // Arrange
        var page = LegalPage.CreatePlatformDefault("terms", "Terms", "<p>Content</p>");
        page.Version.ShouldBe(1);

        // Act
        page.Update("Terms v2", "<p>Version 2</p>");

        // Assert
        page.Version.ShouldBe(2);
    }

    [Fact]
    public void Update_CalledMultipleTimes_ShouldIncrementVersionEachTime()
    {
        // Arrange
        var page = LegalPage.CreatePlatformDefault("terms", "Terms", "<p>V1</p>");

        // Act
        page.Update("V2", "<p>V2</p>");
        page.Update("V3", "<p>V3</p>");
        page.Update("V4", "<p>V4</p>");

        // Assert
        page.Version.ShouldBe(4);
    }

    [Fact]
    public void Update_ShouldUpdateLastModified()
    {
        // Arrange
        var page = LegalPage.CreatePlatformDefault("terms", "Terms", "<p>V1</p>");
        var beforeUpdate = DateTimeOffset.UtcNow;

        // Act
        page.Update("V2", "<p>V2</p>");

        // Assert
        page.LastModified.ShouldBeGreaterThanOrEqualTo(beforeUpdate);
    }

    [Fact]
    public void Update_ShouldRaiseLegalPageUpdatedEvent()
    {
        // Arrange
        var page = LegalPage.CreatePlatformDefault("terms", "Terms", "<p>V1</p>");
        page.ClearDomainEvents();

        // Act
        page.Update("V2", "<p>V2</p>");

        // Assert
        var __evt = page.DomainEvents.ShouldHaveSingleItem()

            .ShouldBeOfType<LegalPageUpdatedEvent>();

        __evt.PageId.ShouldBe(page.Id);

        __evt.PageType.ShouldBe("terms");

        __evt.NewVersion.ShouldBe(2);
    }

    [Fact]
    public void Update_WithNullOptionalParameters_ShouldClearThem()
    {
        // Arrange
        var page = LegalPage.CreatePlatformDefault("terms", "Terms", "<p>Content</p>",
            metaTitle: "Title", metaDescription: "Desc", canonicalUrl: "https://url");

        // Act
        page.Update("Terms", "<p>Updated</p>");

        // Assert
        page.MetaTitle.ShouldBeNull();
        page.MetaDescription.ShouldBeNull();
        page.CanonicalUrl.ShouldBeNull();
        page.AllowIndexing.ShouldBeTrue(); // default
    }

    #endregion

    #region Activate/Deactivate Tests

    [Fact]
    public void Deactivate_ActivePage_ShouldSetInactive()
    {
        // Arrange
        var page = LegalPage.CreatePlatformDefault("terms", "Terms", "<p>Content</p>");
        page.IsActive.ShouldBeTrue();
        page.ClearDomainEvents();

        // Act
        page.Deactivate();

        // Assert
        page.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Deactivate_ShouldRaiseLegalPageDeactivatedEvent()
    {
        // Arrange
        var page = LegalPage.CreatePlatformDefault("terms", "Terms", "<p>Content</p>");
        page.ClearDomainEvents();

        // Act
        page.Deactivate();

        // Assert
        var __evt = page.DomainEvents.ShouldHaveSingleItem()

            .ShouldBeOfType<LegalPageDeactivatedEvent>();

        __evt.PageId.ShouldBe(page.Id);

        __evt.PageType.ShouldBe("terms");
    }

    [Fact]
    public void Deactivate_AlreadyInactivePage_ShouldNotRaiseEvent()
    {
        // Arrange
        var page = LegalPage.CreatePlatformDefault("terms", "Terms", "<p>Content</p>");
        page.Deactivate();
        page.ClearDomainEvents();

        // Act
        page.Deactivate();

        // Assert - idempotent
        page.IsActive.ShouldBeFalse();
        page.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Activate_InactivePage_ShouldSetActive()
    {
        // Arrange
        var page = LegalPage.CreatePlatformDefault("terms", "Terms", "<p>Content</p>");
        page.Deactivate();
        page.ClearDomainEvents();

        // Act
        page.Activate();

        // Assert
        page.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Activate_ShouldRaiseLegalPageActivatedEvent()
    {
        // Arrange
        var page = LegalPage.CreatePlatformDefault("terms", "Terms", "<p>Content</p>");
        page.Deactivate();
        page.ClearDomainEvents();

        // Act
        page.Activate();

        // Assert
        var __evt = page.DomainEvents.ShouldHaveSingleItem()

            .ShouldBeOfType<LegalPageActivatedEvent>();

        __evt.PageId.ShouldBe(page.Id);

        __evt.PageType.ShouldBe("terms");
    }

    [Fact]
    public void Activate_AlreadyActivePage_ShouldNotRaiseEvent()
    {
        // Arrange
        var page = LegalPage.CreatePlatformDefault("terms", "Terms", "<p>Content</p>");
        page.ClearDomainEvents();

        // Act
        page.Activate();

        // Assert - idempotent
        page.IsActive.ShouldBeTrue();
        page.DomainEvents.ShouldBeEmpty();
    }

    #endregion

    #region ResetVersionForSeeding Tests

    [Fact]
    public void ResetVersionForSeeding_ShouldResetToOne()
    {
        // Arrange
        var page = LegalPage.CreatePlatformDefault("terms", "Terms", "<p>V1</p>");
        page.Update("V2", "<p>V2</p>");
        page.Update("V3", "<p>V3</p>");
        page.Version.ShouldBe(3);

        // Act
        page.ResetVersionForSeeding();

        // Assert
        page.Version.ShouldBe(1);
    }

    #endregion

    #region Workflow Tests

    [Fact]
    public void FullWorkflow_CreateUpdateDeactivateReactivate()
    {
        // Create
        var page = LegalPage.CreatePlatformDefault("privacy", "Privacy", "<p>V1</p>");
        page.IsActive.ShouldBeTrue();
        page.Version.ShouldBe(1);

        // Update
        page.Update("Privacy v2", "<p>V2</p>", metaTitle: "Privacy Policy");
        page.Version.ShouldBe(2);
        page.Title.ShouldBe("Privacy v2");

        // Deactivate
        page.Deactivate();
        page.IsActive.ShouldBeFalse();

        // Reactivate
        page.Activate();
        page.IsActive.ShouldBeTrue();

        // Update again
        page.Update("Privacy v3", "<p>V3</p>");
        page.Version.ShouldBe(3);
    }

    [Fact]
    public void PlatformAndTenantOverride_ShouldHaveDifferentTenantIds()
    {
        // Arrange
        var platformPage = LegalPage.CreatePlatformDefault("terms", "Terms", "<p>Platform</p>");
        var tenantPage = LegalPage.CreateTenantOverride(TestTenantId, "terms", "Custom Terms", "<p>Tenant</p>");

        // Assert
        platformPage.TenantId.ShouldBeNull();
        tenantPage.TenantId.ShouldBe(TestTenantId);
        platformPage.Slug.ShouldBe(tenantPage.Slug);
    }

    #endregion
}

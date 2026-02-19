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
        page.Should().NotBeNull();
        page.Id.Should().NotBe(Guid.Empty);
        page.Slug.Should().Be("terms-of-service");
        page.Title.Should().Be("Terms of Service");
        page.HtmlContent.Should().Be("<h1>Terms</h1><p>Content...</p>");
        page.TenantId.Should().BeNull();
    }

    [Fact]
    public void CreatePlatformDefault_ShouldSetDefaultValues()
    {
        // Act
        var page = LegalPage.CreatePlatformDefault("privacy", "Privacy", "<p>Privacy</p>");

        // Assert
        page.IsActive.Should().BeTrue();
        page.Version.Should().Be(1);
        page.AllowIndexing.Should().BeTrue();
        page.MetaTitle.Should().BeNull();
        page.MetaDescription.Should().BeNull();
        page.CanonicalUrl.Should().BeNull();
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
        page.MetaTitle.Should().Be("Our Terms");
        page.MetaDescription.Should().Be("Read our terms of service");
        page.CanonicalUrl.Should().Be("https://example.com/terms");
        page.AllowIndexing.Should().BeFalse();
    }

    [Fact]
    public void CreatePlatformDefault_ShouldSetLastModified()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var page = LegalPage.CreatePlatformDefault("slug", "Title", "<p>Content</p>");

        // Assert
        page.LastModified.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void CreatePlatformDefault_ShouldRaiseLegalPageCreatedEvent()
    {
        // Act
        var page = LegalPage.CreatePlatformDefault("terms", "Terms", "<p>Terms</p>");

        // Assert
        page.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<LegalPageCreatedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                PageId = page.Id,
                PageType = "terms",
                TenantId = (string?)null
            });
    }

    [Fact]
    public void CreatePlatformDefault_ShouldHaveNullTenantId()
    {
        // Act
        var page = LegalPage.CreatePlatformDefault("privacy", "Privacy", "<p>Privacy</p>");

        // Assert
        page.TenantId.Should().BeNull();
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
        page.Should().NotBeNull();
        page.Id.Should().NotBe(Guid.Empty);
        page.Slug.Should().Be("terms-of-service");
        page.Title.Should().Be("Custom Terms");
        page.HtmlContent.Should().Be("<p>Custom Terms</p>");
        page.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void CreateTenantOverride_ShouldSetSameDefaults()
    {
        // Act
        var page = LegalPage.CreateTenantOverride(TestTenantId, "privacy", "Privacy", "<p>Privacy</p>");

        // Assert
        page.IsActive.Should().BeTrue();
        page.Version.Should().Be(1);
        page.AllowIndexing.Should().BeTrue();
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
        page.MetaTitle.Should().Be("Tenant Terms");
        page.MetaDescription.Should().Be("Tenant-specific terms");
        page.CanonicalUrl.Should().Be("https://tenant.example.com/terms");
        page.AllowIndexing.Should().BeFalse();
    }

    [Fact]
    public void CreateTenantOverride_ShouldRaiseLegalPageCreatedEventWithTenantId()
    {
        // Act
        var page = LegalPage.CreateTenantOverride(TestTenantId, "terms", "Terms", "<p>Terms</p>");

        // Assert
        page.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<LegalPageCreatedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                PageId = page.Id,
                PageType = "terms",
                TenantId = TestTenantId
            });
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
        act.Should().Throw<ArgumentException>();
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
        page.Title.Should().Be("Updated Terms");
        page.HtmlContent.Should().Be("<p>New content</p>");
        page.MetaTitle.Should().Be("New Meta");
        page.MetaDescription.Should().Be("New Desc");
        page.CanonicalUrl.Should().Be("https://new.url");
        page.AllowIndexing.Should().BeFalse();
    }

    [Fact]
    public void Update_ShouldIncrementVersion()
    {
        // Arrange
        var page = LegalPage.CreatePlatformDefault("terms", "Terms", "<p>Content</p>");
        page.Version.Should().Be(1);

        // Act
        page.Update("Terms v2", "<p>Version 2</p>");

        // Assert
        page.Version.Should().Be(2);
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
        page.Version.Should().Be(4);
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
        page.LastModified.Should().BeOnOrAfter(beforeUpdate);
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
        page.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<LegalPageUpdatedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                PageId = page.Id,
                PageType = "terms",
                NewVersion = 2
            });
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
        page.MetaTitle.Should().BeNull();
        page.MetaDescription.Should().BeNull();
        page.CanonicalUrl.Should().BeNull();
        page.AllowIndexing.Should().BeTrue(); // default
    }

    #endregion

    #region Activate/Deactivate Tests

    [Fact]
    public void Deactivate_ActivePage_ShouldSetInactive()
    {
        // Arrange
        var page = LegalPage.CreatePlatformDefault("terms", "Terms", "<p>Content</p>");
        page.IsActive.Should().BeTrue();
        page.ClearDomainEvents();

        // Act
        page.Deactivate();

        // Assert
        page.IsActive.Should().BeFalse();
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
        page.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<LegalPageDeactivatedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                PageId = page.Id,
                PageType = "terms"
            });
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
        page.IsActive.Should().BeFalse();
        page.DomainEvents.Should().BeEmpty();
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
        page.IsActive.Should().BeTrue();
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
        page.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<LegalPageActivatedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                PageId = page.Id,
                PageType = "terms"
            });
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
        page.IsActive.Should().BeTrue();
        page.DomainEvents.Should().BeEmpty();
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
        page.Version.Should().Be(3);

        // Act
        page.ResetVersionForSeeding();

        // Assert
        page.Version.Should().Be(1);
    }

    #endregion

    #region Workflow Tests

    [Fact]
    public void FullWorkflow_CreateUpdateDeactivateReactivate()
    {
        // Create
        var page = LegalPage.CreatePlatformDefault("privacy", "Privacy", "<p>V1</p>");
        page.IsActive.Should().BeTrue();
        page.Version.Should().Be(1);

        // Update
        page.Update("Privacy v2", "<p>V2</p>", metaTitle: "Privacy Policy");
        page.Version.Should().Be(2);
        page.Title.Should().Be("Privacy v2");

        // Deactivate
        page.Deactivate();
        page.IsActive.Should().BeFalse();

        // Reactivate
        page.Activate();
        page.IsActive.Should().BeTrue();

        // Update again
        page.Update("Privacy v3", "<p>V3</p>");
        page.Version.Should().Be(3);
    }

    [Fact]
    public void PlatformAndTenantOverride_ShouldHaveDifferentTenantIds()
    {
        // Arrange
        var platformPage = LegalPage.CreatePlatformDefault("terms", "Terms", "<p>Platform</p>");
        var tenantPage = LegalPage.CreateTenantOverride(TestTenantId, "terms", "Custom Terms", "<p>Tenant</p>");

        // Assert
        platformPage.TenantId.Should().BeNull();
        tenantPage.TenantId.Should().Be(TestTenantId);
        platformPage.Slug.Should().Be(tenantPage.Slug);
    }

    #endregion
}

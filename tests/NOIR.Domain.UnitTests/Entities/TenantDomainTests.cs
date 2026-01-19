namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for the TenantDomain entity.
/// Tests subdomain creation, custom domain creation, verification, and primary domain management.
/// </summary>
public class TenantDomainTests
{
    #region CreateSubdomain Tests

    [Fact]
    public void CreateSubdomain_ShouldCreateValidDomain()
    {
        // Arrange
        var tenantId = "tenant-123";
        var subdomain = "acme";
        var platformDomain = "noir.app";

        // Act
        var domain = TenantDomain.CreateSubdomain(tenantId, subdomain, platformDomain);

        // Assert
        domain.Should().NotBeNull();
        domain.Id.Should().NotBe(Guid.Empty);
        domain.TenantId.Should().Be(tenantId);
        domain.Domain.Should().Be("acme.noir.app");
        domain.IsCustomDomain.Should().BeFalse();
        domain.IsVerified.Should().BeTrue(); // Subdomains auto-verified
        domain.VerifiedAt.Should().NotBeNull();
        domain.VerificationToken.Should().BeNull();
    }

    [Fact]
    public void CreateSubdomain_ShouldLowercaseAndTrimDomain()
    {
        // Act
        var domain = TenantDomain.CreateSubdomain("tenant-123", "  ACME  ", "  NOIR.APP  ");

        // Assert
        domain.Domain.Should().Be("acme.noir.app");
    }

    [Fact]
    public void CreateSubdomain_WithPrimaryTrue_ShouldBePrimary()
    {
        // Act
        var domain = TenantDomain.CreateSubdomain("tenant-123", "acme", "noir.app", isPrimary: true);

        // Assert
        domain.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void CreateSubdomain_WithPrimaryFalse_ShouldNotBePrimary()
    {
        // Act
        var domain = TenantDomain.CreateSubdomain("tenant-123", "acme", "noir.app", isPrimary: false);

        // Assert
        domain.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public void CreateSubdomain_WithNullTenantId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => TenantDomain.CreateSubdomain(null!, "acme", "noir.app");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateSubdomain_WithEmptySubdomain_ShouldThrowArgumentException()
    {
        // Act
        var act = () => TenantDomain.CreateSubdomain("tenant-123", "", "noir.app");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateSubdomain_WithEmptyPlatformDomain_ShouldThrowArgumentException()
    {
        // Act
        var act = () => TenantDomain.CreateSubdomain("tenant-123", "acme", "");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region CreateCustomDomain Tests

    [Fact]
    public void CreateCustomDomain_ShouldCreateUnverifiedDomain()
    {
        // Arrange
        var tenantId = "tenant-123";
        var domainName = "crm.acme.com";

        // Act
        var domain = TenantDomain.CreateCustomDomain(tenantId, domainName);

        // Assert
        domain.Should().NotBeNull();
        domain.Id.Should().NotBe(Guid.Empty);
        domain.TenantId.Should().Be(tenantId);
        domain.Domain.Should().Be("crm.acme.com");
        domain.IsCustomDomain.Should().BeTrue();
        domain.IsVerified.Should().BeFalse();
        domain.VerifiedAt.Should().BeNull();
        domain.VerificationToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateCustomDomain_ShouldLowercaseAndTrimDomain()
    {
        // Act
        var domain = TenantDomain.CreateCustomDomain("tenant-123", "  CRM.ACME.COM  ");

        // Assert
        domain.Domain.Should().Be("crm.acme.com");
    }

    [Fact]
    public void CreateCustomDomain_ShouldGenerateVerificationToken()
    {
        // Act
        var domain = TenantDomain.CreateCustomDomain("tenant-123", "crm.acme.com");

        // Assert
        domain.VerificationToken.Should().StartWith("noir-verify-");
        domain.VerificationToken.Should().HaveLength("noir-verify-".Length + 32); // noir-verify- + 32 hex chars
    }

    [Fact]
    public void CreateCustomDomain_DefaultNotPrimary()
    {
        // Act
        var domain = TenantDomain.CreateCustomDomain("tenant-123", "crm.acme.com");

        // Assert
        domain.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public void CreateCustomDomain_CanBePrimary()
    {
        // Act
        var domain = TenantDomain.CreateCustomDomain("tenant-123", "crm.acme.com", isPrimary: true);

        // Assert
        domain.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void CreateCustomDomain_WithNullTenantId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => TenantDomain.CreateCustomDomain(null!, "crm.acme.com");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateCustomDomain_WithEmptyDomain_ShouldThrowArgumentException()
    {
        // Act
        var act = () => TenantDomain.CreateCustomDomain("tenant-123", "");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region MarkAsVerified Tests

    [Fact]
    public void MarkAsVerified_ShouldSetVerifiedAndClearToken()
    {
        // Arrange
        var domain = TenantDomain.CreateCustomDomain("tenant-123", "crm.acme.com");
        var beforeVerify = DateTimeOffset.UtcNow;

        // Act
        domain.MarkAsVerified();

        // Assert
        domain.IsVerified.Should().BeTrue();
        domain.VerifiedAt.Should().NotBeNull();
        domain.VerifiedAt.Should().BeOnOrAfter(beforeVerify);
        domain.VerificationToken.Should().BeNull();
    }

    [Fact]
    public void MarkAsVerified_OnAlreadyVerifiedSubdomain_ShouldUpdateVerifiedAt()
    {
        // Arrange
        var domain = TenantDomain.CreateSubdomain("tenant-123", "acme", "noir.app");
        var originalVerifiedAt = domain.VerifiedAt;
        Thread.Sleep(10);

        // Act
        domain.MarkAsVerified();

        // Assert
        domain.IsVerified.Should().BeTrue();
        domain.VerifiedAt.Should().BeOnOrAfter(originalVerifiedAt!.Value);
    }

    #endregion

    #region SetAsPrimary Tests

    [Fact]
    public void SetAsPrimary_ShouldSetIsPrimaryToTrue()
    {
        // Arrange
        var domain = TenantDomain.CreateSubdomain("tenant-123", "acme", "noir.app", isPrimary: false);

        // Act
        domain.SetAsPrimary();

        // Assert
        domain.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void SetAsPrimary_WhenAlreadyPrimary_ShouldRemainPrimary()
    {
        // Arrange
        var domain = TenantDomain.CreateSubdomain("tenant-123", "acme", "noir.app", isPrimary: true);

        // Act
        domain.SetAsPrimary();

        // Assert
        domain.IsPrimary.Should().BeTrue();
    }

    #endregion

    #region ClearPrimary Tests

    [Fact]
    public void ClearPrimary_ShouldSetIsPrimaryToFalse()
    {
        // Arrange
        var domain = TenantDomain.CreateSubdomain("tenant-123", "acme", "noir.app", isPrimary: true);

        // Act
        domain.ClearPrimary();

        // Assert
        domain.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public void ClearPrimary_WhenNotPrimary_ShouldRemainNotPrimary()
    {
        // Arrange
        var domain = TenantDomain.CreateSubdomain("tenant-123", "acme", "noir.app", isPrimary: false);

        // Act
        domain.ClearPrimary();

        // Assert
        domain.IsPrimary.Should().BeFalse();
    }

    #endregion

    #region RegenerateVerificationToken Tests

    [Fact]
    public void RegenerateVerificationToken_OnUnverifiedCustomDomain_ShouldGenerateNewToken()
    {
        // Arrange
        var domain = TenantDomain.CreateCustomDomain("tenant-123", "crm.acme.com");
        var originalToken = domain.VerificationToken;

        // Act
        domain.RegenerateVerificationToken();

        // Assert
        domain.VerificationToken.Should().NotBeNullOrEmpty();
        domain.VerificationToken.Should().NotBe(originalToken);
    }

    [Fact]
    public void RegenerateVerificationToken_OnVerifiedDomain_ShouldNotChangeToken()
    {
        // Arrange
        var domain = TenantDomain.CreateCustomDomain("tenant-123", "crm.acme.com");
        domain.MarkAsVerified();

        // Act
        domain.RegenerateVerificationToken();

        // Assert
        domain.VerificationToken.Should().BeNull();
    }

    [Fact]
    public void RegenerateVerificationToken_OnSubdomain_ShouldNotGenerateToken()
    {
        // Arrange - Subdomains are not custom domains
        var domain = TenantDomain.CreateSubdomain("tenant-123", "acme", "noir.app");

        // Act
        domain.RegenerateVerificationToken();

        // Assert
        domain.VerificationToken.Should().BeNull();
    }

    #endregion

    #region Domain Type Tests

    [Fact]
    public void Subdomain_IsCustomDomain_ShouldBeFalse()
    {
        // Act
        var domain = TenantDomain.CreateSubdomain("tenant-123", "acme", "noir.app");

        // Assert
        domain.IsCustomDomain.Should().BeFalse();
    }

    [Fact]
    public void CustomDomain_IsCustomDomain_ShouldBeTrue()
    {
        // Act
        var domain = TenantDomain.CreateCustomDomain("tenant-123", "crm.acme.com");

        // Assert
        domain.IsCustomDomain.Should().BeTrue();
    }

    #endregion

    #region IAuditableEntity Tests

    [Fact]
    public void CreateSubdomain_ShouldInitializeAuditableProperties()
    {
        // Act
        var domain = TenantDomain.CreateSubdomain("tenant-123", "acme", "noir.app");

        // Assert
        domain.IsDeleted.Should().BeFalse();
        domain.DeletedAt.Should().BeNull();
        domain.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void CreateCustomDomain_ShouldInitializeAuditableProperties()
    {
        // Act
        var domain = TenantDomain.CreateCustomDomain("tenant-123", "crm.acme.com");

        // Assert
        domain.IsDeleted.Should().BeFalse();
        domain.DeletedAt.Should().BeNull();
        domain.DeletedBy.Should().BeNull();
    }

    #endregion
}

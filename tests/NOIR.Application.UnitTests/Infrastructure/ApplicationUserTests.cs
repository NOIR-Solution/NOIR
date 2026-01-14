namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for ApplicationUser entity.
/// Tests property accessors and computed properties.
/// </summary>
public class ApplicationUserTests
{
    #region Constructor and Default Values Tests

    [Fact]
    public void NewUser_ShouldHaveDefaultValues()
    {
        // Act
        var user = new ApplicationUser();

        // Assert
        user.IsActive.Should().BeTrue();
        user.IsDeleted.Should().BeFalse();
        user.FirstName.Should().BeNull();
        user.LastName.Should().BeNull();
        user.RefreshToken.Should().BeNull();
        user.RefreshTokenExpiryTime.Should().BeNull();
    }

    [Fact]
    public void NewUser_ShouldHaveNullAuditFields()
    {
        // Act
        var user = new ApplicationUser();

        // Assert
        user.CreatedBy.Should().BeNull();
        user.ModifiedAt.Should().BeNull();
        user.ModifiedBy.Should().BeNull();
        user.DeletedAt.Should().BeNull();
        user.DeletedBy.Should().BeNull();
    }

    #endregion

    #region FirstName and LastName Tests

    [Fact]
    public void FirstName_ShouldBeSettable()
    {
        // Arrange
        var user = new ApplicationUser();

        // Act
        user.FirstName = "John";

        // Assert
        user.FirstName.Should().Be("John");
    }

    [Fact]
    public void LastName_ShouldBeSettable()
    {
        // Arrange
        var user = new ApplicationUser();

        // Act
        user.LastName = "Doe";

        // Assert
        user.LastName.Should().Be("Doe");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FirstName_ShouldAcceptNullOrEmpty(string? firstName)
    {
        // Arrange
        var user = new ApplicationUser();

        // Act
        user.FirstName = firstName;

        // Assert
        user.FirstName.Should().Be(firstName);
    }

    #endregion

    #region FullName Computed Property Tests

    [Fact]
    public void FullName_WithBothNames_ShouldReturnCombined()
    {
        // Arrange
        var user = new ApplicationUser
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var fullName = user.FullName;

        // Assert
        fullName.Should().Be("John Doe");
    }

    [Fact]
    public void FullName_WithOnlyFirstName_ShouldReturnFirstName()
    {
        // Arrange
        var user = new ApplicationUser
        {
            FirstName = "John",
            LastName = null
        };

        // Act
        var fullName = user.FullName;

        // Assert
        fullName.Should().Be("John");
    }

    [Fact]
    public void FullName_WithOnlyLastName_ShouldReturnLastName()
    {
        // Arrange
        var user = new ApplicationUser
        {
            FirstName = null,
            LastName = "Doe"
        };

        // Act
        var fullName = user.FullName;

        // Assert
        fullName.Should().Be("Doe");
    }

    [Fact]
    public void FullName_WithNeitherName_ShouldReturnEmpty()
    {
        // Arrange
        var user = new ApplicationUser
        {
            FirstName = null,
            LastName = null
        };

        // Act
        var fullName = user.FullName;

        // Assert
        fullName.Should().BeEmpty();
    }

    [Fact]
    public void FullName_WithWhitespaceNames_ShouldTrim()
    {
        // Arrange
        var user = new ApplicationUser
        {
            FirstName = "  John  ",
            LastName = "  Doe  "
        };

        // Act
        var fullName = user.FullName;

        // Assert - Trim is applied to the result, not individual names
        fullName.Should().Be("John     Doe");
    }

    #endregion

    #region RefreshToken Tests

    [Fact]
    public void RefreshToken_ShouldBeSettable()
    {
        // Arrange
        var user = new ApplicationUser();
        var token = "some-refresh-token";

        // Act
        user.RefreshToken = token;

        // Assert
        user.RefreshToken.Should().Be(token);
    }

    [Fact]
    public void RefreshTokenExpiryTime_ShouldBeSettable()
    {
        // Arrange
        var user = new ApplicationUser();
        var expiry = DateTimeOffset.UtcNow.AddDays(7);

        // Act
        user.RefreshTokenExpiryTime = expiry;

        // Assert
        user.RefreshTokenExpiryTime.Should().Be(expiry);
    }

    #endregion

    #region IsActive Tests

    [Fact]
    public void IsActive_DefaultValue_ShouldBeTrue()
    {
        // Act
        var user = new ApplicationUser();

        // Assert
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_ShouldBeSettableToFalse()
    {
        // Arrange
        var user = new ApplicationUser();

        // Act
        user.IsActive = false;

        // Assert
        user.IsActive.Should().BeFalse();
    }

    #endregion

    #region TenantMemberships Tests

    [Fact]
    public void TenantMemberships_ShouldBeEmptyByDefault()
    {
        // Arrange
        var user = new ApplicationUser();

        // Assert - Users can belong to multiple tenants via memberships
        user.TenantMemberships.Should().BeEmpty();
    }

    #endregion

    #region Audit Fields Tests

    [Fact]
    public void CreatedAt_ShouldBeSettable()
    {
        // Arrange
        var user = new ApplicationUser();
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        user.CreatedAt = createdAt;

        // Assert
        user.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void CreatedBy_ShouldBeSettable()
    {
        // Arrange
        var user = new ApplicationUser();

        // Act
        user.CreatedBy = "admin";

        // Assert
        user.CreatedBy.Should().Be("admin");
    }

    [Fact]
    public void ModifiedAt_ShouldBeSettable()
    {
        // Arrange
        var user = new ApplicationUser();
        var modifiedAt = DateTimeOffset.UtcNow;

        // Act
        user.ModifiedAt = modifiedAt;

        // Assert
        user.ModifiedAt.Should().Be(modifiedAt);
    }

    [Fact]
    public void ModifiedBy_ShouldBeSettable()
    {
        // Arrange
        var user = new ApplicationUser();

        // Act
        user.ModifiedBy = "editor";

        // Assert
        user.ModifiedBy.Should().Be("editor");
    }

    #endregion

    #region Soft Delete Tests

    [Fact]
    public void IsDeleted_DefaultValue_ShouldBeFalse()
    {
        // Act
        var user = new ApplicationUser();

        // Assert
        user.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void IsDeleted_ShouldBeSettable()
    {
        // Arrange
        var user = new ApplicationUser();

        // Act
        user.IsDeleted = true;

        // Assert
        user.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void DeletedAt_ShouldBeSettable()
    {
        // Arrange
        var user = new ApplicationUser();
        var deletedAt = DateTimeOffset.UtcNow;

        // Act
        user.DeletedAt = deletedAt;

        // Assert
        user.DeletedAt.Should().Be(deletedAt);
    }

    [Fact]
    public void DeletedBy_ShouldBeSettable()
    {
        // Arrange
        var user = new ApplicationUser();

        // Act
        user.DeletedBy = "admin";

        // Assert
        user.DeletedBy.Should().Be("admin");
    }

    #endregion

    #region IAuditableEntity Implementation Tests

    [Fact]
    public void ApplicationUser_ShouldImplementIAuditableEntity()
    {
        // Arrange
        var user = new ApplicationUser();

        // Assert
        user.Should().BeAssignableTo<IAuditableEntity>();
    }

    #endregion

    #region Identity Properties Tests

    [Fact]
    public void Email_ShouldBeSettable()
    {
        // Arrange
        var user = new ApplicationUser();

        // Act
        user.Email = "test@example.com";

        // Assert
        user.Email.Should().Be("test@example.com");
    }

    [Fact]
    public void UserName_ShouldBeSettable()
    {
        // Arrange
        var user = new ApplicationUser();

        // Act
        user.UserName = "testuser";

        // Assert
        user.UserName.Should().Be("testuser");
    }

    #endregion
}

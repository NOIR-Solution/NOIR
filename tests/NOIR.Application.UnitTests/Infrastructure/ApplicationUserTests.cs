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
        user.IsActive.ShouldBe(true);
        user.IsDeleted.ShouldBe(false);
        user.FirstName.ShouldBeNull();
        user.LastName.ShouldBeNull();
        user.RefreshToken.ShouldBeNull();
        user.RefreshTokenExpiryTime.ShouldBeNull();
    }

    [Fact]
    public void NewUser_ShouldHaveNullAuditFields()
    {
        // Act
        var user = new ApplicationUser();

        // Assert
        user.CreatedBy.ShouldBeNull();
        user.ModifiedAt.ShouldBeNull();
        user.ModifiedBy.ShouldBeNull();
        user.DeletedAt.ShouldBeNull();
        user.DeletedBy.ShouldBeNull();
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
        user.FirstName.ShouldBe("John");
    }

    [Fact]
    public void LastName_ShouldBeSettable()
    {
        // Arrange
        var user = new ApplicationUser();

        // Act
        user.LastName = "Doe";

        // Assert
        user.LastName.ShouldBe("Doe");
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
        user.FirstName.ShouldBe(firstName);
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
        fullName.ShouldBe("John Doe");
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
        fullName.ShouldBe("John");
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
        fullName.ShouldBe("Doe");
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
        fullName.ShouldBeEmpty();
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
        fullName.ShouldBe("John     Doe");
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
        user.RefreshToken.ShouldBe(token);
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
        user.RefreshTokenExpiryTime.ShouldBe(expiry);
    }

    #endregion

    #region IsActive Tests

    [Fact]
    public void IsActive_DefaultValue_ShouldBeTrue()
    {
        // Act
        var user = new ApplicationUser();

        // Assert
        user.IsActive.ShouldBe(true);
    }

    [Fact]
    public void IsActive_ShouldBeSettableToFalse()
    {
        // Arrange
        var user = new ApplicationUser();

        // Act
        user.IsActive = false;

        // Assert
        user.IsActive.ShouldBe(false);
    }

    #endregion

    #region TenantId Tests

    [Fact]
    public void TenantId_ShouldBeNullByDefault()
    {
        // Arrange
        var user = new ApplicationUser();

        // Assert - Each user belongs to exactly one tenant
        user.TenantId.ShouldBeNull();
    }

    [Fact]
    public void TenantId_ShouldBeSettable()
    {
        // Arrange
        var user = new ApplicationUser();
        var tenantId = Guid.NewGuid().ToString();

        // Act
        user.TenantId = tenantId;

        // Assert
        user.TenantId.ShouldBe(tenantId);
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
        user.CreatedAt.ShouldBe(createdAt);
    }

    [Fact]
    public void CreatedBy_ShouldBeSettable()
    {
        // Arrange
        var user = new ApplicationUser();

        // Act
        user.CreatedBy = "admin";

        // Assert
        user.CreatedBy.ShouldBe("admin");
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
        user.ModifiedAt.ShouldBe(modifiedAt);
    }

    [Fact]
    public void ModifiedBy_ShouldBeSettable()
    {
        // Arrange
        var user = new ApplicationUser();

        // Act
        user.ModifiedBy = "editor";

        // Assert
        user.ModifiedBy.ShouldBe("editor");
    }

    #endregion

    #region Soft Delete Tests

    [Fact]
    public void IsDeleted_DefaultValue_ShouldBeFalse()
    {
        // Act
        var user = new ApplicationUser();

        // Assert
        user.IsDeleted.ShouldBe(false);
    }

    [Fact]
    public void IsDeleted_ShouldBeSettable()
    {
        // Arrange
        var user = new ApplicationUser();

        // Act
        user.IsDeleted = true;

        // Assert
        user.IsDeleted.ShouldBe(true);
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
        user.DeletedAt.ShouldBe(deletedAt);
    }

    [Fact]
    public void DeletedBy_ShouldBeSettable()
    {
        // Arrange
        var user = new ApplicationUser();

        // Act
        user.DeletedBy = "admin";

        // Assert
        user.DeletedBy.ShouldBe("admin");
    }

    #endregion

    #region IAuditableEntity Implementation Tests

    [Fact]
    public void ApplicationUser_ShouldImplementIAuditableEntity()
    {
        // Arrange
        var user = new ApplicationUser();

        // Assert
        user.ShouldBeAssignableTo<IAuditableEntity>();
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
        user.Email.ShouldBe("test@example.com");
    }

    [Fact]
    public void UserName_ShouldBeSettable()
    {
        // Arrange
        var user = new ApplicationUser();

        // Act
        user.UserName = "testuser";

        // Assert
        user.UserName.ShouldBe("testuser");
    }

    #endregion
}

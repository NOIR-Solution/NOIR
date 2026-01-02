namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for CurrentUserService.
/// Tests user information extraction from HTTP context.
/// </summary>
public class CurrentUserServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IMultiTenantContextAccessor<TenantInfo>> _tenantContextAccessorMock;
    private readonly CurrentUserService _sut;

    public CurrentUserServiceTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _tenantContextAccessorMock = new Mock<IMultiTenantContextAccessor<TenantInfo>>();
        _sut = new CurrentUserService(_httpContextAccessorMock.Object, _tenantContextAccessorMock.Object);
    }

    private void SetupHttpContext(ClaimsPrincipal? user = null)
    {
        var httpContext = new DefaultHttpContext();
        if (user != null)
        {
            httpContext.User = user;
        }
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
    }

    private ClaimsPrincipal CreateUser(string userId, string email, string[]? roles = null, bool isAuthenticated = true)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email)
        };

        if (roles != null)
        {
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var identity = new ClaimsIdentity(claims, isAuthenticated ? "Bearer" : null);
        return new ClaimsPrincipal(identity);
    }

    #region UserId Tests

    [Fact]
    public void UserId_WhenUserAuthenticated_ShouldReturnUserId()
    {
        // Arrange
        var user = CreateUser("user123", "test@example.com");
        SetupHttpContext(user);

        // Act
        var result = _sut.UserId;

        // Assert
        result.Should().Be("user123");
    }

    [Fact]
    public void UserId_WhenHttpContextNull_ShouldReturnNull()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _sut.UserId;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void UserId_WhenUserNull_ShouldReturnNull()
    {
        // Arrange
        SetupHttpContext();

        // Act
        var result = _sut.UserId;

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Email Tests

    [Fact]
    public void Email_WhenUserAuthenticated_ShouldReturnEmail()
    {
        // Arrange
        var user = CreateUser("user123", "test@example.com");
        SetupHttpContext(user);

        // Act
        var result = _sut.Email;

        // Assert
        result.Should().Be("test@example.com");
    }

    [Fact]
    public void Email_WhenHttpContextNull_ShouldReturnNull()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _sut.Email;

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region TenantId Tests

    [Fact]
    public void TenantId_WhenNoTenant_ShouldReturnNull()
    {
        // Arrange
        _tenantContextAccessorMock.Setup(x => x.MultiTenantContext).Returns((IMultiTenantContext<TenantInfo>?)null);

        // Act
        var result = _sut.TenantId;

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region IsAuthenticated Tests

    [Fact]
    public void IsAuthenticated_WhenUserAuthenticated_ShouldReturnTrue()
    {
        // Arrange
        var user = CreateUser("user123", "test@example.com", isAuthenticated: true);
        SetupHttpContext(user);

        // Act
        var result = _sut.IsAuthenticated;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_WhenUserNotAuthenticated_ShouldReturnFalse()
    {
        // Arrange
        var user = CreateUser("user123", "test@example.com", isAuthenticated: false);
        SetupHttpContext(user);

        // Act
        var result = _sut.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_WhenHttpContextNull_ShouldReturnFalse()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _sut.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Roles Tests

    [Fact]
    public void Roles_WhenUserHasRoles_ShouldReturnRoles()
    {
        // Arrange
        var user = CreateUser("user123", "test@example.com", roles: new[] { "Admin", "User" });
        SetupHttpContext(user);

        // Act
        var result = _sut.Roles;

        // Assert
        result.Should().BeEquivalentTo(new[] { "Admin", "User" });
    }

    [Fact]
    public void Roles_WhenUserHasNoRoles_ShouldReturnEmpty()
    {
        // Arrange
        var user = CreateUser("user123", "test@example.com");
        SetupHttpContext(user);

        // Act
        var result = _sut.Roles;

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Roles_WhenHttpContextNull_ShouldReturnEmpty()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _sut.Roles;

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region IsInRole Tests

    [Fact]
    public void IsInRole_WhenUserHasRole_ShouldReturnTrue()
    {
        // Arrange
        var user = CreateUser("user123", "test@example.com", roles: new[] { "Admin" });
        SetupHttpContext(user);

        // Act
        var result = _sut.IsInRole("Admin");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsInRole_WhenUserDoesNotHaveRole_ShouldReturnFalse()
    {
        // Arrange
        var user = CreateUser("user123", "test@example.com", roles: new[] { "User" });
        SetupHttpContext(user);

        // Act
        var result = _sut.IsInRole("Admin");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsInRole_WhenHttpContextNull_ShouldReturnFalse()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _sut.IsInRole("Admin");

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}

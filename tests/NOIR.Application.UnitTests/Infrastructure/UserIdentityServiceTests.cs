namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for UserIdentityService.
/// Tests user management operations with mocked UserManager and SignInManager.
/// </summary>
public class UserIdentityServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<IMultiTenantStore<Tenant>> _tenantStoreMock;
    private readonly Mock<IDateTime> _dateTimeMock;
    private readonly UserIdentityService _sut;

    public UserIdentityServiceTests()
    {
        // Setup UserManager mock
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        // Setup default empty Users queryable for async operations using MockQueryable.Moq
        // BuildMockDbSet is extension on IEnumerable<T>, not IQueryable<T>
        var emptyUsersList = new List<ApplicationUser>();
        var mockUsers = emptyUsersList.BuildMockDbSet();
        _userManagerMock.Setup(x => x.Users).Returns(mockUsers.Object);

        // Setup email normalization
        _userManagerMock.Setup(x => x.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(email => email?.ToUpperInvariant() ?? string.Empty);

        // Setup SignInManager mock
        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var userClaimsPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            contextAccessorMock.Object,
            userClaimsPrincipalFactoryMock.Object,
            null!, null!, null!, null!);

        // Setup TenantStore mock
        _tenantStoreMock = new Mock<IMultiTenantStore<Tenant>>();
        _tenantStoreMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync([]);

        _dateTimeMock = new Mock<IDateTime>();
        _dateTimeMock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

        _sut = new UserIdentityService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _tenantStoreMock.Object,
            _dateTimeMock.Object);
    }

    #region FindByIdAsync Tests

    [Fact]
    public async Task FindByIdAsync_WithExistingUser_ShouldReturnUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.FindByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Email.Should().Be(user.Email);
        result.FirstName.Should().Be(user.FirstName);
        result.LastName.Should().Be(user.LastName);
    }

    [Fact]
    public async Task FindByIdAsync_WithNonExistingUser_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.FindByIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByIdAsync_ShouldPassCancellationToken()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var cts = new CancellationTokenSource();
        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        await _sut.FindByIdAsync(userId, cts.Token);

        // Assert - Verify method was called
        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
    }

    #endregion

    #region FindByEmailAsync Tests

    private void SetupUserManagerWithUsers(params ApplicationUser[] users)
    {
        var queryable = users.AsQueryable();
        var mockDbSet = new Mock<DbSet<ApplicationUser>>();

        mockDbSet.As<IAsyncEnumerable<ApplicationUser>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<ApplicationUser>(queryable.GetEnumerator()));

        mockDbSet.As<IQueryable<ApplicationUser>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<ApplicationUser>(queryable.Provider));

        mockDbSet.As<IQueryable<ApplicationUser>>()
            .Setup(m => m.Expression)
            .Returns(queryable.Expression);

        mockDbSet.As<IQueryable<ApplicationUser>>()
            .Setup(m => m.ElementType)
            .Returns(queryable.ElementType);

        mockDbSet.As<IQueryable<ApplicationUser>>()
            .Setup(m => m.GetEnumerator())
            .Returns(queryable.GetEnumerator());

        _userManagerMock.Setup(x => x.Users).Returns(mockDbSet.Object);
    }

    [Fact]
    public async Task FindByEmailAsync_WithExistingUser_ShouldReturnUserDto()
    {
        // Arrange
        var email = "test@example.com";
        var user = CreateTestUser(email: email, tenantId: null);
        user.NormalizedEmail = email.ToUpperInvariant();

        SetupUserManagerWithUsers(user);
        _userManagerMock.Setup(x => x.NormalizeEmail(email))
            .Returns(email.ToUpperInvariant());

        // Act
        var result = await _sut.FindByEmailAsync(email, null);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(email);
    }

    [Fact]
    public async Task FindByEmailAsync_WithNonExistingUser_ShouldReturnNull()
    {
        // Arrange
        var email = "nonexistent@example.com";

        SetupUserManagerWithUsers(); // Empty user list
        _userManagerMock.Setup(x => x.NormalizeEmail(email))
            .Returns(email.ToUpperInvariant());

        // Act
        var result = await _sut.FindByEmailAsync(email, null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindByEmailAsync_ShouldNormalizeEmail()
    {
        // Arrange
        var email = "Test@Example.COM";

        SetupUserManagerWithUsers(); // Empty user list
        _userManagerMock.Setup(x => x.NormalizeEmail(email))
            .Returns(email.ToUpperInvariant());

        // Act
        await _sut.FindByEmailAsync(email, null);

        // Assert
        _userManagerMock.Verify(x => x.NormalizeEmail(email), Times.Once);
    }

    [Fact]
    public async Task FindByEmailAsync_WithTenantId_ShouldFilterByTenant()
    {
        // Arrange
        var email = "test@example.com";
        var tenantId = "tenant-1";
        var userInTenant = CreateTestUser(email: email, tenantId: tenantId);
        userInTenant.NormalizedEmail = email.ToUpperInvariant();
        var userInOtherTenant = CreateTestUser(email: email, tenantId: "other-tenant");
        userInOtherTenant.NormalizedEmail = email.ToUpperInvariant();

        SetupUserManagerWithUsers(userInTenant, userInOtherTenant);
        _userManagerMock.Setup(x => x.NormalizeEmail(email))
            .Returns(email.ToUpperInvariant());

        // Act
        var result = await _sut.FindByEmailAsync(email, tenantId);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(email);
    }

    #endregion

    #region NormalizeEmail Tests

    [Fact]
    public void NormalizeEmail_ShouldReturnNormalizedEmail()
    {
        // Arrange
        var email = "Test@Example.com";
        var normalized = "TEST@EXAMPLE.COM";
        _userManagerMock.Setup(x => x.NormalizeEmail(email))
            .Returns(normalized);

        // Act
        var result = _sut.NormalizeEmail(email);

        // Assert
        result.Should().Be(normalized);
    }

    [Fact]
    public void NormalizeEmail_WhenUserManagerReturnsNull_ShouldReturnOriginal()
    {
        // Arrange
        var email = "test@example.com";
        _userManagerMock.Setup(x => x.NormalizeEmail(email))
            .Returns((string)null!);

        // Act
        var result = _sut.NormalizeEmail(email);

        // Assert
        result.Should().Be(email);
    }

    #endregion

    #region CheckPasswordSignInAsync Tests

    [Fact]
    public async Task CheckPasswordSignInAsync_WithValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var password = "ValidPassword123!";
        var user = CreateTestUser(userId);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Act
        var result = await _sut.CheckPasswordSignInAsync(userId, password);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.IsLockedOut.Should().BeFalse();
        result.IsNotAllowed.Should().BeFalse();
        result.RequiresTwoFactor.Should().BeFalse();
    }

    [Fact]
    public async Task CheckPasswordSignInAsync_WithInvalidPassword_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var password = "WrongPassword";
        var user = CreateTestUser(userId);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var result = await _sut.CheckPasswordSignInAsync(userId, password);

        // Assert
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task CheckPasswordSignInAsync_WithLockedOutUser_ShouldReturnLockedOut()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var password = "Password123!";
        var user = CreateTestUser(userId);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        // Act
        var result = await _sut.CheckPasswordSignInAsync(userId, password);

        // Assert
        result.IsLockedOut.Should().BeTrue();
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task CheckPasswordSignInAsync_WithNonExistingUser_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.CheckPasswordSignInAsync(userId, "password");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.IsLockedOut.Should().BeFalse();
        result.IsNotAllowed.Should().BeFalse();
        result.RequiresTwoFactor.Should().BeFalse();
    }

    [Fact]
    public async Task CheckPasswordSignInAsync_WithRequiresTwoFactor_ShouldReturnRequiresTwoFactor()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var password = "Password123!";
        var user = CreateTestUser(userId);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.TwoFactorRequired);

        // Act
        var result = await _sut.CheckPasswordSignInAsync(userId, password);

        // Assert
        result.RequiresTwoFactor.Should().BeTrue();
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task CheckPasswordSignInAsync_WithLockoutOnFailureFalse_ShouldNotLockout()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var password = "Password123!";
        var user = CreateTestUser(userId);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, password, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        await _sut.CheckPasswordSignInAsync(userId, password, lockoutOnFailure: false);

        // Assert
        _signInManagerMock.Verify(x => x.CheckPasswordSignInAsync(user, password, false), Times.Once);
    }

    #endregion

    #region CreateUserAsync Tests

    [Fact]
    public async Task CreateUserAsync_WithValidData_ShouldReturnSuccessWithUserId()
    {
        // Arrange
        var dto = new CreateUserDto("test@example.com", "John", "Doe", "JohnDoe", null);
        var password = "Password123!";

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), password))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.CreateUserAsync(dto, password);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.UserId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateUserAsync_WithInvalidPassword_ShouldReturnFailure()
    {
        // Arrange
        var dto = new CreateUserDto("test@example.com", "John", "Doe", null, null);
        var password = "weak";
        var errors = new[] { new IdentityError { Description = "Password too weak" } };

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), password))
            .ReturnsAsync(IdentityResult.Failed(errors));

        // Act
        var result = await _sut.CreateUserAsync(dto, password);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Password too weak");
    }

    [Fact]
    public async Task CreateUserAsync_ForPlatformUser_ShouldSetUserNameToEmail()
    {
        // Arrange - Platform user has TenantId = null
        var dto = new CreateUserDto("test@example.com", "John", "Doe", "JohnDoe", TenantId: null);
        var password = "Password123!";
        ApplicationUser? capturedUser = null;

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), password))
            .Callback<ApplicationUser, string>((u, _) => capturedUser = u)
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _sut.CreateUserAsync(dto, password);

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.Email.Should().Be(dto.Email);
        capturedUser.UserName.Should().Be(dto.Email); // Platform users: UserName = Email
        capturedUser.FirstName.Should().Be(dto.FirstName);
        capturedUser.LastName.Should().Be(dto.LastName);
        capturedUser.DisplayName.Should().Be(dto.DisplayName);
        capturedUser.IsActive.Should().BeTrue();
        capturedUser.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task CreateUserAsync_ForTenantUser_ShouldSetUserNameToEmailWithTenantId()
    {
        // Arrange - Tenant user has TenantId set
        var tenantId = "550e8400-e29b-41d4-a716-446655440000";
        var email = "user@example.com";
        var dto = new CreateUserDto(email, "John", "Doe", "JohnDoe", TenantId: tenantId);
        var password = "Password123!";
        ApplicationUser? capturedUser = null;

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), password))
            .Callback<ApplicationUser, string>((u, _) => capturedUser = u)
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _sut.CreateUserAsync(dto, password);

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.Email.Should().Be(email);
        capturedUser.UserName.Should().Be($"{email}#{tenantId}"); // Tenant users: UserName = email#tenantId
        capturedUser.FirstName.Should().Be(dto.FirstName);
        capturedUser.LastName.Should().Be(dto.LastName);
        capturedUser.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task CreateUserAsync_WithEmptyEmail_ShouldReturnFailure()
    {
        // Arrange
        var dto = new CreateUserDto("", "John", "Doe", null, null);
        var password = "Password123!";

        // Act
        var result = await _sut.CreateUserAsync(dto, password);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Email is required.");
    }

    [Fact]
    public async Task CreateUserAsync_WithEmptyPassword_ShouldReturnFailure()
    {
        // Arrange
        var dto = new CreateUserDto("test@example.com", "John", "Doe", null, null);
        var password = "";

        // Act
        var result = await _sut.CreateUserAsync(dto, password);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Password is required.");
    }

    #endregion

    #region UpdateUserAsync Tests

    [Fact]
    public async Task UpdateUserAsync_WithExistingUser_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var updates = new UpdateUserDto(FirstName: "Updated");

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.UpdateUserAsync(userId, updates);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task UpdateUserAsync_WithNonExistingUser_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var updates = new UpdateUserDto(FirstName: "Updated");

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.UpdateUserAsync(userId, updates);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("User not found.");
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldOnlyUpdateProvidedFields()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var originalLastName = user.LastName;
        var updates = new UpdateUserDto(FirstName: "NewFirstName");

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _sut.UpdateUserAsync(userId, updates);

        // Assert
        user.FirstName.Should().Be("NewFirstName");
        user.LastName.Should().Be(originalLastName); // Should remain unchanged
    }

    [Fact]
    public async Task UpdateUserAsync_WithEmptyFirstName_ShouldSetToNull()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        user.FirstName = "OldName";
        var updates = new UpdateUserDto(FirstName: "   ");

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _sut.UpdateUserAsync(userId, updates);

        // Assert
        user.FirstName.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldUpdateAllProvidedFields()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var updates = new UpdateUserDto(
            FirstName: "NewFirst",
            LastName: "NewLast",
            DisplayName: "NewDisplay",
            PhoneNumber: "1234567890",
            AvatarUrl: "/avatars/new.png",
            IsActive: false);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _sut.UpdateUserAsync(userId, updates);

        // Assert
        user.FirstName.Should().Be("NewFirst");
        user.LastName.Should().Be("NewLast");
        user.DisplayName.Should().Be("NewDisplay");
        user.PhoneNumber.Should().Be("1234567890");
        user.AvatarUrl.Should().Be("/avatars/new.png");
        user.IsActive.Should().BeFalse();
    }

    #endregion

    #region SoftDeleteUserAsync Tests

    [Fact]
    public async Task SoftDeleteUserAsync_WithExistingUser_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var deletedBy = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var now = DateTimeOffset.UtcNow;
        _dateTimeMock.Setup(x => x.UtcNow).Returns(now);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.SoftDeleteUserAsync(userId, deletedBy);

        // Assert
        result.Succeeded.Should().BeTrue();
        user.IsDeleted.Should().BeTrue();
        user.DeletedAt.Should().Be(now);
        user.DeletedBy.Should().Be(deletedBy);
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDeleteUserAsync_WithNonExistingUser_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.SoftDeleteUserAsync(userId, "admin");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("User not found.");
    }

    #endregion

    #region SetUserLockoutAsync Tests

    [Fact]
    public async Task SetUserLockoutAsync_Lock_ShouldSetLockoutProperties()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var lockedBy = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var now = DateTimeOffset.UtcNow;
        _dateTimeMock.Setup(x => x.UtcNow).Returns(now);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.SetUserLockoutAsync(userId, true, lockedBy);

        // Assert
        result.Succeeded.Should().BeTrue();
        user.IsActive.Should().BeFalse();
        user.LockoutEnd.Should().Be(DateTimeOffset.MaxValue);
        user.LockedAt.Should().Be(now);
        user.LockedBy.Should().Be(lockedBy);
    }

    [Fact]
    public async Task SetUserLockoutAsync_Unlock_ShouldClearLockoutProperties()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        user.IsActive = false;
        user.LockoutEnd = DateTimeOffset.MaxValue;
        user.LockedAt = DateTimeOffset.UtcNow;
        user.LockedBy = "admin";

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.SetUserLockoutAsync(userId, false);

        // Assert
        result.Succeeded.Should().BeTrue();
        user.IsActive.Should().BeTrue();
        user.LockoutEnd.Should().BeNull();
        user.LockedAt.Should().BeNull();
        user.LockedBy.Should().BeNull();
    }

    [Fact]
    public async Task SetUserLockoutAsync_WithNonExistingUser_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.SetUserLockoutAsync(userId, true);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("User not found.");
    }

    #endregion

    #region ResetPasswordAsync Tests

    [Fact]
    public async Task ResetPasswordAsync_WithExistingUser_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var newPassword = "NewPassword123!";
        var resetToken = "reset-token";
        var now = DateTimeOffset.UtcNow;
        _dateTimeMock.Setup(x => x.UtcNow).Returns(now);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(resetToken);
        _userManagerMock.Setup(x => x.ResetPasswordAsync(user, resetToken, newPassword))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.ResetPasswordAsync(userId, newPassword);

        // Assert
        result.Succeeded.Should().BeTrue();
        user.PasswordLastChangedAt.Should().Be(now);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithNonExistingUser_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.ResetPasswordAsync(userId, "NewPassword123!");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("User not found.");
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidPassword_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var newPassword = "weak";
        var resetToken = "reset-token";
        var errors = new[] { new IdentityError { Description = "Password too weak" } };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(resetToken);
        _userManagerMock.Setup(x => x.ResetPasswordAsync(user, resetToken, newPassword))
            .ReturnsAsync(IdentityResult.Failed(errors));

        // Act
        var result = await _sut.ResetPasswordAsync(userId, newPassword);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Password too weak");
    }

    #endregion

    #region ChangePasswordAsync Tests

    [Fact]
    public async Task ChangePasswordAsync_WithValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var currentPassword = "OldPassword123!";
        var newPassword = "NewPassword123!";
        var now = DateTimeOffset.UtcNow;
        _dateTimeMock.Setup(x => x.UtcNow).Returns(now);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.ChangePasswordAsync(user, currentPassword, newPassword))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.ChangePasswordAsync(userId, currentPassword, newPassword);

        // Assert
        result.Succeeded.Should().BeTrue();
        user.PasswordLastChangedAt.Should().Be(now);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithWrongCurrentPassword_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var errors = new[] { new IdentityError { Description = "Incorrect password" } };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.ChangePasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(errors));

        // Act
        var result = await _sut.ChangePasswordAsync(userId, "wrong", "NewPassword123!");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Incorrect password");
    }

    [Fact]
    public async Task ChangePasswordAsync_WithNonExistingUser_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.ChangePasswordAsync(userId, "old", "new");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("User not found.");
    }

    #endregion

    #region UpdateEmailAsync Tests

    [Fact]
    public async Task UpdateEmailAsync_WithNewEmail_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var newEmail = "newemail@example.com";

        // Setup Users mock for per-tenant email check (empty = no existing user)
        var users = new List<ApplicationUser> { user }.BuildMockDbSet();
        _userManagerMock.Setup(x => x.Users).Returns(users.Object);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(e => e?.ToUpperInvariant() ?? string.Empty);
        _userManagerMock.Setup(x => x.NormalizeName(It.IsAny<string>()))
            .Returns<string>(n => n?.ToUpperInvariant() ?? string.Empty);
        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.UpdateEmailAsync(userId, newEmail);

        // Assert
        result.Succeeded.Should().BeTrue();
        user.Email.Should().Be(newEmail);
        // Platform user (TenantId = null): UserName = email
        user.UserName.Should().Be(newEmail);
    }

    [Fact]
    public async Task UpdateEmailAsync_WithEmailAlreadyInUse_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var newEmail = "taken@example.com";
        var existingUser = CreateTestUser(Guid.NewGuid().ToString(), email: newEmail);
        // Both users in same tenant (null = platform)
        user.TenantId = null;
        existingUser.TenantId = null;

        // Setup Users mock with existing user having the same email in same tenant
        var users = new List<ApplicationUser> { user, existingUser }.BuildMockDbSet();
        _userManagerMock.Setup(x => x.Users).Returns(users.Object);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(e => e?.ToUpperInvariant() ?? string.Empty);

        // Act
        var result = await _sut.UpdateEmailAsync(userId, newEmail);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Email is already in use in this tenant.");
    }

    [Fact]
    public async Task UpdateEmailAsync_WithSameUserEmail_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var sameEmail = user.Email!;

        // Setup Users mock
        var users = new List<ApplicationUser> { user }.BuildMockDbSet();
        _userManagerMock.Setup(x => x.Users).Returns(users.Object);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(e => e?.ToUpperInvariant() ?? string.Empty);
        _userManagerMock.Setup(x => x.NormalizeName(It.IsAny<string>()))
            .Returns<string>(n => n?.ToUpperInvariant() ?? string.Empty);
        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.UpdateEmailAsync(userId, sameEmail);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateEmailAsync_WithNonExistingUser_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.UpdateEmailAsync(userId, "new@example.com");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("User not found.");
    }

    #endregion

    #region Role Management Tests

    [Fact]
    public async Task GetRolesAsync_WithExistingUser_ShouldReturnRoles()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var roles = new List<string> { "Admin", "User" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);

        // Act
        var result = await _sut.GetRolesAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("Admin");
        result.Should().Contain("User");
    }

    [Fact]
    public async Task GetRolesAsync_WithNonExistingUser_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.GetRolesAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AddToRolesAsync_WithExistingUser_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var roles = new[] { "Admin", "User" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.AddToRolesAsync(user, roles))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.AddToRolesAsync(userId, roles);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task AddToRolesAsync_WithNonExistingUser_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.AddToRolesAsync(userId, new[] { "Admin" });

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("User not found.");
    }

    [Fact]
    public async Task RemoveFromRolesAsync_WithExistingUser_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var roles = new[] { "Admin" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.RemoveFromRolesAsync(user, roles))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.RemoveFromRolesAsync(userId, roles);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveFromRolesAsync_WithNonExistingUser_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.RemoveFromRolesAsync(userId, new[] { "Admin" });

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("User not found.");
    }

    [Fact]
    public async Task IsInRoleAsync_WithUserInRole_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.IsInRoleAsync(user, "Admin"))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.IsInRoleAsync(userId, "Admin");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsInRoleAsync_WithUserNotInRole_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.IsInRoleAsync(user, "Admin"))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.IsInRoleAsync(userId, "Admin");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsInRoleAsync_WithNonExistingUser_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.IsInRoleAsync(userId, "Admin");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AssignRolesAsync_WithReplaceExisting_ShouldReplaceAllRoles()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var currentRoles = new List<string> { "OldRole1", "OldRole2" };
        var newRoles = new[] { "NewRole1", "NewRole2" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(currentRoles);
        _userManagerMock.Setup(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRolesAsync(user, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.AssignRolesAsync(userId, newRoles, replaceExisting: true);

        // Assert
        result.Succeeded.Should().BeTrue();
        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(user, It.Is<IEnumerable<string>>(r => r.Contains("OldRole1") && r.Contains("OldRole2"))), Times.Once);
        _userManagerMock.Verify(x => x.AddToRolesAsync(user, It.Is<IEnumerable<string>>(r => r.Contains("NewRole1") && r.Contains("NewRole2"))), Times.Once);
    }

    [Fact]
    public async Task AssignRolesAsync_WithoutReplaceExisting_ShouldOnlyAddNewRoles()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = CreateTestUser(userId);
        var currentRoles = new List<string> { "ExistingRole" };
        var newRoles = new[] { "ExistingRole", "NewRole" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(currentRoles);
        _userManagerMock.Setup(x => x.AddToRolesAsync(user, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.AssignRolesAsync(userId, newRoles, replaceExisting: false);

        // Assert
        result.Succeeded.Should().BeTrue();
        _userManagerMock.Verify(x => x.AddToRolesAsync(user, It.Is<IEnumerable<string>>(r => r.Contains("NewRole") && !r.Contains("ExistingRole"))), Times.Once);
        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task AssignRolesAsync_WithNonExistingUser_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.AssignRolesAsync(userId, new[] { "Admin" });

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("User not found.");
    }

    #endregion

    #region Service Interface Tests

    [Fact]
    public void Service_ShouldImplementIUserIdentityService()
    {
        // Assert
        _sut.Should().BeAssignableTo<IUserIdentityService>();
    }

    [Fact]
    public void Service_ShouldImplementIScopedService()
    {
        // Assert
        _sut.Should().BeAssignableTo<IScopedService>();
    }

    #endregion

    #region Helper Methods

    private static ApplicationUser CreateTestUser(
        string? userId = null,
        string? email = null,
        string? tenantId = null)
    {
        var id = userId ?? Guid.NewGuid().ToString();
        var userEmail = email ?? $"user{id[..8]}@example.com";

        return new ApplicationUser
        {
            Id = id,
            Email = userEmail,
            UserName = userEmail,
            NormalizedEmail = userEmail.ToUpperInvariant(),
            NormalizedUserName = userEmail.ToUpperInvariant(),
            FirstName = "Test",
            LastName = "User",
            DisplayName = "Test User",
            IsActive = true,
            TenantId = tenantId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}

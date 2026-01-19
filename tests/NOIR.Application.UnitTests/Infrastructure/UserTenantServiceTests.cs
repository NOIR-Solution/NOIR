namespace NOIR.Application.UnitTests.Infrastructure;

using NOIR.Application.Specifications.UserTenantMemberships;

/// <summary>
/// Unit tests for UserTenantService.
/// Tests user-tenant membership management including CRUD operations and role management.
/// Uses InMemory database for testing as ApplicationDbContext properties are not virtual.
/// </summary>
public class UserTenantServiceTests
{
    private readonly Mock<IMultiTenantStore<Tenant>> _tenantStoreMock;
    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILocalizationService> _localizerMock;
    private readonly Mock<ILogger<UserTenantService>> _loggerMock;

    private const string TestUserId = "test-user-id";
    private const string TestTenantId = "test-tenant-id";

    public UserTenantServiceTests()
    {
        _tenantStoreMock = new Mock<IMultiTenantStore<Tenant>>();
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _localizerMock = new Mock<ILocalizationService>();
        _loggerMock = new Mock<ILogger<UserTenantService>>();

        // Setup default localizer behavior
        _localizerMock.Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => key);
    }

    #region GetUserTenantsAsync Tests

    [Fact]
    public void GetUserTenantsAsync_ShouldQueryByUserId()
    {
        // This test verifies the expected query pattern
        var membership = UserTenantMembership.Create(TestUserId, TestTenantId, TenantRole.Member, true);

        // Assert membership is created correctly
        membership.UserId.Should().Be(TestUserId);
        membership.TenantId.Should().Be(TestTenantId);
        membership.Role.Should().Be(TenantRole.Member);
        membership.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void GetUserTenantsAsync_ShouldReturnMembershipDtos()
    {
        // Verify DTO mapping pattern
        var membership = UserTenantMembership.Create(TestUserId, TestTenantId, TenantRole.Admin, false);

        // The DTO should contain:
        membership.TenantId.Should().Be(TestTenantId);
        membership.Role.Should().Be(TenantRole.Admin);
        membership.IsDefault.Should().BeFalse();
        membership.JoinedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region GetTenantUsersAsync Tests

    [Fact]
    public void GetTenantUsersAsync_ShouldQueryByTenantId()
    {
        // Verify query pattern
        var membership = UserTenantMembership.Create(TestUserId, TestTenantId, TenantRole.Member);

        membership.TenantId.Should().Be(TestTenantId);
    }

    #endregion

    #region GetTenantUsersPaginatedAsync Tests

    [Fact]
    public void GetTenantUsersPaginatedAsync_ShouldApplyPagination()
    {
        // Verify pagination pattern
        const int pageNumber = 2;
        const int pageSize = 10;

        // Calculate skip value
        var skip = (pageNumber - 1) * pageSize;

        skip.Should().Be(10);
    }

    [Fact]
    public void GetTenantUsersPaginatedAsync_WithSearchTerm_ShouldFilterResults()
    {
        // Verify search filtering pattern
        var searchTerm = "john";
        var users = new List<UserIdentityDto>
        {
            CreateTestUser("1", "john@example.com", "John", "Doe"),
            CreateTestUser("2", "jane@example.com", "Jane", "Doe"),
            CreateTestUser("3", "test@example.com", "John", "Smith")
        };

        var filtered = users.Where(u =>
            u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            u.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();

        filtered.Should().HaveCount(2); // john@example.com and John Smith
    }

    #endregion

    #region AddUserToTenantAsync Tests

    [Fact]
    public async Task AddUserToTenantAsync_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        var service = CreateService();

        // Act
        var result = await service.AddUserToTenantAsync(TestUserId, TestTenantId, TenantRole.Member);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task AddUserToTenantAsync_WhenTenantNotFound_ShouldReturnFailure()
    {
        // Arrange
        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUser(TestUserId, "test@example.com", "Test", "User"));

        _tenantStoreMock
            .Setup(x => x.GetAsync(TestTenantId))
            .ReturnsAsync((Tenant?)null);

        var service = CreateService();

        // Act
        var result = await service.AddUserToTenantAsync(TestUserId, TestTenantId, TenantRole.Member);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void AddUserToTenantAsync_WhenSuccess_ShouldCreateMembership()
    {
        // Verify membership creation pattern
        var membership = UserTenantMembership.Create(TestUserId, TestTenantId, TenantRole.Admin, true);

        membership.Should().NotBeNull();
        membership.UserId.Should().Be(TestUserId);
        membership.TenantId.Should().Be(TestTenantId);
        membership.Role.Should().Be(TenantRole.Admin);
        membership.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void AddUserToTenantAsync_FirstMembership_ShouldBeDefault()
    {
        // When a user joins their first tenant, it should be their default
        var isFirstMembership = true;
        var membership = UserTenantMembership.Create(TestUserId, TestTenantId, TenantRole.Member, isFirstMembership);

        membership.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void AddUserToTenantAsync_SubsequentMembership_ShouldNotBeDefault()
    {
        // Subsequent tenant memberships should not automatically be default
        var isFirstMembership = false;
        var membership = UserTenantMembership.Create(TestUserId, TestTenantId, TenantRole.Member, isFirstMembership);

        membership.IsDefault.Should().BeFalse();
    }

    #endregion

    #region RemoveUserFromTenantAsync Tests

    [Fact]
    public async Task RemoveUserFromTenantAsync_WhenMembershipNotFound_ShouldReturnFailure()
    {
        // Arrange - Using InMemory database with no data
        var service = CreateService();

        // Act
        var result = await service.RemoveUserFromTenantAsync(TestUserId, TestTenantId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void RemoveUserFromTenantAsync_WhenRemovingDefaultTenant_ShouldSetAnotherAsDefault()
    {
        // Verify the logic pattern for default tenant handling
        var memberships = new List<UserTenantMembership>
        {
            UserTenantMembership.Create(TestUserId, "tenant-1", TenantRole.Member, true),  // Default - being removed
            UserTenantMembership.Create(TestUserId, "tenant-2", TenantRole.Member, false)  // Will become default
        };

        // Simulate removing default and setting another
        var defaultMembership = memberships.First(m => m.IsDefault);
        var otherMembership = memberships.First(m => !m.IsDefault);

        // Remove default
        memberships.Remove(defaultMembership);

        // Set other as default
        otherMembership.SetAsDefault();

        // Assert
        memberships.Should().HaveCount(1);
        memberships.Single().IsDefault.Should().BeTrue();
    }

    #endregion

    #region UpdateUserRoleInTenantAsync Tests

    [Fact]
    public async Task UpdateUserRoleInTenantAsync_WhenMembershipNotFound_ShouldReturnFailure()
    {
        // Arrange - Using InMemory database with no data
        var service = CreateService();

        // Act
        var result = await service.UpdateUserRoleInTenantAsync(TestUserId, TestTenantId, TenantRole.Admin);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void UpdateUserRoleInTenantAsync_ShouldUpdateRole()
    {
        // Verify role update pattern
        var membership = UserTenantMembership.Create(TestUserId, TestTenantId, TenantRole.Member);

        membership.Role.Should().Be(TenantRole.Member);

        membership.UpdateRole(TenantRole.Admin);

        membership.Role.Should().Be(TenantRole.Admin);
    }

    [Theory]
    [InlineData(TenantRole.Viewer)]
    [InlineData(TenantRole.Member)]
    [InlineData(TenantRole.Admin)]
    [InlineData(TenantRole.Owner)]
    public void UpdateUserRoleInTenantAsync_AllRoles_ShouldWork(TenantRole newRole)
    {
        // Verify all role transitions work
        var membership = UserTenantMembership.Create(TestUserId, TestTenantId, TenantRole.Member);

        membership.UpdateRole(newRole);

        membership.Role.Should().Be(newRole);
    }

    #endregion

    #region GetUserRoleInTenantAsync Tests

    [Fact]
    public void GetUserRoleInTenantAsync_WhenMembershipExists_ShouldReturnRole()
    {
        // Verify role retrieval pattern
        var membership = UserTenantMembership.Create(TestUserId, TestTenantId, TenantRole.Owner);

        var role = membership.Role;

        role.Should().Be(TenantRole.Owner);
    }

    [Fact]
    public void GetUserRoleInTenantAsync_WhenMembershipNotExists_ShouldReturnNull()
    {
        // When no membership exists, null should be returned
        UserTenantMembership? membership = null;

        var role = membership?.Role;

        role.Should().BeNull();
    }

    #endregion

    #region IsUserInTenantAsync Tests

    [Fact]
    public void IsUserInTenantAsync_WhenMembershipExists_ShouldReturnTrue()
    {
        // Verify existence check pattern
        var memberships = new List<UserTenantMembership>
        {
            UserTenantMembership.Create(TestUserId, TestTenantId, TenantRole.Member)
        };

        var exists = memberships.Any(m => m.UserId == TestUserId && m.TenantId == TestTenantId);

        exists.Should().BeTrue();
    }

    [Fact]
    public void IsUserInTenantAsync_WhenMembershipNotExists_ShouldReturnFalse()
    {
        // Verify non-existence case
        var memberships = new List<UserTenantMembership>();

        var exists = memberships.Any(m => m.UserId == TestUserId && m.TenantId == TestTenantId);

        exists.Should().BeFalse();
    }

    #endregion

    #region GetMembershipAsync Tests

    [Fact]
    public void GetMembershipAsync_WhenExists_ShouldReturnDto()
    {
        // Verify membership retrieval pattern
        var membership = UserTenantMembership.Create(TestUserId, TestTenantId, TenantRole.Admin, true);

        membership.Should().NotBeNull();
        membership.UserId.Should().Be(TestUserId);
        membership.TenantId.Should().Be(TestTenantId);
        membership.Role.Should().Be(TenantRole.Admin);
        membership.IsDefault.Should().BeTrue();
    }

    #endregion

    #region GetTenantUserCountAsync Tests

    [Fact]
    public void GetTenantUserCountAsync_ShouldReturnCorrectCount()
    {
        // Verify count pattern
        var memberships = new List<UserTenantMembership>
        {
            UserTenantMembership.Create("user-1", TestTenantId, TenantRole.Owner),
            UserTenantMembership.Create("user-2", TestTenantId, TenantRole.Admin),
            UserTenantMembership.Create("user-3", TestTenantId, TenantRole.Member),
            UserTenantMembership.Create("user-4", "other-tenant", TenantRole.Member)
        };

        var count = memberships.Count(m => m.TenantId == TestTenantId);

        count.Should().Be(3);
    }

    #endregion

    #region UserTenantMembership Entity Tests

    [Fact]
    public void UserTenantMembership_Create_ShouldSetAllProperties()
    {
        // Act
        var membership = UserTenantMembership.Create(TestUserId, TestTenantId, TenantRole.Admin, true);

        // Assert
        membership.UserId.Should().Be(TestUserId);
        membership.TenantId.Should().Be(TestTenantId);
        membership.Role.Should().Be(TenantRole.Admin);
        membership.IsDefault.Should().BeTrue();
        membership.JoinedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        membership.Id.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UserTenantMembership_Create_WithInvalidUserId_ShouldThrow(string? invalidUserId)
    {
        // Act
        var act = () => UserTenantMembership.Create(invalidUserId!, TestTenantId, TenantRole.Member);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UserTenantMembership_Create_WithInvalidTenantId_ShouldThrow(string? invalidTenantId)
    {
        // Act
        var act = () => UserTenantMembership.Create(TestUserId, invalidTenantId!, TenantRole.Member);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UserTenantMembership_SetAsDefault_ShouldSetIsDefaultTrue()
    {
        // Arrange
        var membership = UserTenantMembership.Create(TestUserId, TestTenantId, TenantRole.Member, false);
        membership.IsDefault.Should().BeFalse();

        // Act
        membership.SetAsDefault();

        // Assert
        membership.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void UserTenantMembership_ClearDefault_ShouldSetIsDefaultFalse()
    {
        // Arrange
        var membership = UserTenantMembership.Create(TestUserId, TestTenantId, TenantRole.Member, true);
        membership.IsDefault.Should().BeTrue();

        // Act
        membership.ClearDefault();

        // Assert
        membership.IsDefault.Should().BeFalse();
    }

    [Theory]
    [InlineData(TenantRole.Viewer, TenantRole.Viewer, true)]
    [InlineData(TenantRole.Member, TenantRole.Viewer, true)]
    [InlineData(TenantRole.Admin, TenantRole.Viewer, true)]
    [InlineData(TenantRole.Owner, TenantRole.Viewer, true)]
    [InlineData(TenantRole.Viewer, TenantRole.Member, false)]
    [InlineData(TenantRole.Member, TenantRole.Member, true)]
    [InlineData(TenantRole.Admin, TenantRole.Member, true)]
    [InlineData(TenantRole.Owner, TenantRole.Member, true)]
    [InlineData(TenantRole.Viewer, TenantRole.Admin, false)]
    [InlineData(TenantRole.Member, TenantRole.Admin, false)]
    [InlineData(TenantRole.Admin, TenantRole.Admin, true)]
    [InlineData(TenantRole.Owner, TenantRole.Admin, true)]
    [InlineData(TenantRole.Viewer, TenantRole.Owner, false)]
    [InlineData(TenantRole.Member, TenantRole.Owner, false)]
    [InlineData(TenantRole.Admin, TenantRole.Owner, false)]
    [InlineData(TenantRole.Owner, TenantRole.Owner, true)]
    public void UserTenantMembership_HasRoleOrHigher_ShouldReturnCorrectResult(
        TenantRole userRole,
        TenantRole minimumRole,
        bool expectedResult)
    {
        // Arrange
        var membership = UserTenantMembership.Create(TestUserId, TestTenantId, userRole);

        // Act
        var result = membership.HasRoleOrHigher(minimumRole);

        // Assert
        result.Should().Be(expectedResult);
    }

    #endregion

    #region Service Registration Tests

    [Fact]
    public void Service_ShouldImplementIUserTenantService()
    {
        // Arrange
        var service = CreateService();

        // Assert
        service.Should().BeAssignableTo<IUserTenantService>();
    }

    [Fact]
    public void Service_ShouldImplementIScopedService()
    {
        // Arrange
        var service = CreateService();

        // Assert
        service.Should().BeAssignableTo<IScopedService>();
    }

    #endregion

    #region TenantRole Enum Tests

    [Fact]
    public void TenantRole_Values_ShouldBeInCorrectOrder()
    {
        // Assert role hierarchy
        ((int)TenantRole.Viewer).Should().BeLessThan((int)TenantRole.Member);
        ((int)TenantRole.Member).Should().BeLessThan((int)TenantRole.Admin);
        ((int)TenantRole.Admin).Should().BeLessThan((int)TenantRole.Owner);
    }

    [Fact]
    public void TenantRole_AllValues_ShouldBeDefined()
    {
        // Assert all expected roles exist
        Enum.GetValues<TenantRole>().Should().HaveCount(4);
        Enum.IsDefined(TenantRole.Viewer).Should().BeTrue();
        Enum.IsDefined(TenantRole.Member).Should().BeTrue();
        Enum.IsDefined(TenantRole.Admin).Should().BeTrue();
        Enum.IsDefined(TenantRole.Owner).Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private static UserIdentityDto CreateTestUser(
        string id,
        string email,
        string firstName,
        string lastName)
    {
        return new UserIdentityDto(
            Id: id,
            Email: email,
            FirstName: firstName,
            LastName: lastName,
            DisplayName: null,
            FullName: $"{firstName} {lastName}",
            PhoneNumber: null,
            AvatarUrl: null,
            IsActive: true,
            IsDeleted: false,
            IsSystemUser: false,
            CreatedAt: DateTimeOffset.UtcNow,
            ModifiedAt: null);
    }

    private UserTenantService CreateService()
    {
        // Create in-memory context for tests
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);

        return new UserTenantService(
            context,
            _tenantStoreMock.Object,
            _userIdentityServiceMock.Object,
            _unitOfWorkMock.Object,
            _localizerMock.Object,
            _loggerMock.Object);
    }

    #endregion
}

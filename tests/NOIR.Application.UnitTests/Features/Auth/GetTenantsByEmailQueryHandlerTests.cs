using NOIR.Application.Features.Auth.Queries.GetTenantsByEmail;

namespace NOIR.Application.UnitTests.Features.Auth;

/// <summary>
/// Unit tests for GetTenantsByEmailQueryHandler.
/// Tests tenant lookup by email for progressive login flow.
/// </summary>
public class GetTenantsByEmailQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly GetTenantsByEmailQueryHandler _handler;

    public GetTenantsByEmailQueryHandlerTests()
    {
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        // Setup NormalizeEmail to just lowercase
        _userIdentityServiceMock
            .Setup(x => x.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(email => email.ToUpperInvariant());

        _handler = new GetTenantsByEmailQueryHandler(
            _userIdentityServiceMock.Object,
            _localizationServiceMock.Object);
    }

    private static UserTenantInfo CreateTestTenantInfo(
        string userId = "user-123",
        string? tenantId = "tenant-abc",
        string tenantName = "Test Tenant",
        string tenantIdentifier = "test-tenant")
    {
        return new UserTenantInfo(userId, tenantId, tenantName, tenantIdentifier);
    }

    #endregion

    #region Success Scenarios - Single Tenant

    [Fact]
    public async Task Handle_WithSingleTenant_ShouldReturnSingleTenantWithAutoSelect()
    {
        // Arrange
        const string email = "test@example.com";
        var tenantInfo = CreateTestTenantInfo();

        _userIdentityServiceMock
            .Setup(x => x.FindTenantsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserTenantInfo> { tenantInfo });

        var query = new GetTenantsByEmailQuery(email);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Email.Should().Be(email);
        result.Value.Tenants.Should().HaveCount(1);
        result.Value.SingleTenant.Should().BeTrue();
        result.Value.AutoSelectedTenantId.Should().Be("tenant-abc");
        result.Value.AutoSelectedTenantIdentifier.Should().Be("test-tenant");
    }

    [Fact]
    public async Task Handle_WithSingleTenant_ShouldReturnCorrectTenantDetails()
    {
        // Arrange
        const string email = "admin@acme.com";
        var tenantInfo = CreateTestTenantInfo(
            userId: "admin-1",
            tenantId: "acme-tenant-id",
            tenantName: "ACME Corporation",
            tenantIdentifier: "acme");

        _userIdentityServiceMock
            .Setup(x => x.FindTenantsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserTenantInfo> { tenantInfo });

        var query = new GetTenantsByEmailQuery(email);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var tenant = result.Value.Tenants.First();
        tenant.TenantId.Should().Be("acme-tenant-id");
        tenant.Name.Should().Be("ACME Corporation");
        tenant.Identifier.Should().Be("acme");
    }

    #endregion

    #region Success Scenarios - Multiple Tenants

    [Fact]
    public async Task Handle_WithMultipleTenants_ShouldReturnAllTenantsNoAutoSelect()
    {
        // Arrange
        const string email = "user@example.com";
        var tenantInfos = new List<UserTenantInfo>
        {
            CreateTestTenantInfo(userId: "user-1", tenantId: "tenant-a", tenantName: "Company A", tenantIdentifier: "company-a"),
            CreateTestTenantInfo(userId: "user-2", tenantId: "tenant-b", tenantName: "Company B", tenantIdentifier: "company-b"),
            CreateTestTenantInfo(userId: "user-3", tenantId: "tenant-c", tenantName: "Company C", tenantIdentifier: "company-c")
        };

        _userIdentityServiceMock
            .Setup(x => x.FindTenantsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantInfos);

        var query = new GetTenantsByEmailQuery(email);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Tenants.Should().HaveCount(3);
        result.Value.SingleTenant.Should().BeFalse();
        result.Value.AutoSelectedTenantId.Should().BeNull();
        result.Value.AutoSelectedTenantIdentifier.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithMultipleTenants_ShouldPreserveOrder()
    {
        // Arrange
        const string email = "user@example.com";
        var tenantInfos = new List<UserTenantInfo>
        {
            CreateTestTenantInfo(tenantId: "first", tenantName: "First Tenant", tenantIdentifier: "first"),
            CreateTestTenantInfo(tenantId: "second", tenantName: "Second Tenant", tenantIdentifier: "second")
        };

        _userIdentityServiceMock
            .Setup(x => x.FindTenantsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantInfos);

        var query = new GetTenantsByEmailQuery(email);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Tenants[0].TenantId.Should().Be("first");
        result.Value.Tenants[1].TenantId.Should().Be("second");
    }

    #endregion

    #region No Tenant Found Scenarios

    [Fact]
    public async Task Handle_WithNoTenantsFound_ShouldReturnEmptyResponse()
    {
        // Arrange
        const string email = "nonexistent@example.com";

        _userIdentityServiceMock
            .Setup(x => x.FindTenantsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserTenantInfo>());

        var query = new GetTenantsByEmailQuery(email);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(email);
        result.Value.Tenants.Should().BeEmpty();
        result.Value.SingleTenant.Should().BeFalse();
        result.Value.AutoSelectedTenantId.Should().BeNull();
        result.Value.AutoSelectedTenantIdentifier.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNoTenantsFound_ShouldNotLeakInformation()
    {
        // Arrange - Security: Don't reveal whether email exists or not
        const string email = "secret@example.com";

        _userIdentityServiceMock
            .Setup(x => x.FindTenantsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserTenantInfo>());

        var query = new GetTenantsByEmailQuery(email);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - Returns success with empty tenants (timing attack prevention)
        result.IsSuccess.Should().BeTrue();
        result.Value.Tenants.Should().BeEmpty();
    }

    #endregion

    #region Validation Scenarios

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WithInvalidEmail_ShouldReturnValidationError(string? email)
    {
        // Arrange
        var query = new GetTenantsByEmailQuery(email!);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be(ErrorCodes.Validation.Required);
    }

    [Fact]
    public async Task Handle_WithWhitespaceEmail_ShouldReturnValidationError()
    {
        // Arrange
        var query = new GetTenantsByEmailQuery("   \t\n   ");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    #endregion

    #region Email Normalization

    [Fact]
    public async Task Handle_ShouldNormalizeEmailBeforeSearch()
    {
        // Arrange
        const string email = "Test@Example.COM";
        var tenantInfo = CreateTestTenantInfo();

        _userIdentityServiceMock
            .Setup(x => x.FindTenantsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserTenantInfo> { tenantInfo });

        var query = new GetTenantsByEmailQuery(email);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _userIdentityServiceMock.Verify(
            x => x.NormalizeEmail(email),
            Times.Once);
        _userIdentityServiceMock.Verify(
            x => x.FindTenantsByEmailAsync("TEST@EXAMPLE.COM", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region CancellationToken Scenarios

    [Fact]
    public async Task Handle_ShouldPassCancellationTokenToService()
    {
        // Arrange
        const string email = "test@example.com";
        var tenantInfo = CreateTestTenantInfo();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _userIdentityServiceMock
            .Setup(x => x.FindTenantsByEmailAsync(It.IsAny<string>(), token))
            .ReturnsAsync(new List<UserTenantInfo> { tenantInfo });

        var query = new GetTenantsByEmailQuery(email);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _userIdentityServiceMock.Verify(
            x => x.FindTenantsByEmailAsync(It.IsAny<string>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        // Arrange
        const string email = "test@example.com";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _userIdentityServiceMock
            .Setup(x => x.FindTenantsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var query = new GetTenantsByEmailQuery(email);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _handler.Handle(query, cts.Token));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithTenantHavingNullTenantId_ShouldHandleGracefully()
    {
        // Arrange - Platform-level users may have null TenantId
        const string email = "platform@example.com";
        var tenantInfo = CreateTestTenantInfo(tenantId: null, tenantName: "Platform", tenantIdentifier: "platform");

        _userIdentityServiceMock
            .Setup(x => x.FindTenantsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserTenantInfo> { tenantInfo });

        var query = new GetTenantsByEmailQuery(email);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Tenants.Should().HaveCount(1);
        result.Value.Tenants[0].TenantId.Should().BeNull();
        result.Value.SingleTenant.Should().BeTrue();
        result.Value.AutoSelectedTenantId.Should().BeNull();
    }

    #endregion
}

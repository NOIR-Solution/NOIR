namespace NOIR.Application.UnitTests.Features.Notifications;

/// <summary>
/// Unit tests for GetPreferencesQueryHandler.
/// </summary>
public class GetPreferencesQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<NotificationPreference, Guid>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILocalizationService> _localizationMock;
    private readonly GetPreferencesQueryHandler _handler;

    private const string TestUserId = "user-123";
    private const string TestTenantId = "default";

    public GetPreferencesQueryHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<NotificationPreference, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();
        _localizationMock = new Mock<ILocalizationService>();

        // Default localization returns key as value
        _localizationMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new GetPreferencesQueryHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _localizationMock.Object);
    }

    private void SetupAuthenticatedUser(string userId = TestUserId)
    {
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
    }

    private void SetupUnauthenticatedUser()
    {
        _currentUserMock.Setup(x => x.UserId).Returns((string?)null);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(false);
    }

    private List<NotificationPreference> CreateTestPreferences()
    {
        return new List<NotificationPreference>
        {
            NotificationPreference.Create(TestUserId, NotificationCategory.System, true, EmailFrequency.Immediate, TestTenantId),
            NotificationPreference.Create(TestUserId, NotificationCategory.Security, true, EmailFrequency.Immediate, TestTenantId),
            NotificationPreference.Create(TestUserId, NotificationCategory.Workflow, true, EmailFrequency.Daily, TestTenantId),
            NotificationPreference.Create(TestUserId, NotificationCategory.UserAction, true, EmailFrequency.Weekly, TestTenantId),
            NotificationPreference.Create(TestUserId, NotificationCategory.Integration, false, EmailFrequency.None, TestTenantId)
        };
    }

    #endregion

    #region Unauthorized Tests

    [Fact]
    public async Task Handle_UnauthenticatedUser_ShouldReturnUnauthorized()
    {
        // Arrange
        SetupUnauthenticatedUser();
        var query = new GetPreferencesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.Auth.Unauthorized);
    }

    #endregion

    #region Success Tests

    [Fact]
    public async Task Handle_WithExistingPreferences_ShouldReturnThem()
    {
        // Arrange
        SetupAuthenticatedUser();
        var preferences = CreateTestPreferences();
        var query = new GetPreferencesQuery();

        _repositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<NotificationPreference>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(5);
    }

    [Fact]
    public async Task Handle_NoExistingPreferences_ShouldCreateDefaults()
    {
        // Arrange
        SetupAuthenticatedUser();
        var query = new GetPreferencesQuery();
        var callCount = 0;

        _repositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<NotificationPreference>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                // First call returns empty, second call returns created defaults
                if (callCount == 1)
                    return new List<NotificationPreference>();
                return NotificationPreference.CreateDefaults(TestUserId, TestTenantId).ToList();
            });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Verify defaults were added
        _repositoryMock.Verify(
            x => x.AddAsync(It.IsAny<NotificationPreference>(), It.IsAny<CancellationToken>()),
            Times.Exactly(5)); // One for each category
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldMapPropertiesCorrectly()
    {
        // Arrange
        SetupAuthenticatedUser();
        var preference = NotificationPreference.Create(
            TestUserId,
            NotificationCategory.Security,
            true,
            EmailFrequency.Immediate,
            TestTenantId);

        var query = new GetPreferencesQuery();

        _repositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<NotificationPreference>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NotificationPreference> { preference });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.First();
        dto.Id.Should().Be(preference.Id);
        dto.Category.Should().Be(NotificationCategory.Security);
        dto.CategoryName.Should().Be("Security");
        dto.InAppEnabled.Should().BeTrue();
        dto.EmailFrequency.Should().Be(EmailFrequency.Immediate);
    }

    #endregion

    #region Category Display Name Tests

    [Theory]
    [InlineData(NotificationCategory.System, "System")]
    [InlineData(NotificationCategory.UserAction, "User Actions")]
    [InlineData(NotificationCategory.Workflow, "Workflow")]
    [InlineData(NotificationCategory.Security, "Security")]
    [InlineData(NotificationCategory.Integration, "Integration")]
    public async Task Handle_ShouldReturnCorrectCategoryDisplayNames(NotificationCategory category, string expectedName)
    {
        // Arrange
        SetupAuthenticatedUser();
        var preference = NotificationPreference.Create(
            TestUserId,
            category,
            true,
            EmailFrequency.Immediate,
            TestTenantId);

        var query = new GetPreferencesQuery();

        _repositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<NotificationPreference>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NotificationPreference> { preference });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.First();
        dto.CategoryName.Should().Be(expectedName);
    }

    #endregion
}

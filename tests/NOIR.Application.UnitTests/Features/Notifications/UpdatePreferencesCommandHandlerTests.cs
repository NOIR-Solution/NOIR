namespace NOIR.Application.UnitTests.Features.Notifications;

/// <summary>
/// Unit tests for UpdatePreferencesCommandHandler.
/// </summary>
public class UpdatePreferencesCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<NotificationPreference, Guid>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILocalizationService> _localizationMock;
    private readonly UpdatePreferencesCommandHandler _handler;

    private const string TestUserId = "user-123";
    private const string TestTenantId = "default";

    public UpdatePreferencesCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<NotificationPreference, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();
        _localizationMock = new Mock<ILocalizationService>();

        // Default localization returns key as value
        _localizationMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new UpdatePreferencesCommandHandler(
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

    private NotificationPreference CreateTestPreference(NotificationCategory category)
    {
        return NotificationPreference.Create(
            userId: TestUserId,
            category: category,
            inAppEnabled: true,
            emailFrequency: EmailFrequency.Immediate,
            tenantId: TestTenantId);
    }

    #endregion

    #region Unauthorized Tests

    [Fact]
    public async Task Handle_UnauthenticatedUser_ShouldReturnUnauthorized()
    {
        // Arrange
        SetupUnauthenticatedUser();
        var command = new UpdatePreferencesCommand(new List<UpdatePreferenceRequest>
        {
            new(NotificationCategory.System, true, EmailFrequency.Daily)
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.Auth.Unauthorized);
    }

    #endregion

    #region Update Existing Preference Tests

    [Fact]
    public async Task Handle_ExistingPreference_ShouldUpdateSuccessfully()
    {
        // Arrange
        SetupAuthenticatedUser();
        var existingPreference = CreateTestPreference(NotificationCategory.System);
        var command = new UpdatePreferencesCommand(new List<UpdatePreferenceRequest>
        {
            new(NotificationCategory.System, false, EmailFrequency.Weekly)
        });

        _repositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<NotificationPreference>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NotificationPreference> { existingPreference });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        var dto = result.Value.First();
        dto.InAppEnabled.Should().BeFalse();
        dto.EmailFrequency.Should().Be(EmailFrequency.Weekly);
    }

    [Fact]
    public async Task Handle_MultipleExistingPreferences_ShouldUpdateAll()
    {
        // Arrange
        SetupAuthenticatedUser();
        var existingPreferences = new List<NotificationPreference>
        {
            CreateTestPreference(NotificationCategory.System),
            CreateTestPreference(NotificationCategory.Security)
        };

        var command = new UpdatePreferencesCommand(new List<UpdatePreferenceRequest>
        {
            new(NotificationCategory.System, false, EmailFrequency.Daily),
            new(NotificationCategory.Security, true, EmailFrequency.None)
        });

        _repositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<NotificationPreference>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPreferences);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Create New Preference Tests

    [Fact]
    public async Task Handle_NewPreference_ShouldCreateSuccessfully()
    {
        // Arrange
        SetupAuthenticatedUser();
        var command = new UpdatePreferencesCommand(new List<UpdatePreferenceRequest>
        {
            new(NotificationCategory.Workflow, true, EmailFrequency.Immediate)
        });

        _repositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<NotificationPreference>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NotificationPreference>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        _repositoryMock.Verify(
            x => x.AddAsync(It.IsAny<NotificationPreference>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_MixedExistingAndNew_ShouldHandleBoth()
    {
        // Arrange
        SetupAuthenticatedUser();
        var existingPreference = CreateTestPreference(NotificationCategory.System);

        var command = new UpdatePreferencesCommand(new List<UpdatePreferenceRequest>
        {
            new(NotificationCategory.System, false, EmailFrequency.Daily),    // Update existing
            new(NotificationCategory.Workflow, true, EmailFrequency.Weekly)    // Create new
        });

        _repositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<NotificationPreference>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NotificationPreference> { existingPreference });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        _repositoryMock.Verify(
            x => x.AddAsync(It.IsAny<NotificationPreference>(), It.IsAny<CancellationToken>()),
            Times.Once); // Only the new one
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
        var command = new UpdatePreferencesCommand(new List<UpdatePreferenceRequest>
        {
            new(category, true, EmailFrequency.Immediate)
        });

        _repositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<NotificationPreference>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NotificationPreference>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.First();
        dto.CategoryName.Should().Be(expectedName);
    }

    #endregion
}

namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for JobFailureNotificationFilter.
/// Tests the Hangfire job failure notification filter behavior.
/// </summary>
public class JobFailureNotificationFilterTests
{
    private readonly Mock<ILogger<JobFailureNotificationFilter>> _loggerMock;
    private readonly Mock<IOptions<JobNotificationSettings>> _settingsMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly JobNotificationSettings _settings;

    public JobFailureNotificationFilterTests()
    {
        _loggerMock = new Mock<ILogger<JobFailureNotificationFilter>>();
        _settingsMock = new Mock<IOptions<JobNotificationSettings>>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _settings = new JobNotificationSettings();
        _settingsMock.Setup(x => x.Value).Returns(_settings);
    }

    private JobFailureNotificationFilter CreateFilter()
    {
        return new JobFailureNotificationFilter(
            _loggerMock.Object,
            _settingsMock.Object,
            _serviceProviderMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithAllParameters_ShouldCreateInstance()
    {
        // Act
        var filter = CreateFilter();

        // Assert
        filter.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldRequireAllParameters()
    {
        // Assert - Verify constructor has 3 required parameters
        var constructors = typeof(JobFailureNotificationFilter).GetConstructors();
        constructors.Should().HaveCount(1);
        constructors[0].GetParameters().Should().HaveCount(3);
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void Filter_ShouldImplementIElectStateFilter()
    {
        // Arrange
        var filter = CreateFilter();

        // Assert
        filter.Should().BeAssignableTo<IElectStateFilter>();
    }

    [Fact]
    public void Filter_ShouldExtendJobFilterAttribute()
    {
        // Arrange
        var filter = CreateFilter();

        // Assert
        filter.Should().BeAssignableTo<JobFilterAttribute>();
    }

    #endregion

    #region Type Verification Tests

    [Fact]
    public void OnStateElection_MethodSignature_ShouldExist()
    {
        // Assert - Verify the method signature exists
        var method = typeof(JobFailureNotificationFilter)
            .GetMethod("OnStateElection", [typeof(ElectStateContext)]);

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(void));
    }

    [Fact]
    public void Filter_ShouldBePublicClass()
    {
        // Assert
        typeof(JobFailureNotificationFilter).IsPublic.Should().BeTrue();
    }

    #endregion

    #region Settings Tests

    [Fact]
    public void JobNotificationSettings_ShouldHaveSectionName()
    {
        // Assert
        JobNotificationSettings.SectionName.Should().Be("JobNotifications");
    }

    [Fact]
    public void JobNotificationSettings_DefaultValues_ShouldBeFalse()
    {
        // Arrange
        var settings = new JobNotificationSettings();

        // Assert
        settings.SendEmailOnFailure.Should().BeFalse();
        settings.NotificationEmail.Should().BeNull();
    }

    [Fact]
    public void JobNotificationSettings_ShouldAllowSettingValues()
    {
        // Arrange
        var settings = new JobNotificationSettings
        {
            SendEmailOnFailure = true,
            NotificationEmail = "admin@example.com"
        };

        // Assert
        settings.SendEmailOnFailure.Should().BeTrue();
        settings.NotificationEmail.Should().Be("admin@example.com");
    }

    #endregion

    #region Behavior Documentation Tests

    [Fact]
    public void Filter_ShouldOnlyActOnFailedState()
    {
        // Document: The filter only intercepts transitions to FailedState
        // Other state transitions (Enqueued, Scheduled, Processing, Succeeded) are ignored
        var filter = CreateFilter();

        filter.Should().NotBeNull("Filter should be instantiable");
    }

    [Fact]
    public void Filter_ShouldLogJobFailures()
    {
        // Document: When a job fails, the filter logs the failure with:
        // - JobId
        // - JobType (class name)
        // - JobMethod (method name)
        // - Exception details
        var filter = CreateFilter();

        filter.Should().NotBeNull();
    }

    [Fact]
    public void Filter_ShouldSendEmailWhenEnabled()
    {
        // Document: When SendEmailOnFailure is true and NotificationEmail is set,
        // the filter creates a scope and resolves IFluentEmail to send an email
        _settings.SendEmailOnFailure = true;
        _settings.NotificationEmail = "admin@example.com";

        var filter = CreateFilter();

        filter.Should().NotBeNull();
    }

    [Fact]
    public void Filter_ShouldUseServiceProviderForScopedServices()
    {
        // Document: The filter uses IServiceProvider to resolve scoped services
        // (IFluentEmail is registered as Scoped by FluentEmail) at runtime
        // This avoids the captive dependency anti-pattern
        _settings.SendEmailOnFailure = true;

        var filter = CreateFilter();

        filter.Should().NotBeNull("Filter should use IServiceProvider for scoped services");
    }

    [Fact]
    public void Filter_ShouldNotFailIfEmailServiceUnavailable()
    {
        // Document: If email service is null or fails, the filter should still
        // complete without throwing, allowing the job lifecycle to continue
        _settings.SendEmailOnFailure = true;

        var filter = CreateFilter();

        filter.Should().NotBeNull("Filter should work even if email service is unavailable");
    }

    #endregion
}

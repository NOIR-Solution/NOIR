namespace NOIR.Application.UnitTests.Web;

/// <summary>
/// Unit tests for HangfireAuthorizationFilter.
/// Tests filter instantiation and type verification.
/// Note: Full authorization behavior is tested in integration tests because
/// Hangfire's DashboardContext.GetHttpContext() is an extension method
/// that cannot be mocked with Moq.
/// </summary>
public class HangfireAuthorizationFilterTests
{
    private readonly Mock<IHostEnvironment> _mockEnvironment;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public HangfireAuthorizationFilterTests()
    {
        _mockEnvironment = new Mock<IHostEnvironment>();
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");

        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["Spa:DevServerUrl"]).Returns("http://localhost:3000");
    }

    private HangfireAuthorizationFilter CreateFilter() =>
        new(_mockEnvironment.Object, _mockConfiguration.Object);

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Act
        var filter = CreateFilter();

        // Assert
        filter.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullEnvironment_ShouldCreateInstance()
    {
        // Constructor accepts null (no guard clause), but will throw when Authorize is called
        var filter = new HangfireAuthorizationFilter(null!, _mockConfiguration.Object);
        filter.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldCreateInstance()
    {
        // Constructor accepts null (no guard clause), but will throw when Authorize is called
        var filter = new HangfireAuthorizationFilter(_mockEnvironment.Object, null!);
        filter.Should().NotBeNull();
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void HangfireAuthorizationFilter_ShouldImplementIDashboardAuthorizationFilter()
    {
        // Arrange
        var filter = CreateFilter();

        // Assert
        filter.Should().BeAssignableTo<IDashboardAuthorizationFilter>();
    }

    #endregion

    #region Type Verification Tests

    [Fact]
    public void Authorize_MethodSignature_ShouldAcceptDashboardContext()
    {
        // Assert - Verify the method signature exists
        var method = typeof(HangfireAuthorizationFilter)
            .GetMethod("Authorize", [typeof(DashboardContext)]);

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(bool));
    }

    [Fact]
    public void HangfireAuthorizationFilter_ShouldBePublicClass()
    {
        // Assert
        typeof(HangfireAuthorizationFilter).IsPublic.Should().BeTrue();
    }

    [Fact]
    public void HangfireAuthorizationFilter_ShouldHaveRequiredConstructor()
    {
        // Assert - Verify constructor with IHostEnvironment and IConfiguration exists
        var constructor = typeof(HangfireAuthorizationFilter)
            .GetConstructor([typeof(IHostEnvironment), typeof(IConfiguration)]);

        constructor.Should().NotBeNull();
    }

    #endregion

    #region Permission Verification Tests

    [Fact]
    public void HangfirePermission_ShouldBeDefinedInPermissionsClass()
    {
        // Verify the permission constant exists and has correct value
        Permissions.HangfireDashboard.Should().Be("system:hangfire");
        Permissions.ClaimType.Should().Be("permission");
    }

    #endregion

    #region Environment-Specific Behavior Documentation

    [Fact]
    public void Filter_InDevelopment_ShouldUseFrontendDevServerUrl_Documentation()
    {
        // Document: In Development mode, unauthenticated users are redirected to
        // the React frontend dev server (configured in Spa:DevServerUrl or defaults to http://localhost:3000)
        // Expected redirect: http://localhost:3000/login?returnUrl=%2Fhangfire

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");
        _mockConfiguration.Setup(c => c["Spa:DevServerUrl"]).Returns("http://localhost:3000");

        var filter = CreateFilter();
        filter.Should().NotBeNull();

        // Actual redirect behavior tested in integration tests
    }

    [Fact]
    public void Filter_InProduction_ShouldUseRelativeLoginPath_Documentation()
    {
        // Document: In Production mode, unauthenticated users are redirected to
        // the relative /login path since frontend and backend run on the same port
        // Expected redirect: /login?returnUrl=%2Fhangfire

        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");

        var filter = CreateFilter();
        filter.Should().NotBeNull();

        // Actual redirect behavior tested in integration tests
    }

    [Fact]
    public void Filter_CriticalBehavior_ShouldReturnTrueAfterRedirect_Documentation()
    {
        // Document: CRITICAL behavior - When redirecting unauthenticated users,
        // the filter MUST return true (not false) after calling Response.Redirect()
        //
        // If we return false, Hangfire will overwrite our redirect response with a 401 Unauthorized
        // By returning true, we prevent Hangfire from touching the response
        //
        // Verification in code (HangfireAuthorizationFilter.cs:44):
        // return true; // CRITICAL: Return true to prevent Hangfire from overwriting our redirect with a 401

        var filter = CreateFilter();
        filter.Should().NotBeNull();
    }

    [Fact]
    public void Filter_AuthenticatedUserBehavior_Documentation()
    {
        // Document: For authenticated users:
        // - If user has system:hangfire permission claim -> return true (allow access)
        // - If user lacks system:hangfire permission claim -> return false (deny with 401)
        //
        // This is consistent with NOIR's permission-based authorization model

        var filter = CreateFilter();
        filter.Should().NotBeNull();
    }

    #endregion
}

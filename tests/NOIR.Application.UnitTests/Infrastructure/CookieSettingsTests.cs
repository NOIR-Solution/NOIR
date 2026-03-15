namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for CookieSettings.
/// Tests configuration and SameSiteMode parsing.
/// </summary>
public class CookieSettingsTests
{
    #region Default Values Tests

    [Fact]
    public void CookieSettings_DefaultAccessTokenCookieName_ShouldBeNoirAccess()
    {
        // Arrange & Act
        var settings = new CookieSettings();

        // Assert
        settings.AccessTokenCookieName.ShouldBe("noir.access");
    }

    [Fact]
    public void CookieSettings_DefaultRefreshTokenCookieName_ShouldBeNoirRefresh()
    {
        // Arrange & Act
        var settings = new CookieSettings();

        // Assert
        settings.RefreshTokenCookieName.ShouldBe("noir.refresh");
    }

    [Fact]
    public void CookieSettings_DefaultSameSiteMode_ShouldBeStrict()
    {
        // Arrange & Act
        var settings = new CookieSettings();

        // Assert
        settings.SameSiteMode.ShouldBe("Strict");
    }

    [Fact]
    public void CookieSettings_DefaultPath_ShouldBeRoot()
    {
        // Arrange & Act
        var settings = new CookieSettings();

        // Assert
        settings.Path.ShouldBe("/");
    }

    [Fact]
    public void CookieSettings_DefaultSecureInProduction_ShouldBeTrue()
    {
        // Arrange & Act
        var settings = new CookieSettings();

        // Assert
        settings.SecureInProduction.ShouldBe(true);
    }

    [Fact]
    public void CookieSettings_DefaultDomain_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new CookieSettings();

        // Assert
        settings.Domain.ShouldBeNull();
    }

    [Fact]
    public void CookieSettings_SectionName_ShouldBeCookieSettings()
    {
        // Assert
        CookieSettings.SectionName.ShouldBe("CookieSettings");
    }

    #endregion

    #region GetSameSiteMode Tests

    [Fact]
    public void GetSameSiteMode_WhenStrict_ShouldReturnStrictEnum()
    {
        // Arrange
        var settings = new CookieSettings { SameSiteMode = "Strict" };

        // Act
        var result = settings.GetSameSiteMode();

        // Assert
        result.ShouldBe(Microsoft.AspNetCore.Http.SameSiteMode.Strict);
    }

    [Fact]
    public void GetSameSiteMode_WhenLax_ShouldReturnLaxEnum()
    {
        // Arrange
        var settings = new CookieSettings { SameSiteMode = "Lax" };

        // Act
        var result = settings.GetSameSiteMode();

        // Assert
        result.ShouldBe(Microsoft.AspNetCore.Http.SameSiteMode.Lax);
    }

    [Fact]
    public void GetSameSiteMode_WhenNone_ShouldReturnNoneEnum()
    {
        // Arrange
        var settings = new CookieSettings { SameSiteMode = "None" };

        // Act
        var result = settings.GetSameSiteMode();

        // Assert
        result.ShouldBe(Microsoft.AspNetCore.Http.SameSiteMode.None);
    }

    [Fact]
    public void GetSameSiteMode_WhenInvalid_ShouldDefaultToStrict()
    {
        // Arrange
        var settings = new CookieSettings { SameSiteMode = "InvalidValue" };

        // Act
        var result = settings.GetSameSiteMode();

        // Assert
        result.ShouldBe(Microsoft.AspNetCore.Http.SameSiteMode.Strict);
    }

    [Fact]
    public void GetSameSiteMode_WhenEmpty_ShouldDefaultToStrict()
    {
        // Arrange
        var settings = new CookieSettings { SameSiteMode = string.Empty };

        // Act
        var result = settings.GetSameSiteMode();

        // Assert
        result.ShouldBe(Microsoft.AspNetCore.Http.SameSiteMode.Strict);
    }

    #endregion

    #region Custom Configuration Tests

    [Fact]
    public void CookieSettings_CanSetCustomCookieNames()
    {
        // Arrange
        var settings = new CookieSettings
        {
            AccessTokenCookieName = "custom.access",
            RefreshTokenCookieName = "custom.refresh"
        };

        // Assert
        settings.AccessTokenCookieName.ShouldBe("custom.access");
        settings.RefreshTokenCookieName.ShouldBe("custom.refresh");
    }

    [Fact]
    public void CookieSettings_CanSetCustomDomain()
    {
        // Arrange
        var settings = new CookieSettings { Domain = ".example.com" };

        // Assert
        settings.Domain.ShouldBe(".example.com");
    }

    [Fact]
    public void CookieSettings_CanSetCustomPath()
    {
        // Arrange
        var settings = new CookieSettings { Path = "/api" };

        // Assert
        settings.Path.ShouldBe("/api");
    }

    [Fact]
    public void CookieSettings_CanDisableSecureInProduction()
    {
        // Arrange
        var settings = new CookieSettings { SecureInProduction = false };

        // Assert
        settings.SecureInProduction.ShouldBe(false);
    }

    #endregion
}

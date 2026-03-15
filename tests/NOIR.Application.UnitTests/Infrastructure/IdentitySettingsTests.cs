namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for IdentitySettings configuration.
/// Tests default values and configuration binding.
/// </summary>
public class IdentitySettingsTests
{
    #region Default Values Tests

    [Fact]
    public void IdentitySettings_DefaultConstructor_ShouldHaveDefaults()
    {
        // Act
        var settings = new IdentitySettings();

        // Assert
        settings.Password.ShouldNotBeNull();
        settings.Lockout.ShouldNotBeNull();
    }

    [Fact]
    public void PasswordSettings_Defaults_ShouldBeProductionStrength()
    {
        // Act
        var settings = new PasswordSettings();

        // Assert - Production defaults (strong policy)
        settings.RequireDigit.ShouldBe(true);
        settings.RequireLowercase.ShouldBe(true);
        settings.RequireUppercase.ShouldBe(true);
        settings.RequireNonAlphanumeric.ShouldBe(true);
        settings.RequiredLength.ShouldBe(12);
        settings.RequiredUniqueChars.ShouldBe(4);
    }

    [Fact]
    public void LockoutSettings_Defaults_ShouldBeProductionStrength()
    {
        // Act
        var settings = new LockoutSettings();

        // Assert - Production defaults
        settings.DefaultLockoutTimeSpanMinutes.ShouldBe(15);
        settings.MaxFailedAccessAttempts.ShouldBe(5);
        settings.AllowedForNewUsers.ShouldBe(true);
    }

    #endregion

    #region Section Name Tests

    [Fact]
    public void IdentitySettings_SectionName_ShouldBeIdentity()
    {
        // Assert
        IdentitySettings.SectionName.ShouldBe("Identity");
    }

    #endregion

    #region Configuration Binding Tests

    [Fact]
    public void PasswordSettings_CanBeConfiguredForDevelopment()
    {
        // Arrange - Development-friendly settings
        var settings = new PasswordSettings
        {
            RequireDigit = false,
            RequireLowercase = false,
            RequireUppercase = false,
            RequireNonAlphanumeric = false,
            RequiredLength = 6,
            RequiredUniqueChars = 1
        };

        // Assert
        settings.RequireDigit.ShouldBe(false);
        settings.RequireLowercase.ShouldBe(false);
        settings.RequireUppercase.ShouldBe(false);
        settings.RequireNonAlphanumeric.ShouldBe(false);
        settings.RequiredLength.ShouldBe(6);
        settings.RequiredUniqueChars.ShouldBe(1);
    }

    [Fact]
    public void LockoutSettings_CanBeConfiguredForDevelopment()
    {
        // Arrange - Development-friendly settings
        var settings = new LockoutSettings
        {
            DefaultLockoutTimeSpanMinutes = 5,
            MaxFailedAccessAttempts = 10,
            AllowedForNewUsers = true
        };

        // Assert
        settings.DefaultLockoutTimeSpanMinutes.ShouldBe(5);
        settings.MaxFailedAccessAttempts.ShouldBe(10);
        settings.AllowedForNewUsers.ShouldBe(true);
    }

    [Fact]
    public void IdentitySettings_CanBeFullyConfigured()
    {
        // Arrange
        var settings = new IdentitySettings
        {
            Password = new PasswordSettings
            {
                RequireDigit = false,
                RequiredLength = 8
            },
            Lockout = new LockoutSettings
            {
                MaxFailedAccessAttempts = 3
            }
        };

        // Assert
        settings.Password.RequireDigit.ShouldBe(false);
        settings.Password.RequiredLength.ShouldBe(8);
        settings.Lockout.MaxFailedAccessAttempts.ShouldBe(3);
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    [InlineData(128)]
    public void PasswordSettings_RequiredLength_AcceptsVariousValues(int length)
    {
        // Arrange
        var settings = new PasswordSettings { RequiredLength = length };

        // Assert
        settings.RequiredLength.ShouldBe(length);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(10)]
    public void PasswordSettings_RequiredUniqueChars_AcceptsVariousValues(int uniqueChars)
    {
        // Arrange
        var settings = new PasswordSettings { RequiredUniqueChars = uniqueChars };

        // Assert
        settings.RequiredUniqueChars.ShouldBe(uniqueChars);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(60)]
    [InlineData(1440)] // 24 hours
    public void LockoutSettings_DefaultLockoutTimeSpanMinutes_AcceptsVariousValues(int minutes)
    {
        // Arrange
        var settings = new LockoutSettings { DefaultLockoutTimeSpanMinutes = minutes };

        // Assert
        settings.DefaultLockoutTimeSpanMinutes.ShouldBe(minutes);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public void LockoutSettings_MaxFailedAccessAttempts_AcceptsVariousValues(int attempts)
    {
        // Arrange
        var settings = new LockoutSettings { MaxFailedAccessAttempts = attempts };

        // Assert
        settings.MaxFailedAccessAttempts.ShouldBe(attempts);
    }

    #endregion
}

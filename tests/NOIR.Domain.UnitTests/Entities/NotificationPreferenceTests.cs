namespace NOIR.Domain.UnitTests.Entities;

using NOIR.Domain.Enums;

/// <summary>
/// Unit tests for the NotificationPreference entity.
/// Tests factory methods, preference updates, and default generation.
/// </summary>
public class NotificationPreferenceTests
{
    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidPreference()
    {
        // Arrange
        var userId = "user-123";
        var category = NotificationCategory.System;

        // Act
        var preference = NotificationPreference.Create(userId, category);

        // Assert
        preference.Should().NotBeNull();
        preference.Id.Should().NotBe(Guid.Empty);
        preference.UserId.Should().Be(userId);
        preference.Category.Should().Be(category);
        preference.InAppEnabled.Should().BeTrue(); // Default
        preference.EmailFrequency.Should().Be(EmailFrequency.Daily); // Default
    }

    [Fact]
    public void Create_WithCustomSettings_ShouldOverrideDefaults()
    {
        // Arrange
        var userId = "user-123";
        var category = NotificationCategory.Security;
        var inAppEnabled = false;
        var emailFrequency = EmailFrequency.Immediate;

        // Act
        var preference = NotificationPreference.Create(userId, category, inAppEnabled, emailFrequency);

        // Assert
        preference.InAppEnabled.Should().BeFalse();
        preference.EmailFrequency.Should().Be(EmailFrequency.Immediate);
    }

    [Fact]
    public void Create_WithNullUserId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => NotificationPreference.Create(null!, NotificationCategory.System);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => NotificationPreference.Create("", NotificationCategory.System);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithTenantId_ShouldBeAssociatedWithTenant()
    {
        // Arrange
        var tenantId = "tenant-abc";

        // Act
        var preference = NotificationPreference.Create("user-123", NotificationCategory.System, tenantId: tenantId);

        // Assert
        preference.TenantId.Should().Be(tenantId);
    }

    [Theory]
    [InlineData(NotificationCategory.System)]
    [InlineData(NotificationCategory.UserAction)]
    [InlineData(NotificationCategory.Workflow)]
    [InlineData(NotificationCategory.Security)]
    [InlineData(NotificationCategory.Integration)]
    public void Create_WithVariousCategories_ShouldSetCorrectCategory(NotificationCategory category)
    {
        // Act
        var preference = NotificationPreference.Create("user-123", category);

        // Assert
        preference.Category.Should().Be(category);
    }

    #endregion

    #region CreateDefaults Tests

    [Fact]
    public void CreateDefaults_ShouldCreatePreferencesForAllCategories()
    {
        // Arrange
        var userId = "user-123";
        var expectedCategories = Enum.GetValues<NotificationCategory>().Length;

        // Act
        var preferences = NotificationPreference.CreateDefaults(userId).ToList();

        // Assert
        preferences.Should().HaveCount(expectedCategories);
    }

    [Fact]
    public void CreateDefaults_ShouldSetImmediateEmailForSecurityCategory()
    {
        // Arrange
        var userId = "user-123";

        // Act
        var preferences = NotificationPreference.CreateDefaults(userId).ToList();

        // Assert
        var securityPref = preferences.Single(p => p.Category == NotificationCategory.Security);
        securityPref.EmailFrequency.Should().Be(EmailFrequency.Immediate);
    }

    [Fact]
    public void CreateDefaults_ShouldSetDailyEmailForNonSecurityCategories()
    {
        // Arrange
        var userId = "user-123";

        // Act
        var preferences = NotificationPreference.CreateDefaults(userId).ToList();

        // Assert
        var nonSecurityPrefs = preferences.Where(p => p.Category != NotificationCategory.Security);
        nonSecurityPrefs.Should().OnlyContain(p => p.EmailFrequency == EmailFrequency.Daily);
    }

    [Fact]
    public void CreateDefaults_AllPreferencesShouldHaveInAppEnabled()
    {
        // Arrange
        var userId = "user-123";

        // Act
        var preferences = NotificationPreference.CreateDefaults(userId).ToList();

        // Assert
        preferences.Should().OnlyContain(p => p.InAppEnabled == true);
    }

    [Fact]
    public void CreateDefaults_WithTenantId_AllPreferencesShouldHaveTenantId()
    {
        // Arrange
        var userId = "user-123";
        var tenantId = "tenant-abc";

        // Act
        var preferences = NotificationPreference.CreateDefaults(userId, tenantId).ToList();

        // Assert
        preferences.Should().OnlyContain(p => p.TenantId == tenantId);
    }

    [Fact]
    public void CreateDefaults_EachPreferenceShouldHaveUniqueId()
    {
        // Arrange
        var userId = "user-123";

        // Act
        var preferences = NotificationPreference.CreateDefaults(userId).ToList();

        // Assert
        var ids = preferences.Select(p => p.Id).ToList();
        ids.Distinct().Count().Should().Be(ids.Count);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldModifyBothSettings()
    {
        // Arrange
        var preference = NotificationPreference.Create("user-123", NotificationCategory.System);

        // Act
        preference.Update(false, EmailFrequency.Weekly);

        // Assert
        preference.InAppEnabled.Should().BeFalse();
        preference.EmailFrequency.Should().Be(EmailFrequency.Weekly);
    }

    [Fact]
    public void Update_ToSameValues_ShouldNotThrow()
    {
        // Arrange
        var preference = NotificationPreference.Create("user-123", NotificationCategory.System);
        var initialInApp = preference.InAppEnabled;
        var initialEmail = preference.EmailFrequency;

        // Act
        var act = () => preference.Update(initialInApp, initialEmail);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region EnableInApp/DisableInApp Tests

    [Fact]
    public void EnableInApp_ShouldSetInAppEnabledToTrue()
    {
        // Arrange
        var preference = NotificationPreference.Create("user-123", NotificationCategory.System, inAppEnabled: false);

        // Act
        preference.EnableInApp();

        // Assert
        preference.InAppEnabled.Should().BeTrue();
    }

    [Fact]
    public void DisableInApp_ShouldSetInAppEnabledToFalse()
    {
        // Arrange
        var preference = NotificationPreference.Create("user-123", NotificationCategory.System);

        // Act
        preference.DisableInApp();

        // Assert
        preference.InAppEnabled.Should().BeFalse();
    }

    [Fact]
    public void EnableInApp_WhenAlreadyEnabled_ShouldRemainEnabled()
    {
        // Arrange
        var preference = NotificationPreference.Create("user-123", NotificationCategory.System);

        // Act
        preference.EnableInApp();

        // Assert
        preference.InAppEnabled.Should().BeTrue();
    }

    [Fact]
    public void DisableInApp_WhenAlreadyDisabled_ShouldRemainDisabled()
    {
        // Arrange
        var preference = NotificationPreference.Create("user-123", NotificationCategory.System, inAppEnabled: false);

        // Act
        preference.DisableInApp();

        // Assert
        preference.InAppEnabled.Should().BeFalse();
    }

    #endregion

    #region SetEmailFrequency Tests

    [Theory]
    [InlineData(EmailFrequency.None)]
    [InlineData(EmailFrequency.Immediate)]
    [InlineData(EmailFrequency.Daily)]
    [InlineData(EmailFrequency.Weekly)]
    public void SetEmailFrequency_ShouldSetCorrectFrequency(EmailFrequency frequency)
    {
        // Arrange
        var preference = NotificationPreference.Create("user-123", NotificationCategory.System);

        // Act
        preference.SetEmailFrequency(frequency);

        // Assert
        preference.EmailFrequency.Should().Be(frequency);
    }

    [Fact]
    public void SetEmailFrequency_FromNoneToImmediate_ShouldUpdate()
    {
        // Arrange
        var preference = NotificationPreference.Create("user-123", NotificationCategory.System, emailFrequency: EmailFrequency.None);

        // Act
        preference.SetEmailFrequency(EmailFrequency.Immediate);

        // Assert
        preference.EmailFrequency.Should().Be(EmailFrequency.Immediate);
    }

    #endregion

    #region Combination Tests

    [Fact]
    public void FullyDisableNotifications_ShouldDisableBothChannels()
    {
        // Arrange
        var preference = NotificationPreference.Create("user-123", NotificationCategory.System);

        // Act
        preference.DisableInApp();
        preference.SetEmailFrequency(EmailFrequency.None);

        // Assert
        preference.InAppEnabled.Should().BeFalse();
        preference.EmailFrequency.Should().Be(EmailFrequency.None);
    }

    [Fact]
    public void UpdateMultipleTimes_ShouldUseLatestSettings()
    {
        // Arrange
        var preference = NotificationPreference.Create("user-123", NotificationCategory.System);

        // Act
        preference.Update(false, EmailFrequency.None);
        preference.Update(true, EmailFrequency.Immediate);
        preference.Update(true, EmailFrequency.Weekly);

        // Assert
        preference.InAppEnabled.Should().BeTrue();
        preference.EmailFrequency.Should().Be(EmailFrequency.Weekly);
    }

    #endregion
}

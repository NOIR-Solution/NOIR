namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for the EmailTemplate entity.
/// Tests factory methods, property updates, and state transitions.
/// </summary>
public class EmailTemplateTests
{
    #region CreatePlatformDefault Factory Tests

    [Fact]
    public void CreatePlatformDefault_WithRequiredParameters_ShouldCreateValidTemplate()
    {
        // Arrange
        var name = "PasswordResetOtp";
        var subject = "Reset Your Password";
        var htmlBody = "<p>Your code is {{OtpCode}}</p>";

        // Act
        var template = EmailTemplate.CreatePlatformDefault(name, subject, htmlBody);

        // Assert
        template.Should().NotBeNull();
        template.Id.Should().NotBe(Guid.Empty);
        template.Name.Should().Be(name);
        template.Subject.Should().Be(subject);
        template.HtmlBody.Should().Be(htmlBody);
        template.TenantId.Should().BeNull();
        template.IsPlatformDefault.Should().BeTrue();
        template.IsTenantOverride.Should().BeFalse();
        template.IsActive.Should().BeTrue();
        template.Version.Should().Be(1);
    }

    [Fact]
    public void CreatePlatformDefault_WithOptionalParameters_ShouldSetAllProperties()
    {
        // Arrange
        var name = "WelcomeEmail";
        var subject = "Welcome {{UserName}}!";
        var htmlBody = "<h1>Welcome!</h1>";
        var plainTextBody = "Welcome!";
        var description = "Email sent to new users";
        var availableVariables = "[\"UserName\", \"Email\"]";

        // Act
        var template = EmailTemplate.CreatePlatformDefault(
            name,
            subject,
            htmlBody,
            plainTextBody,
            description,
            availableVariables);

        // Assert
        template.PlainTextBody.Should().Be(plainTextBody);
        template.Description.Should().Be(description);
        template.AvailableVariables.Should().Be(availableVariables);
        template.TenantId.Should().BeNull();
        template.IsPlatformDefault.Should().BeTrue();
    }

    [Fact]
    public void CreatePlatformDefault_WithoutOptionalParameters_ShouldHaveNullOptionalFields()
    {
        // Act
        var template = EmailTemplate.CreatePlatformDefault("Test", "Subject", "<p>Body</p>");

        // Assert
        template.PlainTextBody.Should().BeNull();
        template.Description.Should().BeNull();
        template.AvailableVariables.Should().BeNull();
        template.TenantId.Should().BeNull();
        template.IsPlatformDefault.Should().BeTrue();
    }

    #endregion

    #region CreateTenantOverride Factory Tests

    [Fact]
    public void CreateTenantOverride_WithRequiredParameters_ShouldCreateValidTenantTemplate()
    {
        // Arrange
        var tenantId = "tenant-123";
        var name = "WelcomeEmail";
        var subject = "Welcome {{UserName}}!";
        var htmlBody = "<h1>Welcome!</h1>";

        // Act
        var template = EmailTemplate.CreateTenantOverride(tenantId, name, subject, htmlBody);

        // Assert
        template.Should().NotBeNull();
        template.Id.Should().NotBe(Guid.Empty);
        template.TenantId.Should().Be(tenantId);
        template.IsPlatformDefault.Should().BeFalse();
        template.IsTenantOverride.Should().BeTrue();
        template.Name.Should().Be(name);
        template.Subject.Should().Be(subject);
        template.HtmlBody.Should().Be(htmlBody);
        template.IsActive.Should().BeTrue();
        template.Version.Should().Be(1);
    }

    [Fact]
    public void CreateTenantOverride_WithNullTenantId_ShouldThrowException()
    {
        // Act & Assert
        var act = () => EmailTemplate.CreateTenantOverride(null!, "Test", "Subject", "<p>Body</p>");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tenantId");
    }

    [Fact]
    public void CreateTenantOverride_WithWhitespaceTenantId_ShouldThrowException()
    {
        // Act & Assert
        var act = () => EmailTemplate.CreateTenantOverride("  ", "Test", "Subject", "<p>Body</p>");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tenantId");
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldModifyContentProperties()
    {
        // Arrange
        var template = EmailTemplate.CreatePlatformDefault("Test", "Old Subject", "<p>Old</p>");
        var newSubject = "New Subject";
        var newHtmlBody = "<p>New Content</p>";

        // Act
        template.Update(newSubject, newHtmlBody);

        // Assert
        template.Subject.Should().Be(newSubject);
        template.HtmlBody.Should().Be(newHtmlBody);
    }

    [Fact]
    public void Update_ShouldIncrementVersion()
    {
        // Arrange
        var template = EmailTemplate.CreatePlatformDefault("Test", "Subject", "<p>Body</p>");
        var initialVersion = template.Version;

        // Act
        template.Update("New Subject", "<p>New Body</p>");

        // Assert
        template.Version.Should().Be(initialVersion + 1);
    }

    [Fact]
    public void Update_MultipleTimes_ShouldIncrementVersionEachTime()
    {
        // Arrange
        var template = EmailTemplate.CreatePlatformDefault("Test", "Subject", "<p>Body</p>");

        // Act
        template.Update("Update 1", "<p>1</p>");
        template.Update("Update 2", "<p>2</p>");
        template.Update("Update 3", "<p>3</p>");

        // Assert
        template.Version.Should().Be(4); // Initial 1 + 3 updates
    }

    [Fact]
    public void Update_WithOptionalParameters_ShouldUpdateAllFields()
    {
        // Arrange
        var template = EmailTemplate.CreatePlatformDefault("Test", "Subject", "<p>Body</p>");
        var newPlainText = "Plain text version";
        var newDescription = "Updated description";
        var newVariables = "[\"Var1\", \"Var2\"]";

        // Act
        template.Update("Subject", "<p>Body</p>", newPlainText, newDescription, newVariables);

        // Assert
        template.PlainTextBody.Should().Be(newPlainText);
        template.Description.Should().Be(newDescription);
        template.AvailableVariables.Should().Be(newVariables);
    }

    #endregion

    #region Activate/Deactivate Tests

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var template = EmailTemplate.CreatePlatformDefault("Test", "Subject", "<p>Body</p>");
        template.Deactivate();

        // Act
        template.Activate();

        // Assert
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var template = EmailTemplate.CreatePlatformDefault("Test", "Subject", "<p>Body</p>");

        // Act
        template.Deactivate();

        // Assert
        template.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldRemainActive()
    {
        // Arrange
        var template = EmailTemplate.CreatePlatformDefault("Test", "Subject", "<p>Body</p>");

        // Act
        template.Activate();

        // Assert
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_MultipleTimes_ShouldRemainInactive()
    {
        // Arrange
        var template = EmailTemplate.CreatePlatformDefault("Test", "Subject", "<p>Body</p>");

        // Act
        template.Deactivate();
        template.Deactivate();

        // Assert
        template.IsActive.Should().BeFalse();
    }

    #endregion

    #region Template Variable Tests

    [Fact]
    public void CreatePlatformDefault_WithTemplateVariablesInSubject_ShouldPreserveVariables()
    {
        // Arrange
        var subject = "Hello {{UserName}}, your order #{{OrderId}} is ready";

        // Act
        var template = EmailTemplate.CreatePlatformDefault("OrderReady", subject, "<p>Body</p>");

        // Assert
        template.Subject.Should().Contain("{{UserName}}");
        template.Subject.Should().Contain("{{OrderId}}");
    }

    [Fact]
    public void CreatePlatformDefault_WithTemplateVariablesInHtmlBody_ShouldPreserveVariables()
    {
        // Arrange
        var htmlBody = "<p>Hello {{UserName}},</p><p>Your OTP is: {{OtpCode}}</p>";

        // Act
        var template = EmailTemplate.CreatePlatformDefault("OtpEmail", "Your OTP", htmlBody);

        // Assert
        template.HtmlBody.Should().Contain("{{UserName}}");
        template.HtmlBody.Should().Contain("{{OtpCode}}");
    }

    #endregion

    #region Platform vs Tenant Tests

    [Fact]
    public void CreateTenantOverride_WithTenantId_ShouldBeAssociatedWithTenant()
    {
        // Arrange
        var tenantId = "tenant-abc-123";

        // Act
        var template = EmailTemplate.CreateTenantOverride(tenantId, "Test", "Subject", "<p>Body</p>");

        // Assert
        template.TenantId.Should().Be(tenantId);
        template.IsTenantOverride.Should().BeTrue();
        template.IsPlatformDefault.Should().BeFalse();
    }

    [Fact]
    public void CreatePlatformDefault_WithoutTenantId_ShouldBeGlobalTemplate()
    {
        // Act
        var template = EmailTemplate.CreatePlatformDefault("Test", "Subject", "<p>Body</p>");

        // Assert
        template.TenantId.Should().BeNull();
        template.IsPlatformDefault.Should().BeTrue();
        template.IsTenantOverride.Should().BeFalse();
    }

    [Fact]
    public void IsPlatformDefault_ShouldReturnTrueForPlatformTemplates()
    {
        // Arrange
        var template = EmailTemplate.CreatePlatformDefault("Test", "Subject", "<p>Body</p>");

        // Act & Assert
        template.IsPlatformDefault.Should().BeTrue();
    }

    [Fact]
    public void IsTenantOverride_ShouldReturnTrueForTenantTemplates()
    {
        // Arrange
        var template = EmailTemplate.CreateTenantOverride("tenant-123", "Test", "Subject", "<p>Body</p>");

        // Act & Assert
        template.IsTenantOverride.Should().BeTrue();
    }

    #endregion
}

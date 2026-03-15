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
        template.ShouldNotBeNull();
        template.Id.ShouldNotBe(Guid.Empty);
        template.Name.ShouldBe(name);
        template.Subject.ShouldBe(subject);
        template.HtmlBody.ShouldBe(htmlBody);
        template.TenantId.ShouldBeNull();
        template.IsPlatformDefault.ShouldBeTrue();
        template.IsTenantOverride.ShouldBeFalse();
        template.IsActive.ShouldBeTrue();
        template.Version.ShouldBe(1);
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
        template.PlainTextBody.ShouldBe(plainTextBody);
        template.Description.ShouldBe(description);
        template.AvailableVariables.ShouldBe(availableVariables);
        template.TenantId.ShouldBeNull();
        template.IsPlatformDefault.ShouldBeTrue();
    }

    [Fact]
    public void CreatePlatformDefault_WithoutOptionalParameters_ShouldHaveNullOptionalFields()
    {
        // Act
        var template = EmailTemplate.CreatePlatformDefault("Test", "Subject", "<p>Body</p>");

        // Assert
        template.PlainTextBody.ShouldBeNull();
        template.Description.ShouldBeNull();
        template.AvailableVariables.ShouldBeNull();
        template.TenantId.ShouldBeNull();
        template.IsPlatformDefault.ShouldBeTrue();
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
        template.ShouldNotBeNull();
        template.Id.ShouldNotBe(Guid.Empty);
        template.TenantId.ShouldBe(tenantId);
        template.IsPlatformDefault.ShouldBeFalse();
        template.IsTenantOverride.ShouldBeTrue();
        template.Name.ShouldBe(name);
        template.Subject.ShouldBe(subject);
        template.HtmlBody.ShouldBe(htmlBody);
        template.IsActive.ShouldBeTrue();
        template.Version.ShouldBe(1);
    }

    [Fact]
    public void CreateTenantOverride_WithNullTenantId_ShouldThrowException()
    {
        // Act & Assert
        var act = () => EmailTemplate.CreateTenantOverride(null!, "Test", "Subject", "<p>Body</p>");
        Should.Throw<ArgumentException>(act)
            .ParamName.ShouldBe("tenantId");
    }

    [Fact]
    public void CreateTenantOverride_WithWhitespaceTenantId_ShouldThrowException()
    {
        // Act & Assert
        var act = () => EmailTemplate.CreateTenantOverride("  ", "Test", "Subject", "<p>Body</p>");
        Should.Throw<ArgumentException>(act)
            .ParamName.ShouldBe("tenantId");
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
        template.Subject.ShouldBe(newSubject);
        template.HtmlBody.ShouldBe(newHtmlBody);
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
        template.Version.ShouldBe(initialVersion + 1);
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
        template.Version.ShouldBe(4); // Initial 1 + 3 updates
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
        template.PlainTextBody.ShouldBe(newPlainText);
        template.Description.ShouldBe(newDescription);
        template.AvailableVariables.ShouldBe(newVariables);
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
        template.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var template = EmailTemplate.CreatePlatformDefault("Test", "Subject", "<p>Body</p>");

        // Act
        template.Deactivate();

        // Assert
        template.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldRemainActive()
    {
        // Arrange
        var template = EmailTemplate.CreatePlatformDefault("Test", "Subject", "<p>Body</p>");

        // Act
        template.Activate();

        // Assert
        template.IsActive.ShouldBeTrue();
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
        template.IsActive.ShouldBeFalse();
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
        template.Subject.ShouldContain("{{UserName}}");
        template.Subject.ShouldContain("{{OrderId}}");
    }

    [Fact]
    public void CreatePlatformDefault_WithTemplateVariablesInHtmlBody_ShouldPreserveVariables()
    {
        // Arrange
        var htmlBody = "<p>Hello {{UserName}},</p><p>Your OTP is: {{OtpCode}}</p>";

        // Act
        var template = EmailTemplate.CreatePlatformDefault("OtpEmail", "Your OTP", htmlBody);

        // Assert
        template.HtmlBody.ShouldContain("{{UserName}}");
        template.HtmlBody.ShouldContain("{{OtpCode}}");
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
        template.TenantId.ShouldBe(tenantId);
        template.IsTenantOverride.ShouldBeTrue();
        template.IsPlatformDefault.ShouldBeFalse();
    }

    [Fact]
    public void CreatePlatformDefault_WithoutTenantId_ShouldBeGlobalTemplate()
    {
        // Act
        var template = EmailTemplate.CreatePlatformDefault("Test", "Subject", "<p>Body</p>");

        // Assert
        template.TenantId.ShouldBeNull();
        template.IsPlatformDefault.ShouldBeTrue();
        template.IsTenantOverride.ShouldBeFalse();
    }

    [Fact]
    public void IsPlatformDefault_ShouldReturnTrueForPlatformTemplates()
    {
        // Arrange
        var template = EmailTemplate.CreatePlatformDefault("Test", "Subject", "<p>Body</p>");

        // Act & Assert
        template.IsPlatformDefault.ShouldBeTrue();
    }

    [Fact]
    public void IsTenantOverride_ShouldReturnTrueForTenantTemplates()
    {
        // Arrange
        var template = EmailTemplate.CreateTenantOverride("tenant-123", "Test", "Subject", "<p>Body</p>");

        // Act & Assert
        template.IsTenantOverride.ShouldBeTrue();
    }

    #endregion
}

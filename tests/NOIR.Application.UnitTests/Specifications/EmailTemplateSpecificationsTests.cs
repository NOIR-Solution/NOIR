namespace NOIR.Application.UnitTests.Specifications;

using NOIR.Application.Features.EmailTemplates.Specifications;

/// <summary>
/// Unit tests for EmailTemplate specifications.
/// Verifies that specifications are correctly configured with expected filters,
/// sorting, tracking behavior, and query tags.
/// </summary>
public class EmailTemplateSpecificationsTests
{
    private static readonly Guid TestId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TestId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

    #region Helper Methods

    private static EmailTemplate CreateEmailTemplate(
        Guid? id = null,
        string? name = null,
        string? subject = null,
        string? htmlBody = null,
        bool isActive = true,
        string? tenantId = null)
    {
        var template = tenantId == null
            ? EmailTemplate.CreatePlatformDefault(
                name: name ?? "TestTemplate",
                subject: subject ?? "Test Subject",
                htmlBody: htmlBody ?? "<p>Test Body</p>",
                plainTextBody: "Test Body",
                description: "Test description",
                availableVariables: "[\"Var1\", \"Var2\"]")
            : EmailTemplate.CreateTenantOverride(
                tenantId: tenantId,
                name: name ?? "TestTemplate",
                subject: subject ?? "Test Subject",
                htmlBody: htmlBody ?? "<p>Test Body</p>",
                plainTextBody: "Test Body",
                description: "Test description",
                availableVariables: "[\"Var1\", \"Var2\"]");

        if (id.HasValue)
        {
            typeof(EmailTemplate).GetProperty("Id")!.SetValue(template, id.Value);
        }

        if (!isActive)
        {
            template.Deactivate();
        }

        return template;
    }

    #endregion

    #region EmailTemplateByIdSpec Tests

    [Fact]
    public void EmailTemplateByIdSpec_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var spec = new EmailTemplateByIdSpec(TestId1);

        // Assert
        spec.WhereExpressions.Should().HaveCount(1);
    }

    [Fact]
    public void EmailTemplateByIdSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new EmailTemplateByIdSpec(TestId1);

        // Assert
        spec.QueryTags.Should().Contain("EmailTemplateById");
    }

    [Fact]
    public void EmailTemplateByIdSpec_DefaultAsNoTracking_ShouldBeTrue()
    {
        // Arrange & Act
        var spec = new EmailTemplateByIdSpec(TestId1);

        // Assert
        spec.AsNoTracking.Should().BeTrue();
    }

    [Fact]
    public void EmailTemplateByIdSpec_WithMatchingId_ShouldSatisfy()
    {
        // Arrange
        var template = CreateEmailTemplate(id: TestId1);
        var spec = new EmailTemplateByIdSpec(TestId1);

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeTrue();
    }

    [Fact]
    public void EmailTemplateByIdSpec_WithNonMatchingId_ShouldNotSatisfy()
    {
        // Arrange
        var template = CreateEmailTemplate(id: TestId1);
        var spec = new EmailTemplateByIdSpec(TestId2);

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeFalse();
    }

    [Fact]
    public void EmailTemplateByIdSpec_MatchesActiveTemplate()
    {
        // Arrange
        var template = CreateEmailTemplate(id: TestId1, isActive: true);
        var spec = new EmailTemplateByIdSpec(TestId1);

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeTrue();
    }

    [Fact]
    public void EmailTemplateByIdSpec_MatchesInactiveTemplate()
    {
        // Arrange - EmailTemplateByIdSpec does not filter by active status
        var template = CreateEmailTemplate(id: TestId1, isActive: false);
        var spec = new EmailTemplateByIdSpec(TestId1);

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void EmailTemplateByIdSpec_WithEmptyGuid_ShouldNotMatch()
    {
        // Arrange
        var template = CreateEmailTemplate(id: TestId1);
        var spec = new EmailTemplateByIdSpec(Guid.Empty);

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeFalse();
    }

    #endregion
}

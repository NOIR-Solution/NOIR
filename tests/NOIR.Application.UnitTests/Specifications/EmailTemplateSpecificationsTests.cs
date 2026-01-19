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
        var template = EmailTemplate.Create(
            name: name ?? "TestTemplate",
            subject: subject ?? "Test Subject",
            htmlBody: htmlBody ?? "<p>Test Body</p>",
            plainTextBody: "Test Body",
            description: "Test description",
            availableVariables: "[\"Var1\", \"Var2\"]",
            tenantId: tenantId);

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

    #region EmailTemplateByIdForUpdateSpec Tests

    [Fact]
    public void EmailTemplateByIdForUpdateSpec_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var spec = new EmailTemplateByIdForUpdateSpec(TestId1);

        // Assert
        spec.WhereExpressions.Should().HaveCount(1);
    }

    [Fact]
    public void EmailTemplateByIdForUpdateSpec_ShouldEnableTracking()
    {
        // Arrange & Act
        var spec = new EmailTemplateByIdForUpdateSpec(TestId1);

        // Assert
        spec.AsNoTracking.Should().BeFalse();
    }

    [Fact]
    public void EmailTemplateByIdForUpdateSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new EmailTemplateByIdForUpdateSpec(TestId1);

        // Assert
        spec.QueryTags.Should().Contain("EmailTemplateByIdForUpdate");
    }

    [Fact]
    public void EmailTemplateByIdForUpdateSpec_WithMatchingId_ShouldSatisfy()
    {
        // Arrange
        var template = CreateEmailTemplate(id: TestId1);
        var spec = new EmailTemplateByIdForUpdateSpec(TestId1);

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeTrue();
    }

    [Fact]
    public void EmailTemplateByIdForUpdateSpec_WithNonMatchingId_ShouldNotSatisfy()
    {
        // Arrange
        var template = CreateEmailTemplate(id: TestId1);
        var spec = new EmailTemplateByIdForUpdateSpec(TestId2);

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeFalse();
    }

    [Fact]
    public void EmailTemplateByIdForUpdateSpec_MatchesActiveTemplate()
    {
        // Arrange
        var template = CreateEmailTemplate(id: TestId1, isActive: true);
        var spec = new EmailTemplateByIdForUpdateSpec(TestId1);

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeTrue();
    }

    [Fact]
    public void EmailTemplateByIdForUpdateSpec_MatchesInactiveTemplate()
    {
        // Arrange - ForUpdate spec does not filter by active status
        var template = CreateEmailTemplate(id: TestId1, isActive: false);
        var spec = new EmailTemplateByIdForUpdateSpec(TestId1);

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeTrue();
    }

    #endregion

    #region EmailTemplateByNameSpec Tests

    [Fact]
    public void EmailTemplateByNameSpec_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var spec = new EmailTemplateByNameSpec("PasswordResetOtp");

        // Assert
        spec.WhereExpressions.Should().HaveCount(1);
    }

    [Fact]
    public void EmailTemplateByNameSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new EmailTemplateByNameSpec("PasswordResetOtp");

        // Assert
        spec.QueryTags.Should().Contain("EmailTemplateByName");
    }

    [Fact]
    public void EmailTemplateByNameSpec_DefaultAsNoTracking_ShouldBeTrue()
    {
        // Arrange & Act
        var spec = new EmailTemplateByNameSpec("PasswordResetOtp");

        // Assert
        spec.AsNoTracking.Should().BeTrue();
    }

    [Fact]
    public void EmailTemplateByNameSpec_WithMatchingNameAndActive_ShouldSatisfy()
    {
        // Arrange
        var template = CreateEmailTemplate(name: "PasswordResetOtp", isActive: true);
        var spec = new EmailTemplateByNameSpec("PasswordResetOtp");

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeTrue();
    }

    [Fact]
    public void EmailTemplateByNameSpec_WithMatchingNameButInactive_ShouldNotSatisfy()
    {
        // Arrange - EmailTemplateByNameSpec filters for active templates only
        var template = CreateEmailTemplate(name: "PasswordResetOtp", isActive: false);
        var spec = new EmailTemplateByNameSpec("PasswordResetOtp");

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeFalse();
    }

    [Fact]
    public void EmailTemplateByNameSpec_WithNonMatchingName_ShouldNotSatisfy()
    {
        // Arrange
        var template = CreateEmailTemplate(name: "WelcomeEmail", isActive: true);
        var spec = new EmailTemplateByNameSpec("PasswordResetOtp");

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeFalse();
    }

    [Fact]
    public void EmailTemplateByNameSpec_IsCaseSensitive()
    {
        // Arrange - Name matching is exact
        var template = CreateEmailTemplate(name: "PasswordResetOtp", isActive: true);
        var spec = new EmailTemplateByNameSpec("passwordresetotp");

        // Act & Assert - Should not match due to case difference
        spec.IsSatisfiedBy(template).Should().BeFalse();
    }

    [Theory]
    [InlineData("PasswordResetOtp")]
    [InlineData("WelcomeEmail")]
    [InlineData("EmailConfirmation")]
    [InlineData("AccountLocked")]
    public void EmailTemplateByNameSpec_MatchesVariousTemplateNames(string templateName)
    {
        // Arrange
        var template = CreateEmailTemplate(name: templateName, isActive: true);
        var spec = new EmailTemplateByNameSpec(templateName);

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeTrue();
    }

    #endregion

    #region EmailTemplatesSpec Tests

    [Fact]
    public void EmailTemplatesSpec_WithNoSearch_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var spec = new EmailTemplatesSpec();

        // Assert
        spec.WhereExpressions.Should().HaveCount(1);
    }

    [Fact]
    public void EmailTemplatesSpec_ShouldHaveOrderByName()
    {
        // Arrange & Act
        var spec = new EmailTemplatesSpec();

        // Assert
        spec.OrderBy.Should().NotBeNull();
    }

    [Fact]
    public void EmailTemplatesSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new EmailTemplatesSpec();

        // Assert
        spec.QueryTags.Should().Contain("EmailTemplates");
    }

    [Fact]
    public void EmailTemplatesSpec_DefaultAsNoTracking_ShouldBeTrue()
    {
        // Arrange & Act
        var spec = new EmailTemplatesSpec();

        // Assert
        spec.AsNoTracking.Should().BeTrue();
    }

    [Fact]
    public void EmailTemplatesSpec_WithNoSearch_ShouldSatisfyAllTemplates()
    {
        // Arrange
        var template = CreateEmailTemplate(name: "AnyTemplate");
        var spec = new EmailTemplatesSpec();

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeTrue();
    }

    [Fact]
    public void EmailTemplatesSpec_WithNullSearch_ShouldSatisfyAllTemplates()
    {
        // Arrange
        var template = CreateEmailTemplate(name: "AnyTemplate");
        var spec = new EmailTemplatesSpec(search: null);

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeTrue();
    }

    [Fact]
    public void EmailTemplatesSpec_WithEmptySearch_ShouldSatisfyAllTemplates()
    {
        // Arrange
        var template = CreateEmailTemplate(name: "AnyTemplate");
        var spec = new EmailTemplatesSpec(search: "");

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeTrue();
    }

    [Fact]
    public void EmailTemplatesSpec_WithMatchingNameSearch_ShouldSatisfy()
    {
        // Arrange
        var template = CreateEmailTemplate(name: "PasswordResetOtp");
        var spec = new EmailTemplatesSpec(search: "Password");

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeTrue();
    }

    [Fact]
    public void EmailTemplatesSpec_WithMatchingSubjectSearch_ShouldSatisfy()
    {
        // Arrange
        var template = CreateEmailTemplate(name: "GenericEmail", subject: "Password Reset Request");
        var spec = new EmailTemplatesSpec(search: "Password");

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeTrue();
    }

    [Fact]
    public void EmailTemplatesSpec_WithNonMatchingSearch_ShouldNotSatisfy()
    {
        // Arrange
        var template = CreateEmailTemplate(name: "WelcomeEmail", subject: "Welcome to our platform");
        var spec = new EmailTemplatesSpec(search: "Password");

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeFalse();
    }

    [Fact]
    public void EmailTemplatesSpec_WithPartialNameMatch_ShouldSatisfy()
    {
        // Arrange
        var template = CreateEmailTemplate(name: "PasswordResetOtp");
        var spec = new EmailTemplatesSpec(search: "Reset");

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeTrue();
    }

    [Fact]
    public void EmailTemplatesSpec_WithPartialSubjectMatch_ShouldSatisfy()
    {
        // Arrange
        var template = CreateEmailTemplate(name: "Generic", subject: "Your verification code");
        var spec = new EmailTemplatesSpec(search: "verification");

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeTrue();
    }

    [Theory]
    [InlineData("Password", true)]
    [InlineData("Reset", true)]
    [InlineData("Otp", true)]
    [InlineData("Welcome", false)]
    [InlineData("NotFound", false)]
    public void EmailTemplatesSpec_SearchMatchesExpectedResults(string searchTerm, bool shouldMatch)
    {
        // Arrange
        var template = CreateEmailTemplate(name: "PasswordResetOtp", subject: "Reset your password");
        var spec = new EmailTemplatesSpec(search: searchTerm);

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().Be(shouldMatch);
    }

    [Fact]
    public void EmailTemplatesSpec_MatchesBothActiveAndInactiveTemplates()
    {
        // Arrange - EmailTemplatesSpec does not filter by active status
        var activeTemplate = CreateEmailTemplate(name: "ActiveTemplate", isActive: true);
        var inactiveTemplate = CreateEmailTemplate(name: "InactiveTemplate", isActive: false);
        var spec = new EmailTemplatesSpec();

        // Act & Assert
        spec.IsSatisfiedBy(activeTemplate).Should().BeTrue();
        spec.IsSatisfiedBy(inactiveTemplate).Should().BeTrue();
    }

    [Fact]
    public void EmailTemplatesSpec_WithNullSubject_SearchesNameOnly()
    {
        // Arrange - Create template without subject using reflection
        var template = CreateEmailTemplate(name: "TestTemplate");
        typeof(EmailTemplate).GetProperty("Subject")!.SetValue(template, null);
        var spec = new EmailTemplatesSpec(search: "Test");

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeTrue();
    }

    #endregion

    #region Specification Behavior Comparison Tests

    [Fact]
    public void EmailTemplateByIdSpec_VsForUpdate_TrackingDifference()
    {
        // Arrange
        var readOnlySpec = new EmailTemplateByIdSpec(TestId1);
        var updateSpec = new EmailTemplateByIdForUpdateSpec(TestId1);

        // Assert - Read-only should be no-tracking, update should enable tracking
        readOnlySpec.AsNoTracking.Should().BeTrue();
        updateSpec.AsNoTracking.Should().BeFalse();
    }

    [Fact]
    public void EmailTemplateByIdSpec_VsForUpdate_SameFiltering()
    {
        // Arrange
        var template = CreateEmailTemplate(id: TestId1);
        var readOnlySpec = new EmailTemplateByIdSpec(TestId1);
        var updateSpec = new EmailTemplateByIdForUpdateSpec(TestId1);

        // Assert - Both should match the same template
        readOnlySpec.IsSatisfiedBy(template).Should().BeTrue();
        updateSpec.IsSatisfiedBy(template).Should().BeTrue();
    }

    [Fact]
    public void EmailTemplateByNameSpec_VsEmailTemplatesSpec_DifferentFiltering()
    {
        // Arrange - Create inactive template
        var inactiveTemplate = CreateEmailTemplate(name: "TestTemplate", isActive: false);
        var byNameSpec = new EmailTemplateByNameSpec("TestTemplate");
        var listSpec = new EmailTemplatesSpec(search: "Test");

        // Assert - ByName requires active, list spec does not filter by active
        byNameSpec.IsSatisfiedBy(inactiveTemplate).Should().BeFalse();
        listSpec.IsSatisfiedBy(inactiveTemplate).Should().BeTrue();
    }

    [Fact]
    public void AllSpecs_HaveQueryTags()
    {
        // Arrange
        var byIdSpec = new EmailTemplateByIdSpec(TestId1);
        var forUpdateSpec = new EmailTemplateByIdForUpdateSpec(TestId1);
        var byNameSpec = new EmailTemplateByNameSpec("Test");
        var listSpec = new EmailTemplatesSpec();

        // Assert - All specifications should have query tags for debugging
        byIdSpec.QueryTags.Should().NotBeEmpty();
        forUpdateSpec.QueryTags.Should().NotBeEmpty();
        byNameSpec.QueryTags.Should().NotBeEmpty();
        listSpec.QueryTags.Should().NotBeEmpty();
    }

    [Fact]
    public void AllSpecs_HaveDistinctQueryTags()
    {
        // Arrange
        var byIdSpec = new EmailTemplateByIdSpec(TestId1);
        var forUpdateSpec = new EmailTemplateByIdForUpdateSpec(TestId1);
        var byNameSpec = new EmailTemplateByNameSpec("Test");
        var listSpec = new EmailTemplatesSpec();

        // Assert - Each specification should have a unique query tag
        var allTags = new[]
        {
            byIdSpec.QueryTags.First(),
            forUpdateSpec.QueryTags.First(),
            byNameSpec.QueryTags.First(),
            listSpec.QueryTags.First()
        };

        allTags.Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void EmailTemplatesSpec_WithWhitespaceSearch_ShouldNotMatch()
    {
        // Arrange
        var template = CreateEmailTemplate(name: "TestTemplate");
        var spec = new EmailTemplatesSpec(search: "   ");

        // Act & Assert - Whitespace-only search should not match (unless template contains whitespace)
        spec.IsSatisfiedBy(template).Should().BeFalse();
    }

    [Fact]
    public void EmailTemplateByIdSpec_WithEmptyGuid_ShouldNotMatch()
    {
        // Arrange
        var template = CreateEmailTemplate(id: TestId1);
        var spec = new EmailTemplateByIdSpec(Guid.Empty);

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeFalse();
    }

    [Fact]
    public void EmailTemplateByNameSpec_WithEmptyName_ShouldNotMatch()
    {
        // Arrange
        var template = CreateEmailTemplate(name: "TestTemplate", isActive: true);
        var spec = new EmailTemplateByNameSpec("");

        // Act & Assert
        spec.IsSatisfiedBy(template).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void EmailTemplatesSpec_WithEmptyOrNullSearch_MatchesAll(string? search)
    {
        // Arrange
        var template = CreateEmailTemplate(name: "AnyTemplate");
        var spec = new EmailTemplatesSpec(search: search);

        // Act & Assert - Empty or null search should match all templates
        // Note: Whitespace-only strings are NOT considered empty by string.IsNullOrEmpty
        spec.IsSatisfiedBy(template).Should().BeTrue();
    }

    #endregion
}

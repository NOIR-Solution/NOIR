using NOIR.Application.Features.ProductAttributes.Queries.GetCategoryAttributeFormSchema;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Validators;

/// <summary>
/// Unit tests for GetCategoryAttributeFormSchemaQueryValidator.
/// Tests all validation rules for the form schema query.
/// </summary>
public class GetCategoryAttributeFormSchemaQueryValidatorTests
{
    private readonly GetCategoryAttributeFormSchemaQueryValidator _validator = new();

    [Fact]
    public async Task Validate_WhenCategoryIdIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var query = new GetCategoryAttributeFormSchemaQuery(Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenCategoryIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var query = new GetCategoryAttributeFormSchemaQuery(Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CategoryId)
            .WithErrorMessage("Category ID is required.");
    }
}

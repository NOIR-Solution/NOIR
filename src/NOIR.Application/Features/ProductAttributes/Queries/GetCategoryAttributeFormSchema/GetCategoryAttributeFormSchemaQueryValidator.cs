namespace NOIR.Application.Features.ProductAttributes.Queries.GetCategoryAttributeFormSchema;

/// <summary>
/// Validator for GetCategoryAttributeFormSchemaQuery.
/// </summary>
public sealed class GetCategoryAttributeFormSchemaQueryValidator : AbstractValidator<GetCategoryAttributeFormSchemaQuery>
{
    public GetCategoryAttributeFormSchemaQueryValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .WithMessage("Category ID is required.");
    }
}

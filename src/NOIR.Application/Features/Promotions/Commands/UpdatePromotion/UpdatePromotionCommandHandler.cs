namespace NOIR.Application.Features.Promotions.Commands.UpdatePromotion;

/// <summary>
/// Wolverine handler for updating a promotion.
/// </summary>
public class UpdatePromotionCommandHandler
{
    private readonly IRepository<Domain.Entities.Promotion.Promotion, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePromotionCommandHandler(
        IRepository<Domain.Entities.Promotion.Promotion, Guid> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PromotionDto>> Handle(
        UpdatePromotionCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Promotions.Specifications.PromotionByIdForUpdateSpec(command.Id);
        var promotion = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (promotion is null)
        {
            return Result.Failure<PromotionDto>(
                Error.NotFound($"Promotion with ID '{command.Id}' not found.", "NOIR-PROMO-002"));
        }

        // Check for duplicate code (different promotion, same code)
        if (!string.Equals(promotion.Code, command.Code, StringComparison.OrdinalIgnoreCase))
        {
            var codeSpec = new Promotions.Specifications.PromotionByCodeSpec(command.Code);
            var existingWithCode = await _repository.FirstOrDefaultAsync(codeSpec, cancellationToken);
            if (existingWithCode is not null)
            {
                return Result.Failure<PromotionDto>(
                    Error.Validation("Code", $"Promotion with code '{command.Code}' already exists.", "NOIR-PROMO-001"));
            }
        }

        promotion.Update(
            command.Name,
            command.Description,
            command.Code,
            command.PromotionType,
            command.DiscountType,
            command.DiscountValue,
            command.StartDate,
            command.EndDate,
            command.ApplyLevel,
            command.MaxDiscountAmount,
            command.MinOrderValue,
            command.MinItemQuantity,
            command.UsageLimitTotal,
            command.UsageLimitPerUser);

        // Update product targeting
        var currentProductIds = promotion.Products.Select(p => p.ProductId).ToHashSet();
        var newProductIds = (command.ProductIds ?? []).ToHashSet();

        // Remove products no longer in the list
        foreach (var productId in currentProductIds.Except(newProductIds))
        {
            promotion.RemoveProduct(productId);
        }

        // Add new products
        foreach (var productId in newProductIds.Except(currentProductIds))
        {
            promotion.AddProduct(productId);
        }

        // Update category targeting
        var currentCategoryIds = promotion.Categories.Select(c => c.CategoryId).ToHashSet();
        var newCategoryIds = (command.CategoryIds ?? []).ToHashSet();

        // Remove categories no longer in the list
        foreach (var categoryId in currentCategoryIds.Except(newCategoryIds))
        {
            promotion.RemoveCategory(categoryId);
        }

        // Add new categories
        foreach (var categoryId in newCategoryIds.Except(currentCategoryIds))
        {
            promotion.AddCategory(categoryId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(PromotionMapper.ToDto(promotion));
    }
}

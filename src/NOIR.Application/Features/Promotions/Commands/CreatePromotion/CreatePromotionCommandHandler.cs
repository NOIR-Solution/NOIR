namespace NOIR.Application.Features.Promotions.Commands.CreatePromotion;

/// <summary>
/// Wolverine handler for creating a new promotion.
/// </summary>
public class CreatePromotionCommandHandler
{
    private readonly IRepository<Domain.Entities.Promotion.Promotion, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreatePromotionCommandHandler(
        IRepository<Domain.Entities.Promotion.Promotion, Guid> repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<PromotionDto>> Handle(
        CreatePromotionCommand command,
        CancellationToken cancellationToken)
    {
        // Check for duplicate code
        var existingSpec = new Promotions.Specifications.PromotionByCodeSpec(command.Code);
        var existing = await _repository.FirstOrDefaultAsync(existingSpec, cancellationToken);
        if (existing is not null)
        {
            return Result.Failure<PromotionDto>(
                Error.Validation("Code", $"Promotion with code '{command.Code}' already exists.", "NOIR-PROMO-001"));
        }

        var promotion = Domain.Entities.Promotion.Promotion.Create(
            command.Name,
            command.Code,
            command.PromotionType,
            command.DiscountType,
            command.DiscountValue,
            command.StartDate,
            command.EndDate,
            command.ApplyLevel,
            command.Description,
            command.MaxDiscountAmount,
            command.MinOrderValue,
            command.MinItemQuantity,
            command.UsageLimitTotal,
            command.UsageLimitPerUser,
            _currentUser.TenantId);

        // Add product targeting
        if (command.ProductIds is { Count: > 0 })
        {
            foreach (var productId in command.ProductIds)
            {
                promotion.AddProduct(productId);
            }
        }

        // Add category targeting
        if (command.CategoryIds is { Count: > 0 })
        {
            foreach (var categoryId in command.CategoryIds)
            {
                promotion.AddCategory(categoryId);
            }
        }

        await _repository.AddAsync(promotion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(PromotionMapper.ToDto(promotion));
    }
}

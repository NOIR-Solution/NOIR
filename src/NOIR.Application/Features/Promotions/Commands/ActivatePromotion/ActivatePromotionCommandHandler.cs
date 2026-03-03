namespace NOIR.Application.Features.Promotions.Commands.ActivatePromotion;

/// <summary>
/// Wolverine handler for activating a promotion.
/// </summary>
public class ActivatePromotionCommandHandler
{
    private readonly IRepository<Domain.Entities.Promotion.Promotion, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public ActivatePromotionCommandHandler(
        IRepository<Domain.Entities.Promotion.Promotion, Guid> repository,
        IUnitOfWork unitOfWork,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<PromotionDto>> Handle(
        ActivatePromotionCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Promotions.Specifications.PromotionByIdForUpdateSpec(command.Id);
        var promotion = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (promotion is null)
        {
            return Result.Failure<PromotionDto>(
                Error.NotFound($"Promotion with ID '{command.Id}' not found.", "NOIR-PROMO-002"));
        }

        try
        {
            promotion.Activate();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<PromotionDto>(
                Error.Validation("Status", ex.Message, "NOIR-PROMO-003"));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Promotion",
            entityId: promotion.Id,
            operation: EntityOperation.Updated,
            tenantId: promotion.TenantId!,
            ct: cancellationToken);

        return Result.Success(PromotionMapper.ToDto(promotion));
    }
}

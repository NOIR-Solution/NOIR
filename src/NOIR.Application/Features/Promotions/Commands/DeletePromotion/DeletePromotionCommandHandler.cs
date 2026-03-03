namespace NOIR.Application.Features.Promotions.Commands.DeletePromotion;

/// <summary>
/// Wolverine handler for soft deleting a promotion.
/// </summary>
public class DeletePromotionCommandHandler
{
    private readonly IRepository<Domain.Entities.Promotion.Promotion, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public DeletePromotionCommandHandler(
        IRepository<Domain.Entities.Promotion.Promotion, Guid> repository,
        IUnitOfWork unitOfWork,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<bool>> Handle(
        DeletePromotionCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Promotions.Specifications.PromotionByIdForUpdateSpec(command.Id);
        var promotion = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (promotion is null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Promotion with ID '{command.Id}' not found.", "NOIR-PROMO-002"));
        }

        _repository.Remove(promotion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Promotion",
            entityId: promotion.Id,
            operation: EntityOperation.Deleted,
            tenantId: promotion.TenantId!,
            ct: cancellationToken);

        return Result.Success(true);
    }
}

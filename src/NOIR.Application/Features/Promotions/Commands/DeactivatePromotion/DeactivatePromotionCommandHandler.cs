namespace NOIR.Application.Features.Promotions.Commands.DeactivatePromotion;

/// <summary>
/// Wolverine handler for deactivating a promotion.
/// </summary>
public class DeactivatePromotionCommandHandler
{
    private readonly IRepository<Domain.Entities.Promotion.Promotion, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivatePromotionCommandHandler(
        IRepository<Domain.Entities.Promotion.Promotion, Guid> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PromotionDto>> Handle(
        DeactivatePromotionCommand command,
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
            promotion.Deactivate();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<PromotionDto>(
                Error.Validation("Status", ex.Message, "NOIR-PROMO-004"));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(PromotionMapper.ToDto(promotion));
    }
}

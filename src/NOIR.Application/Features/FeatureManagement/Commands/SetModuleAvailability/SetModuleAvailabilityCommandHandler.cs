namespace NOIR.Application.Features.FeatureManagement.Commands.SetModuleAvailability;

/// <summary>
/// Wolverine handler for setting module availability for a specific tenant.
/// </summary>
public class SetModuleAvailabilityCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFeatureCacheInvalidator _cacheInvalidator;

    public SetModuleAvailabilityCommandHandler(
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        IFeatureCacheInvalidator cacheInvalidator)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<Result<TenantFeatureStateDto>> Handle(
        SetModuleAvailabilityCommand command,
        CancellationToken ct)
    {
        var state = await _dbContext.TenantModuleStates
            .TagWith("SetModuleAvailability")
            .FirstOrDefaultAsync(
                x => x.TenantId == command.TenantId && x.FeatureName == command.FeatureName,
                ct);

        if (state is null)
        {
            state = TenantModuleState.Create(command.FeatureName);
            state.SetAvailability(command.IsAvailable);
            await _dbContext.TenantModuleStates.AddAsync(state, ct);
        }
        else
        {
            state.SetAvailability(command.IsAvailable);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        await _cacheInvalidator.InvalidateAsync(command.TenantId, ct);

        return Result.Success(new TenantFeatureStateDto(
            command.FeatureName, state.IsAvailable, state.IsEnabled));
    }
}

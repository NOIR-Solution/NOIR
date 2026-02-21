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
        // IgnoreQueryFilters: platform admin has no Finbuckle tenant context;
        // we filter explicitly by command.TenantId instead.
        var state = await _dbContext.TenantModuleStates
            .IgnoreQueryFilters()
            .TagWith("SetModuleAvailability")
            .FirstOrDefaultAsync(
                x => x.TenantId == command.TenantId && x.FeatureName == command.FeatureName && !x.IsDeleted,
                ct);

        if (state is null)
        {
            // Pass tenantId explicitly since platform admin has no tenant context
            state = TenantModuleState.Create(command.FeatureName, command.TenantId);
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
            command.FeatureName, state.IsAvailable, state.IsEnabled, state.IsAvailable && state.IsEnabled));
    }
}

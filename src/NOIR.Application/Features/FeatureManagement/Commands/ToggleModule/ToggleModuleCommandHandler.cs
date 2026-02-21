namespace NOIR.Application.Features.FeatureManagement.Commands.ToggleModule;

/// <summary>
/// Wolverine handler for toggling a module on/off for the current tenant.
/// </summary>
public class ToggleModuleCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFeatureChecker _featureChecker;
    private readonly IFeatureCacheInvalidator _cacheInvalidator;
    private readonly ICurrentUser _currentUser;

    public ToggleModuleCommandHandler(
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        IFeatureChecker featureChecker,
        IFeatureCacheInvalidator cacheInvalidator,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _featureChecker = featureChecker;
        _cacheInvalidator = cacheInvalidator;
        _currentUser = currentUser;
    }

    public async Task<Result<TenantFeatureStateDto>> Handle(
        ToggleModuleCommand command,
        CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;
        if (string.IsNullOrEmpty(tenantId))
            return Result.Failure<TenantFeatureStateDto>(
                Error.Forbidden("Tenant context is required.", ErrorCodes.Feature.NotAvailable));

        // Check if feature is available before allowing toggle
        var featureState = await _featureChecker.GetStateAsync(command.FeatureName, ct);
        if (!featureState.IsAvailable)
            return Result.Failure<TenantFeatureStateDto>(
                Error.Forbidden($"The feature '{command.FeatureName}' is not available for your organization.",
                    ErrorCodes.Feature.NotAvailable));

        var state = await _dbContext.TenantModuleStates
            .TagWith("ToggleModule")
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.FeatureName == command.FeatureName,
                ct);

        if (state is null)
        {
            state = TenantModuleState.Create(command.FeatureName);
            state.SetEnabled(command.IsEnabled);
            await _dbContext.TenantModuleStates.AddAsync(state, ct);
        }
        else
        {
            state.SetEnabled(command.IsEnabled);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        await _cacheInvalidator.InvalidateAsync(tenantId, ct);

        return Result.Success(new TenantFeatureStateDto(
            command.FeatureName, state.IsAvailable, state.IsEnabled));
    }
}

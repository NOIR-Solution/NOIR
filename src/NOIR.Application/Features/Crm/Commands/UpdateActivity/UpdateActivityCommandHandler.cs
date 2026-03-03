namespace NOIR.Application.Features.Crm.Commands.UpdateActivity;

public class UpdateActivityCommandHandler
{
    private readonly IRepository<CrmActivity, Guid> _activityRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public UpdateActivityCommandHandler(
        IRepository<CrmActivity, Guid> activityRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _activityRepository = activityRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<Features.Crm.DTOs.ActivityDto>> Handle(
        UpdateActivityCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Specifications.ActivityByIdSpec(command.Id);
        var activity = await _activityRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (activity is null)
        {
            return Result.Failure<Features.Crm.DTOs.ActivityDto>(
                Error.NotFound($"Activity with ID '{command.Id}' not found.", "NOIR-CRM-040"));
        }

        activity.Update(
            command.Type,
            command.Subject,
            command.Description,
            command.ContactId,
            command.LeadId,
            command.PerformedAt,
            command.DurationMinutes);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "CrmActivity",
            entityId: activity.Id,
            operation: EntityOperation.Updated,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(MapToDto(activity));
    }

    private static Features.Crm.DTOs.ActivityDto MapToDto(CrmActivity a) =>
        new(a.Id, a.Type, a.Subject, a.Description,
            a.ContactId, a.Contact?.FullName,
            a.LeadId, a.Lead?.Title,
            a.PerformedById,
            a.PerformedBy != null ? $"{a.PerformedBy.FirstName} {a.PerformedBy.LastName}" : "",
            a.PerformedAt, a.DurationMinutes,
            a.CreatedAt, a.ModifiedAt);
}

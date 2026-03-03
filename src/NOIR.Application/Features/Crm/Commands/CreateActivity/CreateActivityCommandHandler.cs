namespace NOIR.Application.Features.Crm.Commands.CreateActivity;

public class CreateActivityCommandHandler
{
    private readonly IRepository<CrmActivity, Guid> _activityRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public CreateActivityCommandHandler(
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
        CreateActivityCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ContactId is null && command.LeadId is null)
        {
            return Result.Failure<Features.Crm.DTOs.ActivityDto>(
                Error.Validation("ContactId", "At least one of Contact or Lead must be specified."));
        }

        if (command.PerformedAt > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            return Result.Failure<Features.Crm.DTOs.ActivityDto>(
                Error.Validation("PerformedAt", "Activity date cannot be in the future."));
        }

        var activity = CrmActivity.Create(
            command.Type,
            command.Subject,
            command.PerformedById,
            command.PerformedAt,
            _currentUser.TenantId,
            command.Description,
            command.ContactId,
            command.LeadId,
            command.DurationMinutes);

        await _activityRepository.AddAsync(activity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "CrmActivity",
            entityId: activity.Id,
            operation: EntityOperation.Created,
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

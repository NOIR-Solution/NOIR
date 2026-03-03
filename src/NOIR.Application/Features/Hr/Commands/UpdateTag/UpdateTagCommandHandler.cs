namespace NOIR.Application.Features.Hr.Commands.UpdateTag;

public class UpdateTagCommandHandler
{
    private readonly IRepository<EmployeeTag, Guid> _tagRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public UpdateTagCommandHandler(
        IRepository<EmployeeTag, Guid> tagRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<EmployeeTagDto>> Handle(
        UpdateTagCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Find tag with tracking
        var spec = new EmployeeTagByIdSpec(command.Id, tracking: true);
        var tag = await _tagRepository.FirstOrDefaultAsync(spec, cancellationToken);
        if (tag is null)
        {
            return Result.Failure<EmployeeTagDto>(
                Error.NotFound($"Employee tag with ID '{command.Id}' not found.", "NOIR-HR-031"));
        }

        // Check name+category uniqueness (exclude self)
        // Category cannot be changed after creation — use existing tag's category
        var nameSpec = new EmployeeTagByNameAndCategorySpec(command.Name, tag.Category, tenantId, command.Id);
        var existing = await _tagRepository.FirstOrDefaultAsync(nameSpec, cancellationToken);
        if (existing is not null)
        {
            return Result.Failure<EmployeeTagDto>(
                Error.Conflict($"A tag with name '{command.Name}' in category '{tag.Category}' already exists.", "NOIR-HR-032"));
        }

        // Preserve existing category — cannot change after creation
        tag.Update(command.Name, tag.Category, command.Color, command.Description, command.SortOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "EmployeeTag",
            entityId: tag.Id,
            operation: EntityOperation.Updated,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(new EmployeeTagDto(
            tag.Id, tag.Name, tag.Category, tag.Color, tag.Description,
            tag.SortOrder, tag.IsActive, tag.EmployeeCount, tag.CreatedAt, tag.ModifiedAt));
    }
}

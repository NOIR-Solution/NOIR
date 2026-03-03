namespace NOIR.Application.Features.Hr.Commands.CreateTag;

public class CreateTagCommandHandler
{
    private readonly IRepository<EmployeeTag, Guid> _tagRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public CreateTagCommandHandler(
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
        CreateTagCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Check name+category uniqueness
        var nameSpec = new EmployeeTagByNameAndCategorySpec(command.Name, command.Category, tenantId);
        var existing = await _tagRepository.FirstOrDefaultAsync(nameSpec, cancellationToken);
        if (existing is not null)
        {
            return Result.Failure<EmployeeTagDto>(
                Error.Conflict($"A tag with name '{command.Name}' in category '{command.Category}' already exists.", "NOIR-HR-030"));
        }

        var tag = EmployeeTag.Create(
            command.Name,
            command.Category,
            tenantId,
            command.Color,
            command.Description,
            command.SortOrder);

        await _tagRepository.AddAsync(tag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "EmployeeTag",
            entityId: tag.Id,
            operation: EntityOperation.Created,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(new EmployeeTagDto(
            tag.Id, tag.Name, tag.Category, tag.Color, tag.Description,
            tag.SortOrder, tag.IsActive, 0, tag.CreatedAt, tag.ModifiedAt));
    }
}

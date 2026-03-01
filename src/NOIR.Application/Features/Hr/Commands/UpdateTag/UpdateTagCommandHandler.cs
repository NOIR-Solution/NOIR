namespace NOIR.Application.Features.Hr.Commands.UpdateTag;

public class UpdateTagCommandHandler
{
    private readonly IRepository<EmployeeTag, Guid> _tagRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateTagCommandHandler(
        IRepository<EmployeeTag, Guid> tagRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
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
        var nameSpec = new EmployeeTagByNameAndCategorySpec(command.Name, command.Category, tenantId, command.Id);
        var existing = await _tagRepository.FirstOrDefaultAsync(nameSpec, cancellationToken);
        if (existing is not null)
        {
            return Result.Failure<EmployeeTagDto>(
                Error.Conflict($"A tag with name '{command.Name}' in category '{command.Category}' already exists.", "NOIR-HR-032"));
        }

        tag.Update(command.Name, command.Category, command.Color, command.Description, command.SortOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new EmployeeTagDto(
            tag.Id, tag.Name, tag.Category, tag.Color, tag.Description,
            tag.SortOrder, tag.IsActive, tag.EmployeeCount, tag.CreatedAt, tag.ModifiedAt));
    }
}

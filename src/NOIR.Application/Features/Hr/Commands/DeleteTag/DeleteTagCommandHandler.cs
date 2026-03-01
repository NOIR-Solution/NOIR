namespace NOIR.Application.Features.Hr.Commands.DeleteTag;

public class DeleteTagCommandHandler
{
    private readonly IRepository<EmployeeTag, Guid> _tagRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTagCommandHandler(
        IRepository<EmployeeTag, Guid> tagRepository,
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork)
    {
        _tagRepository = tagRepository;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(
        DeleteTagCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new EmployeeTagByIdSpec(command.Id, tracking: true);
        var tag = await _tagRepository.FirstOrDefaultAsync(spec, cancellationToken);
        if (tag is null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Employee tag with ID '{command.Id}' not found.", "NOIR-HR-033"));
        }

        // Soft-delete all associated assignments
        var assignments = await _dbContext.EmployeeTagAssignments
            .Where(a => a.EmployeeTagId == command.Id)
            .ToListAsync(cancellationToken);

        if (assignments.Count > 0)
        {
            _dbContext.EmployeeTagAssignments.RemoveRange(assignments);
        }

        // Soft-delete the tag
        _tagRepository.Remove(tag);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}

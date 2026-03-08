namespace NOIR.Application.Features.Hr.Commands.ReorderDepartments;

public class ReorderDepartmentsCommandHandler
{
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReorderDepartmentsCommandHandler(
        IRepository<Department, Guid> departmentRepository,
        IUnitOfWork unitOfWork)
    {
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(
        ReorderDepartmentsCommand command,
        CancellationToken cancellationToken)
    {
        foreach (var item in command.Items)
        {
            var spec = new Specifications.DepartmentByIdSpec(item.Id);
            var department = await _departmentRepository.FirstOrDefaultAsync(spec, cancellationToken);
            if (department is not null)
            {
                department.SetSortOrder(item.SortOrder);
                department.SetParentDepartment(item.ParentDepartmentId);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}

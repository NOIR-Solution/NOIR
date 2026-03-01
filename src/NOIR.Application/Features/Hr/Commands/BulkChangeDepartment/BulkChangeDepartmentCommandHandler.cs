namespace NOIR.Application.Features.Hr.Commands.BulkChangeDepartment;

/// <summary>
/// Wolverine handler for bulk changing department for multiple employees.
/// </summary>
public class BulkChangeDepartmentCommandHandler
{
    private readonly IRepository<Employee, Guid> _employeeRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BulkChangeDepartmentCommandHandler(
        IRepository<Employee, Guid> employeeRepository,
        IRepository<Department, Guid> departmentRepository,
        IUnitOfWork unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BulkOperationResultDto>> Handle(
        BulkChangeDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        var successCount = 0;
        var errors = new List<BulkOperationErrorDto>();

        // Validate department exists
        var deptSpec = new DepartmentByIdSpec(command.NewDepartmentId);
        var department = await _departmentRepository.FirstOrDefaultAsync(deptSpec, cancellationToken);
        if (department is null)
        {
            return Result.Failure<BulkOperationResultDto>(
                Error.NotFound($"Department with ID '{command.NewDepartmentId}' not found.", "NOIR-HR-041"));
        }

        // Fetch all employees with tracking
        var spec = new EmployeesByIdsForUpdateSpec(command.EmployeeIds);
        var employees = await _employeeRepository.ListAsync(spec, cancellationToken);

        foreach (var employeeId in command.EmployeeIds)
        {
            var employee = employees.FirstOrDefault(e => e.Id == employeeId);

            if (employee is null)
            {
                errors.Add(new BulkOperationErrorDto(employeeId, null, "Employee not found"));
                continue;
            }

            try
            {
                employee.UpdateDepartment(command.NewDepartmentId);
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new BulkOperationErrorDto(employeeId, employee.FullName, ex.Message));
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new BulkOperationResultDto(
            successCount,
            errors.Count,
            errors));
    }
}

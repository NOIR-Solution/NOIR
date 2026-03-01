namespace NOIR.Application.Features.Hr.Queries.ExportEmployees;

/// <summary>
/// Query to export employees as a downloadable file (CSV or Excel).
/// </summary>
public sealed record ExportEmployeesQuery(
    ExportFormat Format = ExportFormat.CSV,
    Guid? DepartmentId = null,
    EmployeeStatus? Status = null,
    EmploymentType? EmploymentType = null);

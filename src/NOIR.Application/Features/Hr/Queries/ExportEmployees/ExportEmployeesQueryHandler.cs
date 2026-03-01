namespace NOIR.Application.Features.Hr.Queries.ExportEmployees;

/// <summary>
/// Wolverine handler for exporting employees as a downloadable file.
/// </summary>
public class ExportEmployeesQueryHandler
{
    private readonly IRepository<Employee, Guid> _employeeRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IExcelExportService _excelExportService;
    private readonly ILogger<ExportEmployeesQueryHandler> _logger;

    public ExportEmployeesQueryHandler(
        IRepository<Employee, Guid> employeeRepository,
        IApplicationDbContext dbContext,
        IExcelExportService excelExportService,
        ILogger<ExportEmployeesQueryHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _dbContext = dbContext;
        _excelExportService = excelExportService;
        _logger = logger;
    }

    public async Task<Result<ExportResultDto>> Handle(
        ExportEmployeesQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new EmployeesForExportSpec(query.DepartmentId, query.Status, query.EmploymentType);
        var employees = await _employeeRepository.ListAsync(spec, cancellationToken);

        // Load tag names per employee via DbContext (spec doesn't support ThenInclude)
        var employeeIds = employees.Select(e => e.Id).ToList();
        var tagAssignments = await _dbContext.EmployeeTagAssignments
            .Where(a => employeeIds.Contains(a.EmployeeId) && a.EmployeeTag != null)
            .Select(a => new { a.EmployeeId, a.EmployeeTag!.Name })
            .TagWith("ExportEmployees_TagNames")
            .ToListAsync(cancellationToken);

        var tagsByEmployee = tagAssignments
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(a => a.Name).OrderBy(n => n)));

        var headers = new List<string>
        {
            "EmployeeCode", "FirstName", "LastName", "Email", "Phone",
            "Department", "Position", "Manager", "JoinDate",
            "Status", "EmploymentType", "Tags"
        };

        var rows = new List<IReadOnlyList<object?>>();
        foreach (var e in employees)
        {
            tagsByEmployee.TryGetValue(e.Id, out var tags);

            rows.Add(new List<object?>
            {
                e.EmployeeCode,
                e.FirstName,
                e.LastName,
                e.Email,
                e.Phone,
                e.Department?.Name,
                e.Position,
                e.Manager is not null ? $"{e.Manager.FirstName} {e.Manager.LastName}" : null,
                e.JoinDate,
                e.Status.ToString(),
                e.EmploymentType.ToString(),
                tags ?? ""
            });
        }

        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        byte[] fileBytes;
        string contentType;
        string fileName;

        if (query.Format == ExportFormat.Excel)
        {
            fileBytes = _excelExportService.CreateExcelFile("Employees", headers, rows);
            contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            fileName = $"employees-{timestamp}.xlsx";
        }
        else
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", headers.Select(h => $"\"{h}\"")));

            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(",", row.Select(v => v is null ? "" : $"\"{EscapeCsv(v.ToString())}\"")));
            }

            fileBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            contentType = "text/csv";
            fileName = $"employees-{timestamp}.csv";
        }

        _logger.LogInformation("Exported {EmployeeCount} employees as {Format}", employees.Count, query.Format);

        return Result.Success(new ExportResultDto(fileBytes, contentType, fileName));
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Replace("\"", "\"\"");
    }
}

namespace NOIR.Application.Features.Hr.Commands.ImportEmployees;

/// <summary>
/// Wolverine handler for importing employees from CSV.
/// Expected columns: FirstName, LastName, Email, Phone, DepartmentCode, Position, JoinDate, EmploymentType
/// </summary>
public class ImportEmployeesCommandHandler
{
    private readonly IRepository<Employee, Guid> _employeeRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IEmployeeCodeGenerator _codeGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ImportEmployeesCommandHandler> _logger;

    public ImportEmployeesCommandHandler(
        IRepository<Employee, Guid> employeeRepository,
        IRepository<Department, Guid> departmentRepository,
        IEmployeeCodeGenerator codeGenerator,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<ImportEmployeesCommandHandler> logger)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _codeGenerator = codeGenerator;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<ImportResultDto>> Handle(
        ImportEmployeesCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var csvText = Encoding.UTF8.GetString(command.FileData);
        var lines = csvText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            return Result.Failure<ImportResultDto>(
                Error.Validation("FileData", "CSV file must contain a header row and at least one data row.", "NOIR-HR-042"));
        }

        // Parse header
        var headerLine = lines[0].Trim().TrimStart('\uFEFF'); // Remove BOM if present
        var headers = ParseCsvLine(headerLine);
        var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Length; i++)
        {
            headerMap[headers[i].Trim()] = i;
        }

        // Validate required columns
        var requiredColumns = new[] { "FirstName", "LastName", "Email", "DepartmentCode", "JoinDate", "EmploymentType" };
        foreach (var col in requiredColumns)
        {
            if (!headerMap.ContainsKey(col))
            {
                return Result.Failure<ImportResultDto>(
                    Error.Validation("FileData", $"Missing required column: {col}", "NOIR-HR-043"));
            }
        }

        // Pre-load departments for lookup by code
        var allDepartmentsSpec = new AllDepartmentsSpec();
        var departments = await _departmentRepository.ListAsync(allDepartmentsSpec, cancellationToken);
        var departmentByCode = departments.ToDictionary(d => d.Code, d => d, StringComparer.OrdinalIgnoreCase);

        // Track emails to detect duplicates within the file
        var processedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var errors = new List<ImportErrorDto>();
        var successCount = 0;
        var totalRows = lines.Length - 1;

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var rowNumber = i + 1; // 1-indexed, header is row 1
            var values = ParseCsvLine(line);

            try
            {
                var firstName = GetValue(values, headerMap, "FirstName");
                var lastName = GetValue(values, headerMap, "LastName");
                var email = GetValue(values, headerMap, "Email")?.ToLowerInvariant();
                var phone = GetValue(values, headerMap, "Phone");
                var departmentCode = GetValue(values, headerMap, "DepartmentCode");
                var position = GetValue(values, headerMap, "Position");
                var joinDateStr = GetValue(values, headerMap, "JoinDate");
                var employmentTypeStr = GetValue(values, headerMap, "EmploymentType");

                // Validate required fields
                if (string.IsNullOrWhiteSpace(firstName))
                {
                    errors.Add(new ImportErrorDto(rowNumber, "FirstName is required."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(lastName))
                {
                    errors.Add(new ImportErrorDto(rowNumber, "LastName is required."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                {
                    errors.Add(new ImportErrorDto(rowNumber, "Valid email is required."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(departmentCode))
                {
                    errors.Add(new ImportErrorDto(rowNumber, "DepartmentCode is required."));
                    continue;
                }

                if (!departmentByCode.TryGetValue(departmentCode, out var department))
                {
                    errors.Add(new ImportErrorDto(rowNumber, $"Department with code '{departmentCode}' not found."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(joinDateStr) || !DateTimeOffset.TryParse(joinDateStr, out var joinDate))
                {
                    errors.Add(new ImportErrorDto(rowNumber, "Valid JoinDate is required (e.g. 2026-01-15)."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(employmentTypeStr) || !Enum.TryParse<EmploymentType>(employmentTypeStr, true, out var employmentType))
                {
                    errors.Add(new ImportErrorDto(rowNumber, $"Valid EmploymentType is required (FullTime, PartTime, Contract, Intern)."));
                    continue;
                }

                // Check for duplicate emails within file
                if (!processedEmails.Add(email))
                {
                    errors.Add(new ImportErrorDto(rowNumber, $"Duplicate email '{email}' within file."));
                    continue;
                }

                // Check for existing email in database
                var emailSpec = new EmployeeByEmailSpec(email, tenantId);
                var existingByEmail = await _employeeRepository.FirstOrDefaultAsync(emailSpec, cancellationToken);
                if (existingByEmail is not null)
                {
                    errors.Add(new ImportErrorDto(rowNumber, $"Employee with email '{email}' already exists."));
                    continue;
                }

                // Generate employee code and create
                var employeeCode = await _codeGenerator.GenerateNextAsync(tenantId, cancellationToken);
                var employee = Employee.Create(
                    employeeCode, firstName, lastName, email,
                    department.Id, joinDate, employmentType, tenantId,
                    phone, position);

                await _employeeRepository.AddAsync(employee, cancellationToken);
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new ImportErrorDto(rowNumber, $"Unexpected error: {ex.Message}"));
            }
        }

        if (successCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Imported {SuccessCount}/{TotalRows} employees from {FileName}",
            successCount, totalRows, command.FileName);

        return Result.Success(new ImportResultDto(
            totalRows,
            successCount,
            errors.Count,
            errors));
    }

    private static string? GetValue(string[] values, Dictionary<string, int> headerMap, string column)
    {
        if (!headerMap.TryGetValue(column, out var index) || index >= values.Length)
            return null;

        var value = values[index].Trim().Trim('"');
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var current = new StringBuilder();

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++; // Skip escaped quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else if (c == '\r')
            {
                // Skip carriage return
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result.ToArray();
    }
}

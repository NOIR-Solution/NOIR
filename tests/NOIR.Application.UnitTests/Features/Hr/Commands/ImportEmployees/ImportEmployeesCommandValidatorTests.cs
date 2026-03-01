using NOIR.Application.Features.Hr.Commands.ImportEmployees;

namespace NOIR.Application.UnitTests.Features.Hr.Commands.ImportEmployees;

public class ImportEmployeesCommandValidatorTests
{
    private readonly ImportEmployeesCommandValidator _validator;

    public ImportEmployeesCommandValidatorTests()
    {
        _validator = new ImportEmployeesCommandValidator();
    }

    private static ImportEmployeesCommand CreateValidCommand() =>
        new(
            FileData: Encoding.UTF8.GetBytes("FirstName,LastName,Email\nJohn,Doe,john@example.com"),
            FileName: "employees.csv");

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        var result = await _validator.TestValidateAsync(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyFileData_ShouldFail()
    {
        var command = CreateValidCommand() with { FileData = Array.Empty<byte>() };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.FileData);
    }

    [Fact]
    public async Task Validate_WithEmptyFileName_ShouldFail()
    {
        var command = CreateValidCommand() with { FileName = "" };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }
}

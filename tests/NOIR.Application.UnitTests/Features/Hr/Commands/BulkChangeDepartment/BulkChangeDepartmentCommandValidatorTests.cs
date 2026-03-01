using NOIR.Application.Features.Hr.Commands.BulkChangeDepartment;

namespace NOIR.Application.UnitTests.Features.Hr.Commands.BulkChangeDepartment;

public class BulkChangeDepartmentCommandValidatorTests
{
    private readonly BulkChangeDepartmentCommandValidator _validator;

    public BulkChangeDepartmentCommandValidatorTests()
    {
        _validator = new BulkChangeDepartmentCommandValidator();
    }

    private static BulkChangeDepartmentCommand CreateValidCommand() =>
        new(
            EmployeeIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            NewDepartmentId: Guid.NewGuid());

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        var result = await _validator.TestValidateAsync(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyEmployeeIds_ShouldFail()
    {
        var command = CreateValidCommand() with { EmployeeIds = new List<Guid>() };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.EmployeeIds);
    }

    [Fact]
    public async Task Validate_WithEmptyDepartmentId_ShouldFail()
    {
        var command = CreateValidCommand() with { NewDepartmentId = Guid.Empty };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.NewDepartmentId);
    }
}

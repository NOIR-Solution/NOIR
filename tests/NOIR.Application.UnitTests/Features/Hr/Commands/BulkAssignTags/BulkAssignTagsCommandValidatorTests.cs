using NOIR.Application.Features.Hr.Commands.BulkAssignTags;

namespace NOIR.Application.UnitTests.Features.Hr.Commands.BulkAssignTags;

public class BulkAssignTagsCommandValidatorTests
{
    private readonly BulkAssignTagsCommandValidator _validator;

    public BulkAssignTagsCommandValidatorTests()
    {
        _validator = new BulkAssignTagsCommandValidator();
    }

    private static BulkAssignTagsCommand CreateValidCommand() =>
        new(
            EmployeeIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            TagIds: new List<Guid> { Guid.NewGuid() });

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
    public async Task Validate_WithEmptyTagIds_ShouldFail()
    {
        var command = CreateValidCommand() with { TagIds = new List<Guid>() };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.TagIds);
    }

    [Fact]
    public async Task Validate_WithTooManyEmployeeIds_ShouldFail()
    {
        var employeeIds = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();
        var command = CreateValidCommand() with { EmployeeIds = employeeIds };
        var result = await _validator.TestValidateAsync(command);
        result.IsValid.ShouldBe(false);
    }
}

using NOIR.Application.Features.Hr.Commands.UpdateTag;

namespace NOIR.Application.UnitTests.Features.Hr.Commands.UpdateTag;

public class UpdateTagCommandValidatorTests
{
    private readonly UpdateTagCommandValidator _validator;

    public UpdateTagCommandValidatorTests()
    {
        _validator = new UpdateTagCommandValidator();
    }

    private static UpdateTagCommand CreateValidCommand() =>
        new(
            Id: Guid.NewGuid(),
            Name: "Updated Tag",
            Category: EmployeeTagCategory.Skill,
            Color: "#3b82f6",
            Description: "Updated description",
            SortOrder: 1);

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        var result = await _validator.TestValidateAsync(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyId_ShouldFail()
    {
        var command = CreateValidCommand() with { Id = Guid.Empty };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WithEmptyName_ShouldFail(string? name)
    {
        var command = CreateValidCommand() with { Name = name! };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_WithNameTooLong_ShouldFail()
    {
        var command = CreateValidCommand() with { Name = new string('a', 101) };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}

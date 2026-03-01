using NOIR.Application.Features.Hr.Commands.CreateTag;

namespace NOIR.Application.UnitTests.Features.Hr.Commands.CreateTag;

public class CreateTagCommandValidatorTests
{
    private readonly CreateTagCommandValidator _validator;

    public CreateTagCommandValidatorTests()
    {
        _validator = new CreateTagCommandValidator();
    }

    private static CreateTagCommand CreateValidCommand() =>
        new(
            Name: "Senior Developer",
            Category: EmployeeTagCategory.Skill,
            Color: "#ef4444",
            Description: "Senior-level devs");

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        var result = await _validator.TestValidateAsync(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
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

    [Fact]
    public async Task Validate_WithColorTooLong_ShouldFail()
    {
        var command = CreateValidCommand() with { Color = new string('a', 10) };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Color);
    }

    [Fact]
    public async Task Validate_WithNullOptionalFields_ShouldPass()
    {
        var command = CreateValidCommand() with { Color = null, Description = null };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

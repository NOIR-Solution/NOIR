using NOIR.Application.Features.Crm.Commands.CreatePipeline;
using NOIR.Application.Features.Crm.DTOs;

namespace NOIR.Application.UnitTests.Features.Crm.Commands;

public class CreatePipelineCommandValidatorTests
{
    private readonly CreatePipelineCommandValidator _validator;

    public CreatePipelineCommandValidatorTests()
    {
        _validator = new CreatePipelineCommandValidator();
    }

    [Fact]
    public void Validate_ValidInput_ShouldPass()
    {
        // Arrange
        var stages = new List<CreatePipelineStageDto>
        {
            new("Qualification", 1, "#3B82F6"),
            new("Proposal", 2, "#10B981")
        };
        var command = new CreatePipelineCommand("Sales Pipeline", false, stages);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyName_ShouldFail()
    {
        // Arrange
        var stages = new List<CreatePipelineStageDto>
        {
            new("Stage", 1)
        };
        var command = new CreatePipelineCommand("", false, stages);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameTooLong_ShouldFail()
    {
        // Arrange
        var stages = new List<CreatePipelineStageDto>
        {
            new("Stage", 1)
        };
        var command = new CreatePipelineCommand(new string('A', 101), false, stages);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_EmptyStages_ShouldFail()
    {
        // Arrange
        var command = new CreatePipelineCommand(
            "Pipeline", false, new List<CreatePipelineStageDto>());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Stages);
    }

    [Fact]
    public void Validate_StageWithEmptyName_ShouldFail()
    {
        // Arrange
        var stages = new List<CreatePipelineStageDto>
        {
            new("", 1)
        };
        var command = new CreatePipelineCommand("Pipeline", false, stages);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void Validate_StageNameTooLong_ShouldFail()
    {
        // Arrange
        var stages = new List<CreatePipelineStageDto>
        {
            new(new string('A', 101), 1)
        };
        var command = new CreatePipelineCommand("Pipeline", false, stages);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void Validate_StageColorTooLong_ShouldFail()
    {
        // Arrange
        var stages = new List<CreatePipelineStageDto>
        {
            new("Stage", 1, "#3B82F6FF")
        };
        var command = new CreatePipelineCommand("Pipeline", false, stages);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.Errors.ShouldNotBeEmpty();
    }
}

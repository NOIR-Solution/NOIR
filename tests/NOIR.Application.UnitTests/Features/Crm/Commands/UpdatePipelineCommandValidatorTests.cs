using NOIR.Application.Features.Crm.Commands.UpdatePipeline;
using NOIR.Application.Features.Crm.DTOs;

namespace NOIR.Application.UnitTests.Features.Crm.Commands;

public class UpdatePipelineCommandValidatorTests
{
    private readonly UpdatePipelineCommandValidator _validator;

    public UpdatePipelineCommandValidatorTests()
    {
        _validator = new UpdatePipelineCommandValidator();
    }

    [Fact]
    public void Validate_ValidInput_ShouldPass()
    {
        // Arrange
        var stages = new List<UpdatePipelineStageDto>
        {
            new(Guid.NewGuid(), "Qualification", 1, "#3B82F6"),
            new(null, "Proposal", 2, "#10B981")
        };
        var command = new UpdatePipelineCommand(Guid.NewGuid(), "Sales Pipeline", false, stages);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyId_ShouldFail()
    {
        // Arrange
        var stages = new List<UpdatePipelineStageDto>
        {
            new(null, "Stage", 1, "#3B82F6")
        };
        var command = new UpdatePipelineCommand(Guid.Empty, "Pipeline", false, stages);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validate_EmptyName_ShouldFail()
    {
        // Arrange
        var stages = new List<UpdatePipelineStageDto>
        {
            new(null, "Stage", 1, "#3B82F6")
        };
        var command = new UpdatePipelineCommand(Guid.NewGuid(), "", false, stages);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameTooLong_ShouldFail()
    {
        // Arrange
        var stages = new List<UpdatePipelineStageDto>
        {
            new(null, "Stage", 1, "#3B82F6")
        };
        var command = new UpdatePipelineCommand(Guid.NewGuid(), new string('A', 101), false, stages);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_EmptyStages_ShouldFail()
    {
        // Arrange
        var command = new UpdatePipelineCommand(
            Guid.NewGuid(), "Pipeline", false, new List<UpdatePipelineStageDto>());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Stages);
    }

    [Fact]
    public void Validate_StageWithEmptyName_ShouldFail()
    {
        // Arrange
        var stages = new List<UpdatePipelineStageDto>
        {
            new(null, "", 1, "#3B82F6")
        };
        var command = new UpdatePipelineCommand(Guid.NewGuid(), "Pipeline", false, stages);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void Validate_StageNameTooLong_ShouldFail()
    {
        // Arrange
        var stages = new List<UpdatePipelineStageDto>
        {
            new(null, new string('A', 101), 1, "#3B82F6")
        };
        var command = new UpdatePipelineCommand(Guid.NewGuid(), "Pipeline", false, stages);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void Validate_StageColorTooLong_ShouldFail()
    {
        // Arrange
        var stages = new List<UpdatePipelineStageDto>
        {
            new(null, "Stage", 1, "#3B82F6FF")
        };
        var command = new UpdatePipelineCommand(Guid.NewGuid(), "Pipeline", false, stages);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.Errors.ShouldNotBeEmpty();
    }
}

using NOIR.Domain.Entities.Crm;

namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for the Pipeline and PipelineStage entities.
/// Tests factory methods, stage management, and default flag.
/// </summary>
public class PipelineTests
{
    private const string TestTenantId = "test-tenant";

    #region Pipeline Create Tests

    [Fact]
    public void Create_ShouldSetName_AndIsDefault()
    {
        // Act
        var pipeline = Pipeline.Create("Sales Pipeline", TestTenantId, isDefault: true);

        // Assert
        pipeline.ShouldNotBeNull();
        pipeline.Id.ShouldNotBe(Guid.Empty);
        pipeline.Name.ShouldBe("Sales Pipeline");
        pipeline.IsDefault.ShouldBeTrue();
        pipeline.TenantId.ShouldBe(TestTenantId);
        pipeline.Stages.ShouldBeEmpty();
    }

    [Fact]
    public void Create_WithDefaultFalse_ShouldNotBeDefault()
    {
        // Act
        var pipeline = Pipeline.Create("Custom Pipeline", TestTenantId);

        // Assert
        pipeline.IsDefault.ShouldBeFalse();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        // Act & Assert
        var act = () => Pipeline.Create("", TestTenantId);
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        // Act
        var pipeline = Pipeline.Create("  Sales  ", TestTenantId);

        // Assert
        pipeline.Name.ShouldBe("Sales");
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldChangeName()
    {
        // Arrange
        var pipeline = Pipeline.Create("Old Name", TestTenantId);

        // Act
        pipeline.Update("New Name");

        // Assert
        pipeline.Name.ShouldBe("New Name");
    }

    #endregion

    #region SetDefault Tests

    [Fact]
    public void SetDefault_ShouldUpdateIsDefault()
    {
        // Arrange
        var pipeline = Pipeline.Create("Pipeline", TestTenantId, isDefault: false);

        // Act
        pipeline.SetDefault(true);

        // Assert
        pipeline.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public void SetDefault_False_ShouldClearDefault()
    {
        // Arrange
        var pipeline = Pipeline.Create("Pipeline", TestTenantId, isDefault: true);

        // Act
        pipeline.SetDefault(false);

        // Assert
        pipeline.IsDefault.ShouldBeFalse();
    }

    #endregion

    #region PipelineStage Create Tests

    [Fact]
    public void PipelineStage_Create_ShouldSetAllProperties()
    {
        // Arrange
        var pipelineId = Guid.NewGuid();

        // Act
        var stage = PipelineStage.Create(pipelineId, "Qualification", 1, TestTenantId, "#3B82F6");

        // Assert
        stage.ShouldNotBeNull();
        stage.Id.ShouldNotBe(Guid.Empty);
        stage.PipelineId.ShouldBe(pipelineId);
        stage.Name.ShouldBe("Qualification");
        stage.SortOrder.ShouldBe(1);
        stage.Color.ShouldBe("#3B82F6");
        stage.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void PipelineStage_Create_WithDefaultColor_ShouldUseDefault()
    {
        // Act
        var stage = PipelineStage.Create(Guid.NewGuid(), "New", 0, TestTenantId);

        // Assert
        stage.Color.ShouldBe("#6366f1");
    }

    [Fact]
    public void PipelineStage_Create_WithEmptyName_ShouldThrow()
    {
        // Act & Assert
        var act = () => PipelineStage.Create(Guid.NewGuid(), "", 0, TestTenantId);
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void PipelineStage_Update_ShouldModifyProperties()
    {
        // Arrange
        var stage = PipelineStage.Create(Guid.NewGuid(), "Old", 0, TestTenantId, "#000000");

        // Act
        stage.Update("New Stage", 2, "#FF0000");

        // Assert
        stage.Name.ShouldBe("New Stage");
        stage.SortOrder.ShouldBe(2);
        stage.Color.ShouldBe("#FF0000");
    }

    #endregion
}

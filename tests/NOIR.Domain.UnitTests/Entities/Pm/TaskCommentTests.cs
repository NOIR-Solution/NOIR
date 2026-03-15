using NOIR.Domain.Entities.Pm;

namespace NOIR.Domain.UnitTests.Entities.Pm;

/// <summary>
/// Unit tests for the TaskComment entity.
/// Tests factory method and edit behavior.
/// </summary>
public class TaskCommentTests
{
    private const string TestTenantId = "test-tenant";
    private static readonly Guid TestTaskId = Guid.NewGuid();
    private static readonly Guid TestAuthorId = Guid.NewGuid();

    #region Create Factory Tests

    [Fact]
    public void Create_ShouldSetAllProperties_AndIsEditedFalse()
    {
        // Act
        var comment = TaskComment.Create(TestTaskId, TestAuthorId, "This is a comment", TestTenantId);

        // Assert
        comment.ShouldNotBeNull();
        comment.Id.ShouldNotBe(Guid.Empty);
        comment.TaskId.ShouldBe(TestTaskId);
        comment.AuthorId.ShouldBe(TestAuthorId);
        comment.Content.ShouldBe("This is a comment");
        comment.IsEdited.ShouldBeFalse();
        comment.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_WithEmptyContent_ShouldThrow()
    {
        // Act & Assert
        var act = () => TaskComment.Create(TestTaskId, TestAuthorId, "", TestTenantId);
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_ShouldTrimContent()
    {
        // Act
        var comment = TaskComment.Create(TestTaskId, TestAuthorId, "  Padded content  ", TestTenantId);

        // Assert
        comment.Content.ShouldBe("Padded content");
    }

    #endregion

    #region Edit Tests

    [Fact]
    public void Edit_ShouldUpdateContent_AndSetIsEditedTrue()
    {
        // Arrange
        var comment = TaskComment.Create(TestTaskId, TestAuthorId, "Original", TestTenantId);

        // Act
        comment.Edit("Updated content");

        // Assert
        comment.Content.ShouldBe("Updated content");
        comment.IsEdited.ShouldBeTrue();
    }

    [Fact]
    public void Edit_WithEmptyContent_ShouldThrow()
    {
        // Arrange
        var comment = TaskComment.Create(TestTaskId, TestAuthorId, "Original", TestTenantId);

        // Act & Assert
        var act = () => comment.Edit("");
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Edit_ShouldTrimContent()
    {
        // Arrange
        var comment = TaskComment.Create(TestTaskId, TestAuthorId, "Original", TestTenantId);

        // Act
        comment.Edit("  Edited  ");

        // Assert
        comment.Content.ShouldBe("Edited");
    }

    #endregion
}

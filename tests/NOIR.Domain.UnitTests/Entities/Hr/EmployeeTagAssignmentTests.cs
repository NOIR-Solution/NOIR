using NOIR.Domain.Entities.Hr;

namespace NOIR.Domain.UnitTests.Entities.Hr;

/// <summary>
/// Unit tests for the EmployeeTagAssignment junction entity.
/// Tests factory method and property assignment.
/// </summary>
public class EmployeeTagAssignmentTests
{
    private const string TestTenantId = "test-tenant";

    [Fact]
    public void Create_WithValidData_ShouldCreateAssignment()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var tagId = Guid.NewGuid();

        // Act
        var assignment = EmployeeTagAssignment.Create(employeeId, tagId, TestTenantId);

        // Assert
        assignment.ShouldNotBeNull();
        assignment.Id.ShouldNotBe(Guid.Empty);
        assignment.EmployeeId.ShouldBe(employeeId);
        assignment.EmployeeTagId.ShouldBe(tagId);
        assignment.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetAssignedAtToUtcNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var assignment = EmployeeTagAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), TestTenantId);

        // Assert
        var after = DateTimeOffset.UtcNow;
        assignment.AssignedAt.ShouldBeGreaterThanOrEqualTo(before);
        assignment.AssignedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Create_MultipleCalls_ShouldGenerateUniqueIds()
    {
        // Act
        var a1 = EmployeeTagAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), TestTenantId);
        var a2 = EmployeeTagAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), TestTenantId);

        // Assert
        a1.Id.ShouldNotBe(a2.Id);
    }
}

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
        assignment.Should().NotBeNull();
        assignment.Id.Should().NotBe(Guid.Empty);
        assignment.EmployeeId.Should().Be(employeeId);
        assignment.EmployeeTagId.Should().Be(tagId);
        assignment.TenantId.Should().Be(TestTenantId);
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
        assignment.AssignedAt.Should().BeOnOrAfter(before);
        assignment.AssignedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Create_MultipleCalls_ShouldGenerateUniqueIds()
    {
        // Act
        var a1 = EmployeeTagAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), TestTenantId);
        var a2 = EmployeeTagAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), TestTenantId);

        // Assert
        a1.Id.Should().NotBe(a2.Id);
    }
}

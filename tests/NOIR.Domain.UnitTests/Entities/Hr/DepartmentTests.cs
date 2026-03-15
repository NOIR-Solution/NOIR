using NOIR.Domain.Entities.Hr;
using NOIR.Domain.Events.Hr;

namespace NOIR.Domain.UnitTests.Entities.Hr;

/// <summary>
/// Unit tests for the Department aggregate root entity.
/// Tests factory method, updates, activation/deactivation, and sort order.
/// </summary>
public class DepartmentTests
{
    private const string TestTenantId = "test-tenant";
    private const string TestName = "Engineering";
    private const string TestCode = "ENG";
    private const string TestDescription = "Engineering Department";

    private static Department CreateTestDepartment(
        string name = TestName,
        string code = TestCode,
        string? tenantId = TestTenantId,
        string? description = TestDescription,
        Guid? parentDepartmentId = null,
        Guid? managerId = null)
    {
        return Department.Create(name, code, tenantId, description, parentDepartmentId, managerId);
    }

    #region Create Factory Tests

    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        // Act
        var department = CreateTestDepartment(
            parentDepartmentId: parentId,
            managerId: managerId);

        // Assert
        department.ShouldNotBeNull();
        department.Id.ShouldNotBe(Guid.Empty);
        department.Name.ShouldBe(TestName);
        department.Code.ShouldBe(TestCode.ToUpperInvariant());
        department.Description.ShouldBe(TestDescription);
        department.ParentDepartmentId.ShouldBe(parentId);
        department.ManagerId.ShouldBe(managerId);
        department.SortOrder.ShouldBe(0);
        department.IsActive.ShouldBeTrue();
        department.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_ShouldAddDepartmentCreatedEvent()
    {
        // Act
        var department = CreateTestDepartment();

        // Assert
        department.DomainEvents.ShouldHaveSingleItem();
        department.DomainEvents.First().ShouldBeOfType<DepartmentCreatedEvent>();
        var evt = (DepartmentCreatedEvent)department.DomainEvents.First();
        evt.DepartmentId.ShouldBe(department.Id);
    }

    [Fact]
    public void Create_ShouldThrowForNullName()
    {
        // Act
        var act = () => CreateTestDepartment(name: null!);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_ShouldThrowForEmptyName()
    {
        // Act
        var act = () => CreateTestDepartment(name: "");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_ShouldThrowForNullCode()
    {
        // Act
        var act = () => CreateTestDepartment(code: null!);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_ShouldThrowForEmptyCode()
    {
        // Act
        var act = () => CreateTestDepartment(code: "");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_ShouldUppercaseCode()
    {
        // Act
        var department = CreateTestDepartment(code: "eng");

        // Assert
        department.Code.ShouldBe("ENG");
    }

    [Fact]
    public void Create_ShouldTrimNameAndCode()
    {
        // Act
        var department = CreateTestDepartment(name: "  Engineering  ", code: "  eng  ");

        // Assert
        department.Name.ShouldBe("Engineering");
        department.Code.ShouldBe("ENG");
    }

    [Fact]
    public void Create_WithNullOptionalFields_ShouldAllowNulls()
    {
        // Act
        var department = CreateTestDepartment(
            description: null,
            parentDepartmentId: null,
            managerId: null);

        // Assert
        department.Description.ShouldBeNull();
        department.ParentDepartmentId.ShouldBeNull();
        department.ManagerId.ShouldBeNull();
    }

    [Fact]
    public void Create_MultipleCalls_ShouldGenerateUniqueIds()
    {
        // Act
        var dept1 = CreateTestDepartment();
        var dept2 = CreateTestDepartment();

        // Assert
        dept1.Id.ShouldNotBe(dept2.Id);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldChangeProperties()
    {
        // Arrange
        var department = CreateTestDepartment();
        var newManagerId = Guid.NewGuid();
        var newParentId = Guid.NewGuid();
        department.ClearDomainEvents();

        // Act
        department.Update("Marketing", "MKT", "Marketing Department", newManagerId, newParentId);

        // Assert
        department.Name.ShouldBe("Marketing");
        department.Code.ShouldBe("MKT");
        department.Description.ShouldBe("Marketing Department");
        department.ManagerId.ShouldBe(newManagerId);
        department.ParentDepartmentId.ShouldBe(newParentId);
    }

    [Fact]
    public void Update_ShouldAddUpdatedEvent()
    {
        // Arrange
        var department = CreateTestDepartment();
        department.ClearDomainEvents();

        // Act
        department.Update("Marketing", "MKT", null, null, null);

        // Assert
        department.DomainEvents.ShouldHaveSingleItem();
        department.DomainEvents.First().ShouldBeOfType<DepartmentUpdatedEvent>();
    }

    [Fact]
    public void Update_ShouldUppercaseCode()
    {
        // Arrange
        var department = CreateTestDepartment();
        department.ClearDomainEvents();

        // Act
        department.Update("Marketing", "mkt", null, null, null);

        // Assert
        department.Code.ShouldBe("MKT");
    }

    [Fact]
    public void Update_WithNullManagerId_ShouldClearManager()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var department = CreateTestDepartment(managerId: managerId);
        department.ClearDomainEvents();

        // Act
        department.Update("Engineering", "ENG", "Eng Dept", null, null);

        // Assert
        department.ManagerId.ShouldBeNull();
    }

    [Fact]
    public void Update_ShouldTrimDescription()
    {
        // Arrange
        var department = CreateTestDepartment();
        department.ClearDomainEvents();

        // Act
        department.Update("Marketing", "MKT", "  Marketing Dept  ", null, null);

        // Assert
        department.Description.ShouldBe("Marketing Dept");
    }

    #endregion

    #region Deactivate / Activate Tests

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        // Arrange
        var department = CreateTestDepartment();
        department.ClearDomainEvents();

        // Act
        department.Deactivate();

        // Assert
        department.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Deactivate_ShouldAddUpdatedEvent()
    {
        // Arrange
        var department = CreateTestDepartment();
        department.ClearDomainEvents();

        // Act
        department.Deactivate();

        // Assert
        department.DomainEvents.ShouldHaveSingleItem();
        department.DomainEvents.First().ShouldBeOfType<DepartmentUpdatedEvent>();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        // Arrange
        var department = CreateTestDepartment();
        department.Deactivate();
        department.ClearDomainEvents();

        // Act
        department.Activate();

        // Assert
        department.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Activate_ShouldAddUpdatedEvent()
    {
        // Arrange
        var department = CreateTestDepartment();
        department.Deactivate();
        department.ClearDomainEvents();

        // Act
        department.Activate();

        // Assert
        department.DomainEvents.ShouldHaveSingleItem();
        department.DomainEvents.First().ShouldBeOfType<DepartmentUpdatedEvent>();
    }

    #endregion

    #region SetSortOrder Tests

    [Fact]
    public void SetSortOrder_ShouldUpdateOrder()
    {
        // Arrange
        var department = CreateTestDepartment();

        // Act
        department.SetSortOrder(5);

        // Assert
        department.SortOrder.ShouldBe(5);
    }

    [Fact]
    public void SetSortOrder_WithZero_ShouldSetToZero()
    {
        // Arrange
        var department = CreateTestDepartment();
        department.SetSortOrder(10);

        // Act
        department.SetSortOrder(0);

        // Assert
        department.SortOrder.ShouldBe(0);
    }

    #endregion
}

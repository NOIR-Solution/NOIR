using NOIR.Domain.Entities.Hr;
using NOIR.Domain.Events.Hr;

namespace NOIR.Domain.UnitTests.Entities.Hr;

/// <summary>
/// Unit tests for the Employee aggregate root entity.
/// Tests factory method, basic info updates, department/manager changes,
/// user linking, deactivation, and reactivation.
/// </summary>
public class EmployeeTests
{
    private const string TestTenantId = "test-tenant";
    private const string TestEmployeeCode = "EMP-001";
    private const string TestFirstName = "John";
    private const string TestLastName = "Doe";
    private const string TestEmail = "john.doe@example.com";
    private const string TestPhone = "+84912345678";
    private const string TestPosition = "Software Engineer";
    private static readonly Guid TestDepartmentId = Guid.NewGuid();
    private static readonly DateTimeOffset TestJoinDate = new(2025, 1, 15, 0, 0, 0, TimeSpan.Zero);

    private static Employee CreateTestEmployee(
        string employeeCode = TestEmployeeCode,
        string firstName = TestFirstName,
        string lastName = TestLastName,
        string email = TestEmail,
        Guid? departmentId = null,
        DateTimeOffset? joinDate = null,
        EmploymentType employmentType = EmploymentType.FullTime,
        string? tenantId = TestTenantId,
        string? phone = TestPhone,
        string? position = TestPosition,
        Guid? managerId = null,
        string? userId = null,
        string? notes = null)
    {
        return Employee.Create(
            employeeCode,
            firstName,
            lastName,
            email,
            departmentId ?? TestDepartmentId,
            joinDate ?? TestJoinDate,
            employmentType,
            tenantId,
            phone,
            position,
            managerId,
            userId,
            notes);
    }

    #region Create Factory Tests

    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var userId = "user-123";

        // Act
        var employee = CreateTestEmployee(
            managerId: managerId,
            userId: userId,
            notes: "Test notes");

        // Assert
        employee.ShouldNotBeNull();
        employee.Id.ShouldNotBe(Guid.Empty);
        employee.EmployeeCode.ShouldBe(TestEmployeeCode);
        employee.FirstName.ShouldBe(TestFirstName);
        employee.LastName.ShouldBe(TestLastName);
        employee.Email.ShouldBe(TestEmail);
        employee.Phone.ShouldBe(TestPhone);
        employee.DepartmentId.ShouldBe(TestDepartmentId);
        employee.Position.ShouldBe(TestPosition);
        employee.ManagerId.ShouldBe(managerId);
        employee.UserId.ShouldBe(userId);
        employee.JoinDate.ShouldBe(TestJoinDate);
        employee.EmploymentType.ShouldBe(EmploymentType.FullTime);
        employee.Status.ShouldBe(EmployeeStatus.Active);
        employee.EndDate.ShouldBeNull();
        employee.TenantId.ShouldBe(TestTenantId);
        employee.Notes.ShouldBe("Test notes");
        employee.FullName.ShouldBe("John Doe");
    }

    [Fact]
    public void Create_ShouldAddEmployeeCreatedEvent()
    {
        // Act
        var employee = CreateTestEmployee();

        // Assert
        employee.DomainEvents.ShouldHaveSingleItem();
        employee.DomainEvents.First().ShouldBeOfType<EmployeeCreatedEvent>();
        var createdEvent = (EmployeeCreatedEvent)employee.DomainEvents.First();
        createdEvent.EmployeeId.ShouldBe(employee.Id);
    }

    [Fact]
    public void Create_ShouldThrowForNullFirstName()
    {
        // Act
        var act = () => CreateTestEmployee(firstName: null!);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_ShouldThrowForEmptyFirstName()
    {
        // Act
        var act = () => CreateTestEmployee(firstName: "");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_ShouldThrowForNullLastName()
    {
        // Act
        var act = () => CreateTestEmployee(lastName: null!);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_ShouldThrowForNullEmail()
    {
        // Act
        var act = () => CreateTestEmployee(email: null!);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_ShouldThrowForNullEmployeeCode()
    {
        // Act
        var act = () => CreateTestEmployee(employeeCode: null!);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_ShouldTrimAndLowercaseEmail()
    {
        // Act
        var employee = CreateTestEmployee(email: "  JOHN@EXAMPLE.COM  ");

        // Assert
        employee.Email.ShouldBe("john@example.com");
    }

    [Fact]
    public void Create_ShouldTrimFirstAndLastName()
    {
        // Act
        var employee = CreateTestEmployee(firstName: "  John  ", lastName: "  Doe  ");

        // Assert
        employee.FirstName.ShouldBe("John");
        employee.LastName.ShouldBe("Doe");
    }

    [Fact]
    public void Create_WithNullOptionalFields_ShouldAllowNulls()
    {
        // Act
        var employee = CreateTestEmployee(phone: null, position: null, managerId: null, userId: null, notes: null);

        // Assert
        employee.Phone.ShouldBeNull();
        employee.Position.ShouldBeNull();
        employee.ManagerId.ShouldBeNull();
        employee.UserId.ShouldBeNull();
        employee.Notes.ShouldBeNull();
    }

    [Fact]
    public void Create_MultipleCalls_ShouldGenerateUniqueIds()
    {
        // Act
        var employee1 = CreateTestEmployee();
        var employee2 = CreateTestEmployee();

        // Assert
        employee1.Id.ShouldNotBe(employee2.Id);
    }

    #endregion

    #region UpdateBasicInfo Tests

    [Fact]
    public void UpdateBasicInfo_ShouldUpdateProperties()
    {
        // Arrange
        var employee = CreateTestEmployee();
        employee.ClearDomainEvents();

        // Act
        employee.UpdateBasicInfo(
            "Jane", "Smith", "jane@example.com",
            "+84987654321", "https://example.com/avatar.jpg",
            "Senior Engineer", EmploymentType.PartTime, "Updated notes");

        // Assert
        employee.FirstName.ShouldBe("Jane");
        employee.LastName.ShouldBe("Smith");
        employee.Email.ShouldBe("jane@example.com");
        employee.Phone.ShouldBe("+84987654321");
        employee.AvatarUrl.ShouldBe("https://example.com/avatar.jpg");
        employee.Position.ShouldBe("Senior Engineer");
        employee.EmploymentType.ShouldBe(EmploymentType.PartTime);
        employee.Notes.ShouldBe("Updated notes");
    }

    [Fact]
    public void UpdateBasicInfo_ShouldAddUpdatedEvent()
    {
        // Arrange
        var employee = CreateTestEmployee();
        employee.ClearDomainEvents();

        // Act
        employee.UpdateBasicInfo(
            "Jane", "Smith", "jane@example.com",
            null, null, null, EmploymentType.FullTime, null);

        // Assert
        employee.DomainEvents.ShouldHaveSingleItem();
        employee.DomainEvents.First().ShouldBeOfType<EmployeeUpdatedEvent>();
    }

    #endregion

    #region UpdateDepartment Tests

    [Fact]
    public void UpdateDepartment_ShouldChangeDepartment()
    {
        // Arrange
        var employee = CreateTestEmployee();
        var newDepartmentId = Guid.NewGuid();
        employee.ClearDomainEvents();

        // Act
        employee.UpdateDepartment(newDepartmentId);

        // Assert
        employee.DepartmentId.ShouldBe(newDepartmentId);
    }

    [Fact]
    public void UpdateDepartment_ShouldAddDepartmentChangedEvent()
    {
        // Arrange
        var employee = CreateTestEmployee();
        var oldDepartmentId = employee.DepartmentId;
        var newDepartmentId = Guid.NewGuid();
        employee.ClearDomainEvents();

        // Act
        employee.UpdateDepartment(newDepartmentId);

        // Assert
        employee.DomainEvents.ShouldHaveSingleItem();
        var evt = employee.DomainEvents.First().ShouldBeOfType<EmployeeDepartmentChangedEvent>();
        evt.EmployeeId.ShouldBe(employee.Id);
        evt.OldDepartmentId.ShouldBe(oldDepartmentId);
        evt.NewDepartmentId.ShouldBe(newDepartmentId);
    }

    [Fact]
    public void UpdateDepartment_WithSameDepartment_ShouldNotAddEvent()
    {
        // Arrange
        var employee = CreateTestEmployee();
        var sameDepartmentId = employee.DepartmentId;
        employee.ClearDomainEvents();

        // Act
        employee.UpdateDepartment(sameDepartmentId);

        // Assert
        employee.DomainEvents.ShouldBeEmpty();
    }

    #endregion

    #region UpdateManager Tests

    [Fact]
    public void UpdateManager_ShouldSetManagerId()
    {
        // Arrange
        var employee = CreateTestEmployee();
        var managerId = Guid.NewGuid();
        employee.ClearDomainEvents();

        // Act
        employee.UpdateManager(managerId);

        // Assert
        employee.ManagerId.ShouldBe(managerId);
    }

    [Fact]
    public void UpdateManager_WithNull_ShouldClearManagerId()
    {
        // Arrange
        var employee = CreateTestEmployee(managerId: Guid.NewGuid());
        employee.ClearDomainEvents();

        // Act
        employee.UpdateManager(null);

        // Assert
        employee.ManagerId.ShouldBeNull();
    }

    [Fact]
    public void UpdateManager_ShouldAddUpdatedEvent()
    {
        // Arrange
        var employee = CreateTestEmployee();
        employee.ClearDomainEvents();

        // Act
        employee.UpdateManager(Guid.NewGuid());

        // Assert
        employee.DomainEvents.ShouldHaveSingleItem();
        employee.DomainEvents.First().ShouldBeOfType<EmployeeUpdatedEvent>();
    }

    #endregion

    #region Deactivate Tests

    [Fact]
    public void Deactivate_WithResigned_ShouldSetStatusAndEndDate()
    {
        // Arrange
        var employee = CreateTestEmployee();
        employee.ClearDomainEvents();

        // Act
        employee.Deactivate(EmployeeStatus.Resigned);

        // Assert
        employee.Status.ShouldBe(EmployeeStatus.Resigned);
        employee.EndDate.ShouldNotBeNull();
        employee.EndDate!.Value.ShouldBe(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Deactivate_WithTerminated_ShouldSetStatusAndEndDate()
    {
        // Arrange
        var employee = CreateTestEmployee();
        employee.ClearDomainEvents();

        // Act
        employee.Deactivate(EmployeeStatus.Terminated);

        // Assert
        employee.Status.ShouldBe(EmployeeStatus.Terminated);
        employee.EndDate.ShouldNotBeNull();
    }

    [Fact]
    public void Deactivate_ShouldAddDeactivatedEvent()
    {
        // Arrange
        var employee = CreateTestEmployee();
        employee.ClearDomainEvents();

        // Act
        employee.Deactivate(EmployeeStatus.Resigned);

        // Assert
        employee.DomainEvents.ShouldHaveSingleItem();
        var evt = employee.DomainEvents.First().ShouldBeOfType<EmployeeDeactivatedEvent>();
        evt.EmployeeId.ShouldBe(employee.Id);
        evt.NewStatus.ShouldBe(EmployeeStatus.Resigned);
    }

    [Fact]
    public void Deactivate_WithActiveStatus_ShouldThrow()
    {
        // Arrange
        var employee = CreateTestEmployee();

        // Act
        var act = () => employee.Deactivate(EmployeeStatus.Active);

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Deactivation status must be Resigned or Terminated.");
    }

    [Fact]
    public void Deactivate_WithSuspendedStatus_ShouldThrow()
    {
        // Arrange
        var employee = CreateTestEmployee();

        // Act
        var act = () => employee.Deactivate(EmployeeStatus.Suspended);

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Deactivation status must be Resigned or Terminated.");
    }

    [Fact]
    public void Deactivate_AlreadyDeactivatedEmployee_ShouldStillSucceed()
    {
        // Arrange - Employee is already deactivated (Resigned)
        var employee = CreateTestEmployee();
        employee.Deactivate(EmployeeStatus.Resigned);
        var firstEndDate = employee.EndDate;
        employee.ClearDomainEvents();

        // Act - Deactivate again with Terminated (no domain guard against double deactivation)
        employee.Deactivate(EmployeeStatus.Terminated);

        // Assert - Status is updated, EndDate is refreshed
        employee.Status.ShouldBe(EmployeeStatus.Terminated);
        employee.EndDate.ShouldNotBeNull();
        employee.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<EmployeeDeactivatedEvent>();
    }

    #endregion

    #region Reactivate Tests

    [Fact]
    public void Reactivate_ShouldSetActiveAndClearEndDate()
    {
        // Arrange
        var employee = CreateTestEmployee();
        employee.Deactivate(EmployeeStatus.Resigned);
        employee.ClearDomainEvents();

        // Act
        employee.Reactivate();

        // Assert
        employee.Status.ShouldBe(EmployeeStatus.Active);
        employee.EndDate.ShouldBeNull();
    }

    [Fact]
    public void Reactivate_ShouldAddUpdatedEvent()
    {
        // Arrange
        var employee = CreateTestEmployee();
        employee.Deactivate(EmployeeStatus.Resigned);
        employee.ClearDomainEvents();

        // Act
        employee.Reactivate();

        // Assert
        employee.DomainEvents.ShouldHaveSingleItem();
        employee.DomainEvents.First().ShouldBeOfType<EmployeeUpdatedEvent>();
    }

    [Fact]
    public void Reactivate_AlreadyActiveEmployee_ShouldStillSucceed()
    {
        // Arrange - Employee is already active (no domain guard)
        var employee = CreateTestEmployee();
        employee.ClearDomainEvents();

        // Act
        employee.Reactivate();

        // Assert - Remains active, EndDate stays null
        employee.Status.ShouldBe(EmployeeStatus.Active);
        employee.EndDate.ShouldBeNull();
        employee.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<EmployeeUpdatedEvent>();
    }

    #endregion

    #region LinkToUser Tests

    [Fact]
    public void LinkToUser_ShouldSetUserId()
    {
        // Arrange
        var employee = CreateTestEmployee();
        employee.ClearDomainEvents();

        // Act
        employee.LinkToUser("user-456");

        // Assert
        employee.UserId.ShouldBe("user-456");
    }

    [Fact]
    public void LinkToUser_ShouldAddUpdatedEvent()
    {
        // Arrange
        var employee = CreateTestEmployee();
        employee.ClearDomainEvents();

        // Act
        employee.LinkToUser("user-456");

        // Assert
        employee.DomainEvents.ShouldHaveSingleItem();
        employee.DomainEvents.First().ShouldBeOfType<EmployeeUpdatedEvent>();
    }

    [Fact]
    public void LinkToUser_WithNullUserId_ShouldThrow()
    {
        // Arrange
        var employee = CreateTestEmployee();

        // Act
        var act = () => employee.LinkToUser(null!);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void LinkToUser_WithEmptyUserId_ShouldThrow()
    {
        // Arrange
        var employee = CreateTestEmployee();

        // Act
        var act = () => employee.LinkToUser("");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region UnlinkUser Tests

    [Fact]
    public void UnlinkUser_ShouldClearUserId()
    {
        // Arrange
        var employee = CreateTestEmployee(userId: "user-123");
        employee.ClearDomainEvents();

        // Act
        employee.UnlinkUser();

        // Assert
        employee.UserId.ShouldBeNull();
    }

    [Fact]
    public void UnlinkUser_ShouldAddUpdatedEvent()
    {
        // Arrange
        var employee = CreateTestEmployee(userId: "user-123");
        employee.ClearDomainEvents();

        // Act
        employee.UnlinkUser();

        // Assert
        employee.DomainEvents.ShouldHaveSingleItem();
        employee.DomainEvents.First().ShouldBeOfType<EmployeeUpdatedEvent>();
    }

    #endregion
}

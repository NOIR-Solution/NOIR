using NOIR.Domain.Entities.Crm;

namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for the CrmCompany entity.
/// Tests factory methods and update.
/// </summary>
public class CrmCompanyTests
{
    private const string TestTenantId = "test-tenant";

    #region Create Factory Tests

    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        // Arrange
        var ownerId = Guid.NewGuid();

        // Act
        var company = CrmCompany.Create(
            "Acme Corp", TestTenantId,
            domain: "Acme.com", industry: "Technology",
            address: "123 Main St", phone: "555-0100",
            website: "https://acme.com", ownerId: ownerId,
            taxId: "TAX-123", employeeCount: 500,
            notes: "Key account");

        // Assert
        company.ShouldNotBeNull();
        company.Id.ShouldNotBe(Guid.Empty);
        company.Name.ShouldBe("Acme Corp");
        company.Domain.ShouldBe("acme.com"); // lowercased
        company.Industry.ShouldBe("Technology");
        company.Address.ShouldBe("123 Main St");
        company.Phone.ShouldBe("555-0100");
        company.Website.ShouldBe("https://acme.com");
        company.OwnerId.ShouldBe(ownerId);
        company.TaxId.ShouldBe("TAX-123");
        company.EmployeeCount.ShouldBe(500);
        company.Notes.ShouldBe("Key account");
        company.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_WithNullOptionals_ShouldAllowNulls()
    {
        // Act
        var company = CrmCompany.Create("Acme Corp", TestTenantId);

        // Assert
        company.Domain.ShouldBeNull();
        company.Industry.ShouldBeNull();
        company.Address.ShouldBeNull();
        company.Phone.ShouldBeNull();
        company.Website.ShouldBeNull();
        company.OwnerId.ShouldBeNull();
        company.TaxId.ShouldBeNull();
        company.EmployeeCount.ShouldBeNull();
        company.Notes.ShouldBeNull();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        // Act & Assert
        var act = () => CrmCompany.Create("", TestTenantId);
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_ShouldTrimAndLowercaseDomain()
    {
        // Act
        var company = CrmCompany.Create(
            " Acme ", TestTenantId,
            domain: " ACME.COM ");

        // Assert
        company.Name.ShouldBe("Acme");
        company.Domain.ShouldBe("acme.com");
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldModifyProperties()
    {
        // Arrange
        var company = CrmCompany.Create("Old Name", TestTenantId);
        var newOwnerId = Guid.NewGuid();

        // Act
        company.Update(
            "New Name", "newdomain.com", "Finance",
            "456 Elm St", "555-0200", "https://new.com",
            newOwnerId, "TAX-456", 1000, "Updated notes");

        // Assert
        company.Name.ShouldBe("New Name");
        company.Domain.ShouldBe("newdomain.com");
        company.Industry.ShouldBe("Finance");
        company.Address.ShouldBe("456 Elm St");
        company.Phone.ShouldBe("555-0200");
        company.Website.ShouldBe("https://new.com");
        company.OwnerId.ShouldBe(newOwnerId);
        company.TaxId.ShouldBe("TAX-456");
        company.EmployeeCount.ShouldBe(1000);
        company.Notes.ShouldBe("Updated notes");
    }

    [Fact]
    public void Update_WithNulls_ShouldClearOptionalFields()
    {
        // Arrange
        var company = CrmCompany.Create(
            "Acme", TestTenantId,
            domain: "acme.com", industry: "Tech",
            notes: "Some notes");

        // Act
        company.Update("Acme", null, null, null, null, null, null, null, null, null);

        // Assert
        company.Domain.ShouldBeNull();
        company.Industry.ShouldBeNull();
        company.Notes.ShouldBeNull();
    }

    #endregion
}

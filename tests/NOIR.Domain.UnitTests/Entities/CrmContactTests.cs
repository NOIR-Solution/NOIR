using NOIR.Domain.Entities.Crm;

namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for the CrmContact entity.
/// Tests factory methods, update, and customer linking.
/// </summary>
public class CrmContactTests
{
    private const string TestTenantId = "test-tenant";

    #region Create Factory Tests

    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        // Arrange
        var firstName = "John";
        var lastName = "Doe";
        var email = "John.Doe@Example.COM";
        var source = ContactSource.Web;
        var phone = "555-0100";
        var jobTitle = "CTO";
        var companyId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var notes = "Important contact";

        // Act
        var contact = CrmContact.Create(
            firstName, lastName, email, source, TestTenantId,
            phone, jobTitle, companyId, ownerId, notes: notes);

        // Assert
        contact.ShouldNotBeNull();
        contact.Id.ShouldNotBe(Guid.Empty);
        contact.FirstName.ShouldBe("John");
        contact.LastName.ShouldBe("Doe");
        contact.Email.ShouldBe("john.doe@example.com"); // lowercased
        contact.Source.ShouldBe(ContactSource.Web);
        contact.Phone.ShouldBe("555-0100");
        contact.JobTitle.ShouldBe("CTO");
        contact.CompanyId.ShouldBe(companyId);
        contact.OwnerId.ShouldBe(ownerId);
        contact.Notes.ShouldBe("Important contact");
        contact.TenantId.ShouldBe(TestTenantId);
        contact.CustomerId.ShouldBeNull();
        contact.FullName.ShouldBe("John Doe");
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        // Act
        var contact = CrmContact.Create(
            " John ", " Doe ", " john@example.com ",
            ContactSource.Cold, TestTenantId,
            phone: " 555-0100 ", jobTitle: " CTO ", notes: " Note ");

        // Assert
        contact.FirstName.ShouldBe("John");
        contact.LastName.ShouldBe("Doe");
        contact.Email.ShouldBe("john@example.com");
        contact.Phone.ShouldBe("555-0100");
        contact.JobTitle.ShouldBe("CTO");
        contact.Notes.ShouldBe("Note");
    }

    [Fact]
    public void Create_WithNullOptionals_ShouldAllowNulls()
    {
        // Act
        var contact = CrmContact.Create(
            "John", "Doe", "john@example.com",
            ContactSource.Other, TestTenantId);

        // Assert
        contact.Phone.ShouldBeNull();
        contact.JobTitle.ShouldBeNull();
        contact.CompanyId.ShouldBeNull();
        contact.OwnerId.ShouldBeNull();
        contact.CustomerId.ShouldBeNull();
        contact.Notes.ShouldBeNull();
    }

    [Fact]
    public void Create_WithEmptyFirstName_ShouldThrow()
    {
        // Act & Assert
        var act = () => CrmContact.Create(
            "", "Doe", "john@example.com",
            ContactSource.Web, TestTenantId);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_WithEmptyLastName_ShouldThrow()
    {
        // Act & Assert
        var act = () => CrmContact.Create(
            "John", "", "john@example.com",
            ContactSource.Web, TestTenantId);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_WithEmptyEmail_ShouldThrow()
    {
        // Act & Assert
        var act = () => CrmContact.Create(
            "John", "Doe", "",
            ContactSource.Web, TestTenantId);

        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldModifyProperties()
    {
        // Arrange
        var contact = CrmContact.Create(
            "John", "Doe", "john@example.com",
            ContactSource.Web, TestTenantId);

        var newCompanyId = Guid.NewGuid();
        var newOwnerId = Guid.NewGuid();
        var newCustomerId = Guid.NewGuid();

        // Act
        contact.Update(
            "Jane", "Smith", "Jane.Smith@Example.COM",
            ContactSource.Referral, "555-0200", "CEO",
            newCompanyId, newOwnerId, newCustomerId, "Updated notes");

        // Assert
        contact.FirstName.ShouldBe("Jane");
        contact.LastName.ShouldBe("Smith");
        contact.Email.ShouldBe("jane.smith@example.com");
        contact.Source.ShouldBe(ContactSource.Referral);
        contact.Phone.ShouldBe("555-0200");
        contact.JobTitle.ShouldBe("CEO");
        contact.CompanyId.ShouldBe(newCompanyId);
        contact.OwnerId.ShouldBe(newOwnerId);
        contact.CustomerId.ShouldBe(newCustomerId);
        contact.Notes.ShouldBe("Updated notes");
    }

    #endregion

    #region SetCustomerId Tests

    [Fact]
    public void Update_WithCustomerId_ShouldLinkToCustomer()
    {
        // Arrange
        var contact = CrmContact.Create(
            "John", "Doe", "john@example.com",
            ContactSource.Web, TestTenantId);
        var customerId = Guid.NewGuid();

        // Act
        contact.Update(
            contact.FirstName, contact.LastName, contact.Email,
            contact.Source, contact.Phone, contact.JobTitle,
            contact.CompanyId, contact.OwnerId, customerId, contact.Notes);

        // Assert
        contact.CustomerId.ShouldBe(customerId);
    }

    #endregion

    #region FullName Tests

    [Fact]
    public void FullName_ShouldReturnFirstNameAndLastName()
    {
        // Arrange
        var contact = CrmContact.Create(
            "John", "Doe", "john@example.com",
            ContactSource.Web, TestTenantId);

        // Assert
        contact.FullName.ShouldBe("John Doe");
    }

    #endregion
}

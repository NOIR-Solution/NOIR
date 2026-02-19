using NOIR.Domain.Entities.Customer;

namespace NOIR.Domain.UnitTests.Entities.Customer;

/// <summary>
/// Unit tests for the CustomerAddress entity.
/// Tests factory methods, updates, and default address management.
/// </summary>
public class CustomerAddressTests
{
    private const string TestTenantId = "test-tenant";
    private static readonly Guid TestCustomerId = Guid.NewGuid();

    private static CustomerAddress CreateTestAddress(
        Guid? customerId = null,
        AddressType addressType = AddressType.Shipping,
        string fullName = "John Doe",
        string phone = "+84912345678",
        string addressLine1 = "123 Main Street",
        string province = "Ho Chi Minh City",
        string? addressLine2 = null,
        string? ward = "Ward 1",
        string? district = "District 1",
        string? postalCode = "700000",
        bool isDefault = false,
        string? tenantId = TestTenantId)
    {
        return CustomerAddress.Create(
            customerId ?? TestCustomerId,
            addressType,
            fullName,
            phone,
            addressLine1,
            province,
            addressLine2,
            ward,
            district,
            postalCode,
            isDefault,
            tenantId);
    }

    #region Create Factory Tests

    [Fact]
    public void Create_WithAllParameters_ShouldCreateValidAddress()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        // Act
        var address = CreateTestAddress(
            customerId: customerId,
            addressType: AddressType.Shipping,
            fullName: "John Doe",
            phone: "+84912345678",
            addressLine1: "123 Main Street",
            province: "Ho Chi Minh City",
            addressLine2: "Apt 4B",
            ward: "Ward 1",
            district: "District 1",
            postalCode: "700000",
            isDefault: true);

        // Assert
        address.Should().NotBeNull();
        address.Id.Should().NotBe(Guid.Empty);
        address.CustomerId.Should().Be(customerId);
        address.AddressType.Should().Be(AddressType.Shipping);
        address.FullName.Should().Be("John Doe");
        address.Phone.Should().Be("+84912345678");
        address.AddressLine1.Should().Be("123 Main Street");
        address.AddressLine2.Should().Be("Apt 4B");
        address.Ward.Should().Be("Ward 1");
        address.District.Should().Be("District 1");
        address.Province.Should().Be("Ho Chi Minh City");
        address.PostalCode.Should().Be("700000");
        address.IsDefault.Should().BeTrue();
        address.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldDefaultIsDefaultToFalse()
    {
        // Act
        var address = CreateTestAddress();

        // Assert
        address.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void Create_WithOptionalFieldsNull_ShouldAllowNulls()
    {
        // Act
        var address = CreateTestAddress(
            addressLine2: null,
            ward: null,
            district: null,
            postalCode: null);

        // Assert
        address.AddressLine2.Should().BeNull();
        address.Ward.Should().BeNull();
        address.District.Should().BeNull();
        address.PostalCode.Should().BeNull();
    }

    [Theory]
    [InlineData(AddressType.Shipping)]
    [InlineData(AddressType.Billing)]
    [InlineData(AddressType.Both)]
    public void Create_WithAllAddressTypes_ShouldSetCorrectType(AddressType addressType)
    {
        // Act
        var address = CreateTestAddress(addressType: addressType);

        // Assert
        address.AddressType.Should().Be(addressType);
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var address = CreateTestAddress(tenantId: null);

        // Assert
        address.TenantId.Should().BeNull();
    }

    [Fact]
    public void Create_MultipleCalls_ShouldGenerateUniqueIds()
    {
        // Act
        var address1 = CreateTestAddress();
        var address2 = CreateTestAddress();

        // Assert
        address1.Id.Should().NotBe(address2.Id);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldChangeAllFields()
    {
        // Arrange
        var address = CreateTestAddress();

        // Act
        address.Update(
            AddressType.Billing,
            "Jane Smith",
            "+84987654321",
            "456 Oak Avenue",
            "Ha Noi",
            "Suite 200",
            "Ward 5",
            "District 3",
            "100000",
            true);

        // Assert
        address.AddressType.Should().Be(AddressType.Billing);
        address.FullName.Should().Be("Jane Smith");
        address.Phone.Should().Be("+84987654321");
        address.AddressLine1.Should().Be("456 Oak Avenue");
        address.AddressLine2.Should().Be("Suite 200");
        address.Ward.Should().Be("Ward 5");
        address.District.Should().Be("District 3");
        address.Province.Should().Be("Ha Noi");
        address.PostalCode.Should().Be("100000");
        address.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void Update_ShouldNotChangeCustomerId()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var address = CreateTestAddress(customerId: customerId);

        // Act
        address.Update(
            AddressType.Billing,
            "Jane Smith",
            "+84987654321",
            "New Address",
            "Ha Noi");

        // Assert - CustomerId should remain unchanged
        address.CustomerId.Should().Be(customerId);
    }

    [Fact]
    public void Update_WithNullOptionalFields_ShouldClearThem()
    {
        // Arrange
        var address = CreateTestAddress(
            addressLine2: "Apt 4B",
            ward: "Ward 1",
            district: "District 1",
            postalCode: "700000");

        // Act
        address.Update(
            AddressType.Shipping,
            "John Doe",
            "+84912345678",
            "123 Main Street",
            "Ho Chi Minh City",
            addressLine2: null,
            ward: null,
            district: null,
            postalCode: null);

        // Assert
        address.AddressLine2.Should().BeNull();
        address.Ward.Should().BeNull();
        address.District.Should().BeNull();
        address.PostalCode.Should().BeNull();
    }

    [Fact]
    public void Update_ChangeAddressType_ShouldUpdateType()
    {
        // Arrange
        var address = CreateTestAddress(addressType: AddressType.Shipping);

        // Act
        address.Update(
            AddressType.Both,
            address.FullName,
            address.Phone,
            address.AddressLine1,
            address.Province);

        // Assert
        address.AddressType.Should().Be(AddressType.Both);
    }

    #endregion

    #region SetAsDefault / RemoveDefault Tests

    [Fact]
    public void SetAsDefault_ShouldSetIsDefaultTrue()
    {
        // Arrange
        var address = CreateTestAddress(isDefault: false);

        // Act
        address.SetAsDefault();

        // Assert
        address.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void SetAsDefault_AlreadyDefault_ShouldRemainTrue()
    {
        // Arrange
        var address = CreateTestAddress(isDefault: true);

        // Act
        address.SetAsDefault();

        // Assert
        address.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void RemoveDefault_ShouldSetIsDefaultFalse()
    {
        // Arrange
        var address = CreateTestAddress(isDefault: true);

        // Act
        address.RemoveDefault();

        // Assert
        address.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void RemoveDefault_AlreadyNonDefault_ShouldRemainFalse()
    {
        // Arrange
        var address = CreateTestAddress(isDefault: false);

        // Act
        address.RemoveDefault();

        // Assert
        address.IsDefault.Should().BeFalse();
    }

    #endregion
}

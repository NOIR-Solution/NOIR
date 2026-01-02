namespace NOIR.Domain.UnitTests.Common;

/// <summary>
/// Unit tests for the ValueObject base class.
/// Tests equality semantics based on components, not reference.
/// </summary>
public class ValueObjectTests
{
    #region Test Value Objects

    private sealed class Address : ValueObject
    {
        public string Street { get; }
        public string City { get; }
        public string ZipCode { get; }

        public Address(string street, string city, string zipCode)
        {
            Street = street;
            City = city;
            ZipCode = zipCode;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
            yield return ZipCode;
        }
    }

    private sealed class Money : ValueObject
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    private sealed class OptionalValue : ValueObject
    {
        public string? OptionalField { get; }
        public int RequiredField { get; }

        public OptionalValue(string? optionalField, int requiredField)
        {
            OptionalField = optionalField;
            RequiredField = requiredField;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return OptionalField;
            yield return RequiredField;
        }
    }

    private sealed class EmptyValueObject : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield break;
        }
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameValues_ShouldReturnTrue()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("123 Main St", "Springfield", "12345");

        // Act & Assert
        address1.Equals(address2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("456 Oak Ave", "Springfield", "12345");

        // Act & Assert
        address1.Equals(address2).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentType_ShouldReturnFalse()
    {
        // Arrange
        var address = new Address("123 Main St", "Springfield", "12345");
        var money = new Money(100m, "USD");

        // Act & Assert
        address.Equals(money).Should().BeFalse();
    }

    [Fact]
    public void Equals_Null_ShouldReturnFalse()
    {
        // Arrange
        var address = new Address("123 Main St", "Springfield", "12345");

        // Act & Assert
        address.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_SameReference_ShouldReturnTrue()
    {
        // Arrange
        var address = new Address("123 Main St", "Springfield", "12345");

        // Act & Assert
        address.Equals(address).Should().BeTrue();
    }

    [Fact]
    public void Equals_ObjectOverload_SameValues_ShouldReturnTrue()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        object address2 = new Address("123 Main St", "Springfield", "12345");

        // Act & Assert
        address1.Equals(address2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ObjectOverload_NullObject_ShouldReturnFalse()
    {
        // Arrange
        var address = new Address("123 Main St", "Springfield", "12345");
        object? nullObject = null;

        // Act & Assert
        address.Equals(nullObject).Should().BeFalse();
    }

    [Fact]
    public void Equals_ObjectOverload_DifferentType_ShouldReturnFalse()
    {
        // Arrange
        var address = new Address("123 Main St", "Springfield", "12345");
        object notAValueObject = "not a value object";

        // Act & Assert
        address.Equals(notAValueObject).Should().BeFalse();
    }

    #endregion

    #region Null Component Tests

    [Fact]
    public void Equals_BothHaveNullComponent_ShouldReturnTrue()
    {
        // Arrange
        var value1 = new OptionalValue(null, 42);
        var value2 = new OptionalValue(null, 42);

        // Act & Assert
        value1.Equals(value2).Should().BeTrue();
    }

    [Fact]
    public void Equals_OneHasNullComponent_ShouldReturnFalse()
    {
        // Arrange
        var value1 = new OptionalValue(null, 42);
        var value2 = new OptionalValue("has value", 42);

        // Act & Assert
        value1.Equals(value2).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentNonNullComponents_ShouldReturnFalse()
    {
        // Arrange
        var value1 = new OptionalValue("value1", 42);
        var value2 = new OptionalValue("value2", 42);

        // Act & Assert
        value1.Equals(value2).Should().BeFalse();
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_SameValues_ShouldReturnSameHash()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("123 Main St", "Springfield", "12345");

        // Act & Assert
        address1.GetHashCode().Should().Be(address2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentValues_ShouldReturnDifferentHash()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("456 Oak Ave", "Shelbyville", "67890");

        // Act & Assert
        // Different values typically produce different hashes
        address1.GetHashCode().Should().NotBe(address2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_ConsistentAcrossMultipleCalls()
    {
        // Arrange
        var address = new Address("123 Main St", "Springfield", "12345");

        // Act
        var hash1 = address.GetHashCode();
        var hash2 = address.GetHashCode();
        var hash3 = address.GetHashCode();

        // Assert
        hash1.Should().Be(hash2).And.Be(hash3);
    }

    [Fact]
    public void GetHashCode_WithNullComponent_ShouldNotThrow()
    {
        // Arrange
        var value = new OptionalValue(null, 42);

        // Act
        var act = () => value.GetHashCode();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void GetHashCode_EmptyValueObject_ShouldNotThrow()
    {
        // Arrange
        var empty = new EmptyValueObject();

        // Act
        var act = () => empty.GetHashCode();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void GetHashCode_OrderMatters()
    {
        // This tests that (a,b) has different hash than (b,a) when using proper hash combination
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(100m, "EUR");

        // Act & Assert
        money1.GetHashCode().Should().NotBe(money2.GetHashCode());
    }

    #endregion

    #region Operator Tests

    [Fact]
    public void OperatorEquals_SameValues_ShouldReturnTrue()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("123 Main St", "Springfield", "12345");

        // Act & Assert
        (address1 == address2).Should().BeTrue();
    }

    [Fact]
    public void OperatorEquals_DifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("456 Oak Ave", "Springfield", "12345");

        // Act & Assert
        (address1 == address2).Should().BeFalse();
    }

    [Fact]
    public void OperatorEquals_BothNull_ShouldReturnTrue()
    {
        // Arrange
        Address? address1 = null;
        Address? address2 = null;

        // Act & Assert
        (address1 == address2).Should().BeTrue();
    }

    [Fact]
    public void OperatorEquals_LeftNull_ShouldReturnFalse()
    {
        // Arrange
        Address? address1 = null;
        var address2 = new Address("123 Main St", "Springfield", "12345");

        // Act & Assert
        (address1 == address2).Should().BeFalse();
    }

    [Fact]
    public void OperatorEquals_RightNull_ShouldReturnFalse()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        Address? address2 = null;

        // Act & Assert
        (address1 == address2).Should().BeFalse();
    }

    [Fact]
    public void OperatorNotEquals_DifferentValues_ShouldReturnTrue()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("456 Oak Ave", "Springfield", "12345");

        // Act & Assert
        (address1 != address2).Should().BeTrue();
    }

    [Fact]
    public void OperatorNotEquals_SameValues_ShouldReturnFalse()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("123 Main St", "Springfield", "12345");

        // Act & Assert
        (address1 != address2).Should().BeFalse();
    }

    #endregion

    #region Money Value Object Tests

    [Theory]
    [InlineData(100.00, "USD", 100.00, "USD", true)]
    [InlineData(100.00, "USD", 100.00, "EUR", false)]
    [InlineData(100.00, "USD", 200.00, "USD", false)]
    [InlineData(0.00, "USD", 0.00, "USD", true)]
    public void Money_Equals_VariousInputs(
        decimal amount1, string currency1,
        decimal amount2, string currency2,
        bool expected)
    {
        // Arrange
        var money1 = new Money(amount1, currency1);
        var money2 = new Money(amount2, currency2);

        // Act & Assert
        money1.Equals(money2).Should().Be(expected);
    }

    [Fact]
    public void Money_DecimalPrecision_ShouldBeRespected()
    {
        // Arrange
        var money1 = new Money(100.00m, "USD");
        var money2 = new Money(100.000m, "USD");

        // Act & Assert
        // Decimal equality respects trailing zeros in internal representation
        // but the values are mathematically equal
        (money1.Amount == money2.Amount).Should().BeTrue();
        money1.Equals(money2).Should().BeTrue();
    }

    #endregion

    #region Empty Value Object Tests

    [Fact]
    public void EmptyValueObject_TwoInstances_ShouldBeEqual()
    {
        // Arrange
        var empty1 = new EmptyValueObject();
        var empty2 = new EmptyValueObject();

        // Act & Assert
        empty1.Equals(empty2).Should().BeTrue();
    }

    [Fact]
    public void EmptyValueObject_GetHashCode_ShouldBeSame()
    {
        // Arrange
        var empty1 = new EmptyValueObject();
        var empty2 = new EmptyValueObject();

        // Act & Assert
        empty1.GetHashCode().Should().Be(empty2.GetHashCode());
    }

    #endregion

    #region Dictionary and Set Usage Tests

    [Fact]
    public void ValueObject_CanBeUsedAsDictionaryKey()
    {
        // Arrange
        var address = new Address("123 Main St", "Springfield", "12345");
        var sameAddress = new Address("123 Main St", "Springfield", "12345");
        var dictionary = new Dictionary<Address, string>
        {
            { address, "Home" }
        };

        // Act & Assert
        dictionary.ContainsKey(sameAddress).Should().BeTrue();
        dictionary[sameAddress].Should().Be("Home");
    }

    [Fact]
    public void ValueObject_CanBeUsedInHashSet()
    {
        // Arrange
        var address1 = new Address("123 Main St", "Springfield", "12345");
        var address2 = new Address("123 Main St", "Springfield", "12345");
        var address3 = new Address("456 Oak Ave", "Springfield", "12345");

        var set = new HashSet<Address> { address1, address2, address3 };

        // Act & Assert
        set.Count.Should().Be(2); // address1 and address2 are equal, so only 2 items
    }

    #endregion
}

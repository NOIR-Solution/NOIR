namespace NOIR.Application.UnitTests.Services;

/// <summary>
/// Unit tests for JsonDiffService.
/// Tests simple field-level diff generation: {"fieldName": {"from": oldValue, "to": newValue}}
/// </summary>
public class JsonDiffServiceTests
{
    private readonly JsonDiffService _sut = new();

    #region CreateDiff<T> Tests

    [Fact]
    public void CreateDiff_BothNull_ShouldReturnNull()
    {
        // Act
        var result = _sut.CreateDiff<TestDto>(null, null);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void CreateDiff_BeforeNullAfterExists_ShouldReturnFieldsWithNullFrom()
    {
        // Arrange
        var after = new TestDto { Name = "John", Age = 30 };

        // Act
        var result = _sut.CreateDiff<TestDto>(null, after);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("\"from\":null");
        result.ShouldContain("\"to\":\"John\"");
    }

    [Fact]
    public void CreateDiff_BeforeExistsAfterNull_ShouldReturnFieldsWithNullTo()
    {
        // Arrange
        var before = new TestDto { Name = "John", Age = 30 };

        // Act
        var result = _sut.CreateDiff<TestDto>(before, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("\"from\":\"John\"");
        result.ShouldContain("\"to\":null");
    }

    [Fact]
    public void CreateDiff_IdenticalObjects_ShouldReturnNull()
    {
        // Arrange
        var before = new TestDto { Name = "John", Age = 30 };
        var after = new TestDto { Name = "John", Age = 30 };

        // Act
        var result = _sut.CreateDiff(before, after);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void CreateDiff_SinglePropertyChange_ShouldReturnFieldLevelChange()
    {
        // Arrange
        var before = new TestDto { Name = "John", Age = 30 };
        var after = new TestDto { Name = "Jane", Age = 30 };

        // Act
        var result = _sut.CreateDiff(before, after);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("\"name\":");
        result.ShouldContain("\"from\":\"John\"");
        result.ShouldContain("\"to\":\"Jane\"");
    }

    [Fact]
    public void CreateDiff_MultiplePropertyChanges_ShouldReturnMultipleFieldChanges()
    {
        // Arrange
        var before = new TestDto { Name = "John", Age = 30 };
        var after = new TestDto { Name = "Jane", Age = 25 };

        // Act
        var result = _sut.CreateDiff(before, after);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("\"name\":");
        result.ShouldContain("\"age\":");
    }

    [Fact]
    public void CreateDiff_PropertyAddedToObject_ShouldReturnNullToValueChange()
    {
        // Arrange
        var before = new TestDtoNullable { Name = "John", Email = null };
        var after = new TestDtoNullable { Name = "John", Email = "john@example.com" };

        // Act
        var result = _sut.CreateDiff(before, after);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("\"email\":");
        result.ShouldContain("\"to\":\"john@example.com\"");
    }

    #endregion

    #region CreateDiffFromJson Tests

    [Fact]
    public void CreateDiffFromJson_BothNull_ShouldReturnNull()
    {
        // Act
        var result = _sut.CreateDiffFromJson(null, null);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void CreateDiffFromJson_BeforeNullAfterExists_ShouldReturnFieldsWithNullFrom()
    {
        // Arrange
        var afterJson = "{\"name\":\"John\",\"age\":30}";

        // Act
        var result = _sut.CreateDiffFromJson(null, afterJson);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("\"from\":null");
        result.ShouldContain("\"to\":\"John\"");
    }

    [Fact]
    public void CreateDiffFromJson_BeforeExistsAfterNull_ShouldReturnFieldsWithNullTo()
    {
        // Arrange
        var beforeJson = "{\"name\":\"John\",\"age\":30}";

        // Act
        var result = _sut.CreateDiffFromJson(beforeJson, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("\"from\":\"John\"");
        result.ShouldContain("\"to\":null");
    }

    [Fact]
    public void CreateDiffFromJson_IdenticalJson_ShouldReturnNull()
    {
        // Arrange
        var json = "{\"name\":\"John\",\"age\":30}";

        // Act
        var result = _sut.CreateDiffFromJson(json, json);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void CreateDiffFromJson_PropertyChange_ShouldReturnFromToChange()
    {
        // Arrange
        var beforeJson = "{\"name\":\"John\",\"age\":30}";
        var afterJson = "{\"name\":\"Jane\",\"age\":30}";

        // Act
        var result = _sut.CreateDiffFromJson(beforeJson, afterJson);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("\"from\":\"John\"");
        result.ShouldContain("\"to\":\"Jane\"");
    }

    [Fact]
    public void CreateDiffFromJson_PropertyRemoved_ShouldReturnNullTo()
    {
        // Arrange
        var beforeJson = "{\"name\":\"John\",\"age\":30,\"email\":\"john@test.com\"}";
        var afterJson = "{\"name\":\"John\",\"age\":30}";

        // Act
        var result = _sut.CreateDiffFromJson(beforeJson, afterJson);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("\"email\":");
        result.ShouldContain("\"from\":\"john@test.com\"");
        result.ShouldContain("\"to\":null");
    }

    [Fact]
    public void CreateDiffFromJson_PropertyAdded_ShouldReturnNullFrom()
    {
        // Arrange
        var beforeJson = "{\"name\":\"John\",\"age\":30}";
        var afterJson = "{\"name\":\"John\",\"age\":30,\"email\":\"john@test.com\"}";

        // Act
        var result = _sut.CreateDiffFromJson(beforeJson, afterJson);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("\"email\":");
        result.ShouldContain("\"from\":null");
        result.ShouldContain("\"to\":\"john@test.com\"");
    }

    #endregion

    #region CreateDiffFromDictionaries Tests

    [Fact]
    public void CreateDiffFromDictionaries_BothNull_ShouldReturnNull()
    {
        // Act
        var result = _sut.CreateDiffFromDictionaries(null, null);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void CreateDiffFromDictionaries_IdenticalDictionaries_ShouldReturnNull()
    {
        // Arrange
        var before = new Dictionary<string, object?> { ["Name"] = "John", ["Age"] = 30 };
        var after = new Dictionary<string, object?> { ["Name"] = "John", ["Age"] = 30 };

        // Act
        var result = _sut.CreateDiffFromDictionaries(before, after);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void CreateDiffFromDictionaries_PropertyChanged_ShouldReturnFromToChange()
    {
        // Arrange
        var before = new Dictionary<string, object?> { ["Name"] = "John", ["Age"] = 30 };
        var after = new Dictionary<string, object?> { ["Name"] = "Jane", ["Age"] = 30 };

        // Act
        var result = _sut.CreateDiffFromDictionaries(before, after);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("\"Name\":");
        result.ShouldContain("\"from\":\"John\"");
        result.ShouldContain("\"to\":\"Jane\"");
    }

    [Fact]
    public void CreateDiffFromDictionaries_PropertyAdded_ShouldReturnNullFrom()
    {
        // Arrange
        var before = new Dictionary<string, object?> { ["Name"] = "John" };
        var after = new Dictionary<string, object?> { ["Name"] = "John", ["Age"] = 30 };

        // Act
        var result = _sut.CreateDiffFromDictionaries(before, after);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("\"Age\":");
        result.ShouldContain("\"from\":null");
        result.ShouldContain("\"to\":30");
    }

    [Fact]
    public void CreateDiffFromDictionaries_PropertyRemoved_ShouldReturnNullTo()
    {
        // Arrange
        var before = new Dictionary<string, object?> { ["Name"] = "John", ["Age"] = 30 };
        var after = new Dictionary<string, object?> { ["Name"] = "John" };

        // Act
        var result = _sut.CreateDiffFromDictionaries(before, after);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("\"Age\":");
        result.ShouldContain("\"from\":30");
        result.ShouldContain("\"to\":null");
    }

    [Fact]
    public void CreateDiffFromDictionaries_NullValueToValue_ShouldReturnFromToChange()
    {
        // Arrange
        var before = new Dictionary<string, object?> { ["Name"] = null };
        var after = new Dictionary<string, object?> { ["Name"] = "John" };

        // Act
        var result = _sut.CreateDiffFromDictionaries(before, after);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("\"from\":null");
        result.ShouldContain("\"to\":\"John\"");
    }

    [Fact]
    public void CreateDiffFromDictionaries_ValueToNull_ShouldReturnFromToChange()
    {
        // Arrange
        var before = new Dictionary<string, object?> { ["Name"] = "John" };
        var after = new Dictionary<string, object?> { ["Name"] = null };

        // Act
        var result = _sut.CreateDiffFromDictionaries(before, after);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("\"from\":\"John\"");
        result.ShouldContain("\"to\":null");
    }

    [Fact]
    public void CreateDiffFromDictionaries_BeforeNullAfterExists_ShouldReturnNullFromFields()
    {
        // Arrange
        var after = new Dictionary<string, object?> { ["Name"] = "John", ["Age"] = 30 };

        // Act
        var result = _sut.CreateDiffFromDictionaries(null, after);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("\"from\":null");
        result.ShouldContain("\"to\":\"John\"");
    }

    [Fact]
    public void CreateDiffFromDictionaries_BeforeExistsAfterNull_ShouldReturnNullToFields()
    {
        // Arrange
        var before = new Dictionary<string, object?> { ["Name"] = "John", ["Age"] = 30 };

        // Act
        var result = _sut.CreateDiffFromDictionaries(before, null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("\"from\":\"John\"");
        result.ShouldContain("\"to\":null");
    }

    #endregion

    #region Nested Object Tests

    [Fact]
    public void CreateDiff_NestedObjectChange_ShouldUseDotNotationPath()
    {
        // Arrange
        var before = new TestDtoWithNested
        {
            Name = "John",
            Address = new AddressDto { City = "New York", Country = "USA" }
        };
        var after = new TestDtoWithNested
        {
            Name = "John",
            Address = new AddressDto { City = "Los Angeles", Country = "USA" }
        };

        // Act
        var result = _sut.CreateDiff(before, after);

        // Assert
        result.ShouldNotBeNull();
        // Nested path uses dot notation: address.city
        result.ShouldContain("\"address.city\":");
        result.ShouldContain("\"from\":\"New York\"");
        result.ShouldContain("\"to\":\"Los Angeles\"");
    }

    [Fact]
    public void CreateDiffFromJson_DeeplyNestedChange_ShouldUseDotNotationPath()
    {
        // Arrange
        var beforeJson = "{\"level1\":{\"level2\":{\"level3\":{\"value\":\"old\"}}}}";
        var afterJson = "{\"level1\":{\"level2\":{\"level3\":{\"value\":\"new\"}}}}";

        // Act
        var result = _sut.CreateDiffFromJson(beforeJson, afterJson);

        // Assert
        result.ShouldNotBeNull();
        // Nested path uses dot notation: level1.level2.level3.value
        result.ShouldContain("\"level1.level2.level3.value\":");
        result.ShouldContain("\"from\":\"old\"");
        result.ShouldContain("\"to\":\"new\"");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void CreateDiff_EmptyObjects_ShouldReturnNull()
    {
        // Arrange
        var before = new TestDto();
        var after = new TestDto();

        // Act
        var result = _sut.CreateDiff(before, after);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void CreateDiffFromJson_EmptyObjects_ShouldReturnNull()
    {
        // Act
        var result = _sut.CreateDiffFromJson("{}", "{}");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void CreateDiff_ArrayPropertyChange_ShouldDetectChange()
    {
        // Arrange
        var before = new TestDtoWithArray { Tags = new[] { "tag1", "tag2" } };
        var after = new TestDtoWithArray { Tags = new[] { "tag1", "tag3" } };

        // Act
        var result = _sut.CreateDiff(before, after);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("\"tags\":");
        result.ShouldContain("\"from\":");
        result.ShouldContain("\"to\":");
    }

    [Fact]
    public void CreateDiff_BooleanChange_ShouldDetectChange()
    {
        // Arrange
        var before = new TestDtoWithBool { IsActive = true };
        var after = new TestDtoWithBool { IsActive = false };

        // Act
        var result = _sut.CreateDiff(before, after);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("\"isActive\":");
        result.ShouldContain("\"from\":true");
        result.ShouldContain("\"to\":false");
    }

    [Fact]
    public void CreateDiff_NumericTypeChange_ShouldDetectChange()
    {
        // Arrange
        var before = new TestDto { Age = 30 };
        var after = new TestDto { Age = 31 };

        // Act
        var result = _sut.CreateDiff(before, after);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("\"age\":");
        result.ShouldContain("\"from\":30");
        result.ShouldContain("\"to\":31");
    }

    [Fact]
    public void CreateDiffFromDictionaries_ComplexObjectValue_ShouldCompareCorrectly()
    {
        // Arrange
        var complexValue1 = new { City = "NYC", Zip = "10001" };
        var complexValue2 = new { City = "LA", Zip = "90001" };

        var before = new Dictionary<string, object?> { ["Address"] = complexValue1 };
        var after = new Dictionary<string, object?> { ["Address"] = complexValue2 };

        // Act
        var result = _sut.CreateDiffFromDictionaries(before, after);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("\"Address\":");
        result.ShouldContain("\"from\":");
        result.ShouldContain("\"to\":");
    }

    #endregion

    #region Test DTOs

    private class TestDto
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    private class TestDtoNullable
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
    }

    private class TestDtoWithNested
    {
        public string? Name { get; set; }
        public AddressDto? Address { get; set; }
    }

    private class AddressDto
    {
        public string? City { get; set; }
        public string? Country { get; set; }
    }

    private class TestDtoWithArray
    {
        public string[]? Tags { get; set; }
    }

    private class TestDtoWithBool
    {
        public bool IsActive { get; set; }
    }

    #endregion
}

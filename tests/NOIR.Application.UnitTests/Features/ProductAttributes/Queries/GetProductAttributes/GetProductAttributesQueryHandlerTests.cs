using NOIR.Application.Features.ProductAttributes.Queries.GetProductAttributes;
using NOIR.Application.Features.ProductAttributes.Specifications;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Queries.GetProductAttributes;

/// <summary>
/// Unit tests for GetProductAttributesQueryHandler.
/// Tests getting paged list of product attributes.
/// </summary>
public class GetProductAttributesQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ProductAttribute, Guid>> _attributeRepositoryMock;
    private readonly GetProductAttributesQueryHandler _handler;

    public GetProductAttributesQueryHandlerTests()
    {
        _attributeRepositoryMock = new Mock<IRepository<ProductAttribute, Guid>>();

        _handler = new GetProductAttributesQueryHandler(_attributeRepositoryMock.Object);
    }

    private static ProductAttribute CreateTestAttribute(
        string code = "test_attr",
        string name = "Test Attribute",
        AttributeType type = AttributeType.Text,
        bool isActive = true,
        bool isFilterable = false,
        bool isVariantAttribute = false,
        string? tenantId = "tenant-1")
    {
        var attr = ProductAttribute.Create(code, name, type, tenantId);
        attr.SetActive(isActive);
        attr.SetBehaviorFlags(isFilterable, isSearchable: false, isRequired: false, isVariantAttribute);
        return attr;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithNoFilters_ShouldReturnPagedResults()
    {
        // Arrange
        var attribute1 = CreateTestAttribute("attr1", "Attribute 1");
        var attribute2 = CreateTestAttribute("attr2", "Attribute 2");
        var attribute3 = CreateTestAttribute("attr3", "Attribute 3");

        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductAttributesPagedSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute> { attribute1, attribute2, attribute3 });

        _attributeRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductAttributesCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var query = new GetProductAttributesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WithSearchFilter_ShouldPassToSpecification()
    {
        // Arrange
        var attribute = CreateTestAttribute("color", "Color");

        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductAttributesPagedSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute> { attribute });

        _attributeRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductAttributesCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new GetProductAttributesQuery(Search: "color");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Name.Should().Be("Color");
    }

    [Fact]
    public async Task Handle_WithIsActiveFilter_ShouldPassToSpecification()
    {
        // Arrange
        var activeAttribute = CreateTestAttribute("active", "Active Attr", isActive: true);

        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductAttributesPagedSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute> { activeAttribute });

        _attributeRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductAttributesCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new GetProductAttributesQuery(IsActive: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithIsFilterableFilter_ShouldPassToSpecification()
    {
        // Arrange
        var filterableAttribute = CreateTestAttribute("filterable", "Filterable Attr", isFilterable: true);

        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductAttributesPagedSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute> { filterableAttribute });

        _attributeRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductAttributesCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new GetProductAttributesQuery(IsFilterable: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().IsFilterable.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithIsVariantAttributeFilter_ShouldPassToSpecification()
    {
        // Arrange
        var variantAttribute = CreateTestAttribute("variant", "Variant Attr", isVariantAttribute: true);

        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductAttributesPagedSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute> { variantAttribute });

        _attributeRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductAttributesCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new GetProductAttributesQuery(IsVariantAttribute: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().IsVariantAttribute.Should().BeTrue();
    }

    [Theory]
    [InlineData("Text")]
    [InlineData("Number")]
    [InlineData("Select")]
    [InlineData("Boolean")]
    public async Task Handle_WithTypeFilter_ShouldPassToSpecification(string typeFilter)
    {
        // Arrange
        var type = Enum.Parse<AttributeType>(typeFilter);
        var attribute = CreateTestAttribute($"attr_{typeFilter.ToLower()}", $"Test {typeFilter}", type);

        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductAttributesPagedSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute> { attribute });

        _attributeRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductAttributesCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new GetProductAttributesQuery(Type: typeFilter);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Type.Should().Be(typeFilter);
    }

    [Fact]
    public async Task Handle_WithPagination_ShouldReturnCorrectPageInfo()
    {
        // Arrange
        var attributes = Enumerable.Range(1, 5)
            .Select(i => CreateTestAttribute($"attr{i}", $"Attribute {i}"))
            .ToList();

        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductAttributesPagedSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attributes);

        _attributeRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductAttributesCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(50); // Total of 50 items

        var query = new GetProductAttributesQuery(Page: 2, PageSize: 5);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(50);
        result.Value.PageIndex.Should().Be(1); // PageNumber 2 = PageIndex 1 (0-based)
        result.Value.PageSize.Should().Be(5);
        result.Value.TotalPages.Should().Be(10); // 50 items / 5 per page
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithNoResults_ShouldReturnEmptyList()
    {
        // Arrange
        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductAttributesPagedSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute>());

        _attributeRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductAttributesCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetProductAttributesQuery(Search: "nonexistent");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassToAllServices()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductAttributesPagedSpec>(),
                token))
            .ReturnsAsync(new List<ProductAttribute>());

        _attributeRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductAttributesCountSpec>(),
                token))
            .ReturnsAsync(0);

        var query = new GetProductAttributesQuery();

        // Act
        await _handler.Handle(query, token);

        // Assert
        _attributeRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ProductAttributesPagedSpec>(), token),
            Times.Once);
        _attributeRepositoryMock.Verify(
            x => x.CountAsync(It.IsAny<ProductAttributesCountSpec>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleFilters_ShouldApplyAll()
    {
        // Arrange
        var attribute = CreateTestAttribute(
            "color",
            "Color",
            AttributeType.Select,
            isActive: true,
            isFilterable: true,
            isVariantAttribute: true);

        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductAttributesPagedSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute> { attribute });

        _attributeRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductAttributesCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new GetProductAttributesQuery(
            Search: "color",
            IsActive: true,
            IsFilterable: true,
            IsVariantAttribute: true,
            Type: "Select");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_DefaultPagination_ShouldUseDefaultValues()
    {
        // Arrange
        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductAttributesPagedSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute>());

        _attributeRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductAttributesCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Query with default values
        var query = new GetProductAttributesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PageSize.Should().Be(20); // Default page size
        result.Value.PageIndex.Should().Be(0); // PageNumber 1 = PageIndex 0
    }

    #endregion
}

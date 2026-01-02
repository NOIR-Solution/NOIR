namespace NOIR.Application.UnitTests.Specifications;

/// <summary>
/// Unit tests for the Projection Specification pattern implementation.
/// Tests <see cref="Specification{T, TResult}"/> and <see cref="ProjectionSpecificationBuilder{T, TResult}"/>.
/// </summary>
public class ProjectionSpecificationTests
{
    #region Test Entities and DTOs

    private sealed class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    private sealed class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    #endregion

    #region Test Specifications

    private sealed class ProductSummarySpec : Specification<Product, ProductDto>
    {
        public ProductSummarySpec(bool activeOnly = false)
        {
            if (activeOnly)
                Query.Where(p => p.IsActive);

            Query.Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price
                })
                .OrderBy(p => p.Name)
                .TagWith("ProductSummary");
        }
    }

    private sealed class PagedProductSummarySpec : Specification<Product, ProductDto>
    {
        public PagedProductSummarySpec(int pageIndex, int pageSize)
        {
            Query.Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price
                })
                .Paginate(pageIndex, pageSize)
                .TagWith("PagedProductSummary");
        }
    }

    private sealed class TrackedProductSummarySpec : Specification<Product, ProductDto>
    {
        public TrackedProductSummarySpec()
        {
            Query.Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price
                })
                .AsTracking()
                .TagWith("TrackedProductSummary");
        }
    }

    private sealed class FullFeaturedProjectionSpec : Specification<Product, ProductDto>
    {
        public FullFeaturedProjectionSpec()
        {
            Query.Where(p => p.IsActive)
                .Select(p => new ProductDto { Id = p.Id, Name = p.Name, Price = p.Price })
                .OrderByDescending(p => p.Price)
                .ThenBy(p => p.Name)
                .ThenByDescending(p => p.Id)
                .Skip(5)
                .Take(10)
                .TagWith("FullFeatured");
        }
    }

    #endregion

    #region Selector Tests

    [Fact]
    public void ProjectionSpec_ShouldHaveSelector()
    {
        // Arrange
        var spec = new ProductSummarySpec();

        // Assert
        spec.Selector.Should().NotBeNull();
    }

    [Fact]
    public void ProjectionSpec_SelectorCanCompile()
    {
        // Arrange
        var spec = new ProductSummarySpec();
        var product = new Product { Id = 1, Name = "Test", Price = 10.00m };

        // Act
        var compiled = spec.Selector!.Compile();
        var result = compiled(product);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("Test");
        result.Price.Should().Be(10.00m);
    }

    #endregion

    #region Query Tag Tests

    [Fact]
    public void ProjectionSpec_ShouldHaveQueryTags()
    {
        // Arrange
        var spec = new ProductSummarySpec();

        // Assert
        spec.QueryTags.Should().Contain("ProductSummary");
    }

    [Fact]
    public void ProjectionSpec_FullFeatured_ShouldHaveQueryTag()
    {
        // Arrange
        var spec = new FullFeaturedProjectionSpec();

        // Assert
        spec.QueryTags.Should().Contain("FullFeatured");
    }

    #endregion

    #region Where Expression Tests

    [Fact]
    public void ProjectionSpec_WithWhere_ShouldHaveWhereExpression()
    {
        // Arrange
        var spec = new ProductSummarySpec(activeOnly: true);

        // Assert
        spec.WhereExpressions.Should().HaveCount(1);
    }

    [Fact]
    public void ProjectionSpec_WithoutWhere_ShouldHaveNoWhereExpression()
    {
        // Arrange
        var spec = new ProductSummarySpec(activeOnly: false);

        // Assert
        spec.WhereExpressions.Should().BeEmpty();
    }

    #endregion

    #region OrderBy Tests

    [Fact]
    public void ProjectionSpec_ShouldHaveOrderBy()
    {
        // Arrange
        var spec = new ProductSummarySpec();

        // Assert
        spec.OrderBy.Should().NotBeNull();
    }

    [Fact]
    public void ProjectionSpec_OrderByDescending_ShouldSetProperty()
    {
        // Arrange
        var spec = new FullFeaturedProjectionSpec();

        // Assert
        spec.OrderByDescending.Should().NotBeNull();
        spec.OrderBy.Should().BeNull(); // OrderByDescending clears OrderBy
    }

    [Fact]
    public void ProjectionSpec_ThenBy_ShouldAddToList()
    {
        // Arrange
        var spec = new FullFeaturedProjectionSpec();

        // Assert
        spec.ThenByExpressions.Should().HaveCount(1);
    }

    [Fact]
    public void ProjectionSpec_ThenByDescending_ShouldAddToList()
    {
        // Arrange
        var spec = new FullFeaturedProjectionSpec();

        // Assert
        spec.ThenByDescendingExpressions.Should().HaveCount(1);
    }

    #endregion

    #region Paging Tests

    [Fact]
    public void ProjectionSpec_WithPaging_ShouldHavePagingProperties()
    {
        // Arrange
        var spec = new PagedProductSummarySpec(pageIndex: 2, pageSize: 5);

        // Assert
        spec.Skip.Should().Be(10); // 2 * 5
        spec.Take.Should().Be(5);
        spec.IsPagingEnabled.Should().BeTrue();
    }

    [Fact]
    public void ProjectionSpec_Skip_ShouldSetValue()
    {
        // Arrange
        var spec = new FullFeaturedProjectionSpec();

        // Assert
        spec.Skip.Should().Be(5);
    }

    [Fact]
    public void ProjectionSpec_Take_ShouldSetValue()
    {
        // Arrange
        var spec = new FullFeaturedProjectionSpec();

        // Assert
        spec.Take.Should().Be(10);
    }

    [Fact]
    public void ProjectionSpec_FirstPage_ShouldCalculateCorrectSkip()
    {
        // Arrange
        var spec = new PagedProductSummarySpec(pageIndex: 0, pageSize: 10);

        // Assert
        spec.Skip.Should().Be(0);
        spec.Take.Should().Be(10);
    }

    #endregion

    #region Tracking Tests

    [Fact]
    public void ProjectionSpec_WithAsTracking_ShouldDisableNoTracking()
    {
        // Arrange
        var spec = new TrackedProductSummarySpec();

        // Assert
        spec.AsNoTracking.Should().BeFalse();
        spec.AsNoTrackingWithIdentityResolution.Should().BeFalse();
    }

    [Fact]
    public void ProjectionSpec_DefaultAsNoTracking_ShouldBeTrue()
    {
        // Arrange
        var spec = new ProductSummarySpec();

        // Assert
        spec.AsNoTracking.Should().BeTrue();
    }

    #endregion

    #region IsSatisfiedBy Inheritance Tests

    [Fact]
    public void ProjectionSpec_IsSatisfiedBy_WithMatchingEntity_ShouldReturnTrue()
    {
        // Arrange - Projection spec with Where clause
        var spec = new ProductSummarySpec(activeOnly: true);
        var product = new Product { IsActive = true };

        // Act
        var result = spec.IsSatisfiedBy(product);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ProjectionSpec_IsSatisfiedBy_WithNonMatchingEntity_ShouldReturnFalse()
    {
        // Arrange
        var spec = new ProductSummarySpec(activeOnly: true);
        var product = new Product { IsActive = false };

        // Act
        var result = spec.IsSatisfiedBy(product);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ProjectionSpec_IsSatisfiedBy_WithNoWhere_ShouldReturnTrue()
    {
        // Arrange - Spec without where clause matches everything
        var spec = new ProductSummarySpec(activeOnly: false);
        var product = new Product { IsActive = false };

        // Act
        var result = spec.IsSatisfiedBy(product);

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}

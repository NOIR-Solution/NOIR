namespace NOIR.Application.UnitTests.Common;

/// <summary>
/// Unit tests for PagedResult and pagination helpers.
/// </summary>
public class PagedResultTests
{
    #region PagedResult Tests

    [Fact]
    public void PagedResult_Create_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var items = new List<string> { "item1", "item2", "item3" };

        // Act
        var result = PagedResult<string>.Create(items, 100, 0, 10);

        // Assert
        result.Items.Should().BeEquivalentTo(items);
        result.TotalCount.Should().Be(100);
        result.PageIndex.Should().Be(0);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(10);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PagedResult_LastPage_ShouldIndicateNoNextPage()
    {
        // Arrange
        var items = new List<string> { "item1" };

        // Act
        var result = PagedResult<string>.Create(items, 100, 9, 10);

        // Assert
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void PagedResult_MiddlePage_ShouldIndicateBothPages()
    {
        // Arrange
        var items = new List<string> { "item1", "item2" };

        // Act
        var result = PagedResult<string>.Create(items, 100, 5, 10);

        // Assert
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PagedResult_Empty_ShouldCreateEmptyResult()
    {
        // Act
        var result = PagedResult<string>.Empty(0, 10);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void PagedResult_SinglePage_ShouldCalculateCorrectly()
    {
        // Arrange
        var items = new List<string> { "item1", "item2", "item3" };

        // Act
        var result = PagedResult<string>.Create(items, 3, 0, 10);

        // Assert
        result.TotalPages.Should().Be(1);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void PagedResult_FirstItemOnPage_ShouldCalculateCorrectly()
    {
        // Arrange
        var items = new List<string> { "item1", "item2" };

        // Act
        var result = PagedResult<string>.Create(items, 50, 2, 10);

        // Assert
        result.FirstItemOnPage.Should().Be(21); // Page 2 (0-indexed) starts at item 21
        result.LastItemOnPage.Should().Be(30);
    }

    [Fact]
    public void PagedResult_FirstItemOnPage_WhenEmpty_ShouldReturnZero()
    {
        // Act
        var result = PagedResult<string>.Empty();

        // Assert
        result.FirstItemOnPage.Should().Be(0);
        result.LastItemOnPage.Should().Be(0);
    }

    [Fact]
    public void PagedResult_Map_ShouldTransformItems()
    {
        // Arrange
        var items = new List<int> { 1, 2, 3 };
        var result = PagedResult<int>.Create(items, 10, 0, 5);

        // Act
        var mapped = result.Map(x => x.ToString());

        // Assert
        mapped.Items.Should().BeEquivalentTo(["1", "2", "3"]);
        mapped.TotalCount.Should().Be(10);
        mapped.PageIndex.Should().Be(0);
        mapped.PageSize.Should().Be(5);
    }

    #endregion

    #region PagedResultExtensions Tests

    [Fact]
    public void ToPagedResult_ShouldPaginateInMemory()
    {
        // Arrange
        var source = Enumerable.Range(1, 100);

        // Act
        var result = source.ToPagedResult(2, 10);

        // Assert
        result.Items.Should().HaveCount(10);
        result.Items.First().Should().Be(21);
        result.Items.Last().Should().Be(30);
        result.TotalCount.Should().Be(100);
        result.PageIndex.Should().Be(2);
        result.TotalPages.Should().Be(10);
    }

    [Fact]
    public void ToPagedResult_EmptySource_ShouldReturnEmptyResult()
    {
        // Arrange
        var source = Enumerable.Empty<int>();

        // Act
        var result = source.ToPagedResult(0, 10);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion
}

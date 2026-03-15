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
        result.Items.ShouldBe(items);
        result.TotalCount.ShouldBe(100);
        result.PageIndex.ShouldBe(0);
        result.PageSize.ShouldBe(10);
        result.TotalPages.ShouldBe(10);
        result.HasPreviousPage.ShouldBe(false);
        result.HasNextPage.ShouldBe(true);
    }

    [Fact]
    public void PagedResult_LastPage_ShouldIndicateNoNextPage()
    {
        // Arrange
        var items = new List<string> { "item1" };

        // Act
        var result = PagedResult<string>.Create(items, 100, 9, 10);

        // Assert
        result.HasPreviousPage.ShouldBe(true);
        result.HasNextPage.ShouldBe(false);
    }

    [Fact]
    public void PagedResult_MiddlePage_ShouldIndicateBothPages()
    {
        // Arrange
        var items = new List<string> { "item1", "item2" };

        // Act
        var result = PagedResult<string>.Create(items, 100, 5, 10);

        // Assert
        result.HasPreviousPage.ShouldBe(true);
        result.HasNextPage.ShouldBe(true);
    }

    [Fact]
    public void PagedResult_Empty_ShouldCreateEmptyResult()
    {
        // Act
        var result = PagedResult<string>.Empty(0, 10);

        // Assert
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.TotalPages.ShouldBe(0);
        result.HasPreviousPage.ShouldBe(false);
        result.HasNextPage.ShouldBe(false);
    }

    [Fact]
    public void PagedResult_SinglePage_ShouldCalculateCorrectly()
    {
        // Arrange
        var items = new List<string> { "item1", "item2", "item3" };

        // Act
        var result = PagedResult<string>.Create(items, 3, 0, 10);

        // Assert
        result.TotalPages.ShouldBe(1);
        result.HasPreviousPage.ShouldBe(false);
        result.HasNextPage.ShouldBe(false);
    }

    [Fact]
    public void PagedResult_FirstItemOnPage_ShouldCalculateCorrectly()
    {
        // Arrange
        var items = new List<string> { "item1", "item2" };

        // Act
        var result = PagedResult<string>.Create(items, 50, 2, 10);

        // Assert
        result.FirstItemOnPage.ShouldBe(21); // Page 2 (0-indexed) starts at item 21
        result.LastItemOnPage.ShouldBe(30);
    }

    [Fact]
    public void PagedResult_FirstItemOnPage_WhenEmpty_ShouldReturnZero()
    {
        // Act
        var result = PagedResult<string>.Empty();

        // Assert
        result.FirstItemOnPage.ShouldBe(0);
        result.LastItemOnPage.ShouldBe(0);
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
        mapped.Items.ShouldBe(["1", "2", "3"]);
        mapped.TotalCount.ShouldBe(10);
        mapped.PageIndex.ShouldBe(0);
        mapped.PageSize.ShouldBe(5);
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
        result.Items.Count().ShouldBe(10);
        result.Items.First().ShouldBe(21);
        result.Items.Last().ShouldBe(30);
        result.TotalCount.ShouldBe(100);
        result.PageIndex.ShouldBe(2);
        result.TotalPages.ShouldBe(10);
    }

    [Fact]
    public void ToPagedResult_EmptySource_ShouldReturnEmptyResult()
    {
        // Arrange
        var source = Enumerable.Empty<int>();

        // Act
        var result = source.ToPagedResult(0, 10);

        // Assert
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    #endregion
}

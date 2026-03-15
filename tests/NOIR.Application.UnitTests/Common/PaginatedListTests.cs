namespace NOIR.Application.UnitTests.Common;

/// <summary>
/// Unit tests for PaginatedList class.
/// </summary>
public class PaginatedListTests
{
    [Fact]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var items = new List<string> { "item1", "item2", "item3" };
        var totalCount = 100;
        var pageNumber = 2;
        var pageSize = 10;

        // Act
        var list = PaginatedList<string>.Create(items, totalCount, pageNumber, pageSize);

        // Assert
        list.Items.ShouldBe(items);
        list.TotalCount.ShouldBe(100);
        list.PageNumber.ShouldBe(2);
        list.TotalPages.ShouldBe(10);
    }

    [Fact]
    public void HasPreviousPage_OnFirstPage_ShouldBeFalse()
    {
        // Arrange
        var items = new List<string> { "item1" };

        // Act
        var list = PaginatedList<string>.Create(items, 100, 1, 10);

        // Assert
        list.HasPreviousPage.ShouldBe(false);
    }

    [Fact]
    public void HasPreviousPage_OnSecondPage_ShouldBeTrue()
    {
        // Arrange
        var items = new List<string> { "item1" };

        // Act
        var list = PaginatedList<string>.Create(items, 100, 2, 10);

        // Assert
        list.HasPreviousPage.ShouldBe(true);
    }

    [Fact]
    public void HasNextPage_OnLastPage_ShouldBeFalse()
    {
        // Arrange
        var items = new List<string> { "item1" };

        // Act
        var list = PaginatedList<string>.Create(items, 100, 10, 10);

        // Assert
        list.HasNextPage.ShouldBe(false);
    }

    [Fact]
    public void HasNextPage_OnFirstPage_ShouldBeTrue()
    {
        // Arrange
        var items = new List<string> { "item1" };

        // Act
        var list = PaginatedList<string>.Create(items, 100, 1, 10);

        // Assert
        list.HasNextPage.ShouldBe(true);
    }

    [Fact]
    public void TotalPages_ShouldCalculateCorrectly()
    {
        // Arrange
        var items = new List<string>();

        // Act - 95 items with page size 10 = 10 pages
        var list = PaginatedList<string>.Create(items, 95, 1, 10);

        // Assert
        list.TotalPages.ShouldBe(10);
    }

    [Fact]
    public void TotalPages_WithExactMultiple_ShouldCalculateCorrectly()
    {
        // Arrange
        var items = new List<string>();

        // Act - 100 items with page size 10 = 10 pages
        var list = PaginatedList<string>.Create(items, 100, 1, 10);

        // Assert
        list.TotalPages.ShouldBe(10);
    }

    [Fact]
    public void TotalPages_WithZeroItems_ShouldBeZero()
    {
        // Arrange
        var items = new List<string>();

        // Act
        var list = PaginatedList<string>.Create(items, 0, 1, 10);

        // Assert
        list.TotalPages.ShouldBe(0);
    }

    [Fact]
    public void MiddlePage_ShouldHaveBothPrevAndNext()
    {
        // Arrange
        var items = new List<string> { "item1" };

        // Act
        var list = PaginatedList<string>.Create(items, 100, 5, 10);

        // Assert
        list.HasPreviousPage.ShouldBe(true);
        list.HasNextPage.ShouldBe(true);
    }

    [Fact]
    public void SinglePage_ShouldHaveNoPrevOrNext()
    {
        // Arrange
        var items = new List<string> { "item1", "item2", "item3" };

        // Act
        var list = PaginatedList<string>.Create(items, 3, 1, 10);

        // Assert
        list.HasPreviousPage.ShouldBe(false);
        list.HasNextPage.ShouldBe(false);
        list.TotalPages.ShouldBe(1);
    }

    [Fact]
    public void Items_ShouldBeReadOnly()
    {
        // Arrange
        var items = new List<string> { "item1", "item2" };

        // Act
        var list = PaginatedList<string>.Create(items, 2, 1, 10);

        // Assert
        list.Items.ShouldBeAssignableTo<IReadOnlyList<string>>();
    }
}

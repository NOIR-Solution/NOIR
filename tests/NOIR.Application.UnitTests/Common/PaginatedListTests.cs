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
        list.Items.Should().BeEquivalentTo(items);
        list.TotalCount.Should().Be(100);
        list.PageNumber.Should().Be(2);
        list.TotalPages.Should().Be(10);
    }

    [Fact]
    public void HasPreviousPage_OnFirstPage_ShouldBeFalse()
    {
        // Arrange
        var items = new List<string> { "item1" };

        // Act
        var list = PaginatedList<string>.Create(items, 100, 1, 10);

        // Assert
        list.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_OnSecondPage_ShouldBeTrue()
    {
        // Arrange
        var items = new List<string> { "item1" };

        // Act
        var list = PaginatedList<string>.Create(items, 100, 2, 10);

        // Assert
        list.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_OnLastPage_ShouldBeFalse()
    {
        // Arrange
        var items = new List<string> { "item1" };

        // Act
        var list = PaginatedList<string>.Create(items, 100, 10, 10);

        // Assert
        list.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasNextPage_OnFirstPage_ShouldBeTrue()
    {
        // Arrange
        var items = new List<string> { "item1" };

        // Act
        var list = PaginatedList<string>.Create(items, 100, 1, 10);

        // Assert
        list.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void TotalPages_ShouldCalculateCorrectly()
    {
        // Arrange
        var items = new List<string>();

        // Act - 95 items with page size 10 = 10 pages
        var list = PaginatedList<string>.Create(items, 95, 1, 10);

        // Assert
        list.TotalPages.Should().Be(10);
    }

    [Fact]
    public void TotalPages_WithExactMultiple_ShouldCalculateCorrectly()
    {
        // Arrange
        var items = new List<string>();

        // Act - 100 items with page size 10 = 10 pages
        var list = PaginatedList<string>.Create(items, 100, 1, 10);

        // Assert
        list.TotalPages.Should().Be(10);
    }

    [Fact]
    public void TotalPages_WithZeroItems_ShouldBeZero()
    {
        // Arrange
        var items = new List<string>();

        // Act
        var list = PaginatedList<string>.Create(items, 0, 1, 10);

        // Assert
        list.TotalPages.Should().Be(0);
    }

    [Fact]
    public void MiddlePage_ShouldHaveBothPrevAndNext()
    {
        // Arrange
        var items = new List<string> { "item1" };

        // Act
        var list = PaginatedList<string>.Create(items, 100, 5, 10);

        // Assert
        list.HasPreviousPage.Should().BeTrue();
        list.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void SinglePage_ShouldHaveNoPrevOrNext()
    {
        // Arrange
        var items = new List<string> { "item1", "item2", "item3" };

        // Act
        var list = PaginatedList<string>.Create(items, 3, 1, 10);

        // Assert
        list.HasPreviousPage.Should().BeFalse();
        list.HasNextPage.Should().BeFalse();
        list.TotalPages.Should().Be(1);
    }

    [Fact]
    public void Items_ShouldBeReadOnly()
    {
        // Arrange
        var items = new List<string> { "item1", "item2" };

        // Act
        var list = PaginatedList<string>.Create(items, 2, 1, 10);

        // Assert
        list.Items.Should().BeAssignableTo<IReadOnlyList<string>>();
    }
}

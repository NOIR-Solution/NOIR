namespace NOIR.IntegrationTests.Common;

/// <summary>
/// Integration tests for PaginatedList.CreateAsync method.
/// Tests the static factory method that requires IQueryable with EF Core.
/// </summary>
[Collection("Integration")]
public class PaginatedListIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public PaginatedListIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Clean up test data
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var testTokens = context.RefreshTokens.Where(t => t.UserId.StartsWith("paginated-test-"));
            context.RefreshTokens.RemoveRange(testTokens);
            await context.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCorrectPageOfItems()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange - Add 15 tokens
            for (int i = 1; i <= 15; i++)
            {
                context.RefreshTokens.Add(RefreshToken.Create($"paginated-test-user-{i:D2}", 7));
            }
            await context.SaveChangesAsync();

            // Act - Get second page (page 2, size 5)
            var query = context.RefreshTokens
                .Where(t => t.UserId.StartsWith("paginated-test-user-"))
                .OrderBy(t => t.UserId);

            var result = await PaginatedList<RefreshToken>.CreateAsync(query, 2, 5);

            // Assert
            result.Items.Should().HaveCount(5);
            result.PageNumber.Should().Be(2);
            result.TotalCount.Should().Be(15);
            result.TotalPages.Should().Be(3);
            result.HasPreviousPage.Should().BeTrue();
            result.HasNextPage.Should().BeTrue();
        });
    }

    [Fact]
    public async Task CreateAsync_FirstPage_ShouldHaveNoPreviousPage()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            for (int i = 1; i <= 10; i++)
            {
                context.RefreshTokens.Add(RefreshToken.Create($"paginated-test-first-{i}", 7));
            }
            await context.SaveChangesAsync();

            // Act
            var query = context.RefreshTokens
                .Where(t => t.UserId.StartsWith("paginated-test-first-"))
                .OrderBy(t => t.UserId);

            var result = await PaginatedList<RefreshToken>.CreateAsync(query, 1, 5);

            // Assert
            result.HasPreviousPage.Should().BeFalse();
            result.HasNextPage.Should().BeTrue();
            result.PageNumber.Should().Be(1);
        });
    }

    [Fact]
    public async Task CreateAsync_LastPage_ShouldHaveNoNextPage()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            for (int i = 1; i <= 10; i++)
            {
                context.RefreshTokens.Add(RefreshToken.Create($"paginated-test-last-{i}", 7));
            }
            await context.SaveChangesAsync();

            // Act - Last page
            var query = context.RefreshTokens
                .Where(t => t.UserId.StartsWith("paginated-test-last-"))
                .OrderBy(t => t.UserId);

            var result = await PaginatedList<RefreshToken>.CreateAsync(query, 2, 5);

            // Assert
            result.HasPreviousPage.Should().BeTrue();
            result.HasNextPage.Should().BeFalse();
            result.PageNumber.Should().Be(2);
        });
    }

    [Fact]
    public async Task CreateAsync_EmptyQuery_ShouldReturnEmptyList()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Act - Query that matches nothing
            var query = context.RefreshTokens
                .Where(t => t.UserId == "non-existent-user-xyz-123")
                .OrderBy(t => t.UserId);

            var result = await PaginatedList<RefreshToken>.CreateAsync(query, 1, 10);

            // Assert
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.TotalPages.Should().Be(0);
            result.HasPreviousPage.Should().BeFalse();
            result.HasNextPage.Should().BeFalse();
        });
    }

    [Fact]
    public async Task CreateAsync_SinglePage_ShouldHaveNoPrevOrNext()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange - Add only 3 items
            for (int i = 1; i <= 3; i++)
            {
                context.RefreshTokens.Add(RefreshToken.Create($"paginated-test-single-{i}", 7));
            }
            await context.SaveChangesAsync();

            // Act - Page size larger than total count
            var query = context.RefreshTokens
                .Where(t => t.UserId.StartsWith("paginated-test-single-"))
                .OrderBy(t => t.UserId);

            var result = await PaginatedList<RefreshToken>.CreateAsync(query, 1, 10);

            // Assert
            result.Items.Should().HaveCount(3);
            result.TotalCount.Should().Be(3);
            result.TotalPages.Should().Be(1);
            result.HasPreviousPage.Should().BeFalse();
            result.HasNextPage.Should().BeFalse();
        });
    }

    [Fact]
    public async Task CreateAsync_WithCancellationToken_ShouldWork()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Arrange
            context.RefreshTokens.Add(RefreshToken.Create("paginated-test-cancel-1", 7));
            await context.SaveChangesAsync();

            // Act
            var cts = new CancellationTokenSource();
            var query = context.RefreshTokens
                .Where(t => t.UserId.StartsWith("paginated-test-cancel-"))
                .OrderBy(t => t.UserId);

            var result = await PaginatedList<RefreshToken>.CreateAsync(query, 1, 10, cts.Token);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().NotBeEmpty();
        });
    }
}

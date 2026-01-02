namespace NOIR.IntegrationTests.Persistence;

/// <summary>
/// Integration tests for DomainEventInterceptor.
/// Tests domain event dispatching with real DbContext and entities.
/// </summary>
[Collection("LocalDb")]
public class DomainEventInterceptorIntegrationTests : IAsyncLifetime
{
    private readonly LocalDbWebApplicationFactory _factory;

    public DomainEventInterceptorIntegrationTests(LocalDbWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    #region SavedChangesAsync Tests

    [Fact]
    public async Task SavedChangesAsync_WithContext_ShouldNotThrow()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            // Add an entity
            var token = RefreshToken.Create("domain-event-test", 7);
            context.RefreshTokens.Add(token);

            // Act - SaveChangesAsync should work with the interceptor
            await context.SaveChangesAsync();

            // Assert
            var saved = await context.RefreshTokens.FindAsync(token.Id);
            saved.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task SavedChangesAsync_WithMultipleEntities_ShouldProcessAll()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            // Add multiple entities
            var tokens = new[]
            {
                RefreshToken.Create("event-user-1", 7),
                RefreshToken.Create("event-user-2", 7),
                RefreshToken.Create("event-user-3", 7)
            };

            context.RefreshTokens.AddRange(tokens);

            // Act
            var result = await context.SaveChangesAsync();

            // Assert
            result.Should().BeGreaterThan(0);

            foreach (var token in tokens)
            {
                var saved = await context.RefreshTokens.FindAsync(token.Id);
                saved.Should().NotBeNull();
            }
        });
    }

    #endregion

    #region Sync SaveChanges Blocked Tests

    [Fact]
    public async Task SaveChanges_Sync_ShouldThrowInvalidOperationException()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var token = RefreshToken.Create("sync-test", 7);
            context.RefreshTokens.Add(token);

            // Act
            var act = () => context.SaveChanges();

            // Assert - The DomainEventInterceptor throws on sync SaveChanges
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*SaveChangesAsync*");

            await Task.CompletedTask;
        });
    }

    #endregion

    #region Entity State Tests

    [Fact]
    public async Task SavedChangesAsync_AddedEntity_ShouldBeTracked()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var token = RefreshToken.Create("state-test", 7);
            context.RefreshTokens.Add(token);

            // Before save - Added state
            var entryBefore = context.Entry(token);
            entryBefore.State.Should().Be(EntityState.Added);

            // Act
            await context.SaveChangesAsync();

            // After save - Unchanged state
            var entryAfter = context.Entry(token);
            entryAfter.State.Should().Be(EntityState.Unchanged);
        });
    }

    [Fact]
    public async Task SavedChangesAsync_ModifiedEntity_ShouldBeTracked()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var token = RefreshToken.Create("modify-state-test", 7);
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();

            // Modify
            token.Revoke("127.0.0.1", "Test");

            // Before save - Modified state
            var entryBefore = context.Entry(token);
            entryBefore.State.Should().Be(EntityState.Modified);

            // Act
            await context.SaveChangesAsync();

            // After save - Unchanged state
            var entryAfter = context.Entry(token);
            entryAfter.State.Should().Be(EntityState.Unchanged);
        });
    }

    #endregion

    #region Return Value Tests

    [Fact]
    public async Task SavedChangesAsync_ShouldReturnNumberOfAffectedRows()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var token1 = RefreshToken.Create("return-test-1", 7);
            var token2 = RefreshToken.Create("return-test-2", 7);
            context.RefreshTokens.AddRange(token1, token2);

            // Act
            var result = await context.SaveChangesAsync();

            // Assert - Should return count of affected entities
            result.Should().BeGreaterThanOrEqualTo(2);
        });
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task SavedChangesAsync_WithCancellationToken_ShouldWork()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var token = RefreshToken.Create("cancel-test", 7);
            context.RefreshTokens.Add(token);

            using var cts = new CancellationTokenSource();

            // Act
            var result = await context.SaveChangesAsync(cts.Token);

            // Assert
            result.Should().BeGreaterThan(0);
        });
    }

    [Fact]
    public async Task SavedChangesAsync_WithCancelledToken_ShouldThrow()
    {
        await _factory.ExecuteWithTenantAsync(async sp =>
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            var token = RefreshToken.Create("cancelled-test", 7);
            context.RefreshTokens.Add(token);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            var act = async () => await context.SaveChangesAsync(cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        });
    }

    #endregion
}

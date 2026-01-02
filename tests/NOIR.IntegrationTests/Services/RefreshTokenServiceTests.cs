namespace NOIR.IntegrationTests.Services;

/// <summary>
/// Integration tests for RefreshTokenService.
/// Tests token creation, rotation, validation, and revocation.
/// All operations use ExecuteWithTenantAsync for proper multi-tenant support.
/// </summary>
[Collection("Integration")]
public class RefreshTokenServiceTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public RefreshTokenServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            context.RefreshTokens.RemoveRange(context.RefreshTokens);
            await context.SaveChangesAsync();
        });
    }

    private static IRefreshTokenService CreateService(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var jwtSettings = services.GetRequiredService<IOptions<JwtSettings>>();
        var logger = services.GetRequiredService<ILogger<RefreshTokenService>>();
        return new RefreshTokenService(context, jwtSettings, logger);
    }

    #region CreateTokenAsync Tests

    [Fact]
    public async Task CreateTokenAsync_ShouldCreateNewToken()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var service = CreateService(services);
            var userId = $"create-test-{Guid.NewGuid()}";

            // Act
            var token = await service.CreateTokenAsync(userId);

            // Assert
            token.Should().NotBeNull();
            token.UserId.Should().Be(userId);
            token.Token.Should().NotBeNullOrEmpty();
            token.IsActive.Should().BeTrue();
            token.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
        });
    }

    [Fact]
    public async Task CreateTokenAsync_WithTenantId_ShouldSetTenantId()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var service = CreateService(services);
            var userId = $"tenant-test-{Guid.NewGuid()}";
            var tenantId = "test-tenant";

            // Act
            var token = await service.CreateTokenAsync(userId, tenantId);

            // Assert
            token.TenantId.Should().Be(tenantId);
        });
    }

    [Fact]
    public async Task CreateTokenAsync_WithDeviceInfo_ShouldStoreDeviceInfo()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var service = CreateService(services);
            var userId = $"device-test-{Guid.NewGuid()}";

            // Act
            var token = await service.CreateTokenAsync(
                userId,
                ipAddress: "192.168.1.1",
                deviceFingerprint: "test-fingerprint",
                userAgent: "Test Browser",
                deviceName: "Test Device");

            // Assert
            token.CreatedByIp.Should().Be("192.168.1.1");
            token.DeviceFingerprint.Should().Be("test-fingerprint");
            token.UserAgent.Should().Be("Test Browser");
            token.DeviceName.Should().Be("Test Device");
        });
    }

    #endregion

    #region ValidateTokenAsync Tests

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ShouldReturnToken()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var service = CreateService(services);
            var userId = $"validate-test-{Guid.NewGuid()}";
            var createdToken = await service.CreateTokenAsync(userId);

            // Act
            var validatedToken = await service.ValidateTokenAsync(createdToken.Token);

            // Assert
            validatedToken.Should().NotBeNull();
            validatedToken!.Id.Should().Be(createdToken.Id);
        });
    }

    [Fact]
    public async Task ValidateTokenAsync_WithNonExistentToken_ShouldReturnNull()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var service = CreateService(services);

            // Act
            var result = await service.ValidateTokenAsync("non-existent-token");

            // Assert
            result.Should().BeNull();
        });
    }

    [Fact]
    public async Task ValidateTokenAsync_WithRevokedToken_ShouldReturnNull()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var service = CreateService(services);
            var userId = $"revoked-validate-{Guid.NewGuid()}";
            var token = await service.CreateTokenAsync(userId);
            await service.RevokeTokenAsync(token.Token);

            // Act
            var result = await service.ValidateTokenAsync(token.Token);

            // Assert
            result.Should().BeNull();
        });
    }

    #endregion

    #region RotateTokenAsync Tests

    [Fact]
    public async Task RotateTokenAsync_WithValidToken_ShouldCreateNewToken()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var service = CreateService(services);
            var userId = $"rotate-test-{Guid.NewGuid()}";
            var originalToken = await service.CreateTokenAsync(userId);

            // Act
            var newToken = await service.RotateTokenAsync(originalToken.Token);

            // Assert
            newToken.Should().NotBeNull();
            newToken!.Token.Should().NotBe(originalToken.Token);
            newToken.UserId.Should().Be(userId);
            newToken.TokenFamily.Should().Be(originalToken.TokenFamily);
        });
    }

    [Fact]
    public async Task RotateTokenAsync_ShouldRevokeOldToken()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var service = CreateService(services);
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userId = $"rotate-revoke-{Guid.NewGuid()}";
            var originalToken = await service.CreateTokenAsync(userId);
            var originalTokenValue = originalToken.Token;

            // Act
            await service.RotateTokenAsync(originalTokenValue);

            // Assert
            var oldToken = await context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == originalTokenValue);
            oldToken.Should().NotBeNull();
            oldToken!.IsRevoked.Should().BeTrue();
            oldToken.ReplacedByToken.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task RotateTokenAsync_WithNonExistentToken_ShouldReturnNull()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var service = CreateService(services);

            // Act
            var result = await service.RotateTokenAsync("non-existent-token");

            // Assert
            result.Should().BeNull();
        });
    }

    [Fact]
    public async Task RotateTokenAsync_WithRevokedToken_ShouldReturnNull()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var service = CreateService(services);
            var userId = $"rotate-revoked-{Guid.NewGuid()}";
            var token = await service.CreateTokenAsync(userId);
            await service.RevokeTokenAsync(token.Token);

            // Act
            var result = await service.RotateTokenAsync(token.Token);

            // Assert
            result.Should().BeNull();
        });
    }

    #endregion

    #region RevokeTokenAsync Tests

    [Fact]
    public async Task RevokeTokenAsync_ShouldRevokeToken()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var service = CreateService(services);
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userId = $"revoke-test-{Guid.NewGuid()}";
            var token = await service.CreateTokenAsync(userId);

            // Act
            await service.RevokeTokenAsync(token.Token, "127.0.0.1", "User logout");

            // Assert
            var revokedToken = await context.RefreshTokens.FindAsync(token.Id);
            revokedToken.Should().NotBeNull();
            revokedToken!.IsRevoked.Should().BeTrue();
            revokedToken.RevokedByIp.Should().Be("127.0.0.1");
            revokedToken.ReasonRevoked.Should().Be("User logout");
        });
    }

    [Fact]
    public async Task RevokeTokenAsync_WithNonExistentToken_ShouldNotThrow()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var service = CreateService(services);

            // Act
            var act = () => service.RevokeTokenAsync("non-existent-token");

            // Assert
            await act.Should().NotThrowAsync();
        });
    }

    #endregion

    #region RevokeAllUserTokensAsync Tests

    [Fact]
    public async Task RevokeAllUserTokensAsync_ShouldRevokeAllUserTokens()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var service = CreateService(services);
            var userId = $"revoke-all-{Guid.NewGuid()}";
            await service.CreateTokenAsync(userId);
            await service.CreateTokenAsync(userId);
            await service.CreateTokenAsync(userId);

            // Act
            await service.RevokeAllUserTokensAsync(userId, "127.0.0.1", "Logout all");

            // Assert
            var activeCount = await service.GetActiveSessionCountAsync(userId);
            activeCount.Should().Be(0);
        });
    }

    #endregion

    #region RevokeTokenFamilyAsync Tests

    [Fact]
    public async Task RevokeTokenFamilyAsync_ShouldRevokeEntireFamily()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var service = CreateService(services);
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userId = $"family-revoke-{Guid.NewGuid()}";
            var token1 = await service.CreateTokenAsync(userId);
            var token2 = await service.RotateTokenAsync(token1.Token);
            var token3 = await service.RotateTokenAsync(token2!.Token);

            // Act
            await service.RevokeTokenFamilyAsync(token1.TokenFamily, "127.0.0.1", "Security incident");

            // Assert
            var familyTokens = await context.RefreshTokens
                .Where(t => t.TokenFamily == token1.TokenFamily)
                .ToListAsync();

            familyTokens.Should().HaveCountGreaterThanOrEqualTo(3);
            familyTokens.Should().OnlyContain(t => t.IsRevoked);
        });
    }

    #endregion

    #region GetActiveSessionsAsync Tests

    [Fact]
    public async Task GetActiveSessionsAsync_ShouldReturnOnlyActiveSessions()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var service = CreateService(services);
            var userId = $"sessions-{Guid.NewGuid()}";
            await service.CreateTokenAsync(userId);
            await service.CreateTokenAsync(userId);
            var tokenToRevoke = await service.CreateTokenAsync(userId);
            await service.RevokeTokenAsync(tokenToRevoke.Token);

            // Act
            var sessions = await service.GetActiveSessionsAsync(userId);

            // Assert
            sessions.Should().HaveCount(2);
            sessions.Should().OnlyContain(t => !t.RevokedAt.HasValue);
        });
    }

    #endregion

    #region GetActiveSessionCountAsync Tests

    [Fact]
    public async Task GetActiveSessionCountAsync_ShouldReturnCorrectCount()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var service = CreateService(services);
            var userId = $"count-{Guid.NewGuid()}";
            await service.CreateTokenAsync(userId);
            await service.CreateTokenAsync(userId);

            // Act
            var count = await service.GetActiveSessionCountAsync(userId);

            // Assert
            count.Should().Be(2);
        });
    }

    #endregion

    #region Token Reuse Detection Tests

    [Fact]
    public async Task RotateTokenAsync_WithAlreadyUsedToken_ShouldRevokeFamily()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var service = CreateService(services);
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userId = $"reuse-{Guid.NewGuid()}";
            var token1 = await service.CreateTokenAsync(userId);
            var token1Value = token1.Token;
            var token2 = await service.RotateTokenAsync(token1Value);

            // Act - Try to reuse the already-rotated token
            var result = await service.RotateTokenAsync(token1Value);

            // Assert
            result.Should().BeNull();

            // The entire family should be revoked
            var familyTokens = await context.RefreshTokens
                .Where(t => t.TokenFamily == token1.TokenFamily)
                .ToListAsync();
            familyTokens.Should().OnlyContain(t => t.IsRevoked);
        });
    }

    #endregion

    #region MaxConcurrentSessions Tests

    [Fact]
    public async Task CreateTokenAsync_WhenMaxSessionsReached_ShouldRevokeOldestSession()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var logger = services.GetRequiredService<ILogger<RefreshTokenService>>();

            // Create service with MaxConcurrentSessions = 2
            var jwtSettings = new JwtSettings
            {
                Secret = "test-secret-key-that-is-long-enough-for-jwt",
                Issuer = "test",
                Audience = "test",
                MaxConcurrentSessions = 2,
                RefreshTokenExpirationInDays = 7
            };
            var service = new RefreshTokenService(
                context,
                Options.Create(jwtSettings),
                logger);

            var userId = $"max-sessions-{Guid.NewGuid()}";

            // Create first two tokens
            var token1 = await service.CreateTokenAsync(userId);
            var token2 = await service.CreateTokenAsync(userId);

            // Act - Create third token (should revoke oldest)
            var token3 = await service.CreateTokenAsync(userId);

            // Assert
            token3.Should().NotBeNull();

            // Reload token1 from database
            var reloadedToken1 = await context.RefreshTokens.FindAsync(token1.Id);
            reloadedToken1!.IsRevoked.Should().BeTrue();
            reloadedToken1.ReasonRevoked.Should().Contain("Session limit reached");
        });
    }

    [Fact]
    public async Task CreateTokenAsync_WhenMaxSessionsZero_ShouldAllowUnlimited()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var logger = services.GetRequiredService<ILogger<RefreshTokenService>>();

            // Create service with MaxConcurrentSessions = 0 (unlimited)
            var jwtSettings = new JwtSettings
            {
                Secret = "test-secret-key-that-is-long-enough-for-jwt",
                Issuer = "test",
                Audience = "test",
                MaxConcurrentSessions = 0,
                RefreshTokenExpirationInDays = 7
            };
            var service = new RefreshTokenService(
                context,
                Options.Create(jwtSettings),
                logger);

            var userId = $"unlimited-sessions-{Guid.NewGuid()}";

            // Create many tokens
            var tokens = new List<RefreshToken>();
            for (var i = 0; i < 10; i++)
            {
                tokens.Add(await service.CreateTokenAsync(userId));
            }

            // Act
            var activeCount = await service.GetActiveSessionCountAsync(userId);

            // Assert - All 10 should be active
            activeCount.Should().Be(10);
        });
    }

    #endregion

    #region Device Fingerprint Validation Tests

    [Fact]
    public async Task ValidateTokenAsync_WithFingerprintMismatch_ShouldReturnNull()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var logger = services.GetRequiredService<ILogger<RefreshTokenService>>();

            // Create service with device fingerprinting enabled
            var jwtSettings = new JwtSettings
            {
                Secret = "test-secret-key-that-is-long-enough-for-jwt",
                Issuer = "test",
                Audience = "test",
                EnableDeviceFingerprinting = true,
                RefreshTokenExpirationInDays = 7
            };
            var service = new RefreshTokenService(
                context,
                Options.Create(jwtSettings),
                logger);

            var userId = $"fingerprint-validate-{Guid.NewGuid()}";

            // Create token with specific fingerprint
            var token = await service.CreateTokenAsync(
                userId,
                deviceFingerprint: "original-fingerprint");

            // Act - Validate with different fingerprint
            var result = await service.ValidateTokenAsync(
                token.Token,
                deviceFingerprint: "different-fingerprint");

            // Assert
            result.Should().BeNull();
        });
    }

    [Fact]
    public async Task ValidateTokenAsync_WithMatchingFingerprint_ShouldReturnToken()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var logger = services.GetRequiredService<ILogger<RefreshTokenService>>();

            var jwtSettings = new JwtSettings
            {
                Secret = "test-secret-key-that-is-long-enough-for-jwt",
                Issuer = "test",
                Audience = "test",
                EnableDeviceFingerprinting = true,
                RefreshTokenExpirationInDays = 7
            };
            var service = new RefreshTokenService(
                context,
                Options.Create(jwtSettings),
                logger);

            var userId = $"fingerprint-match-{Guid.NewGuid()}";
            var fingerprint = "test-fingerprint";

            // Create token with fingerprint
            var token = await service.CreateTokenAsync(
                userId,
                deviceFingerprint: fingerprint);

            // Act - Validate with same fingerprint
            var result = await service.ValidateTokenAsync(token.Token, fingerprint);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(token.Id);
        });
    }

    [Fact]
    public async Task RotateTokenAsync_WithFingerprintMismatch_ShouldRevokeAndReturnNull()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var logger = services.GetRequiredService<ILogger<RefreshTokenService>>();

            var jwtSettings = new JwtSettings
            {
                Secret = "test-secret-key-that-is-long-enough-for-jwt",
                Issuer = "test",
                Audience = "test",
                EnableDeviceFingerprinting = true,
                RefreshTokenExpirationInDays = 7
            };
            var service = new RefreshTokenService(
                context,
                Options.Create(jwtSettings),
                logger);

            var userId = $"fingerprint-rotate-{Guid.NewGuid()}";

            // Create token with fingerprint
            var token = await service.CreateTokenAsync(
                userId,
                deviceFingerprint: "original-fingerprint");

            // Act - Rotate with different fingerprint
            var result = await service.RotateTokenAsync(
                token.Token,
                deviceFingerprint: "different-fingerprint");

            // Assert
            result.Should().BeNull();

            // Token should be revoked with specific reason
            var revokedToken = await context.RefreshTokens.FindAsync(token.Id);
            revokedToken!.IsRevoked.Should().BeTrue();
            revokedToken.ReasonRevoked.Should().Contain("fingerprint mismatch");
        });
    }

    [Fact]
    public async Task ValidateTokenAsync_WithNullStoredFingerprint_ShouldSucceed()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var logger = services.GetRequiredService<ILogger<RefreshTokenService>>();

            var jwtSettings = new JwtSettings
            {
                Secret = "test-secret-key-that-is-long-enough-for-jwt",
                Issuer = "test",
                Audience = "test",
                EnableDeviceFingerprinting = true,
                RefreshTokenExpirationInDays = 7
            };
            var service = new RefreshTokenService(
                context,
                Options.Create(jwtSettings),
                logger);

            var userId = $"no-fingerprint-{Guid.NewGuid()}";

            // Create token WITHOUT fingerprint
            var token = await service.CreateTokenAsync(userId);

            // Act - Validate with any fingerprint (should succeed since stored is null)
            var result = await service.ValidateTokenAsync(token.Token, "any-fingerprint");

            // Assert
            result.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task ValidateTokenAsync_WithFingerprintingDisabled_ShouldIgnoreMismatch()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var logger = services.GetRequiredService<ILogger<RefreshTokenService>>();

            // Create service with fingerprinting DISABLED
            var jwtSettings = new JwtSettings
            {
                Secret = "test-secret-key-that-is-long-enough-for-jwt",
                Issuer = "test",
                Audience = "test",
                EnableDeviceFingerprinting = false,
                RefreshTokenExpirationInDays = 7
            };
            var service = new RefreshTokenService(
                context,
                Options.Create(jwtSettings),
                logger);

            var userId = $"disabled-fingerprint-{Guid.NewGuid()}";

            // Create token with fingerprint
            var token = await service.CreateTokenAsync(
                userId,
                deviceFingerprint: "original-fingerprint");

            // Act - Validate with different fingerprint (should succeed since disabled)
            var result = await service.ValidateTokenAsync(
                token.Token,
                deviceFingerprint: "different-fingerprint");

            // Assert - Should succeed because fingerprinting is disabled
            result.Should().NotBeNull();
        });
    }

    #endregion

    #region CleanupExpiredTokensAsync Tests

    [Fact]
    public async Task CleanupExpiredTokensAsync_ShouldDeleteExpiredTokens()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var service = CreateService(services);
            var userId = $"cleanup-{Guid.NewGuid()}";

            // Create a token
            var token = await service.CreateTokenAsync(userId);

            // Manually expire and age the token
            token.GetType()
                .GetProperty(nameof(RefreshToken.ExpiresAt))!
                .SetValue(token, DateTimeOffset.UtcNow.AddDays(-60));
            token.GetType()
                .GetProperty(nameof(RefreshToken.CreatedAt))!
                .SetValue(token, DateTimeOffset.UtcNow.AddDays(-60));
            await context.SaveChangesAsync();

            var tokenId = token.Id;

            // Act
            await service.CleanupExpiredTokensAsync(daysToKeep: 30);

            // Assert - Token should be deleted
            var deletedToken = await context.RefreshTokens
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tokenId);
            deletedToken.Should().BeNull();
        });
    }

    [Fact]
    public async Task CleanupExpiredTokensAsync_ShouldNotDeleteRecentTokens()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var service = CreateService(services);
            var userId = $"cleanup-recent-{Guid.NewGuid()}";

            // Create active token
            var token = await service.CreateTokenAsync(userId);

            // Act
            await service.CleanupExpiredTokensAsync(daysToKeep: 30);

            // Assert - Token should still exist
            var existingToken = await service.ValidateTokenAsync(token.Token);
            existingToken.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task CleanupExpiredTokensAsync_ShouldDeleteRevokedOldTokens()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var service = CreateService(services);
            var userId = $"cleanup-revoked-{Guid.NewGuid()}";

            // Create and revoke a token
            var token = await service.CreateTokenAsync(userId);
            await service.RevokeTokenAsync(token.Token);

            // Age the token
            token.GetType()
                .GetProperty(nameof(RefreshToken.CreatedAt))!
                .SetValue(token, DateTimeOffset.UtcNow.AddDays(-60));
            await context.SaveChangesAsync();

            var tokenId = token.Id;

            // Act
            await service.CleanupExpiredTokensAsync(daysToKeep: 30);

            // Assert - Token should be deleted
            var deletedToken = await context.RefreshTokens
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == tokenId);
            deletedToken.Should().BeNull();
        });
    }

    #endregion

    #region RevokeTokenAsync Edge Cases

    [Fact]
    public async Task RevokeTokenAsync_WithAlreadyRevokedToken_ShouldNotThrow()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var service = CreateService(services);
            var userId = $"double-revoke-{Guid.NewGuid()}";
            var token = await service.CreateTokenAsync(userId);

            // Revoke once
            await service.RevokeTokenAsync(token.Token);

            // Act - Revoke again
            var act = () => service.RevokeTokenAsync(token.Token);

            // Assert - Should not throw
            await act.Should().NotThrowAsync();
        });
    }

    [Fact]
    public async Task RevokeTokenAsync_WithDefaultReason_ShouldUseManuallyRevoked()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var service = CreateService(services);
            var userId = $"default-reason-{Guid.NewGuid()}";
            var token = await service.CreateTokenAsync(userId);

            // Act - Revoke without reason
            await service.RevokeTokenAsync(token.Token);

            // Assert
            var revokedToken = await context.RefreshTokens.FindAsync(token.Id);
            revokedToken!.ReasonRevoked.Should().Be("Manually revoked");
        });
    }

    #endregion

    #region RevokeAllUserTokensAsync Edge Cases

    [Fact]
    public async Task RevokeAllUserTokensAsync_WithNoTokens_ShouldNotThrow()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var service = CreateService(services);
            var userId = $"no-tokens-{Guid.NewGuid()}";

            // Act - Revoke all for user with no tokens
            var act = () => service.RevokeAllUserTokensAsync(userId);

            // Assert
            await act.Should().NotThrowAsync();
        });
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_WithDefaultReason_ShouldUseAllSessionsRevoked()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var service = CreateService(services);
            var userId = $"all-revoke-reason-{Guid.NewGuid()}";
            var token = await service.CreateTokenAsync(userId);

            // Act - Revoke all without reason
            await service.RevokeAllUserTokensAsync(userId);

            // Assert
            var revokedToken = await context.RefreshTokens.FindAsync(token.Id);
            revokedToken!.ReasonRevoked.Should().Be("All sessions revoked");
        });
    }

    #endregion

    #region RevokeTokenFamilyAsync Edge Cases

    [Fact]
    public async Task RevokeTokenFamilyAsync_WithNoActiveTokens_ShouldNotThrow()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var service = CreateService(services);

            // Act - Revoke non-existent family
            var act = () => service.RevokeTokenFamilyAsync(Guid.NewGuid());

            // Assert
            await act.Should().NotThrowAsync();
        });
    }

    [Fact]
    public async Task RevokeTokenFamilyAsync_WithDefaultReason_ShouldUseTokenFamilyRevoked()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var service = CreateService(services);
            var userId = $"family-reason-{Guid.NewGuid()}";
            var token = await service.CreateTokenAsync(userId);

            // Act - Revoke family without reason
            await service.RevokeTokenFamilyAsync(token.TokenFamily);

            // Assert
            var revokedToken = await context.RefreshTokens.FindAsync(token.Id);
            revokedToken!.ReasonRevoked.Should().Be("Token family revoked");
        });
    }

    #endregion
}

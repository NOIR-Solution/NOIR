using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using NOIR.Domain.Entities;

namespace NOIR.Web.Authentication;

/// <summary>
/// ASP.NET Core authentication handler for API Key + API Secret header-based auth.
/// External systems send X-API-Key and X-API-Secret headers.
/// Creates a ClaimsPrincipal with the key owner's identity and key-scoped permissions.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    public const string SchemeName = "ApiKey";
    public const string ApiKeyHeaderName = "X-API-Key";
    public const string ApiSecretHeaderName = "X-API-Secret";

    private readonly IApplicationDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMultiTenantStore<Tenant> _tenantStore;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Evicts a cached API key by its identifier. Call after rotate/revoke.
    /// </summary>
    public static void EvictCachedKey(IMemoryCache cache, string keyIdentifier)
    {
        cache.Remove($"apikey:{keyIdentifier}");
    }

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApplicationDbContext dbContext,
        IMemoryCache cache,
        IServiceProvider serviceProvider,
        IMultiTenantStore<Tenant> tenantStore)
        : base(options, logger, encoder)
    {
        _dbContext = dbContext;
        _cache = cache;
        _serviceProvider = serviceProvider;
        _tenantStore = tenantStore;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Only attempt if API Key headers are present
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeader) ||
            !Request.Headers.TryGetValue(ApiSecretHeaderName, out var apiSecretHeader))
        {
            return AuthenticateResult.NoResult();
        }

        var keyIdentifier = apiKeyHeader.ToString();
        var plaintextSecret = apiSecretHeader.ToString();

        if (string.IsNullOrWhiteSpace(keyIdentifier) || string.IsNullOrWhiteSpace(plaintextSecret))
        {
            return AuthenticateResult.Fail("API Key and API Secret headers must not be empty.");
        }

        // Look up key by identifier (with short cache)
        var cacheKey = $"apikey:{keyIdentifier}";
        var apiKey = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(CacheTtl);
            // IgnoreQueryFilters: tenant filter can't apply yet — API Key auth resolves tenant
            return await _dbContext.ApiKeys
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.KeyIdentifier == keyIdentifier && !k.IsDeleted);
        });

        if (apiKey is null)
        {
            return AuthenticateResult.Fail("Invalid API key.");
        }

        if (apiKey.IsRevoked)
        {
            return AuthenticateResult.Fail("API key has been revoked.");
        }

        if (apiKey.IsExpired)
        {
            return AuthenticateResult.Fail("API key has expired.");
        }

        // Verify secret
        if (!apiKey.VerifySecret(plaintextSecret))
        {
            return AuthenticateResult.Fail("Invalid API secret.");
        }

        // Record usage (fire and forget — don't block auth)
        _ = RecordUsageAsync(apiKey.Id, Context.Connection.RemoteIpAddress?.ToString());

        // Build claims principal with user identity and key-scoped permissions
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, apiKey.UserId),
            new("api_key_id", apiKey.Id.ToString()),
            new("auth_method", "api_key"),
        };

        // Add tenant claim and inject X-Tenant header so Finbuckle resolves tenant
        if (apiKey.TenantId is not null)
        {
            claims.Add(new Claim("tenant_id", apiKey.TenantId));

            // Resolve tenant Identifier from store (TenantId is the GUID Id, header strategy needs Identifier)
            if (!Request.Headers.ContainsKey("X-Tenant"))
            {
                var tenantIdentifier = await ResolveTenantIdentifierAsync(apiKey.TenantId);
                if (tenantIdentifier is not null)
                {
                    Context.Request.Headers.Append("X-Tenant", tenantIdentifier);
                }
            }
        }

        // Add key-scoped permissions as claims
        var permissions = apiKey.GetPermissions();
        foreach (var permission in permissions)
        {
            claims.Add(new Claim(Permissions.ClaimType, permission));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }

    private async Task<string?> ResolveTenantIdentifierAsync(string tenantId)
    {
        var cacheKey = $"tenant_id_to_identifier:{tenantId}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
            var allTenants = await _tenantStore.GetAllAsync();
            return allTenants.FirstOrDefault(t => t.Id == tenantId)?.Identifier;
        });
    }

    private async Task RecordUsageAsync(Guid apiKeyId, string? ipAddress)
    {
        try
        {
            // Use a new scope to avoid concurrent DbContext access with the request pipeline
            await using var scope = _serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var key = await dbContext.ApiKeys
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(k => k.Id == apiKeyId);
            if (key is not null)
            {
                key.RecordUsage(ipAddress);
                await dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to record API key usage for {ApiKeyId}", apiKeyId);
        }
    }
}

/// <summary>
/// Options for API Key authentication. Currently empty but required by the handler pattern.
/// </summary>
public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
}

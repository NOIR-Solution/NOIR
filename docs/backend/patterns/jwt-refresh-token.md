# JWT Refresh Token Pattern

**Created:** 2025-12-31
**Based on:** OWASP recommendations, token rotation research

---

## Overview

NOIR implements **secure refresh token rotation** with family tracking for theft detection.

---

## Key Features

| Feature | Description |
|---------|-------------|
| Token Rotation | New token issued on each refresh |
| Family Tracking | All rotated tokens share a family ID |
| Theft Detection | Reused tokens trigger family revocation |
| Device Fingerprinting | Optional binding to device characteristics |
| Session Management | Configurable max concurrent sessions |

---

## Token Flow

```
1. Login → Access Token (15 min) + Refresh Token (7 days)
2. Access expires → Client sends refresh token
3. Server validates → Issues new access + refresh tokens
4. Old refresh token revoked, linked to new one
5. If old token reused → Entire family revoked (theft detected)
```

---

## Entities

### RefreshToken

```csharp
public class RefreshToken : Entity<Guid>, IAuditableEntity, ITenantEntity
{
    public string Token { get; private set; }
    public string UserId { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public Guid TokenFamily { get; private set; }  // For theft detection
    public string? DeviceFingerprint { get; private set; }

    public bool IsActive => !IsRevoked && !IsExpired;
}
```

---

## Services

### IRefreshTokenService

```csharp
public interface IRefreshTokenService
{
    Task<RefreshToken> CreateTokenAsync(...);
    Task<RefreshToken?> RotateTokenAsync(string currentToken, ...);
    Task RevokeAllUserTokensAsync(string userId, ...);
    Task RevokeTokenFamilyAsync(Guid tokenFamily, ...);
}
```

### IDeviceFingerprintService

```csharp
public interface IDeviceFingerprintService
{
    string? GenerateFingerprint();  // SHA256 of UA + Accept + IP
    string? GetClientIpAddress();
    string? GetUserAgent();
    string? GetDeviceName();
}
```

---

## Configuration

In `appsettings.json`:

```json
{
  "JwtSettings": {
    "Secret": "your-256-bit-secret-key-here",
    "Issuer": "NOIR",
    "Audience": "NOIR.Web",
    "ExpirationInMinutes": 15,
    "RefreshTokenExpirationInDays": 7,
    "EnableDeviceFingerprinting": true,
    "MaxConcurrentSessions": 5,
    "TokenRetentionDays": 30
  }
}
```

---

## Security Best Practices

1. **Always use HTTPS** - Tokens transmitted securely
2. **Short access token lifetime** - 15 minutes recommended
3. **Rotate on every refresh** - Limits exposure window
4. **Family revocation** - Detect and respond to theft
5. **Device fingerprinting** - Optional additional binding
6. **Session limits** - Prevent unlimited concurrent logins

---

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/auth/login` | POST | Login, returns tokens |
| `/api/auth/refresh` | POST | Rotate tokens |
| `/api/auth/logout` | POST | Revoke current token |
| `/api/auth/sessions` | GET | List active sessions |

---

## References

- [OWASP JWT Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/JSON_Web_Token_Cheat_Sheet_for_Java.html)
- [Refresh Token Rotation](https://auth0.com/docs/secure/tokens/refresh-tokens/refresh-token-rotation)

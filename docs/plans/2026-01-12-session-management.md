# Session Management Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Allow users to view and manage their active sessions (devices), revoke individual sessions, and sign out from all devices.

**Architecture:** Feature follows NOIR's vertical slice pattern with co-located Query/Command + Handler. Backend uses existing `RefreshToken` entity and `IRefreshTokenService`. Frontend adds new "Sessions" tab in Settings page with 21st.dev components.

**Tech Stack:** C# (.NET 9), Wolverine handlers, Ardalis.Specification, React 19, TypeScript, Tailwind CSS, shadcn/ui, 21st.dev components

---

## Task 1: Add LastActivityAt Field to RefreshToken Entity

**Files:**
- Modify: `src/NOIR.Domain/Entities/RefreshToken.cs`
- Modify: `src/NOIR.Infrastructure/Identity/RefreshTokenService.cs`

**Step 1: Add LastActivityAt property to RefreshToken entity**

```csharp
// In RefreshToken.cs, add after ExpiresAt property (line ~22)

/// <summary>
/// When this token was last used for refresh (last activity).
/// </summary>
public DateTimeOffset? LastActivityAt { get; private set; }
```

**Step 2: Add method to update last activity**

```csharp
// In RefreshToken.cs, add after Revoke method (line ~138)

/// <summary>
/// Updates the last activity timestamp.
/// </summary>
public void UpdateLastActivity()
{
    LastActivityAt = DateTimeOffset.UtcNow;
}
```

**Step 3: Update RotateTokenAsync to set LastActivityAt**

In `RefreshTokenService.cs`, in the `RotateTokenAsync` method, after creating the new token, call `UpdateLastActivity()` on the new token before saving.

**Step 4: Run build to verify changes compile**

Run: `dotnet build src/NOIR.sln`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add src/NOIR.Domain/Entities/RefreshToken.cs src/NOIR.Infrastructure/Identity/RefreshTokenService.cs
git commit -m "feat(auth): add LastActivityAt tracking to RefreshToken"
```

---

## Task 2: Add EF Migration for LastActivityAt

**Files:**
- Create: `src/NOIR.Infrastructure/Persistence/Migrations/[timestamp]_AddRefreshTokenLastActivityAt.cs` (generated)

**Step 1: Generate migration**

Run: `dotnet ef migrations add AddRefreshTokenLastActivityAt --project src/NOIR.Infrastructure --startup-project src/NOIR.Web`
Expected: Migration file created

**Step 2: Verify migration looks correct**

The Up() method should add a nullable DateTimeOffset column `LastActivityAt` to the `RefreshTokens` table.

**Step 3: Commit**

```bash
git add src/NOIR.Infrastructure/Persistence/Migrations/
git commit -m "chore(db): add migration for RefreshToken.LastActivityAt"
```

---

## Task 3: Create SessionDto

**Files:**
- Create: `src/NOIR.Application/Features/Sessions/DTOs/SessionDto.cs`

**Step 1: Create the DTOs folder and file**

```csharp
namespace NOIR.Application.Features.Sessions.DTOs;

/// <summary>
/// Represents an active user session for display in the UI.
/// </summary>
public sealed record SessionDto(
    Guid Id,
    string DeviceName,
    string? Browser,
    string? OperatingSystem,
    string? IpAddress,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? LastActivityAt,
    bool IsCurrentSession);
```

**Step 2: Run build to verify**

Run: `dotnet build src/NOIR.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/NOIR.Application/Features/Sessions/
git commit -m "feat(sessions): add SessionDto for session management"
```

---

## Task 4: Create UserAgentParser Utility

**Files:**
- Create: `src/NOIR.Application/Common/Utilities/UserAgentParser.cs`

**Step 1: Create the parser**

```csharp
namespace NOIR.Application.Common.Utilities;

/// <summary>
/// Parses User-Agent strings to extract device information.
/// </summary>
public static class UserAgentParser
{
    public static (string DeviceName, string? Browser, string? OperatingSystem) Parse(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return ("Unknown Device", null, null);
        }

        var browser = ParseBrowser(userAgent);
        var os = ParseOperatingSystem(userAgent);
        var deviceName = $"{browser ?? "Browser"} on {os ?? "Unknown"}";

        return (deviceName, browser, os);
    }

    private static string? ParseBrowser(string userAgent)
    {
        if (userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase))
            return "Microsoft Edge";
        if (userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase) && !userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase))
            return "Chrome";
        if (userAgent.Contains("Firefox/", StringComparison.OrdinalIgnoreCase))
            return "Firefox";
        if (userAgent.Contains("Safari/", StringComparison.OrdinalIgnoreCase) && !userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase))
            return "Safari";
        if (userAgent.Contains("Opera", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("OPR/", StringComparison.OrdinalIgnoreCase))
            return "Opera";

        return null;
    }

    private static string? ParseOperatingSystem(string userAgent)
    {
        if (userAgent.Contains("Windows NT 10", StringComparison.OrdinalIgnoreCase))
            return "Windows";
        if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
            return "Windows";
        if (userAgent.Contains("Mac OS X", StringComparison.OrdinalIgnoreCase))
            return "macOS";
        if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase) && !userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
            return "Linux";
        if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
            return "Android";
        if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
            return "iOS";

        return null;
    }
}
```

**Step 2: Run build to verify**

Run: `dotnet build src/NOIR.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/NOIR.Application/Common/Utilities/
git commit -m "feat(utils): add UserAgentParser for session device names"
```

---

## Task 5: Create GetUserSessionsQuery and Handler

**Files:**
- Create: `src/NOIR.Application/Features/Sessions/Queries/GetUserSessions/GetUserSessionsQuery.cs`
- Create: `src/NOIR.Application/Features/Sessions/Queries/GetUserSessions/GetUserSessionsQueryHandler.cs`

**Step 1: Create the query**

```csharp
namespace NOIR.Application.Features.Sessions.Queries.GetUserSessions;

/// <summary>
/// Query to get all active sessions for the current user.
/// </summary>
public sealed record GetUserSessionsQuery;
```

**Step 2: Create the handler**

```csharp
namespace NOIR.Application.Features.Sessions.Queries.GetUserSessions;

/// <summary>
/// Handler for GetUserSessionsQuery.
/// Returns all active sessions for the current user.
/// </summary>
public class GetUserSessionsQueryHandler
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICurrentUser _currentUser;
    private readonly ICookieAuthService _cookieAuthService;
    private readonly ILocalizationService _localization;

    public GetUserSessionsQueryHandler(
        IRefreshTokenService refreshTokenService,
        ICurrentUser currentUser,
        ICookieAuthService cookieAuthService,
        ILocalizationService localization)
    {
        _refreshTokenService = refreshTokenService;
        _currentUser = currentUser;
        _cookieAuthService = cookieAuthService;
        _localization = localization;
    }

    public async Task<Result<IReadOnlyList<SessionDto>>> Handle(
        GetUserSessionsQuery query,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result.Failure<IReadOnlyList<SessionDto>>(
                Error.Unauthorized(_localization["auth.user.notAuthenticated"], ErrorCodes.Auth.Unauthorized));
        }

        var sessions = await _refreshTokenService.GetActiveSessionsAsync(
            _currentUser.UserId,
            cancellationToken);

        // Get current refresh token to identify current session
        var currentToken = _cookieAuthService.GetRefreshTokenFromCookie();

        var sessionDtos = sessions.Select(s =>
        {
            var (deviceName, browser, os) = UserAgentParser.Parse(s.UserAgent);

            // Use stored DeviceName if available, otherwise parse from UserAgent
            var displayName = !string.IsNullOrEmpty(s.DeviceName) ? s.DeviceName : deviceName;

            return new SessionDto(
                s.Id,
                displayName,
                browser,
                os,
                s.CreatedByIp,
                s.CreatedAt,
                s.ExpiresAt,
                s.LastActivityAt,
                s.Token == currentToken);
        }).ToList();

        return Result.Success<IReadOnlyList<SessionDto>>(sessionDtos);
    }
}
```

**Step 3: Run build to verify**

Run: `dotnet build src/NOIR.sln`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add src/NOIR.Application/Features/Sessions/
git commit -m "feat(sessions): add GetUserSessionsQuery to list active sessions"
```

---

## Task 6: Create RevokeSessionCommand and Handler

**Files:**
- Create: `src/NOIR.Application/Features/Sessions/Commands/RevokeSession/RevokeSessionCommand.cs`
- Create: `src/NOIR.Application/Features/Sessions/Commands/RevokeSession/RevokeSessionCommandHandler.cs`
- Create: `src/NOIR.Application/Features/Sessions/Commands/RevokeSession/RevokeSessionCommandValidator.cs`

**Step 1: Create the command**

```csharp
namespace NOIR.Application.Features.Sessions.Commands.RevokeSession;

/// <summary>
/// Command to revoke a specific session by ID.
/// </summary>
public sealed record RevokeSessionCommand(Guid SessionId);
```

**Step 2: Create the validator**

```csharp
namespace NOIR.Application.Features.Sessions.Commands.RevokeSession;

public class RevokeSessionCommandValidator : AbstractValidator<RevokeSessionCommand>
{
    public RevokeSessionCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage(localization["sessions.sessionIdRequired"]);
    }
}
```

**Step 3: Create the handler**

```csharp
namespace NOIR.Application.Features.Sessions.Commands.RevokeSession;

/// <summary>
/// Handler for RevokeSessionCommand.
/// Revokes a specific session (refresh token) by ID.
/// </summary>
public class RevokeSessionCommandHandler
{
    private readonly IRepository<RefreshToken, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ICookieAuthService _cookieAuthService;
    private readonly ILocalizationService _localization;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RevokeSessionCommandHandler(
        IRepository<RefreshToken, Guid> repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ICookieAuthService cookieAuthService,
        ILocalizationService localization,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _cookieAuthService = cookieAuthService;
        _localization = localization;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result> Handle(RevokeSessionCommand command, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result.Failure(
                Error.Unauthorized(_localization["auth.user.notAuthenticated"], ErrorCodes.Auth.Unauthorized));
        }

        // Find the session
        var spec = new RefreshTokenByIdSpec(command.SessionId);
        var session = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (session is null)
        {
            return Result.Failure(
                Error.NotFound(_localization["sessions.notFound"], ErrorCodes.Sessions.NotFound));
        }

        // Verify ownership
        if (session.UserId != _currentUser.UserId)
        {
            return Result.Failure(
                Error.Forbidden(_localization["sessions.notOwner"], ErrorCodes.Sessions.NotOwner));
        }

        // Prevent revoking current session
        var currentToken = _cookieAuthService.GetRefreshTokenFromCookie();
        if (session.Token == currentToken)
        {
            return Result.Failure(
                Error.Validation(_localization["sessions.cannotRevokeCurrent"], ErrorCodes.Sessions.CannotRevokeCurrent));
        }

        // Revoke the session
        var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        session.Revoke(ipAddress, "User revoked session");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

**Step 4: Run build to verify**

Run: `dotnet build src/NOIR.sln`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add src/NOIR.Application/Features/Sessions/
git commit -m "feat(sessions): add RevokeSessionCommand to revoke individual sessions"
```

---

## Task 7: Create RefreshTokenByIdSpec

**Files:**
- Create: `src/NOIR.Application/Specifications/RefreshTokens/RefreshTokenByIdSpec.cs`

**Step 1: Create the specification**

```csharp
namespace NOIR.Application.Specifications.RefreshTokens;

/// <summary>
/// Specification to find a refresh token by its ID.
/// </summary>
public sealed class RefreshTokenByIdSpec : Specification<RefreshToken>
{
    public RefreshTokenByIdSpec(Guid id)
    {
        Query.Where(t => t.Id == id)
             .AsTracking()
             .TagWith("RefreshTokenById");
    }
}
```

**Step 2: Run build to verify**

Run: `dotnet build src/NOIR.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/NOIR.Application/Specifications/RefreshTokens/
git commit -m "feat(specs): add RefreshTokenByIdSpec"
```

---

## Task 8: Create RevokeOtherSessionsCommand and Handler

**Files:**
- Create: `src/NOIR.Application/Features/Sessions/Commands/RevokeOtherSessions/RevokeOtherSessionsCommand.cs`
- Create: `src/NOIR.Application/Features/Sessions/Commands/RevokeOtherSessions/RevokeOtherSessionsCommandHandler.cs`

**Step 1: Create the command**

```csharp
namespace NOIR.Application.Features.Sessions.Commands.RevokeOtherSessions;

/// <summary>
/// Command to revoke all sessions except the current one ("Sign out everywhere else").
/// </summary>
public sealed record RevokeOtherSessionsCommand;
```

**Step 2: Create the handler**

```csharp
namespace NOIR.Application.Features.Sessions.Commands.RevokeOtherSessions;

/// <summary>
/// Handler for RevokeOtherSessionsCommand.
/// Revokes all sessions except the current one.
/// </summary>
public class RevokeOtherSessionsCommandHandler
{
    private readonly IRepository<RefreshToken, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ICookieAuthService _cookieAuthService;
    private readonly ILocalizationService _localization;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RevokeOtherSessionsCommandHandler(
        IRepository<RefreshToken, Guid> repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ICookieAuthService cookieAuthService,
        ILocalizationService localization,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _cookieAuthService = cookieAuthService;
        _localization = localization;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<int>> Handle(RevokeOtherSessionsCommand command, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result.Failure<int>(
                Error.Unauthorized(_localization["auth.user.notAuthenticated"], ErrorCodes.Auth.Unauthorized));
        }

        // Get all active sessions for user
        var spec = new ActiveRefreshTokensByUserSpec(_currentUser.UserId);
        var sessions = await _repository.ListAsync(spec, cancellationToken);

        // Get current token to exclude
        var currentToken = _cookieAuthService.GetRefreshTokenFromCookie();
        var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        var revokedCount = 0;
        foreach (var session in sessions)
        {
            if (session.Token != currentToken)
            {
                session.Revoke(ipAddress, "User signed out from all other devices");
                revokedCount++;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(revokedCount);
    }
}
```

**Step 3: Run build to verify**

Run: `dotnet build src/NOIR.sln`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add src/NOIR.Application/Features/Sessions/
git commit -m "feat(sessions): add RevokeOtherSessionsCommand for sign-out-everywhere"
```

---

## Task 9: Add Error Codes for Sessions

**Files:**
- Modify: `src/NOIR.Application/Common/Constants/ErrorCodes.cs`

**Step 1: Add Sessions error codes**

```csharp
// Add new static class inside ErrorCodes
public static class Sessions
{
    public const string NotFound = "SESSIONS_NOT_FOUND";
    public const string NotOwner = "SESSIONS_NOT_OWNER";
    public const string CannotRevokeCurrent = "SESSIONS_CANNOT_REVOKE_CURRENT";
}
```

**Step 2: Run build to verify**

Run: `dotnet build src/NOIR.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/NOIR.Application/Common/Constants/ErrorCodes.cs
git commit -m "feat(errors): add session management error codes"
```

---

## Task 10: Add Backend Localization Strings

**Files:**
- Modify: `src/NOIR.Infrastructure/Localization/Resources/Messages.en.resx`
- Modify: `src/NOIR.Infrastructure/Localization/Resources/Messages.vi.resx`

**Step 1: Add English translations**

Add these keys to `Messages.en.resx`:

| Key | Value |
|-----|-------|
| sessions.sessionIdRequired | Session ID is required |
| sessions.notFound | Session not found |
| sessions.notOwner | You do not own this session |
| sessions.cannotRevokeCurrent | Cannot revoke your current session. Use logout instead. |

**Step 2: Add Vietnamese translations**

Add corresponding keys to `Messages.vi.resx`:

| Key | Value |
|-----|-------|
| sessions.sessionIdRequired | ID phien lam viec la bat buoc |
| sessions.notFound | Khong tim thay phien lam viec |
| sessions.notOwner | Ban khong so huu phien lam viec nay |
| sessions.cannotRevokeCurrent | Khong the thu hoi phien hien tai. Hay dang xuat thay the. |

**Step 3: Run build to verify**

Run: `dotnet build src/NOIR.sln`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add src/NOIR.Infrastructure/Localization/Resources/
git commit -m "feat(i18n): add session management backend translations"
```

---

## Task 11: Create SessionEndpoints

**Files:**
- Create: `src/NOIR.Web/Endpoints/SessionEndpoints.cs`

**Step 1: Create the endpoints file**

```csharp
namespace NOIR.Web.Endpoints;

/// <summary>
/// Session management API endpoints.
/// Allows users to view and manage their active sessions.
/// </summary>
public static class SessionEndpoints
{
    public static void MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sessions")
            .WithTags("Sessions")
            .RequireAuthorization()
            .RequireRateLimiting("fixed");

        group.MapGet("/", async (IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<IReadOnlyList<SessionDto>>>(new GetUserSessionsQuery());
            return result.ToHttpResult();
        })
        .WithName("GetUserSessions")
        .WithSummary("Get all active sessions for the current user")
        .WithDescription("Returns a list of active sessions including device info, IP address, and last activity time.")
        .Produces<IReadOnlyList<SessionDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

        group.MapPost("/{sessionId:guid}/revoke", async (Guid sessionId, IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result>(new RevokeSessionCommand(sessionId));
            return result.ToHttpResult();
        })
        .WithName("RevokeSession")
        .WithSummary("Revoke a specific session")
        .WithDescription("Revokes a specific session by ID. Cannot revoke the current session.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/revoke-others", async (IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<int>>(new RevokeOtherSessionsCommand());
            return result.ToHttpResult();
        })
        .WithName("RevokeOtherSessions")
        .WithSummary("Sign out from all other devices")
        .WithDescription("Revokes all sessions except the current one. Returns the count of revoked sessions.")
        .Produces<int>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);
    }
}
```

**Step 2: Register endpoints in Program.cs**

In `src/NOIR.Web/Program.cs`, add `app.MapSessionEndpoints();` after the other endpoint mappings.

**Step 3: Run build to verify**

Run: `dotnet build src/NOIR.sln`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add src/NOIR.Web/Endpoints/SessionEndpoints.cs src/NOIR.Web/Program.cs
git commit -m "feat(api): add session management endpoints"
```

---

## Task 12: Add GlobalUsings for Sessions Feature

**Files:**
- Modify: `src/NOIR.Application/GlobalUsings.cs`

**Step 1: Add required usings**

Add if not already present:
```csharp
global using NOIR.Application.Features.Sessions.DTOs;
global using NOIR.Application.Features.Sessions.Queries.GetUserSessions;
global using NOIR.Application.Features.Sessions.Commands.RevokeSession;
global using NOIR.Application.Features.Sessions.Commands.RevokeOtherSessions;
global using NOIR.Application.Common.Utilities;
```

**Step 2: Run build to verify**

Run: `dotnet build src/NOIR.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/NOIR.Application/GlobalUsings.cs
git commit -m "chore: add GlobalUsings for Sessions feature"
```

---

## Task 13: Regenerate API Types for Frontend

**Files:**
- Modify: `src/NOIR.Web/frontend/src/types/api.generated.ts` (generated)

**Step 1: Run the API type generator**

Run: `cd src/NOIR.Web/frontend && npm run generate:api`
Expected: Types regenerated with new Session endpoints

**Step 2: Verify new types exist**

Check that `SessionDto`, `GetUserSessions`, `RevokeSession`, `RevokeOtherSessions` types are in the generated file.

**Step 3: Commit**

```bash
git add src/NOIR.Web/frontend/src/types/api.generated.ts
git commit -m "chore(frontend): regenerate API types with session endpoints"
```

---

## Task 14: Create Sessions Service (Frontend)

**Files:**
- Create: `src/NOIR.Web/frontend/src/services/sessions.ts`

**Step 1: Create the service**

```typescript
/**
 * Sessions API service
 * Handles session management operations
 */
import { apiClient } from './apiClient'

export interface Session {
  id: string
  deviceName: string
  browser: string | null
  operatingSystem: string | null
  ipAddress: string | null
  createdAt: string
  expiresAt: string
  lastActivityAt: string | null
  isCurrentSession: boolean
}

/**
 * Get all active sessions for the current user
 */
export async function getSessions(): Promise<Session[]> {
  return apiClient<Session[]>('/sessions')
}

/**
 * Revoke a specific session
 */
export async function revokeSession(sessionId: string): Promise<void> {
  await apiClient(`/sessions/${sessionId}/revoke`, { method: 'POST' })
}

/**
 * Revoke all sessions except the current one
 * Returns the number of sessions revoked
 */
export async function revokeOtherSessions(): Promise<number> {
  return apiClient<number>('/sessions/revoke-others', { method: 'POST' })
}
```

**Step 2: Commit**

```bash
git add src/NOIR.Web/frontend/src/services/sessions.ts
git commit -m "feat(frontend): add sessions API service"
```

---

## Task 15: Add Frontend i18n Translations

**Files:**
- Modify: `src/NOIR.Web/frontend/public/locales/en/auth.json`
- Modify: `src/NOIR.Web/frontend/public/locales/vi/auth.json`

**Step 1: Add English translations**

Add to `auth.json` under a new "sessions" key:

```json
"sessions": {
  "title": "Active Sessions",
  "description": "Manage your active sessions across devices",
  "currentSession": "Current",
  "lastActive": "Last active",
  "activeNow": "Active now",
  "revoke": "Sign out",
  "revokeConfirmTitle": "Sign out from this device?",
  "revokeConfirmDescription": "This will end the session on {{device}}. They will need to sign in again to access the account.",
  "revokeConfirm": "Sign out",
  "revokeCancel": "Cancel",
  "revokeSuccess": "Session revoked successfully",
  "revokeFailed": "Failed to revoke session",
  "revokeOthers": "Sign out from all other devices",
  "revokeOthersConfirmTitle": "Sign out from all other devices?",
  "revokeOthersConfirmDescription": "This will end {{count}} session(s) on other devices. They will need to sign in again.",
  "revokeOthersSuccess": "Signed out from {{count}} device(s)",
  "revokeOthersFailed": "Failed to sign out from other devices",
  "noOtherSessions": "No other active sessions",
  "loadFailed": "Failed to load sessions",
  "ipAddress": "IP Address",
  "createdAt": "Signed in",
  "expiresAt": "Expires"
}
```

**Step 2: Add Vietnamese translations**

Add corresponding translations to the Vietnamese locale file.

**Step 3: Commit**

```bash
git add src/NOIR.Web/frontend/public/locales/
git commit -m "feat(i18n): add session management frontend translations"
```

---

## Task 16: Create SessionManager Component with 21st.dev

**Files:**
- Create: `src/NOIR.Web/frontend/src/components/settings/SessionManager.tsx`

**Step 1: Use 21st.dev to get component inspiration**

Use 21st.dev magic component builder to create a session list card component with:
- Device icon (desktop/mobile)
- Device name and browser info
- IP address
- Last activity time (relative)
- Current session badge
- Revoke button with confirmation dialog

**Step 2: Create the SessionManager component**

```tsx
import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Monitor, Smartphone, Tablet, Globe, MoreVertical, LogOut } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { getSessions, revokeSession, revokeOtherSessions, Session } from '@/services/sessions'
import { formatDistanceToNow } from 'date-fns'

function getDeviceIcon(os: string | null) {
  if (!os) return Monitor
  const osLower = os.toLowerCase()
  if (osLower.includes('android') || osLower.includes('ios')) return Smartphone
  if (osLower.includes('ipad')) return Tablet
  return Monitor
}

export function SessionManager() {
  const { t } = useTranslation('auth')
  const [sessions, setSessions] = useState<Session[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [revokeDialog, setRevokeDialog] = useState<{ open: boolean; session?: Session }>({ open: false })
  const [revokeAllDialog, setRevokeAllDialog] = useState(false)
  const [isRevoking, setIsRevoking] = useState(false)

  const loadSessions = async () => {
    try {
      setIsLoading(true)
      const data = await getSessions()
      setSessions(data)
    } catch {
      toast.error(t('sessions.loadFailed'))
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    loadSessions()
  }, [])

  const handleRevoke = async () => {
    if (!revokeDialog.session) return

    setIsRevoking(true)
    try {
      await revokeSession(revokeDialog.session.id)
      toast.success(t('sessions.revokeSuccess'))
      setSessions(prev => prev.filter(s => s.id !== revokeDialog.session!.id))
      setRevokeDialog({ open: false })
    } catch {
      toast.error(t('sessions.revokeFailed'))
    } finally {
      setIsRevoking(false)
    }
  }

  const handleRevokeAll = async () => {
    setIsRevoking(true)
    try {
      const count = await revokeOtherSessions()
      toast.success(t('sessions.revokeOthersSuccess', { count }))
      setSessions(prev => prev.filter(s => s.isCurrentSession))
      setRevokeAllDialog(false)
    } catch {
      toast.error(t('sessions.revokeOthersFailed'))
    } finally {
      setIsRevoking(false)
    }
  }

  const otherSessions = sessions.filter(s => !s.isCurrentSession)

  const formatLastActivity = (session: Session) => {
    if (session.isCurrentSession) return t('sessions.activeNow')
    if (!session.lastActivityAt) return t('sessions.lastActive') + ': ' + formatDistanceToNow(new Date(session.createdAt), { addSuffix: true })
    return t('sessions.lastActive') + ': ' + formatDistanceToNow(new Date(session.lastActivityAt), { addSuffix: true })
  }

  if (isLoading) {
    return (
      <Card className="max-w-2xl">
        <CardContent className="py-8">
          <div className="flex items-center justify-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <>
      <Card className="max-w-2xl">
        <CardHeader>
          <div className="flex items-center gap-3">
            <div className="flex items-center justify-center w-10 h-10 rounded-lg bg-blue-600/10">
              <Globe className="h-5 w-5 text-blue-600" />
            </div>
            <div>
              <CardTitle>{t('sessions.title')}</CardTitle>
              <CardDescription>{t('sessions.description')}</CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          {sessions.map((session) => {
            const DeviceIcon = getDeviceIcon(session.operatingSystem)

            return (
              <div
                key={session.id}
                className="flex items-center justify-between p-4 rounded-lg border bg-card hover:bg-accent/50 transition-colors"
              >
                <div className="flex items-center gap-4">
                  <div className="flex items-center justify-center w-10 h-10 rounded-full bg-muted">
                    <DeviceIcon className="h-5 w-5 text-muted-foreground" />
                  </div>
                  <div className="space-y-1">
                    <div className="flex items-center gap-2">
                      <span className="font-medium">{session.deviceName}</span>
                      {session.isCurrentSession && (
                        <Badge variant="secondary" className="bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400">
                          {t('sessions.currentSession')}
                        </Badge>
                      )}
                    </div>
                    <div className="text-sm text-muted-foreground space-y-0.5">
                      <p>{formatLastActivity(session)}</p>
                      {session.ipAddress && (
                        <p>{t('sessions.ipAddress')}: {session.ipAddress}</p>
                      )}
                    </div>
                  </div>
                </div>

                {!session.isCurrentSession && (
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="ghost" size="icon">
                        <MoreVertical className="h-4 w-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      <DropdownMenuItem
                        className="text-destructive focus:text-destructive"
                        onClick={() => setRevokeDialog({ open: true, session })}
                      >
                        <LogOut className="mr-2 h-4 w-4" />
                        {t('sessions.revoke')}
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                )}
              </div>
            )
          })}

          {otherSessions.length > 0 && (
            <Button
              variant="outline"
              className="w-full mt-4 text-destructive hover:text-destructive hover:bg-destructive/10"
              onClick={() => setRevokeAllDialog(true)}
            >
              <LogOut className="mr-2 h-4 w-4" />
              {t('sessions.revokeOthers')}
            </Button>
          )}

          {otherSessions.length === 0 && sessions.length > 0 && (
            <p className="text-center text-muted-foreground py-4">
              {t('sessions.noOtherSessions')}
            </p>
          )}
        </CardContent>
      </Card>

      {/* Revoke Single Session Dialog */}
      <Dialog open={revokeDialog.open} onOpenChange={(open) => setRevokeDialog({ open })}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('sessions.revokeConfirmTitle')}</DialogTitle>
            <DialogDescription>
              {t('sessions.revokeConfirmDescription', { device: revokeDialog.session?.deviceName })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setRevokeDialog({ open: false })} disabled={isRevoking}>
              {t('sessions.revokeCancel')}
            </Button>
            <Button variant="destructive" onClick={handleRevoke} disabled={isRevoking}>
              {isRevoking ? '...' : t('sessions.revokeConfirm')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Revoke All Sessions Dialog */}
      <Dialog open={revokeAllDialog} onOpenChange={setRevokeAllDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('sessions.revokeOthersConfirmTitle')}</DialogTitle>
            <DialogDescription>
              {t('sessions.revokeOthersConfirmDescription', { count: otherSessions.length })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setRevokeAllDialog(false)} disabled={isRevoking}>
              {t('sessions.revokeCancel')}
            </Button>
            <Button variant="destructive" onClick={handleRevokeAll} disabled={isRevoking}>
              {isRevoking ? '...' : t('sessions.revokeConfirm')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
```

**Step 3: Commit**

```bash
git add src/NOIR.Web/frontend/src/components/settings/SessionManager.tsx
git commit -m "feat(frontend): add SessionManager component"
```

---

## Task 17: Add Sessions Tab to Settings Page

**Files:**
- Modify: `src/NOIR.Web/frontend/src/pages/portal/Settings.tsx`

**Step 1: Add Sessions to nav items**

```tsx
// Add import at top
import { Smartphone } from 'lucide-react'
import { SessionManager } from '@/components/settings/SessionManager'

// Update SettingsSection type
type SettingsSection = 'profile' | 'security' | 'sessions'

// Add to navItems array
{ id: 'sessions' as const, icon: Smartphone, labelKey: 'sessions.title' },

// Add rendering in content area
{activeSection === 'sessions' && <SessionManager />}
```

**Step 2: Verify changes work**

Run: `cd src/NOIR.Web/frontend && npm run dev`
Expected: Settings page shows Sessions tab

**Step 3: Commit**

```bash
git add src/NOIR.Web/frontend/src/pages/portal/Settings.tsx
git commit -m "feat(frontend): add Sessions tab to Settings page"
```

---

## Task 18: Add Session Notification Email Service

**Files:**
- Create: `src/NOIR.Application/Common/Interfaces/ISessionNotificationService.cs`
- Create: `src/NOIR.Infrastructure/Services/SessionNotificationService.cs`

**Step 1: Create the interface**

```csharp
namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for sending session-related security notifications.
/// </summary>
public interface ISessionNotificationService
{
    /// <summary>
    /// Sends a notification when a session is revoked.
    /// </summary>
    Task NotifySessionRevokedAsync(
        string userId,
        string deviceName,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
```

**Step 2: Create the implementation**

```csharp
namespace NOIR.Infrastructure.Services;

/// <summary>
/// Sends email notifications for session security events.
/// </summary>
public class SessionNotificationService : ISessionNotificationService, IScopedService
{
    private readonly IUserIdentityService _userIdentityService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<SessionNotificationService> _logger;

    public SessionNotificationService(
        IUserIdentityService userIdentityService,
        IEmailSender emailSender,
        ILogger<SessionNotificationService> logger)
    {
        _userIdentityService = userIdentityService;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task NotifySessionRevokedAsync(
        string userId,
        string deviceName,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userIdentityService.FindByIdAsync(userId, cancellationToken);
            if (user?.Email is null) return;

            await _emailSender.SendTemplatedEmailAsync(
                user.Email,
                "session-revoked",
                new Dictionary<string, object>
                {
                    ["DeviceName"] = deviceName,
                    ["IpAddress"] = ipAddress ?? "Unknown",
                    ["RevokedAt"] = DateTimeOffset.UtcNow.ToString("f"),
                    ["UserName"] = user.FullName ?? user.Email
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send session revoked notification to user {UserId}", userId);
        }
    }
}
```

**Step 3: Run build to verify**

Run: `dotnet build src/NOIR.sln`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add src/NOIR.Application/Common/Interfaces/ISessionNotificationService.cs src/NOIR.Infrastructure/Services/SessionNotificationService.cs
git commit -m "feat(notifications): add SessionNotificationService for security alerts"
```

---

## Task 19: Integrate Notification into RevokeSessionCommandHandler

**Files:**
- Modify: `src/NOIR.Application/Features/Sessions/Commands/RevokeSession/RevokeSessionCommandHandler.cs`

**Step 1: Inject and call notification service**

Add `ISessionNotificationService` to constructor and call after revoking:

```csharp
// After session.Revoke() and SaveChangesAsync:
var (deviceName, _, _) = UserAgentParser.Parse(session.UserAgent);
await _notificationService.NotifySessionRevokedAsync(
    _currentUser.UserId,
    deviceName,
    session.CreatedByIp,
    cancellationToken);
```

**Step 2: Run build to verify**

Run: `dotnet build src/NOIR.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add src/NOIR.Application/Features/Sessions/Commands/RevokeSession/RevokeSessionCommandHandler.cs
git commit -m "feat(sessions): send email notification on session revocation"
```

---

## Task 20: Run Full Test Suite

**Step 1: Run all tests**

Run: `dotnet test src/NOIR.sln`
Expected: All tests pass

**Step 2: Fix any failing tests**

If tests fail, fix them before proceeding.

**Step 3: Commit any test fixes**

```bash
git add -A
git commit -m "test: fix any tests affected by session management"
```

---

## Task 21: Final Integration Test

**Step 1: Start the application**

Run: `dotnet run --project src/NOIR.Web`

**Step 2: Manual testing checklist**

- [ ] Login as admin@noir.local
- [ ] Navigate to Settings > Sessions
- [ ] Verify current session shows with "Current" badge
- [ ] If available, login from another browser/incognito
- [ ] Verify second session appears in list
- [ ] Click revoke on second session
- [ ] Confirm dialog appears
- [ ] Confirm revocation works
- [ ] Test "Sign out from all other devices" button

**Step 3: Final commit**

```bash
git add -A
git commit -m "feat(sessions): complete session management feature"
```

---

## Summary

This implementation adds comprehensive session management with:

1. **Backend (10 tasks):**
   - LastActivityAt tracking on RefreshToken
   - UserAgentParser for device names
   - GetUserSessionsQuery to list sessions
   - RevokeSessionCommand for individual revocation
   - RevokeOtherSessionsCommand for "sign out everywhere"
   - SessionEndpoints for REST API
   - Email notifications on revocation

2. **Frontend (7 tasks):**
   - Sessions service for API calls
   - SessionManager component with 21st.dev styling
   - Sessions tab in Settings page
   - Full i18n support (EN/VI)

3. **Security features:**
   - Cannot revoke current session (prevents self-lockout)
   - Ownership verification before revocation
   - Email notification on session revocation
   - IP address tracking for audit

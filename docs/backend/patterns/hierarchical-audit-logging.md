# Hierarchical Audit Logging Pattern

**Created:** 2026-01-01
**Updated:** 2026-01-15
**Status:** Implemented
**Based on:** Simple field-level diff format, ABP Framework Audit Logging, Wolverine Middleware

---

## Overview

NOIR implements a 3-level hierarchical audit logging system that captures the complete flow from HTTP request to entity changes, with diff tracking at each level.

```
HTTP Request (1)
    │
    └── Handler Executions (0-N)
            │
            └── Entity Changes (0-N)
```

**Key Features:**
- Full HTTP request/response logging
- DTO-level diff tracking (what user saw/submitted)
- Entity-level diff tracking (what changed in database)
- GitHub-style diff visualization in UI
- All levels linked by CorrelationId

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         AUDIT FLOW                                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  User clicks "Update Customer"                                          │
│         │                                                               │
│         ▼                                                               │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ HTTP Request                                                     │   │
│  │ POST /api/customers/123                                          │   │
│  │ CorrelationId: "abc-xyz-789"                                     │   │
│  └───────────────────────────────┬─────────────────────────────────┘   │
│                                  │                                      │
│         ┌────────────────────────┴────────────────────────┐            │
│         ▼                                                  ▼            │
│  ┌─────────────────────────┐              ┌─────────────────────────┐  │
│  │ Handler 1               │              │ Handler 2               │  │
│  │ UpdateCustomerCommand   │              │ SendNotificationCommand │  │
│  │                         │              │                         │  │
│  │ DTO Diff:               │              │ DTO Diff: null (create) │  │
│  │ - name: "A" → "B"       │              └────────────┬────────────┘  │
│  │ - email: changed        │                           │               │
│  └────────────┬────────────┘                           │               │
│               │                                        │               │
│       ┌───────┴───────┐                               ▼               │
│       ▼               ▼                    ┌─────────────────────┐    │
│  ┌─────────┐    ┌─────────┐               │ Notification        │    │
│  │Customer │    │AuditLog │               │ (Added)             │    │
│  │(Modified)│   │(Added)  │               │ EntityDiff: {...}   │    │
│  │EntityDiff│   │EntityDiff│              └─────────────────────┘    │
│  └─────────┘    └─────────┘                                          │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Database Schema

### Table 1: HttpRequestAuditLogs

Captures the HTTP request/response context.

```sql
CREATE TABLE HttpRequestAuditLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CorrelationId NVARCHAR(100) NOT NULL UNIQUE,
    TenantId NVARCHAR(100),
    UserId NVARCHAR(100),
    UserEmail NVARCHAR(200),

    -- Request
    HttpMethod NVARCHAR(10) NOT NULL,
    Url NVARCHAR(2000) NOT NULL,
    QueryString NVARCHAR(2000),
    RequestHeaders NVARCHAR(MAX),  -- JSON (sanitized)
    RequestBody NVARCHAR(MAX),     -- JSON (sanitized)

    -- Response
    ResponseStatusCode INT,
    ResponseBody NVARCHAR(MAX),    -- JSON (optional, size-limited)

    -- Context
    IpAddress NVARCHAR(50),
    UserAgent NVARCHAR(500),

    -- Timing
    StartTime DATETIMEOFFSET NOT NULL,
    EndTime DATETIMEOFFSET,
    DurationMs BIGINT,

    -- Indexes
    INDEX IX_HttpRequestAuditLogs_CorrelationId (CorrelationId),
    INDEX IX_HttpRequestAuditLogs_UserId (UserId),
    INDEX IX_HttpRequestAuditLogs_StartTime (StartTime)
);
```

### Table 2: HandlerAuditLogs

Captures handler/command executions with DTO diff.

```sql
CREATE TABLE HandlerAuditLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    HttpRequestAuditLogId UNIQUEIDENTIFIER,  -- FK to parent
    CorrelationId NVARCHAR(100) NOT NULL,
    TenantId NVARCHAR(100),

    -- Handler Info
    HandlerName NVARCHAR(200) NOT NULL,
    OperationType NVARCHAR(20) NOT NULL,  -- Create/Update/Delete/Query

    -- Target DTO
    TargetDtoType NVARCHAR(200),
    TargetDtoId NVARCHAR(100),
    DtoDiff NVARCHAR(MAX),  -- RFC 6902 JSON Patch

    -- Input/Output (for debugging, sanitized)
    InputParameters NVARCHAR(MAX),  -- JSON
    OutputResult NVARCHAR(MAX),     -- JSON

    -- Timing & Status
    StartTime DATETIMEOFFSET NOT NULL,
    EndTime DATETIMEOFFSET,
    DurationMs BIGINT,
    IsSuccess BIT NOT NULL DEFAULT 1,
    ErrorMessage NVARCHAR(MAX),

    -- Constraints
    FOREIGN KEY (HttpRequestAuditLogId) REFERENCES HttpRequestAuditLogs(Id),

    -- Indexes
    INDEX IX_HandlerAuditLogs_CorrelationId (CorrelationId),
    INDEX IX_HandlerAuditLogs_HandlerName (HandlerName),
    INDEX IX_HandlerAuditLogs_TargetDto (TargetDtoType, TargetDtoId)
);
```

### Table 3: EntityAuditLogs

Captures entity-level changes with property diff.

```sql
CREATE TABLE EntityAuditLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    HandlerAuditLogId UNIQUEIDENTIFIER,  -- FK to parent
    CorrelationId NVARCHAR(100) NOT NULL,
    TenantId NVARCHAR(100),

    -- Entity Info
    EntityType NVARCHAR(200) NOT NULL,
    EntityId NVARCHAR(100) NOT NULL,
    Operation NVARCHAR(20) NOT NULL,  -- Added/Modified/Deleted

    -- Diff (RFC 6902 JSON Patch - contains all property changes)
    EntityDiff NVARCHAR(MAX),

    -- Ordering
    Timestamp DATETIMEOFFSET NOT NULL,
    Version INT NOT NULL DEFAULT 1,

    -- Constraints
    FOREIGN KEY (HandlerAuditLogId) REFERENCES HandlerAuditLogs(Id),

    -- Indexes
    INDEX IX_EntityAuditLogs_CorrelationId (CorrelationId),
    INDEX IX_EntityAuditLogs_Entity (EntityType, EntityId),
    INDEX IX_EntityAuditLogs_Timestamp (Timestamp)
);
```

---

## Diff Format (Simple Field-Level)

All diffs are stored in a simple, human-readable field-level format:

```json
{
  "fieldName": {
    "from": oldValue,
    "to": newValue
  }
}
```

### Why Simple Format (Not RFC 6902)

| Aspect | RFC 6902 JSON Patch | Simple Field-Level |
|--------|---------------------|-------------------|
| Format | Array of operations | Object with field keys |
| Readability | Low (path: "/name") | High (fieldName: "name") |
| Query | Complex (array search) | Simple (JSON path) |
| SQL Queries | Hard to filter | Easy: `WHERE EntityDiff LIKE '%"name":%'` |
| Nested Paths | `/address/city` | `address.city` (dot notation) |

### Example: Update Customer

**Before State:**
```json
{
  "id": "cust-123",
  "name": "Acme Corp",
  "email": "old@acme.com",
  "creditLimit": 10000
}
```

**After State:**
```json
{
  "id": "cust-123",
  "name": "Acme Inc",
  "email": "new@acme.com",
  "creditLimit": 25000,
  "phone": "+1-555-0123"
}
```

**Stored Diff (DtoDiff / EntityDiff):**
```json
{
  "name": { "from": "Acme Corp", "to": "Acme Inc" },
  "email": { "from": "old@acme.com", "to": "new@acme.com" },
  "creditLimit": { "from": 10000, "to": 25000 },
  "phone": { "from": null, "to": "+1-555-0123" }
}
```

### Nested Objects

Nested paths use dot notation:

```json
{
  "address.city": { "from": "New York", "to": "Los Angeles" },
  "address.zip": { "from": "10001", "to": "90001" }
}
```

### Operations Inferred from Values

| Scenario | From | To | Meaning |
|----------|------|-----|---------|
| Property added | `null` | `"value"` | Added |
| Property removed | `"value"` | `null` | Removed |
| Property changed | `"old"` | `"new"` | Modified |

---

## Implementation Components

### 1. Entities

```
src/NOIR.Domain/Entities/
├── HttpRequestAuditLog.cs
├── HandlerAuditLog.cs
└── EntityAuditLog.cs
```

### 2. Interfaces

```csharp
// Commands that support DTO diff auditing
public interface IAuditableCommand
{
    object? GetTargetId();
    AuditOperationType OperationType { get; }
}

public interface IAuditableCommand<TDto> : IAuditableCommand { }

public enum AuditOperationType
{
    Create,   // No before state
    Update,   // Before → After diff
    Delete,   // Capture before state
    Query     // Optional
}
```

### 3. Services

```
src/NOIR.Application/Common/Interfaces/
├── IDiffService.cs           # JSON diff creation
└── IBeforeStateProvider.cs   # Fetch DTO before state

src/NOIR.Infrastructure/Services/
├── JsonDiffService.cs
└── BeforeStateProvider.cs
```

### 4. Middleware/Interceptors

```
src/NOIR.Infrastructure/
├── Middleware/
│   └── HttpRequestAuditMiddleware.cs   # HTTP level
├── Behaviors/
│   └── HandlerAuditMiddleware.cs       # Handler level (Wolverine)
└── Persistence/Interceptors/
    └── EntityAuditLogInterceptor.cs    # Entity level (EF Core)
```

### 5. Async Context

```
src/NOIR.Infrastructure/Audit/
└── AuditContext.cs   # AsyncLocal to pass IDs between layers
```

---

## Capturing "Before" State for DTO Diff

The key challenge is knowing what DTO to fetch before a handler modifies it.

### Solution: IAuditableCommand Interface

Commands declare their target DTO type and ID:

```csharp
public class UpdateCustomerCommand : IRequest<CustomerDto>, IAuditableCommand<CustomerDto>
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }

    // IAuditableCommand implementation
    public object? GetTargetId() => Id;
    public AuditOperationType OperationType => AuditOperationType.Update;
}
```

### Before State Provider

Fetches the DTO before the handler runs:

```csharp
public interface IBeforeStateProvider
{
    Task<object?> GetBeforeStateAsync(Type dtoType, object targetId, CancellationToken ct);
}
```

Registration:
```csharp
services.AddSingleton<IBeforeStateProvider>(sp =>
{
    var provider = new BeforeStateProvider(sp);

    // Register resolver for each auditable DTO type
    provider.Register<CustomerDto>(async (id, ct) =>
    {
        var bus = sp.GetRequiredService<IMessageBus>();
        return await bus.InvokeAsync<CustomerDto>(new GetCustomerQuery((Guid)id), ct);
    });

    return provider;
});
```

---

## Middleware Flow

```
1. HTTP Request arrives
   └─> HttpRequestAuditMiddleware.Before()
       └─> Creates HttpRequestAuditLog (Id = httpLogId)
       └─> Sets AuditContext.Current.HttpRequestAuditLogId = httpLogId

2. Handler invoked (e.g., UpdateCustomerCommand)
   └─> HandlerAuditMiddleware.Before()
       ├─> Check: command implements IAuditableCommand<TDto>?
       ├─> If Update/Delete: beforeState = BeforeStateProvider.GetBeforeStateAsync()
       ├─> Creates HandlerAuditLog (Id = handlerLogId, links to httpLogId)
       └─> Sets AuditContext.Current.HandlerAuditLogId = handlerLogId

3. Handler executes
   └─> Calls SaveChangesAsync()
       └─> EntityAuditLogInterceptor
           ├─> For each changed entity: create EntityAuditLog
           ├─> Calculate EntityDiff (RFC 6902)
           └─> Links to handlerLogId from AuditContext

4. Handler returns response (afterState)
   └─> HandlerAuditMiddleware.After()
       ├─> afterState = response DTO
       ├─> DtoDiff = DiffService.CreateDiff(beforeState, afterState)
       └─> Updates HandlerAuditLog with DtoDiff

5. HTTP Response sent
   └─> HttpRequestAuditMiddleware.After()
       └─> Updates HttpRequestAuditLog with response info
```

---

## Sensitive Data Handling

### Excluded Properties

```csharp
private static readonly HashSet<string> SensitiveProperties = new(StringComparer.OrdinalIgnoreCase)
{
    "Password", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
    "Secret", "Token", "ApiKey", "PrivateKey", "Salt", "RefreshToken",
    "CreditCard", "CVV", "SSN", "SocialSecurityNumber"
};
```

### Redaction in Diff

Properties matching sensitive patterns are redacted:

```json
[
  { "op": "replace", "path": "/email", "value": "new@x.com", "oldValue": "old@x.com" },
  { "op": "replace", "path": "/password", "value": "[REDACTED]", "oldValue": "[REDACTED]" }
]
```

---

## Querying Audit Data

### Get Complete Audit Trail

```csharp
public async Task<AuditTrailDto> GetAuditTrail(string correlationId)
{
    var httpLog = await _context.HttpRequestAuditLogs
        .FirstOrDefaultAsync(h => h.CorrelationId == correlationId);

    var handlerLogs = await _context.HandlerAuditLogs
        .Where(h => h.CorrelationId == correlationId)
        .OrderBy(h => h.StartTime)
        .ToListAsync();

    var entityLogs = await _context.EntityAuditLogs
        .Where(e => e.CorrelationId == correlationId)
        .OrderBy(e => e.Timestamp)
        .ToListAsync();

    return new AuditTrailDto
    {
        HttpRequest = MapToDto(httpLog),
        Handlers = handlerLogs.Select(h => new HandlerAuditDto
        {
            Id = h.Id,
            HandlerName = h.HandlerName,
            OperationType = h.OperationType,
            DtoDiff = ParsePatch(h.DtoDiff),
            DurationMs = h.DurationMs,
            IsSuccess = h.IsSuccess,
            EntityChanges = entityLogs
                .Where(e => e.HandlerAuditLogId == h.Id)
                .Select(MapEntityChange)
                .ToList()
        }).ToList()
    };
}
```

### Get Entity History

```csharp
public async Task<List<EntityAuditDto>> GetEntityHistory(string entityType, string entityId)
{
    return await _context.EntityAuditLogs
        .Where(e => e.EntityType == entityType && e.EntityId == entityId)
        .OrderByDescending(e => e.Timestamp)
        .Select(e => new EntityAuditDto
        {
            Operation = e.Operation,
            Diff = ParsePatch(e.EntityDiff),
            Timestamp = e.Timestamp,
            CorrelationId = e.CorrelationId
        })
        .ToListAsync();
}
```

---

## UI Visualization

### Activity Timeline Page

The Activity Timeline UI (`/portal/admin/activity-timeline`) provides a comprehensive view of all audited user actions.

**Features:**
- **Search**: Filter by correlation ID, user email, or handler name
- **Context Filter**: Filter by page context (Users, Roles, Tenants, etc.)
- **Action Filter**: Filter by operation type (Create, Update, Delete)
- **Date Range Picker**: Filter entries by date range (uses react-day-picker v9)
- **Failed Only Toggle**: Show only failed operations
- **User Filter**: Filter by specific user (via URL params or "View Activity" from Users page)
- **Entry Details Dialog**: Expandable entries showing HTTP, Handler, Database, and Raw tabs

**Navigation from Users Page:**
The Users page includes a "View Activity" option in each user's action menu. Clicking this navigates to the Activity Timeline with the user's ID pre-filtered, showing all audit entries for that specific user.

```
Users Page → Action Menu → "View Activity" → Activity Timeline?userId=xxx&userEmail=xxx
```

### React Components

Recommended libraries:
- [react-diff-viewer](https://github.com/praneshr/react-diff-viewer) - GitHub-style diff
- [git-diff-view](https://github.com/MrWangJustToDo/git-diff-view) - Full-featured diff view

### Diff Table View

```tsx
interface FieldChange {
  from: any;
  to: any;
}

type DiffData = Record<string, FieldChange>;

function DiffTable({ diff }: { diff: DiffData }) {
  const getOperation = (change: FieldChange) => {
    if (change.from === null && change.to !== null) return 'add';
    if (change.from !== null && change.to === null) return 'remove';
    return 'replace';
  };

  return (
    <table>
      <thead>
        <tr>
          <th>Operation</th>
          <th>Field</th>
          <th>Old Value</th>
          <th>New Value</th>
        </tr>
      </thead>
      <tbody>
        {Object.entries(diff).map(([field, change]) => {
          const op = getOperation(change);
          return (
            <tr key={field} className={`op-${op}`}>
              <td><Badge variant={op}>{op}</Badge></td>
              <td>{field}</td>
              <td className="text-red-600">
                {change.from !== null ? JSON.stringify(change.from) : '-'}
              </td>
              <td className="text-green-600">
                {change.to !== null ? JSON.stringify(change.to) : '-'}
              </td>
            </tr>
          );
        })}
      </tbody>
    </table>
  );
}
```

### Visual Example

```
┌────────────────────────────────────────────────────────────────────────┐
│  Audit Trail: abc-xyz-789                                              │
│  POST /api/customers/123 | 2026-01-01 14:30:22 | 127ms | 200 OK       │
├────────────────────────────────────────────────────────────────────────┤
│                                                                        │
│  ▼ UpdateCustomerCommand (45ms) ✓                                      │
│    ┌──────────────────────────────────────────────────────────────┐   │
│    │ DTO Changes (CustomerDto)                                     │   │
│    ├──────────┬─────────────┬─────────────────┬───────────────────┤   │
│    │ Operation│ Property    │ Old Value       │ New Value         │   │
│    ├──────────┼─────────────┼─────────────────┼───────────────────┤   │
│    │ replace  │ name        │ "Acme Corp"     │ "Acme Inc"        │   │
│    │ replace  │ email       │ "old@acme.com"  │ "new@acme.com"    │   │
│    │ add      │ phone       │ -               │ "+1-555-0123"     │   │
│    └──────────┴─────────────┴─────────────────┴───────────────────┘   │
│                                                                        │
│    ▼ Entity Changes                                                    │
│      • Customer (cust-123) Modified                                    │
│        - Name: "Acme Corp" → "Acme Inc"                               │
│        - Email: "old@acme.com" → "new@acme.com"                       │
│        - Phone: null → "+1-555-0123"                                  │
│        - ModifiedAt: → 2026-01-01T14:30:22Z                          │
│                                                                        │
│  ▼ SendNotificationCommand (12ms) ✓                                    │
│    DTO Changes: (Create - no before state)                            │
│                                                                        │
│    ▼ Entity Changes                                                    │
│      • Notification (notif-456) Added                                 │
│        + Type: "CustomerUpdated"                                      │
│        + RecipientId: "user-789"                                      │
│        + CreatedAt: 2026-01-01T14:30:22Z                             │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

## Implementation Checklist

### Core Features
- [x] Create domain entities (HttpRequestAuditLog, HandlerAuditLog, EntityAuditLog)
- [x] Create EF Core configurations
- [x] Create migration
- [x] Implement IDiffService with JsonDiffPatch
- [x] Implement IBeforeStateProvider
- [x] Implement AuditContext (AsyncLocal)
- [x] Implement HttpRequestAuditMiddleware
- [x] Implement HandlerAuditMiddleware (Wolverine)
- [x] Update EntityAuditLogInterceptor to use new schema
- [x] Create IAuditableCommand interface
- [x] Create audit query endpoints (GET /api/audit/*)

### Enhanced Features (NEW)
- [x] **Data Retention Policy** - Hangfire job with archive/delete lifecycle
- [x] **IBeforeStateProvider Implementation** - Wolverine-based DTO state resolution
- [x] **Audit Control Attributes** - `[Audited]`, `[DisableAuditing]`, `[AuditSensitive]`, `[DisableHandlerAuditing]`, `[AuditCollection]`
- [x] **CSV/JSON Export** - Compliance reporting via `/api/audit/export`
- [x] **Navigation Property Tracking** - Collection add/remove tracking

### Pending
- [x] Update existing commands to implement IAuditableCommand (see checklist below)
- [ ] Implement React diff viewer component

---

## Activity Timeline: IAuditableCommand Checklist

The Activity Timeline feature displays user actions grouped by page context. For actions to appear in the timeline, commands must:

1. **Implement `IAuditableCommand` or `IAuditableCommand<TResult>`**
2. **Set UserId in the endpoint** for audit tracking
3. **Add `usePageContext('PageName')` hook** in the frontend page component

### Pattern for Adding Audit Logging

**Step 1: Update Command**
```csharp
public sealed record MyCommand(
    string SomeProperty) : IAuditableCommand<MyResult>  // or just IAuditableCommand
{
    // UserId for audit tracking - set by endpoint
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public object? GetTargetId() => UserId;
    public AuditOperationType OperationType => AuditOperationType.Update; // or Create/Delete
    public string? GetTargetDisplayName() => SomeProperty; // Human-readable identifier
    public string? GetActionDescription() => $"Did something with '{SomeProperty}'";
}
```

**Step 2: Update Handler (if returning typed result)**
```csharp
public async Task<Result<MyResult>> Handle(MyCommand command, CancellationToken ct)
{
    // ... logic ...
    return Result.Success(new MyResult(true, "Success message"));
    // For failures: return Result.Failure<MyResult>(Error.xxx(...));
}
```

**Step 3: Update Endpoint**
```csharp
group.MapPost("/my-endpoint", async (
    MyCommand command,
    [FromServices] ICurrentUser currentUser,
    IMessageBus bus) =>
{
    // Set UserId for audit tracking
    var auditableCommand = command with { UserId = currentUser.UserId };
    var result = await bus.InvokeAsync<Result<MyResult>>(auditableCommand);
    return result.ToHttpResult();
});
```

**Step 4: Add Page Context in Frontend**
```tsx
import { usePageContext } from '@/hooks/usePageContext'

export default function MyPage() {
  usePageContext('MyPageName')  // This sets X-Page-Context header
  // ...
}
```

### Commands with IAuditableCommand (Complete Checklist)

#### Admin - User Management
| Command | Audited | Action Description |
|---------|---------|-------------------|
| `CreateUserCommand` | ✅ | "Created user '{DisplayName}'" |
| `UpdateUserCommand` | ✅ | "Updated user '{DisplayName}'" |
| `DeleteUserCommand` | ✅ | "Deleted user '{Email}'" |
| `LockUserCommand` | ✅ | "Locked/Unlocked user '{Email}'" |
| `AssignRolesToUserCommand` | ✅ | "Assigned {count} roles to user '{Email}'" |

#### Admin - Role Management
| Command | Audited | Action Description |
|---------|---------|-------------------|
| `CreateRoleCommand` | ✅ | "Created role '{Name}'" |
| `UpdateRoleCommand` | ✅ | "Updated role '{Name}'" |
| `DeleteRoleCommand` | ✅ | "Deleted role '{Name}'" |
| `AssignPermissionToRoleCommand` | ✅ | "Assigned {count} permissions to role '{Name}'" |
| `RemovePermissionFromRoleCommand` | ✅ | "Removed {count} permissions from role '{Name}'" |

#### Admin - Tenant Management
| Command | Audited | Action Description |
|---------|---------|-------------------|
| `CreateTenantCommand` | ✅ | "Created tenant '{Name}'" |
| `UpdateTenantCommand` | ✅ | "Updated tenant '{Name}'" |
| `DeleteTenantCommand` | ✅ | "Deleted tenant '{Name}'" |

#### Admin - Email Templates
| Command | Audited | Action Description |
|---------|---------|-------------------|
| `UpdateEmailTemplateCommand` | ✅ | "Updated email template '{TemplateName}'" |
| `SendTestEmailCommand` | ❌ | Not audited (test action) |

#### Profile/Settings (User's Own Data)
| Command | Audited | Action Description |
|---------|---------|-------------------|
| `UpdateUserProfileCommand` | ✅ | "Updated profile" |
| `ChangePasswordCommand` | ✅ | "Changed password" |
| `UploadAvatarCommand` | ✅ | "Uploaded avatar" |
| `DeleteAvatarCommand` | ✅ | "Deleted avatar" |
| `RequestEmailChangeCommand` | ✅ | "Requested email change to '{NewEmail}'" |
| `VerifyEmailChangeCommand` | ✅ | "Verified email change" |
| `ResendEmailChangeOtpCommand` | ✅ | "Resent email change OTP" |
| `RevokeSessionCommand` | ✅ | "Revoked session" |

#### Commands NOT Requiring Audit (Intentional)
| Command | Reason |
|---------|--------|
| `LoginCommand` | Already in HTTP audit logs |
| `LogoutCommand` | User's own action, tracked in HTTP audit |
| `RefreshTokenCommand` | Automatic token refresh |
| `MarkAsReadCommand` | Notification read, trivial action |
| `MarkAllAsReadCommand` | Notification read, trivial action |
| `DeleteNotificationCommand` | User's own notifications |
| `UpdatePreferencesCommand` | Notification preferences, trivial |
| Password Reset Commands | Anonymous/public, no user context |

### Frontend Page Context Mapping

For Activity Timeline to work, each page must call `usePageContext()`:

| Page | Context Value | Hook Call |
|------|--------------|-----------|
| Settings/Profile | `'Profile'` | `usePageContext('Profile')` |
| Users Management | `'Users'` | `usePageContext('Users')` |
| Roles Management | `'Roles'` | `usePageContext('Roles')` |
| Tenants Management | `'Tenants'` | `usePageContext('Tenants')` |
| Email Templates | `'EmailTemplates'` | `usePageContext('EmailTemplates')` |

### Activity Timeline Query Filter

The Activity Timeline only shows entries where `PageContext IS NOT NULL`. This ensures:
- Only user-initiated actions from the UI are shown
- API calls without page context (automated, scripts) are excluded
- Each entry links to a specific UI page for context

---

## Enhanced Features Documentation

### Data Retention Policy

Configuration in `appsettings.json`:
```json
{
  "AuditRetention": {
    "Enabled": true,
    "ArchiveAfterDays": 90,
    "DeleteAfterDays": 365,
    "BatchSize": 10000,
    "CronSchedule": "0 2 * * *",
    "EnableArchiving": true,
    "ExportBeforeDelete": false,
    "ExportPath": "audit-archives"
  }
}
```

### Audit Control Attributes

```csharp
// Disable auditing for entire entity
[DisableAuditing]
public class TempData : Entity<Guid> { }

// Disable auditing for specific property
public class Customer : Entity<Guid>
{
    [DisableAuditing]
    public string InternalNotes { get; set; }

    [AuditSensitive]  // Shows as "[REDACTED]" in audit logs
    public string TaxId { get; set; }
}

// Disable handler auditing
[DisableHandlerAuditing(Reason = "High-frequency health check")]
public class HealthCheckQuery { }

// Track collection changes
public class Customer : Entity<Guid>
{
    [AuditCollection(ChildDisplayProperty = "OrderNumber")]
    public ICollection<Order> Orders { get; set; }
}
```

### Export Endpoint

```
GET /api/audit/export?fromDate={date}&toDate={date}&entityType={type}&userId={id}&format=csv|json
```

Returns downloadable CSV or JSON file with audit records for compliance reporting

---

## Troubleshooting

### "No handler diff available" in Activity Timeline

**Symptom:** The Handler tab in Activity Timeline shows "No handler diff available" despite the command implementing `IAuditableCommand<TDto>`.

**Root Cause:** Missing before-state resolver registration in `DependencyInjection.cs`.

**Solution:**

For commands with `OperationType.Update`, you MUST register a before-state resolver to capture the entity state before modification:

```csharp
// In src/NOIR.Infrastructure/DependencyInjection.cs
services.AddBeforeStateResolver<YourDto, GetYourEntityQuery>(
    targetId => new GetYourEntityQuery(targetId));
```

**Example:**

```csharp
// For UpdateCustomerCommand that returns CustomerDto
services.AddBeforeStateResolver<CustomerDto, GetCustomerByIdQuery>(
    customerId => new GetCustomerByIdQuery((Guid)customerId));

// For UpdateTenantSettingsCommand that returns TenantSettingDto
services.AddBeforeStateResolver<TenantSettingDto, GetTenantSettingsQuery>(
    tenantId => new GetTenantSettingsQuery(tenantId?.ToString()));
```

**Verification:**
1. Check that resolver is registered in `DependencyInjection.cs`
2. Verify the query returns the same DTO type as the command result
3. Check that the query accepts the correct ID type from `GetTargetId()`
4. Test by updating an entity and viewing the Handler tab in Activity Timeline

**Cross-Reference:** See CLAUDE.md Critical Rule #13 for before-state resolver pattern.

---

## References

- [ABP Framework Audit Logging](https://abp.io/docs/latest/framework/infrastructure/audit-logging)
- [Wolverine Middleware](https://wolverinefx.net/guide/handlers/middleware)
- [react-diff-viewer](https://github.com/praneshr/react-diff-viewer)
- [git-diff-view](https://github.com/MrWangJustToDo/git-diff-view)

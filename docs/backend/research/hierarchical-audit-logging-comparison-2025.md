# Hierarchical Audit Logging Research Report

**Date:** 2026-01-01
**Scope:** HTTP → Handler → Entity audit logging comparison
**Status:** Research Complete

---

## Executive Summary

NOIR implements a **sophisticated 3-level hierarchical audit logging system** that surpasses many open-source alternatives. Our implementation captures HTTP requests, handler/command executions, and entity changes with proper correlation using AsyncLocal context. However, there are several enhancement opportunities identified through comparison with industry leaders like ABP Framework, Audit.NET, and Skoruba.AuditLogging.

### Comparison Matrix

| Feature | NOIR | ABP Framework | Audit.NET | Skoruba |
|---------|------|---------------|-----------|---------|
| HTTP Request Logging | ✅ Full | ✅ Full | ✅ Via Extension | ✅ Basic |
| Handler/Action Logging | ✅ Wolverine Middleware | ✅ App Service Methods | ✅ Via Extension | ❌ None |
| Entity Change Logging | ✅ EF Core Interceptor | ✅ EF Core Only | ✅ Multi-DB | ❌ None |
| Correlation ID | ✅ AsyncLocal | ✅ Per Request | ✅ AuditScope | ✅ TraceIdentifier |
| DTO Diff (Before/After) | ✅ RFC 6902 + oldValue | ❌ None | ❌ None | ❌ None |
| Entity Diff | ✅ RFC 6902 + oldValue | ✅ Property Changes | ✅ Customizable | ❌ None |
| Sensitive Data Redaction | ✅ Pattern-based | ✅ Attribute-based | ✅ Configurable | ✅ Basic |
| Nested Object Diff | ✅ Recursive | ❌ Flat Properties | ✅ Configurable | ❌ None |
| Multi-Tenancy | ✅ Finbuckle | ✅ Built-in | ⚠️ Manual | ❌ None |
| Query Endpoints | ✅ Paginated + Filters | ✅ Full CRUD + Export | ⚠️ Store-based | ⚠️ Basic |
| Data Retention/Archival | ❌ Not Implemented | ⚠️ Manual Cleanup | ✅ Configurable | ❌ None |
| MongoDB Support | ❌ EF Core Only | ⚠️ No Entity Changes | ✅ Full | ✅ Full |
| Export to Excel/CSV | ❌ Not Implemented | ✅ Pro Feature | ❌ None | ❌ None |
| Real-time Streaming | ❌ Not Implemented | ❌ None | ✅ Multiple Sinks | ❌ None |

---

## Detailed Analysis

### 1. What NOIR Does Well (Strengths)

#### 1.1 Hierarchical Correlation
```
HTTP Request (CorrelationId: abc-123)
  └── Handler 1 (HttpRequestAuditLogId → parent)
  │     └── Entity Change 1 (HandlerAuditLogId → parent)
  │     └── Entity Change 2
  └── Handler 2
        └── Entity Change 3
```

**NOIR's AsyncLocal-based `AuditContext` elegantly passes IDs across async boundaries:**
```csharp
// AuditContext.cs - Clean ambient context pattern
using var scope = AuditContext.BeginRequestScope(httpLogId, correlationId);
AuditContext.SetCurrentHandler(handlerLogId);
// EntityAuditLogInterceptor reads from AuditContext.Current
```

**ABP Framework** uses a similar pattern but requires explicit scope management. **Audit.NET** uses `AuditScope` but doesn't have built-in HTTP-to-Entity linking.

#### 1.2 DTO Diff Tracking (Unique to NOIR)
NOIR captures **both DTO-level and Entity-level diffs**, which is rare:
- **DTO Diff**: What the user saw/submitted (UI perspective)
- **Entity Diff**: What changed in the database (storage perspective)

No other framework reviewed captures DTO-level before/after state automatically.

#### 1.3 RFC 6902 JSON Patch with oldValue Extension
```json
[
  { "op": "replace", "path": "/name", "value": "New", "oldValue": "Old" },
  { "op": "add", "path": "/phone", "value": "+1-555" }
]
```

While RFC 6902 doesn't define `oldValue`, NOIR's extension is practical for audit UIs. ABP stores changes in separate `EntityPropertyChange` records; Audit.NET stores full before/after objects.

#### 1.4 Wolverine Middleware Integration
NOIR's `HandlerAuditMiddleware` integrates seamlessly with Wolverine's middleware chain:
```csharp
// Before() → captures input, fetches before state
// After() → captures output, calculates diff
// Finally() → handles exceptions, always clears context
```

This is cleaner than ABP's attribute-based approach or Audit.NET's decorator pattern.

#### 1.5 Sensitive Data Handling
Both HTTP body and entity properties are sanitized using pattern-based redaction:
```csharp
private static readonly HashSet<string> SensitiveProperties = new()
{
    "Password", "PasswordHash", "Token", "ApiKey", ...
};
```

---

### 2. What's Missing (Gaps)

#### 2.1 Data Retention & Archival (HIGH PRIORITY)
**Current State:** No automatic cleanup or archival strategy.

**Industry Best Practices:**
- Hot/Cold storage tiering (90 days hot, 1 year cold, 7 years archive)
- Compression for older records
- Partitioning by date for efficient cleanup
- Configurable retention per log type

**ABP Pro:** Manual cleanup commands
**Audit.NET:** Configurable retention per data provider
**Microsoft Purview:** Up to 10 years with automated policies

**Recommendation:**
```csharp
public class AuditRetentionSettings
{
    public int HotStorageDays { get; set; } = 90;
    public int WarmStorageDays { get; set; } = 365;
    public int ArchiveYears { get; set; } = 7;
    public bool EnableCompression { get; set; } = true;
}
```

#### 2.2 Attribute-Based Audit Control (MEDIUM PRIORITY)
**Current State:** All entities are audited unless explicitly excluded by type name.

**ABP Framework Pattern:**
```csharp
[Audited]
public class Customer : Entity { }

[DisableAuditing]
public string CacheKey { get; set; }

[DisableAuditingForEntity]
public class TempData : Entity { }
```

**Recommendation:** Add `[Audited]` and `[DisableAuditing]` attributes for fine-grained control.

#### 2.3 Navigation Property Change Tracking (LOW-MEDIUM PRIORITY)
**Current State:** Only scalar properties are tracked.

**ABP Framework:** `SaveEntityHistoryWhenNavigationChanges` option tracks collection changes.

**Recommendation:** Add option to track navigation property additions/removals:
```json
{ "op": "add", "path": "/orders/-", "value": { "id": "order-123" } }
```

#### 2.4 Export Functionality (MEDIUM PRIORITY)
**Current State:** Only JSON API endpoints.

**ABP Pro:** Excel export with background job support.

**Recommendation:** Add Excel/CSV export for compliance reporting:
```csharp
group.MapGet("/export", async (ExportFormat format, IMessageBus bus) => ...);
```

#### 2.5 Real-time Streaming (LOW PRIORITY)
**Current State:** Database storage only.

**Audit.NET:** Supports multiple sinks (Elasticsearch, Azure Event Hub, Kafka, etc.)

**Recommendation:** Consider `IMultiSinkAuditLogger` interface for future extensibility.

#### 2.6 IBeforeStateProvider Not Registered (IMPLEMENTATION GAP)
**Current State:** `IBeforeStateProvider` interface exists but has no default implementation.

**Impact:** DTO diff is always null for Update operations unless manually registered.

**Recommendation:** Implement a generic `WolverineBeforeStateProvider` that uses message bus to fetch DTOs.

#### 2.7 Composite Primary Key Support (EDGE CASE)
**Current State:** `GetPrimaryKey()` only returns the first primary key property.

**Recommendation:** Handle composite keys by joining key values.

---

### 3. Comparison with Industry Leaders

#### 3.1 ABP Framework
**Architecture:**
```
AuditLogInfo (Root)
├── AuditLogActionInfo[] (Service/Controller calls)
├── EntityChangeInfo[] (Entity modifications)
│   └── EntityPropertyChangeInfo[] (Property-level)
└── ExceptionInfo[] (Error details)
```

**Strengths over NOIR:**
- Attribute-based control (`[Audited]`, `[DisableAuditing]`)
- Entity history selectors for opt-in entity tracking
- Built-in UI module (ABP Pro)
- Exception aggregation at request level

**NOIR Advantages:**
- RFC 6902 diff format (interoperable)
- DTO-level diff (ABP only tracks entities)
- Wolverine-native (vs. ABP's custom middleware)
- `oldValue` extension for better UI display

#### 3.2 Audit.NET
**Architecture:**
```
AuditScope
├── Environment (user, machine, caller)
├── Event (action details)
└── Target (entity changes)
```

**Strengths over NOIR:**
- Multi-provider support (EF, MongoDB, Cosmos, etc.)
- Multiple output sinks (file, DB, Elasticsearch, cloud)
- MediatR integration (for MediatR users)
- Transaction grouping

**NOIR Advantages:**
- Hierarchical HTTP → Handler → Entity linking
- Built-in multi-tenancy via Finbuckle
- Cleaner async context flow

#### 3.3 Skoruba.AuditLogging
**Strengths:**
- Simple HTTP-level auditing
- Easy to set up

**Weaknesses:**
- No entity change tracking
- No handler-level auditing
- Basic compared to NOIR

---

### 4. Enhancement Recommendations (Prioritized)

#### Priority 1: Critical (Should implement soon)
| Enhancement | Effort | Impact | Description |
|-------------|--------|--------|-------------|
| Data Retention Policy | Medium | High | Hangfire job to archive/delete old logs |
| IBeforeStateProvider Impl | Low | High | Enable DTO diff for update operations |
| Attribute-Based Control | Low | Medium | `[Audited]`, `[DisableAuditing]` attributes |

#### Priority 2: Important (Near-term backlog)
| Enhancement | Effort | Impact | Description |
|-------------|--------|--------|-------------|
| Excel/CSV Export | Medium | Medium | Compliance reporting |
| Exception Aggregation | Low | Medium | Link exceptions to HTTP audit log |
| Composite Key Support | Low | Low | Handle entities with composite PKs |
| Navigation Changes | Medium | Low | Track collection modifications |

#### Priority 3: Nice-to-Have (Long-term)
| Enhancement | Effort | Impact | Description |
|-------------|--------|--------|-------------|
| Multi-Sink Support | High | Medium | Elasticsearch, cloud storage options |
| UI Admin Module | High | Medium | React admin panel for audit browsing |
| Performance Optimization | Medium | Medium | Async batch writes, reduced allocations |
| Audit Log Search | Medium | Medium | Full-text search on audit data |

---

### 5. Proposed Schema Enhancements

#### 5.1 Add Exception Tracking to HttpRequestAuditLog
```sql
ALTER TABLE HttpRequestAuditLogs ADD
    ExceptionType NVARCHAR(200) NULL,
    ExceptionMessage NVARCHAR(MAX) NULL,
    ExceptionStackTrace NVARCHAR(MAX) NULL;
```

#### 5.2 Add Partitioning for Data Retention
```sql
-- Partition by month for efficient archival
CREATE PARTITION FUNCTION pf_AuditDate (DATETIMEOFFSET)
AS RANGE RIGHT FOR VALUES (
    '2026-01-01', '2026-02-01', '2026-03-01', ...
);
```

#### 5.3 Add Audit Metadata Table
```sql
CREATE TABLE AuditMetadata (
    Id INT PRIMARY KEY IDENTITY,
    EntityType NVARCHAR(200) NOT NULL,
    IsAuditEnabled BIT NOT NULL DEFAULT 1,
    RetentionDays INT NULL,
    ExcludedProperties NVARCHAR(MAX) NULL -- JSON array
);
```

---

### 6. Code Examples for Key Enhancements

#### 6.1 Data Retention Job
```csharp
public class AuditRetentionJob : IBackgroundJob
{
    public async Task ExecuteAsync(IServiceProvider sp)
    {
        var settings = sp.GetRequiredService<IOptions<AuditRetentionSettings>>().Value;
        var db = sp.GetRequiredService<ApplicationDbContext>();

        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-settings.HotStorageDays);

        // Archive to cold storage
        await ArchiveOldLogsAsync(db, cutoffDate);

        // Delete very old logs
        var deleteCutoff = DateTimeOffset.UtcNow.AddYears(-settings.ArchiveYears);
        await db.EntityAuditLogs
            .Where(e => e.Timestamp < deleteCutoff)
            .ExecuteDeleteAsync();
    }
}
```

#### 6.2 Attribute-Based Control
```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class DisableAuditingAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class)]
public class AuditedAttribute : Attribute
{
    public bool IncludeNavigations { get; set; }
}

// In EntityAuditLogInterceptor:
if (entityType.GetCustomAttribute<DisableAuditingAttribute>() is not null)
    continue;
```

#### 6.3 IBeforeStateProvider Implementation
```csharp
public class WolverineBeforeStateProvider : IBeforeStateProvider
{
    private readonly IMessageBus _bus;
    private readonly Dictionary<Type, Func<object, CancellationToken, Task<object?>>> _resolvers = new();

    public void Register<TDto, TQuery>(Func<object, TQuery> queryFactory)
        where TQuery : class
    {
        _resolvers[typeof(TDto)] = async (id, ct) =>
        {
            var query = queryFactory(id);
            return await _bus.InvokeAsync<TDto>(query, ct);
        };
    }

    public async Task<object?> GetBeforeStateAsync(Type dtoType, object targetId, CancellationToken ct)
    {
        if (_resolvers.TryGetValue(dtoType, out var resolver))
        {
            return await resolver(targetId, ct);
        }
        return null;
    }
}
```

---

## Sources

### GitHub Repositories
- [ABP Framework - Audit Logging Module](https://github.com/abpframework/abp/tree/dev/modules/audit-logging)
- [Audit.NET](https://github.com/thepirat000/Audit.NET)
- [Skoruba.AuditLogging](https://github.com/skoruba/AuditLogging)

### Documentation
- [ABP Audit Logging Documentation](https://abp.io/docs/latest/framework/infrastructure/audit-logging)
- [Audit.NET Entity Framework README](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.EntityFramework/README.md)
- [Audit.NET WebAPI README](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.WebApi/README.md)
- [RFC 6902 - JSON Patch](https://datatracker.ietf.org/doc/html/rfc6902)

### Best Practice Articles
- [EF Core Interceptors Guide - Code Maze](https://code-maze.com/efcore-interceptors/)
- [Audit Trail in ASP.NET Core - CodeWithMukesh](https://codewithmukesh.com/blog/audit-trail-implementation-in-aspnet-core/)
- [SaveChanges Interception for Auditing](https://www.woodruff.dev/tracking-every-change-using-savechanges-interception-for-ef-core-auditing/)
- [Clean Architecture with CQRS](https://www.apriorit.com/dev-blog/783-web-clean-architecture-and-cqrs-in-net-core-apps)

### Data Retention & Compliance
- [Security Log Retention Best Practices - ManageEngine](https://www.manageengine.com/products/active-directory-audit/kb/best-practices/security-log-retention-best-practices.html)
- [Audit Log Retention Policies - Microsoft Purview](https://learn.microsoft.com/en-us/purview/audit-log-retention-policies)
- [Log Retention Best Practices - AuditBoard](https://auditboard.com/blog/security-log-retention-best-practices-guide)

---

## Conclusion

**NOIR's hierarchical audit logging is already industry-leading** in several aspects:
1. True HTTP → Handler → Entity correlation
2. DTO-level diff tracking (unique)
3. RFC 6902 with oldValue extension (practical for UIs)
4. Clean Wolverine middleware integration

**Key gaps to address:**
1. Data retention/archival (critical for compliance)
2. IBeforeStateProvider implementation (enables DTO diff)
3. Attribute-based audit control (developer ergonomics)

The architecture is sound and extensible. The recommended enhancements build on existing patterns rather than requiring redesign.

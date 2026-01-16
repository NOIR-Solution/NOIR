# JSON Enum Serialization Pattern

## Overview

All C# enums are serialized as **strings** (not integers) for JavaScript/TypeScript compatibility. This convention applies across all API communication channels.

## Configuration

### 1. HTTP Endpoints (REST API)

Location: `src/NOIR.Web/Program.cs` (line ~126)

```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    // Serialize enums as strings for JavaScript compatibility
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
```

### 2. SignalR (Real-time Communication)

Location: `src/NOIR.Web/Program.cs` (line ~176)

```csharp
builder.Services.AddSignalR(options =>
{
    // ... other options
}).AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
```

### 3. JSON Source Generator (AOT/Performance)

Location: `src/NOIR.Web/Json/AppJsonSerializerContext.cs`

```csharp
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
    UseStringEnumConverter = true)]  // Ensures string enums in generated code
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
```

## API Response Format

### Correct (String Enums)

```json
{
  "type": "Success",
  "category": "Security",
  "level": "Information"
}
```

### Incorrect (Numeric Enums)

```json
{
  "type": 1,
  "category": 3,
  "level": 2
}
```

## Frontend TypeScript Types

Define enums as union types matching the C# enum member names:

```typescript
// Matches C# NotificationType enum
export type NotificationType = 'info' | 'success' | 'warning' | 'error'

// Matches C# DevLogLevel enum
export type DevLogLevel = 'Verbose' | 'Debug' | 'Information' | 'Warning' | 'Error' | 'Fatal'

// Matches C# EmailFrequency enum
export type EmailFrequency = 'None' | 'Instant' | 'Daily' | 'Weekly'
```

## Defensive Mapping (Optional)

For backward compatibility during migration, frontend code can handle both formats:

```typescript
export function mapEnumValue<T extends string>(
  value: number | string,
  numericMap: Record<number, T>,
  defaultValue: T
): T {
  if (typeof value === 'number') {
    return numericMap[value] ?? defaultValue
  }
  return value as T
}
```

## Common Enums

| C# Enum | TypeScript Type | Example Values |
|---------|-----------------|----------------|
| `NotificationType` | `NotificationType` | `'info'`, `'success'`, `'warning'`, `'error'` |
| `NotificationCategory` | `NotificationCategory` | `'system'`, `'userAction'`, `'workflow'`, `'security'` |
| `EmailFrequency` | `EmailFrequency` | `'None'`, `'Instant'`, `'Daily'`, `'Weekly'` |
| `DevLogLevel` | `DevLogLevel` | `'Verbose'`, `'Debug'`, `'Information'`, `'Warning'`, `'Error'`, `'Fatal'` |
| `AuditOperationType` | `AuditOperationType` | `'Create'`, `'Update'`, `'Delete'` |

## Why String Enums?

1. **JavaScript Compatibility**: TypeScript/JavaScript works more naturally with string literals
2. **Self-Documenting**: API responses are human-readable without lookup tables
3. **Refactoring Safety**: Adding enum members doesn't shift numeric values
4. **Debugging**: Easier to inspect network traffic and logs
5. **Consistency**: Same format across REST and SignalR channels

## Related Files

- `src/NOIR.Web/Program.cs` - HTTP and SignalR JSON configuration
- `src/NOIR.Web/Json/AppJsonSerializerContext.cs` - Source generator configuration
- `src/NOIR.Web/frontend/src/types/notification.ts` - Frontend type definitions
- `src/NOIR.Application/Features/DeveloperLogs/DTOs/LogEntryDto.cs` - DevLogLevel enum

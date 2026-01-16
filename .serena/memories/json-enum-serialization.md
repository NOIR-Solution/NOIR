# JSON Enum Serialization Convention

## Quick Reference

All C# enums in NOIR are serialized as **strings** for JavaScript compatibility.

## Configuration Locations

1. **HTTP JSON** - `src/NOIR.Web/Program.cs:126`
   - `JsonStringEnumConverter` added to `ConfigureHttpJsonOptions`

2. **SignalR JSON** - `src/NOIR.Web/Program.cs:176`
   - `JsonStringEnumConverter` added to `AddJsonProtocol`

3. **Source Generator** - `src/NOIR.Web/Json/AppJsonSerializerContext.cs`
   - `UseStringEnumConverter = true` in `JsonSourceGenerationOptions`

## When Adding New Enums

1. Define enum in Domain/Application layer as usual
2. Frontend TypeScript type should use union of string literals matching C# names
3. No special configuration needed - string serialization is automatic

## Example

```csharp
// C# enum
public enum Status { Active, Inactive, Pending }
```

```typescript
// TypeScript type
type Status = 'Active' | 'Inactive' | 'Pending'
```

```json
// API response
{ "status": "Active" }
```

## Documentation

Full documentation: `docs/backend/patterns/json-enum-serialization.md`

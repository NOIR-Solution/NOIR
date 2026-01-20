namespace NOIR.Infrastructure.Services;

/// <summary>
/// Service for creating simple field-level diffs for audit logging.
/// Output format: { "fieldName": { "from": oldValue, "to": newValue } }
/// </summary>
public class JsonDiffService : IDiffService, IScopedService
{
    /// <summary>
    /// System fields that should be excluded from diff comparison.
    /// These are auto-managed fields that change on every update but don't represent
    /// meaningful user-initiated changes.
    /// </summary>
    private static readonly HashSet<string> ExcludedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        // Common audit fields
        "modifiedAt",
        "modifiedBy",
        "createdAt",
        "createdBy",
        "lastModifiedAt",
        "lastModifiedBy",
        "updatedAt",
        "updatedBy",

        // Version/concurrency fields
        "rowVersion",
        "version",
        "concurrencyStamp",
        "securityStamp",

        // Identity timestamps
        "lockoutEnd",
        "lastLoginAt",
        "lastActivityAt",
        "passwordChangedAt"
    };

    /// <summary>
    /// Options for serializing input objects (ignores nulls for cleaner comparison).
    /// </summary>
    private static readonly JsonSerializerOptions InputSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Options for serializing diff output (includes nulls to show from/to null changes clearly).
    /// </summary>
    private static readonly JsonSerializerOptions DiffSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <inheritdoc />
    public string? CreateDiff<T>(T? before, T? after) where T : class
    {
        var beforeJson = before is null ? null : JsonSerializer.Serialize(before, InputSerializerOptions);
        var afterJson = after is null ? null : JsonSerializer.Serialize(after, InputSerializerOptions);

        return CreateDiffFromJson(beforeJson, afterJson);
    }

    /// <inheritdoc />
    public string? CreateDiffFromJson(string? beforeJson, string? afterJson)
    {
        // Handle null cases
        if (beforeJson is null && afterJson is null)
            return null;

        // Parse to JsonNode
        var beforeNode = beforeJson is null ? null : JsonNode.Parse(beforeJson);
        var afterNode = afterJson is null ? null : JsonNode.Parse(afterJson);

        // Create simple field-level diff
        var changes = CreateFieldLevelDiff(beforeNode, afterNode);

        if (changes.Count == 0)
            return null;

        return JsonSerializer.Serialize(changes, DiffSerializerOptions);
    }

    /// <inheritdoc />
    public string? CreateDiffFromDictionaries(
        IReadOnlyDictionary<string, object?>? before,
        IReadOnlyDictionary<string, object?>? after)
    {
        // Handle null cases
        if (before is null && after is null)
            return null;

        var changes = new Dictionary<string, FieldChange>();

        var allKeys = new HashSet<string>();
        if (before is not null)
            allKeys.UnionWith(before.Keys);
        if (after is not null)
            allKeys.UnionWith(after.Keys);

        foreach (var key in allKeys)
        {
            // Skip system/audit fields that are auto-managed
            if (ExcludedFields.Contains(key))
                continue;

            object? beforeValue = null;
            object? afterValue = null;
            var hadBefore = before?.TryGetValue(key, out beforeValue) ?? false;
            var hasAfter = after?.TryGetValue(key, out afterValue) ?? false;

            if (hadBefore && hasAfter)
            {
                // Both exist - check if changed
                if (!AreEqual(beforeValue, afterValue))
                {
                    changes[key] = new FieldChange { From = beforeValue, To = afterValue };
                }
            }
            else if (hadBefore && !hasAfter)
            {
                // Removed (skip if beforeValue was null - no meaningful change)
                if (beforeValue is not null)
                {
                    changes[key] = new FieldChange { From = beforeValue, To = null };
                }
            }
            else if (!hadBefore && hasAfter)
            {
                // Added (skip if afterValue is null - no meaningful change)
                if (afterValue is not null)
                {
                    changes[key] = new FieldChange { From = null, To = afterValue };
                }
            }
        }

        if (changes.Count == 0)
            return null;

        return JsonSerializer.Serialize(changes, DiffSerializerOptions);
    }

    /// <summary>
    /// Creates a simple field-level diff between two JSON nodes.
    /// </summary>
    private static Dictionary<string, FieldChange> CreateFieldLevelDiff(JsonNode? before, JsonNode? after)
    {
        var changes = new Dictionary<string, FieldChange>();

        if (before is null && after is not null)
        {
            // Full create - show all fields as added (except system fields and null values)
            if (after is JsonObject afterObj)
            {
                foreach (var prop in afterObj)
                {
                    // Skip system/audit fields
                    if (ExcludedFields.Contains(prop.Key))
                        continue;

                    var toValue = ConvertNodeToObject(prop.Value);

                    // Skip properties that are null - no point showing null → null as a change
                    if (toValue is null)
                        continue;

                    changes[prop.Key] = new FieldChange
                    {
                        From = null,
                        To = toValue
                    };
                }
            }
            return changes;
        }

        if (before is not null && after is null)
        {
            // Full delete - show all fields as removed (except system fields)
            if (before is JsonObject beforeObj)
            {
                foreach (var prop in beforeObj)
                {
                    // Skip system/audit fields
                    if (ExcludedFields.Contains(prop.Key))
                        continue;

                    changes[prop.Key] = new FieldChange
                    {
                        From = ConvertNodeToObject(prop.Value),
                        To = null
                    };
                }
            }
            return changes;
        }

        if (before is JsonObject beforeObject && after is JsonObject afterObject)
        {
            CompareObjects(beforeObject, afterObject, "", changes);
        }

        return changes;
    }

    // Maximum recursion depth to prevent stack overflow on deeply nested objects
    private const int MaxRecursionDepth = 32;

    /// <summary>
    /// Recursively compares two JSON objects and generates field-level changes.
    /// Uses dot notation for nested paths (e.g., "address.city").
    /// </summary>
    private static void CompareObjects(
        JsonObject before,
        JsonObject after,
        string basePath,
        Dictionary<string, FieldChange> changes,
        int depth = 0)
    {
        // Prevent stack overflow on deeply nested objects
        if (depth >= MaxRecursionDepth)
        {
            // At max depth, treat the whole object as a single change
            if (!JsonNode.DeepEquals(before, after))
            {
                var fieldName = string.IsNullOrEmpty(basePath) ? "_root" : basePath;
                changes[fieldName] = new FieldChange
                {
                    From = ConvertNodeToObject(before),
                    To = ConvertNodeToObject(after)
                };
            }
            return;
        }

        var allKeys = new HashSet<string>();
        foreach (var prop in before)
            allKeys.Add(prop.Key);
        foreach (var prop in after)
            allKeys.Add(prop.Key);

        foreach (var key in allKeys)
        {
            // Skip system/audit fields that are auto-managed
            if (ExcludedFields.Contains(key))
                continue;

            // Use dot notation for nested paths
            var fieldName = string.IsNullOrEmpty(basePath) ? key : $"{basePath}.{key}";
            var hasBefore = before.TryGetPropertyValue(key, out var beforeValue);
            var hasAfter = after.TryGetPropertyValue(key, out var afterValue);

            if (hasBefore && hasAfter)
            {
                // Both exist - compare
                if (beforeValue is JsonObject beforeNestedObj && afterValue is JsonObject afterNestedObj)
                {
                    // Recurse into nested objects with incremented depth
                    CompareObjects(beforeNestedObj, afterNestedObj, fieldName, changes, depth + 1);
                }
                else if (!JsonNode.DeepEquals(beforeValue, afterValue))
                {
                    changes[fieldName] = new FieldChange
                    {
                        From = ConvertNodeToObject(beforeValue),
                        To = ConvertNodeToObject(afterValue)
                    };
                }
            }
            else if (hasBefore && !hasAfter)
            {
                var fromValue = ConvertNodeToObject(beforeValue);
                // Skip if beforeValue was null - no meaningful change
                if (fromValue is not null)
                {
                    changes[fieldName] = new FieldChange
                    {
                        From = fromValue,
                        To = null
                    };
                }
            }
            else if (!hasBefore && hasAfter)
            {
                var toValue = ConvertNodeToObject(afterValue);
                // Skip if afterValue is null - no meaningful change
                if (toValue is not null)
                {
                    changes[fieldName] = new FieldChange
                    {
                        From = null,
                        To = toValue
                    };
                }
            }
        }
    }

    /// <summary>
    /// Converts a JsonNode to a regular object for serialization.
    /// </summary>
    private static object? ConvertNodeToObject(JsonNode? node)
    {
        if (node is null)
            return null;

        return node.Deserialize<object>(InputSerializerOptions);
    }

    /// <summary>
    /// Compares two values for equality.
    /// </summary>
    private static bool AreEqual(object? a, object? b)
    {
        if (a is null && b is null)
            return true;
        if (a is null || b is null)
            return false;

        // For complex objects, serialize and compare JSON
        var aJson = JsonSerializer.Serialize(a, InputSerializerOptions);
        var bJson = JsonSerializer.Serialize(b, InputSerializerOptions);

        return aJson == bJson;
    }

    /// <summary>
    /// Represents a field-level change with from/to values.
    /// </summary>
    private sealed class FieldChange
    {
        public object? From { get; set; }
        public object? To { get; set; }
    }
}

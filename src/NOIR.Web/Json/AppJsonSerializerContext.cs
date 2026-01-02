namespace NOIR.Web.Json;

/// <summary>
/// JSON Source Generator context for improved serialization performance.
/// Add types that are frequently serialized/deserialized here.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
// Auth DTOs
[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(CurrentUserDto))]
// Auth Commands
[JsonSerializable(typeof(RegisterCommand))]
[JsonSerializable(typeof(LoginCommand))]
[JsonSerializable(typeof(RefreshTokenCommand))]
[JsonSerializable(typeof(UpdateUserProfileCommand))]
// User DTOs
[JsonSerializable(typeof(UserProfileDto))]
[JsonSerializable(typeof(Result<UserProfileDto>))]
// Audit DTOs
[JsonSerializable(typeof(HttpRequestAuditDto))]
[JsonSerializable(typeof(HttpRequestAuditDetailDto))]
[JsonSerializable(typeof(HandlerAuditDto))]
[JsonSerializable(typeof(EntityAuditDto))]
[JsonSerializable(typeof(AuditTrailDto))]
[JsonSerializable(typeof(EntityHistoryDto))]
[JsonSerializable(typeof(EntityHistoryEntryDto))]
// Audit Result types
[JsonSerializable(typeof(Result<AuditTrailDto>))]
[JsonSerializable(typeof(Result<EntityHistoryDto>))]
[JsonSerializable(typeof(Result<PaginatedList<HttpRequestAuditDto>>))]
[JsonSerializable(typeof(Result<PaginatedList<HandlerAuditDto>>))]
[JsonSerializable(typeof(PaginatedList<HttpRequestAuditDto>))]
[JsonSerializable(typeof(PaginatedList<HandlerAuditDto>))]
// Result types
[JsonSerializable(typeof(Result<AuthResponse>))]
[JsonSerializable(typeof(Result<CurrentUserDto>))]
// Error types
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(ValidationProblemDetails))]
// Collection types
[JsonSerializable(typeof(Dictionary<string, string[]>))]
[JsonSerializable(typeof(IEnumerable<string>))]
[JsonSerializable(typeof(string[]))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}

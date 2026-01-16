namespace NOIR.Web.Json;

/// <summary>
/// JSON Source Generator context for improved serialization performance.
/// Add types that are frequently serialized/deserialized here.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
    UseStringEnumConverter = true)]
// Auth DTOs
[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(CurrentUserDto))]
// Auth Commands
[JsonSerializable(typeof(LoginCommand))]
[JsonSerializable(typeof(RefreshTokenCommand))]
[JsonSerializable(typeof(UpdateUserProfileCommand))]
// User DTOs
[JsonSerializable(typeof(UserProfileDto))]
[JsonSerializable(typeof(Result<UserProfileDto>))]
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

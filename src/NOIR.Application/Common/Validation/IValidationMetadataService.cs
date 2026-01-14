namespace NOIR.Application.Common.Validation;

/// <summary>
/// Service for extracting validation metadata from FluentValidation validators.
/// Used by:
/// - OpenAPI schema transformer (Scalar docs)
/// - Validation metadata endpoint (frontend codegen)
/// </summary>
public interface IValidationMetadataService
{
    /// <summary>
    /// Gets metadata for all registered validators
    /// </summary>
    IReadOnlyList<ValidatorMetadata> GetAllValidatorMetadata();

    /// <summary>
    /// Gets metadata for a specific command validator by command name
    /// </summary>
    /// <param name="commandName">The command class name (e.g., "CreateTenantCommand")</param>
    ValidatorMetadata? GetValidatorMetadata(string commandName);

    /// <summary>
    /// Gets metadata for validators matching a filter predicate
    /// </summary>
    /// <param name="filter">Predicate to filter command names</param>
    IReadOnlyList<ValidatorMetadata> GetValidatorMetadata(Func<string, bool> filter);

    /// <summary>
    /// Gets validation rules for a specific type (used by OpenAPI transformer)
    /// </summary>
    /// <param name="commandType">The command type</param>
    ValidatorMetadata? GetValidatorMetadataForType(Type commandType);
}

namespace NOIR.Domain.Entities.Crm;

/// <summary>
/// Defines the type of a pipeline stage.
/// Active stages are user-configurable; Won and Lost are system stages.
/// </summary>
public enum StageType
{
    Active = 0,
    Won = 1,
    Lost = 2,
}

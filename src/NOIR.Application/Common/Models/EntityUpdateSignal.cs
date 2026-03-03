namespace NOIR.Application.Common.Models;

public record EntityUpdateSignal(
    string EntityType,
    string EntityId,
    EntityOperation Operation,
    DateTimeOffset UpdatedAt);

public enum EntityOperation
{
    Created,
    Updated,
    Deleted
}

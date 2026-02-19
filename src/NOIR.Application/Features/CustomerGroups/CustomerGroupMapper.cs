namespace NOIR.Application.Features.CustomerGroups;

/// <summary>
/// Mapper for CustomerGroup entity to DTOs.
/// </summary>
public static class CustomerGroupMapper
{
    /// <summary>
    /// Maps a CustomerGroup entity to a full CustomerGroupDto.
    /// </summary>
    public static CustomerGroupDto ToDto(CustomerGroup group) => new(
        group.Id,
        group.Name,
        group.Description,
        group.Slug,
        group.IsActive,
        group.MemberCount,
        group.CreatedAt,
        group.ModifiedAt);

    /// <summary>
    /// Maps a CustomerGroup entity to a CustomerGroupListDto.
    /// </summary>
    public static CustomerGroupListDto ToListDto(CustomerGroup group) => new(
        group.Id,
        group.Name,
        group.Slug,
        group.IsActive,
        group.MemberCount);
}

using System.ComponentModel;
using ModelContextProtocol.Server;
using NOIR.Application.Features.Hr.Commands.AssignTagsToEmployee;
using NOIR.Application.Features.Hr.Commands.CreateTag;
using NOIR.Application.Features.Hr.Commands.DeleteTag;
using NOIR.Application.Features.Hr.Commands.UpdateTag;
using NOIR.Application.Features.Hr.DTOs;
using NOIR.Application.Features.Hr.Queries.GetEmployeeTagById;
using NOIR.Application.Features.Hr.Queries.GetTags;
using NOIR.Web.Mcp.Filters;
using NOIR.Web.Mcp.Helpers;

namespace NOIR.Web.Mcp.Tools;

/// <summary>
/// MCP tools for HR employee tag management.
/// </summary>
[McpServerToolType]
[RequiresModule(ModuleNames.Erp.HrSub.Tags)]
public sealed class HrTagTools(IMessageBus bus, ICurrentUser currentUser)
{
    [McpServerTool(Name = "noir_hr_tags_list", ReadOnly = true, Idempotent = true)]
    [Description("List all employee tags with optional category filter. Returns tag name, category, color, description, sort order, and employee count.")]
    public async Task<List<EmployeeTagDto>> ListTags(
        [Description("Filter by category: Team, Skill, Project, Location, Seniority, Employment, Custom")] string? category = null,
        CancellationToken ct = default)
    {
        var cat = category is not null && Enum.TryParse<EmployeeTagCategory>(category, true, out var c) ? c : (EmployeeTagCategory?)null;
        var result = await bus.InvokeAsync<Result<List<EmployeeTagDto>>>(
            new GetTagsQuery(cat), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_hr_tags_get", ReadOnly = true, Idempotent = true)]
    [Description("Get full employee tag details by ID, including employee count and audit timestamps.")]
    public async Task<EmployeeTagDto> GetTag(
        [Description("The employee tag ID (GUID)")] string tagId,
        CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<EmployeeTagDto>>(
            new GetEmployeeTagByIdQuery(Guid.Parse(tagId)), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_hr_tags_create", Destructive = false)]
    [Description("Create a new employee tag for categorizing employees. Tags are grouped by category.")]
    public async Task<EmployeeTagDto> CreateTag(
        [Description("Tag display name (1-100 characters)")] string name,
        [Description("Category: Team, Skill, Project, Location, Seniority, Employment, Custom")] string category,
        [Description("Hex color code (e.g. '#3B82F6')")] string? color = null,
        [Description("Optional tag description (max 500 chars)")] string? description = null,
        [Description("Sort order within category (default: 0)")] int sortOrder = 0,
        CancellationToken ct = default)
    {
        var cat = Enum.TryParse<EmployeeTagCategory>(category, true, out var c) ? c : EmployeeTagCategory.Custom;
        var command = new CreateTagCommand(name, cat, color, description, sortOrder)
        {
            UserId = currentUser.UserId
        };
        var result = await bus.InvokeAsync<Result<EmployeeTagDto>>(command, ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_hr_tags_update", Destructive = false)]
    [Description("Update an existing employee tag's name, color, description, or sort order. Category cannot be changed.")]
    public async Task<EmployeeTagDto> UpdateTag(
        [Description("The employee tag ID (GUID)")] string tagId,
        [Description("New tag name (1-100 characters)")] string name,
        [Description("Category (preserved from creation): Team, Skill, Project, Location, Seniority, Employment, Custom")] string category,
        [Description("Hex color code (e.g. '#3B82F6')")] string? color = null,
        [Description("Optional tag description (max 500 chars)")] string? description = null,
        [Description("Sort order within category (default: 0)")] int sortOrder = 0,
        CancellationToken ct = default)
    {
        var cat = Enum.TryParse<EmployeeTagCategory>(category, true, out var c) ? c : EmployeeTagCategory.Custom;
        var command = new UpdateTagCommand(Guid.Parse(tagId), name, cat, color, description, sortOrder)
        {
            UserId = currentUser.UserId
        };
        var result = await bus.InvokeAsync<Result<EmployeeTagDto>>(command, ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_hr_tags_delete", Destructive = true)]
    [Description("Soft-delete an employee tag. Removes all tag assignments from employees.")]
    public async Task<bool> DeleteTag(
        [Description("The employee tag ID (GUID)")] string tagId,
        [Description("Tag name for audit trail")] string? tagName = null,
        CancellationToken ct = default)
    {
        var command = new DeleteTagCommand(Guid.Parse(tagId), tagName)
        {
            UserId = currentUser.UserId
        };
        var result = await bus.InvokeAsync<Result<bool>>(command, ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_hr_tags_assign", Destructive = false)]
    [Description("Assign one or more tags to an employee. Already-assigned tags are skipped.")]
    public async Task<List<TagBriefDto>> AssignTagsToEmployee(
        [Description("The employee ID (GUID)")] string employeeId,
        [Description("Comma-separated tag IDs (GUIDs) to assign")] string tagIds,
        CancellationToken ct = default)
    {
        var ids = tagIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(id => Guid.Parse(id.Trim())).ToList();
        var command = new AssignTagsToEmployeeCommand(Guid.Parse(employeeId), ids)
        {
            UserId = currentUser.UserId
        };
        var result = await bus.InvokeAsync<Result<List<TagBriefDto>>>(command, ct);
        return result.Unwrap();
    }
}

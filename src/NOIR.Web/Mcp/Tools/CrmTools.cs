using System.ComponentModel;
using ModelContextProtocol.Server;
using NOIR.Application.Features.Crm.Commands.LoseLead;
using NOIR.Application.Features.Crm.Commands.MoveLeadStage;
using NOIR.Application.Features.Crm.Commands.WinLead;
using NOIR.Application.Features.Crm.DTOs;
using NOIR.Application.Features.Crm.Queries.GetContactById;
using NOIR.Application.Features.Crm.Queries.GetContacts;
using NOIR.Application.Features.Crm.Queries.GetCrmDashboard;
using NOIR.Application.Features.Crm.Queries.GetLeadById;
using NOIR.Application.Features.Crm.Queries.GetLeads;
using NOIR.Web.Mcp.Filters;
using NOIR.Web.Mcp.Helpers;

namespace NOIR.Web.Mcp.Tools;

/// <summary>
/// MCP tools for CRM (Customer Relationship Management).
/// </summary>
[McpServerToolType]
[RequiresModule(ModuleNames.Erp.Crm)]
public sealed class CrmTools(IMessageBus bus, ICurrentUser currentUser)
{
    [McpServerTool(Name = "noir_crm_contacts_list", ReadOnly = true, Idempotent = true)]
    [Description("List CRM contacts with pagination and filtering. Supports search, company, owner, and source filters.")]
    public async Task<PagedResult<ContactListDto>> ListContacts(
        [Description("Search by name, email, or phone")] string? search = null,
        [Description("Filter by company ID (GUID)")] string? companyId = null,
        [Description("Filter by owner user ID (GUID)")] string? ownerId = null,
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Page size, max 100 (default: 20)")] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var cId = companyId is not null ? Guid.Parse(companyId) : (Guid?)null;
        var oId = ownerId is not null ? Guid.Parse(ownerId) : (Guid?)null;

        var result = await bus.InvokeAsync<Result<PagedResult<ContactListDto>>>(
            new GetContactsQuery(search, cId, oId, null, page, pageSize), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_crm_contacts_get", ReadOnly = true, Idempotent = true)]
    [Description("Get full CRM contact details by ID, including company, leads, activities, and notes.")]
    public async Task<ContactDto> GetContact(
        [Description("The contact ID (GUID)")] string contactId,
        CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<ContactDto>>(
            new GetContactByIdQuery(Guid.Parse(contactId)), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_crm_leads_list", ReadOnly = true, Idempotent = true)]
    [Description("List CRM leads with pagination and filtering. Supports pipeline, stage, owner, and status filters.")]
    public async Task<PagedResult<LeadDto>> ListLeads(
        [Description("Filter by pipeline ID (GUID)")] string? pipelineId = null,
        [Description("Filter by stage ID (GUID)")] string? stageId = null,
        [Description("Filter by owner user ID (GUID)")] string? ownerId = null,
        [Description("Filter by status: Open, Won, Lost")] string? status = null,
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Page size, max 100 (default: 20)")] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var pId = pipelineId is not null ? Guid.Parse(pipelineId) : (Guid?)null;
        var sId = stageId is not null ? Guid.Parse(stageId) : (Guid?)null;
        var oId = ownerId is not null ? Guid.Parse(ownerId) : (Guid?)null;
        var leadStatus = status is not null && Enum.TryParse<LeadStatus>(status, true, out var s) ? s : (LeadStatus?)null;

        var result = await bus.InvokeAsync<Result<PagedResult<LeadDto>>>(
            new GetLeadsQuery(pId, sId, oId, leadStatus, page, pageSize), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_crm_leads_get", ReadOnly = true, Idempotent = true)]
    [Description("Get full lead details by ID, including contact info, pipeline stage, deal value, activities, and history.")]
    public async Task<LeadDto> GetLead(
        [Description("The lead ID (GUID)")] string leadId,
        CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<LeadDto>>(
            new GetLeadByIdQuery(Guid.Parse(leadId)), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_crm_leads_move", Destructive = false)]
    [Description("Move a lead to a different pipeline stage (Kanban drag-drop). Updates the lead's position in the sales pipeline.")]
    public async Task<LeadDto> MoveLeadStage(
        [Description("The lead ID (GUID)")] string leadId,
        [Description("Target stage ID (GUID)")] string newStageId,
        [Description("Sort order position within the stage (default: 0)")] double sortOrder = 0,
        CancellationToken ct = default)
    {
        var command = new MoveLeadStageCommand(Guid.Parse(leadId), Guid.Parse(newStageId), sortOrder)
        {
            AuditUserId = currentUser.UserId
        };
        var result = await bus.InvokeAsync<Result<LeadDto>>(command, ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_crm_leads_win", Destructive = false)]
    [Description("Mark a lead as Won. If the lead's contact has no linked customer, a new Customer record is auto-created.")]
    public async Task<LeadDto> WinLead(
        [Description("The lead ID (GUID)")] string leadId,
        CancellationToken ct = default)
    {
        var command = new WinLeadCommand(Guid.Parse(leadId)) { AuditUserId = currentUser.UserId };
        var result = await bus.InvokeAsync<Result<LeadDto>>(command, ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_crm_leads_lose", Destructive = false)]
    [Description("Mark a lead as Lost with an optional reason.")]
    public async Task<LeadDto> LoseLead(
        [Description("The lead ID (GUID)")] string leadId,
        [Description("Reason for losing the lead (optional)")] string? reason = null,
        CancellationToken ct = default)
    {
        var command = new LoseLeadCommand(Guid.Parse(leadId), reason) { AuditUserId = currentUser.UserId };
        var result = await bus.InvokeAsync<Result<LeadDto>>(command, ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_crm_dashboard", ReadOnly = true, Idempotent = true)]
    [Description("Get CRM dashboard: pipeline summary, lead funnel, recent activities, win/loss rates, and revenue forecast.")]
    public async Task<CrmDashboardDto> GetDashboard(CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<CrmDashboardDto>>(
            new GetCrmDashboardQuery(), ct);
        return result.Unwrap();
    }
}

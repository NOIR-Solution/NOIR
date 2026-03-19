using NOIR.Application.Features.Crm.Commands.CreatePipeline;
using NOIR.Application.Features.Crm.Commands.UpdatePipeline;
using NOIR.Application.Features.Crm.Commands.DeletePipeline;
using NOIR.Application.Features.Crm.Commands.CreateStage;
using NOIR.Application.Features.Crm.Commands.UpdateStage;
using NOIR.Application.Features.Crm.Commands.DeleteStage;
using NOIR.Application.Features.Crm.Commands.ReorderStages;
using NOIR.Application.Features.Crm.Queries.GetPipelines;
using NOIR.Application.Features.Crm.Queries.GetPipelineView;
using NOIR.Application.Features.Crm.Queries.GetCrmDashboard;
using NOIR.Application.Features.Crm.DTOs;

namespace NOIR.Web.Endpoints;

public static class PipelineEndpoints
{
    public static void MapPipelineEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/crm/pipelines")
            .WithTags("CRM - Pipelines")
            .RequireFeature(ModuleNames.Erp.Crm)
            .RequireAuthorization();

        group.MapGet("/", async (IMessageBus bus) =>
        {
            var query = new GetPipelinesQuery();
            var result = await bus.InvokeAsync<Result<List<PipelineDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CrmLeadsRead)
        .WithName("GetPipelines")
        .WithSummary("Get all pipelines with stages")
        .Produces<List<PipelineDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}/view", async (
            Guid id,
            [FromQuery] bool? includeClosedDeals,
            IMessageBus bus) =>
        {
            var query = new GetPipelineViewQuery(id, includeClosedDeals ?? false);
            var result = await bus.InvokeAsync<Result<PipelineViewDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CrmLeadsRead)
        .WithName("GetPipelineView")
        .WithSummary("Get pipeline Kanban view with leads grouped by stage")
        .Produces<PipelineViewDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/", async (
            CreatePipelineRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CreatePipelineCommand(request.Name, request.IsDefault, request.Stages)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<PipelineDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CrmPipelineManage)
        .WithName("CreatePipeline")
        .WithSummary("Create a new pipeline with stages")
        .Produces<PipelineDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdatePipelineRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdatePipelineCommand(id, request.Name, request.IsDefault, request.Stages)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<PipelineDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CrmPipelineManage)
        .WithName("UpdatePipeline")
        .WithSummary("Update a pipeline and its stages")
        .Produces<PipelineDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeletePipelineCommand(id) { AuditUserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<PipelineDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CrmPipelineManage)
        .WithName("DeletePipeline")
        .WithSummary("Delete a pipeline")
        .Produces<PipelineDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // ── Stage CRUD ─────────────────────────────────────────────────────────

        group.MapPost("/{pipelineId:guid}/stages", async (
            Guid pipelineId,
            CreateStageRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CreateStageCommand(pipelineId, request.Name, request.Color)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<PipelineStageDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CrmPipelineManage)
        .WithName("CreateStage")
        .WithSummary("Add a new stage to a pipeline")
        .Produces<PipelineStageDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("/{pipelineId:guid}/stages/{stageId:guid}", async (
            Guid pipelineId,
            Guid stageId,
            UpdateStageRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateStageCommand(stageId, request.Name, request.Color)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<PipelineStageDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CrmPipelineManage)
        .WithName("UpdateStage")
        .WithSummary("Update a pipeline stage (system stages: color only)")
        .Produces<PipelineStageDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapDelete("/{pipelineId:guid}/stages/{stageId:guid}", async (
            Guid pipelineId,
            Guid stageId,
            [FromQuery] Guid moveLeadsToStageId,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteStageCommand(stageId, moveLeadsToStageId)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<PipelineStageDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CrmPipelineManage)
        .WithName("DeleteStage")
        .WithSummary("Delete a stage, migrating its leads to another stage first")
        .Produces<PipelineStageDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPut("/{pipelineId:guid}/stages/reorder", async (
            Guid pipelineId,
            ReorderStagesRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ReorderStagesCommand(pipelineId, request.StageIds)
            {
                AuditUserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<List<PipelineStageDto>>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CrmPipelineManage)
        .WithName("ReorderStages")
        .WithSummary("Reorder active pipeline stages (system stages always remain at end)")
        .Produces<List<PipelineStageDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // CRM Dashboard (placed here as a general CRM endpoint)
        app.MapGroup("/api/crm")
            .WithTags("CRM - Dashboard")
            .RequireFeature(ModuleNames.Erp.Crm)
            .RequireAuthorization()
            .MapGet("/dashboard", async (IMessageBus bus) =>
            {
                var query = new GetCrmDashboardQuery();
                var result = await bus.InvokeAsync<Result<CrmDashboardDto>>(query);
                return result.ToHttpResult();
            })
            .RequireAuthorization(Permissions.CrmLeadsRead)
            .WithName("GetCrmDashboard")
            .WithSummary("Get CRM dashboard aggregate data")
            .Produces<CrmDashboardDto>(StatusCodes.Status200OK);
    }
}

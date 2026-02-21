using NOIR.Application.Features.Payments.Commands.ApproveRefund;
using NOIR.Application.Features.Payments.Commands.CancelPayment;
using NOIR.Application.Features.Payments.Commands.ConfigureGateway;
using NOIR.Application.Features.Payments.Commands.ConfirmCodCollection;
using NOIR.Application.Features.Payments.Commands.CreatePayment;
using NOIR.Application.Features.Payments.Commands.ProcessWebhook;
using NOIR.Application.Features.Payments.Commands.RecordManualPayment;
using NOIR.Application.Features.Payments.Commands.RefreshPaymentStatus;
using NOIR.Application.Features.Payments.Commands.RejectRefund;
using NOIR.Application.Features.Payments.Commands.RequestRefund;
using NOIR.Application.Features.Payments.Commands.TestGatewayConnection;
using NOIR.Application.Features.Payments.Commands.UpdateGateway;
using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Queries.GetActiveGateways;
using NOIR.Application.Features.Payments.Queries.GetGatewaySchemas;
using NOIR.Application.Features.Payments.Queries.GetOrderPayments;
using NOIR.Application.Features.Payments.Queries.GetPaymentDetails;
using NOIR.Application.Features.Payments.Queries.GetPaymentGateway;
using NOIR.Application.Features.Payments.Queries.GetPaymentGateways;
using NOIR.Application.Features.Payments.Queries.GetPaymentTransaction;
using NOIR.Application.Features.Payments.Queries.GetPaymentTransactions;
using NOIR.Application.Features.Payments.Queries.GetPaymentTimeline;
using NOIR.Application.Features.Payments.Queries.GetPendingCodPayments;
using NOIR.Application.Features.Payments.Queries.GetRefunds;
using NOIR.Application.Features.Payments.Queries.GetOperationLogs;
using NOIR.Application.Features.Payments.Queries.GetWebhookLogs;
using NOIR.Application.Common.Models;
using PaymentPagedResult = NOIR.Application.Common.Models.PagedResult<NOIR.Application.Features.Payments.DTOs.PaymentTransactionListDto>;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Payment API endpoints.
/// Provides payment processing, gateway management, and refund operations.
/// </summary>
public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        MapPaymentTransactionEndpoints(app);
        MapPaymentGatewayEndpoints(app);
        MapRefundEndpoints(app);
        MapWebhookEndpoints(app);
    }

    private static void MapPaymentTransactionEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments")
            .WithTags("Payments")
            .RequireFeature(ModuleNames.Ecommerce.Payments)
            .RequireAuthorization();

        // Get all payment transactions (paginated)
        group.MapGet("/", async (
            [FromQuery] string? search,
            [FromQuery] PaymentStatus? status,
            [FromQuery] PaymentMethod? paymentMethod,
            [FromQuery] string? provider,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            IMessageBus bus) =>
        {
            var query = new GetPaymentTransactionsQuery(
                search,
                status,
                paymentMethod,
                provider,
                page ?? 1,
                pageSize ?? 20);
            var result = await bus.InvokeAsync<Result<PaymentPagedResult>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentsRead)
        .WithName("GetPaymentTransactions")
        .WithSummary("Get paginated list of payment transactions")
        .WithDescription("Returns payment transactions with optional filtering by search, status, payment method, and provider.")
        .Produces<PaymentPagedResult>(StatusCodes.Status200OK);

        // Get payment transaction by ID
        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var query = new GetPaymentTransactionQuery(id);
            var result = await bus.InvokeAsync<Result<PaymentTransactionDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentsRead)
        .WithName("GetPaymentTransactionById")
        .WithSummary("Get payment transaction by ID")
        .WithDescription("Returns full payment transaction details including metadata.")
        .Produces<PaymentTransactionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get payments for an order
        group.MapGet("/order/{orderId:guid}", async (Guid orderId, IMessageBus bus) =>
        {
            var query = new GetOrderPaymentsQuery(orderId);
            var result = await bus.InvokeAsync<Result<List<PaymentTransactionDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentsRead)
        .WithName("GetOrderPayments")
        .WithSummary("Get payments for an order")
        .WithDescription("Returns all payment transactions associated with a specific order.")
        .Produces<List<PaymentTransactionDto>>(StatusCodes.Status200OK);

        // Get pending COD payments
        group.MapGet("/cod/pending", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            IMessageBus bus) =>
        {
            var query = new GetPendingCodPaymentsQuery(page ?? 1, pageSize ?? 20);
            var result = await bus.InvokeAsync<Result<PagedResult<PaymentTransactionListDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentsManage)
        .WithName("GetPendingCodPayments")
        .WithSummary("Get pending COD payments")
        .WithDescription("Returns COD payments awaiting cash collection confirmation.")
        .Produces<PagedResult<PaymentTransactionListDto>>(StatusCodes.Status200OK);

        // Create payment (checkout)
        group.MapPost("/", async (
            CreatePaymentRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CreatePaymentCommand(
                request.OrderId,
                request.Amount,
                request.Currency,
                request.PaymentMethod,
                request.Provider,
                request.ReturnUrl,
                request.IdempotencyKey,
                request.Metadata)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<PaymentTransactionDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentsCreate)
        .WithName("CreatePayment")
        .WithSummary("Create a new payment")
        .WithDescription("Initiates a payment transaction for an order. Returns payment URL for redirect-based gateways.")
        .Produces<PaymentTransactionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Cancel payment
        group.MapPost("/{id:guid}/cancel", async (
            Guid id,
            CancelPaymentRequest? request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CancelPaymentCommand(id, request?.Reason)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<PaymentTransactionDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentsManage)
        .WithName("CancelPayment")
        .WithSummary("Cancel a pending payment")
        .WithDescription("Cancels a payment that is in pending status.")
        .Produces<PaymentTransactionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Confirm COD collection
        group.MapPost("/{id:guid}/cod/confirm", async (
            Guid id,
            ConfirmCodRequest? request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ConfirmCodCollectionCommand(id, request?.Notes)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<PaymentTransactionDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentsManage)
        .WithName("ConfirmCodCollection")
        .WithSummary("Confirm COD cash collection")
        .WithDescription("Marks a COD payment as collected after receiving cash.")
        .Produces<PaymentTransactionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Refresh payment status from gateway
        group.MapPost("/{id:guid}/refresh", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new RefreshPaymentStatusCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<PaymentTransactionDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentsManage)
        .WithName("RefreshPaymentStatus")
        .WithSummary("Refresh payment status from gateway")
        .WithDescription("Queries the payment gateway for the current status of a payment and updates accordingly.")
        .Produces<PaymentTransactionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Record manual/offline payment
        group.MapPost("/manual", async (
            RecordManualPaymentRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new RecordManualPaymentCommand(
                request.OrderId,
                request.Amount,
                request.Currency,
                request.PaymentMethod,
                request.ReferenceNumber,
                request.Notes,
                request.PaidAt)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<PaymentTransactionDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentsManage)
        .WithName("RecordManualPayment")
        .WithSummary("Record a manual/offline payment")
        .WithDescription("Records a manual payment (bank transfer, cash, etc.) for an order.")
        .Produces<PaymentTransactionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get comprehensive payment details with logs
        group.MapGet("/{id:guid}/details", async (
            Guid id,
            IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<PaymentDetailsDto>>(new GetPaymentDetailsQuery(id));
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentsRead)
        .WithName("GetPaymentDetails")
        .WithSummary("Get comprehensive payment details with logs")
        .WithDescription("Returns payment transaction with operation logs, webhook logs, and refunds.")
        .Produces<PaymentDetailsDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get payment event timeline
        group.MapGet("/{id:guid}/timeline", async (
            Guid id,
            IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<IReadOnlyList<PaymentTimelineEventDto>>>(new GetPaymentTimelineQuery(id));
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentsRead)
        .WithName("GetPaymentTimeline")
        .WithSummary("Get payment event timeline")
        .WithDescription("Returns a chronological timeline of all events related to a payment.")
        .Produces<IReadOnlyList<PaymentTimelineEventDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    private static void MapPaymentGatewayEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payment-gateways")
            .WithTags("Payment Gateways")
            .RequireFeature(ModuleNames.Ecommerce.Payments)
            .RequireAuthorization();

        // Get all gateways (admin)
        group.MapGet("/", async (IMessageBus bus) =>
        {
            var query = new GetPaymentGatewaysQuery();
            var result = await bus.InvokeAsync<Result<List<PaymentGatewayDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentGatewaysRead)
        .WithName("GetPaymentGateways")
        .WithSummary("Get all payment gateways")
        .WithDescription("Returns all configured payment gateways for the tenant.")
        .Produces<List<PaymentGatewayDto>>(StatusCodes.Status200OK);

        // Get active gateways for checkout
        group.MapGet("/active", async (IMessageBus bus) =>
        {
            var query = new GetActiveGatewaysQuery();
            var result = await bus.InvokeAsync<Result<List<CheckoutGatewayDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentsCreate)
        .WithName("GetActivePaymentGateways")
        .WithSummary("Get active payment gateways for checkout")
        .WithDescription("Returns active gateways available for payment selection during checkout.")
        .Produces<List<CheckoutGatewayDto>>(StatusCodes.Status200OK);

        // Get gateway by ID
        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var query = new GetPaymentGatewayQuery(id);
            var result = await bus.InvokeAsync<Result<PaymentGatewayDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentGatewaysRead)
        .WithName("GetPaymentGatewayById")
        .WithSummary("Get payment gateway by ID")
        .WithDescription("Returns gateway configuration details (credentials are not exposed).")
        .Produces<PaymentGatewayDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Configure new gateway
        group.MapPost("/", async (
            ConfigureGatewayRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ConfigureGatewayCommand(
                request.Provider,
                request.DisplayName,
                request.Environment,
                request.Credentials,
                request.SupportedMethods,
                request.SortOrder,
                request.IsActive)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<PaymentGatewayDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentGatewaysManage)
        .WithName("ConfigurePaymentGateway")
        .WithSummary("Configure a new payment gateway")
        .WithDescription("Sets up a new payment gateway with credentials (encrypted at rest).")
        .Produces<PaymentGatewayDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Update gateway
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateGatewayRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateGatewayCommand(
                id,
                request.DisplayName,
                request.Environment,
                request.Credentials,
                request.SupportedMethods,
                request.SortOrder,
                request.IsActive)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<PaymentGatewayDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentGatewaysManage)
        .WithName("UpdatePaymentGateway")
        .WithSummary("Update payment gateway")
        .WithDescription("Updates gateway configuration. Pass null for fields to keep unchanged.")
        .Produces<PaymentGatewayDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get gateway credential schemas
        group.MapGet("/schemas", async (IMessageBus bus) =>
        {
            var query = new GetGatewaySchemasQuery();
            var result = await bus.InvokeAsync<Result<GatewaySchemasDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentGatewaysRead)
        .WithName("GetGatewaySchemas")
        .WithSummary("Get gateway credential schemas")
        .WithDescription("Returns credential field definitions for all supported payment gateways.")
        .Produces<GatewaySchemasDto>(StatusCodes.Status200OK);

        // Test gateway connection
        group.MapPost("/{id:guid}/test", async (Guid id, IMessageBus bus) =>
        {
            var command = new TestGatewayConnectionCommand(id);
            var result = await bus.InvokeAsync<Result<TestConnectionResultDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentGatewaysManage)
        .WithName("TestGatewayConnection")
        .WithSummary("Test gateway connection")
        .WithDescription("Tests connectivity to a payment gateway using stored credentials.")
        .Produces<TestConnectionResultDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    private static void MapRefundEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/refunds")
            .WithTags("Payment Refunds")
            .RequireFeature(ModuleNames.Ecommerce.Payments)
            .RequireAuthorization();

        // Get refunds for a payment
        group.MapGet("/payment/{paymentTransactionId:guid}", async (Guid paymentTransactionId, IMessageBus bus) =>
        {
            var query = new GetRefundsQuery(paymentTransactionId);
            var result = await bus.InvokeAsync<Result<List<RefundDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentRefundsRead)
        .WithName("GetRefunds")
        .WithSummary("Get refunds for a payment")
        .WithDescription("Returns all refund requests for a specific payment transaction.")
        .Produces<List<RefundDto>>(StatusCodes.Status200OK);

        // Request refund
        group.MapPost("/", async (
            RequestRefundRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new RequestRefundCommand(
                request.PaymentTransactionId,
                request.Amount,
                request.Reason,
                request.Notes)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<RefundDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentRefundsManage)
        .WithName("RequestRefund")
        .WithSummary("Request a refund")
        .WithDescription("Creates a refund request for a paid transaction. May be auto-approved if under threshold.")
        .Produces<RefundDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Approve refund
        group.MapPost("/{id:guid}/approve", async (
            Guid id,
            ApproveRefundRequest? request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ApproveRefundCommand(id, request?.Notes)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<RefundDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentRefundsManage)
        .WithName("ApproveRefund")
        .WithSummary("Approve a refund request")
        .WithDescription("Approves a pending refund request for processing.")
        .Produces<RefundDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Reject refund
        group.MapPost("/{id:guid}/reject", async (
            Guid id,
            RejectRefundRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new RejectRefundCommand(id, request.Reason)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<RefundDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentRefundsManage)
        .WithName("RejectRefund")
        .WithSummary("Reject a refund request")
        .WithDescription("Rejects a pending refund request with a reason.")
        .Produces<RefundDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    private static void MapWebhookEndpoints(IEndpointRouteBuilder app)
    {
        // Webhook callback endpoint (no auth - validated by signature)
        app.MapPost("/api/payments/webhook/{provider}", async (
            string provider,
            HttpRequest request,
            IMessageBus bus) =>
        {
            // Read raw body for signature verification
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, leaveOpen: true);
            var rawBody = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            // Extract headers
            var headers = request.Headers.ToDictionary(
                h => h.Key,
                h => h.Value.ToString());

            // Get signature header (varies by provider)
            var signature = headers.GetValueOrDefault("X-Signature")
                ?? headers.GetValueOrDefault("X-Webhook-Signature")
                ?? headers.GetValueOrDefault("Signature");

            // Get client IP address
            var ipAddress = request.HttpContext.Connection.RemoteIpAddress?.ToString();

            var command = new ProcessWebhookCommand(
                provider,
                rawBody,
                signature,
                ipAddress,
                headers);
            var result = await bus.InvokeAsync<Result<WebhookLogDto>>(command);

            // Always return 200 to acknowledge receipt (prevent retries)
            return result.IsSuccess
                ? Results.Ok(new { status = "processed" })
                : Results.Ok(new { status = "acknowledged", error = result.Error?.Message });
        })
        .WithTags("Payment Webhooks")
        .WithName("ProcessPaymentWebhook")
        .WithSummary("Process payment webhook")
        .WithDescription("Receives and processes payment gateway webhook callbacks. No authentication required - validated by signature.")
        .Produces(StatusCodes.Status200OK)
        .AllowAnonymous();

        // Admin endpoint for viewing webhook logs
        var adminGroup = app.MapGroup("/api/payment-webhooks")
            .WithTags("Payment Webhooks")
            .RequireAuthorization();

        adminGroup.MapGet("/", async (
            [FromQuery] string? provider,
            [FromQuery] WebhookProcessingStatus? status,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            IMessageBus bus) =>
        {
            var query = new GetWebhookLogsQuery(
                provider,
                status,
                page ?? 1,
                pageSize ?? 20);
            var result = await bus.InvokeAsync<Result<PagedResult<WebhookLogDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentWebhooksRead)
        .WithName("GetWebhookLogs")
        .WithSummary("Get webhook logs")
        .WithDescription("Returns payment webhook processing logs for debugging and auditing.")
        .Produces<PagedResult<WebhookLogDto>>(StatusCodes.Status200OK);

        // Operation logs endpoint for debugging gateway API calls
        adminGroup.MapGet("/operations", async (
            [FromQuery] string? provider,
            [FromQuery] PaymentOperationType? operationType,
            [FromQuery] bool? success,
            [FromQuery] string? transactionNumber,
            [FromQuery] string? correlationId,
            [FromQuery] DateTimeOffset? fromDate,
            [FromQuery] DateTimeOffset? toDate,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            IMessageBus bus) =>
        {
            var query = new GetOperationLogsQuery(
                provider,
                operationType,
                success,
                transactionNumber,
                correlationId,
                fromDate,
                toDate,
                page ?? 1,
                pageSize ?? 20);
            var result = await bus.InvokeAsync<Result<PagedResult<PaymentOperationLogDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PaymentWebhooksRead)
        .WithName("GetOperationLogs")
        .WithSummary("Get payment operation logs")
        .WithDescription("Returns payment gateway operation logs for debugging API calls, including request/response data and timing.")
        .Produces<PagedResult<PaymentOperationLogDto>>(StatusCodes.Status200OK);
    }
}

// Request DTOs for endpoints
public sealed record CreatePaymentRequest(
    Guid OrderId,
    decimal Amount,
    string Currency,
    PaymentMethod PaymentMethod,
    string Provider,
    string? ReturnUrl = null,
    string? IdempotencyKey = null,
    Dictionary<string, string>? Metadata = null);

public sealed record CancelPaymentRequest(
    string? Reason = null);

public sealed record ConfirmCodRequest(
    string? Notes = null);

public sealed record ConfigureGatewayRequest(
    string Provider,
    string DisplayName,
    GatewayEnvironment Environment,
    Dictionary<string, string> Credentials,
    List<PaymentMethod> SupportedMethods,
    int SortOrder = 0,
    bool IsActive = true);

public sealed record UpdateGatewayRequest(
    string? DisplayName = null,
    GatewayEnvironment? Environment = null,
    Dictionary<string, string>? Credentials = null,
    List<PaymentMethod>? SupportedMethods = null,
    int? SortOrder = null,
    bool? IsActive = null);

public sealed record RequestRefundRequest(
    Guid PaymentTransactionId,
    decimal Amount,
    RefundReason Reason,
    string? Notes = null);

public sealed record ApproveRefundRequest(
    string? Notes = null);

public sealed record RejectRefundRequest(string Reason);

public sealed record RecordManualPaymentRequest(
    Guid OrderId,
    decimal Amount,
    string Currency,
    PaymentMethod PaymentMethod,
    string? ReferenceNumber = null,
    string? Notes = null,
    DateTimeOffset? PaidAt = null);

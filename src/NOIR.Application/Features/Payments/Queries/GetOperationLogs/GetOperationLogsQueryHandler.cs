namespace NOIR.Application.Features.Payments.Queries.GetOperationLogs;

/// <summary>
/// Handler for getting payment operation logs.
/// </summary>
public class GetOperationLogsQueryHandler
{
    private readonly IRepository<PaymentOperationLog, Guid> _operationLogRepository;

    public GetOperationLogsQueryHandler(IRepository<PaymentOperationLog, Guid> operationLogRepository)
    {
        _operationLogRepository = operationLogRepository;
    }

    public async Task<Result<PagedResult<PaymentOperationLogDto>>> Handle(
        GetOperationLogsQuery query,
        CancellationToken cancellationToken)
    {
        // If filtering by correlation ID, use dedicated spec
        if (!string.IsNullOrEmpty(query.CorrelationId))
        {
            var correlationSpec = new PaymentOperationLogsByCorrelationIdSpec(query.CorrelationId);
            var correlationLogs = await _operationLogRepository.ListAsync(correlationSpec, cancellationToken);

            var correlationItems = correlationLogs.Select(MapToDto).ToList();
            var correlationResult = PagedResult<PaymentOperationLogDto>.Create(
                correlationItems,
                correlationItems.Count,
                0,
                correlationItems.Count);

            return Result.Success(correlationResult);
        }

        // Use search spec for other filters
        var spec = new PaymentOperationLogsSearchSpec(
            query.Provider,
            query.OperationType,
            query.Success,
            query.FromDate,
            query.ToDate,
            query.TransactionNumber,
            query.Page,
            query.PageSize);

        var logs = await _operationLogRepository.ListAsync(spec, cancellationToken);

        // Count without pagination
        var countSpec = new PaymentOperationLogsSearchSpec(
            query.Provider,
            query.OperationType,
            query.Success,
            query.FromDate,
            query.ToDate,
            query.TransactionNumber,
            pageNumber: 1,
            pageSize: int.MaxValue);
        var totalCount = await _operationLogRepository.CountAsync(countSpec, cancellationToken);

        var items = logs.Select(MapToDto).ToList();

        var result = PagedResult<PaymentOperationLogDto>.Create(
            items,
            totalCount,
            query.Page - 1,
            query.PageSize);

        return Result.Success(result);
    }

    private static PaymentOperationLogDto MapToDto(PaymentOperationLog log)
    {
        return new PaymentOperationLogDto(
            log.Id,
            log.OperationType,
            log.Provider,
            log.PaymentTransactionId,
            log.TransactionNumber,
            log.RefundId,
            log.CorrelationId,
            log.RequestData,
            log.ResponseData,
            log.HttpStatusCode,
            log.DurationMs,
            log.Success,
            log.ErrorCode,
            log.ErrorMessage,
            log.UserId,
            log.IpAddress,
            log.CreatedAt);
    }
}

namespace NOIR.Application.Features.Payments.Queries.GetPendingCodPayments;

/// <summary>
/// Query to get pending COD payments awaiting collection.
/// </summary>
public sealed record GetPendingCodPaymentsQuery(
    int Page = 1,
    int PageSize = 20);

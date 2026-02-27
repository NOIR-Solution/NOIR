namespace NOIR.Domain.Enums;

public enum WebhookDeliveryStatus
{
    Pending = 0,
    Succeeded = 1,
    Failed = 2,
    Retrying = 3,
    Exhausted = 4
}

# Webhook System Pattern

**Location:** `Features/Webhooks/`
**Module:** `Ecommerce.Webhooks` (toggleable)

---

## Overview

NOIR provides outbound webhook subscriptions that allow tenants to register HTTPS URLs to receive real-time event notifications. The system bridges domain events to external HTTP endpoints with HMAC-SHA256 signature verification, wildcard pattern matching, and exponential-backoff retry logic.

**Key points:**
- Tenant-configurable — each tenant manages their own subscriptions
- Pattern-based filtering — `order.*`, `payment.succeeded`, or `*` for all events
- HMAC-SHA256 signed payloads — `X-Webhook-Signature` header
- 55 event types across 16 categories
- 5-attempt exponential backoff via Wolverine scheduled messages
- SSRF protection via `WebhookUrlValidator`

---

## Entity Model

### WebhookSubscription (`TenantAggregateRoot<Guid>`)

| Property | Type | Description |
|----------|------|-------------|
| `Name` | string | Display name |
| `Url` | string | Target HTTPS URL |
| `Secret` | string | 32-byte hex HMAC-SHA256 key (auto-generated) |
| `EventPatterns` | string | Comma-separated patterns (`order.*,payment.succeeded`) |
| `IsActive` | bool | Whether subscription receives events |
| `Status` | enum | Active / Inactive / Suspended |
| `MaxRetries` | int | Max delivery attempts (default: 5) |
| `TimeoutSeconds` | int | HTTP timeout per delivery (default: 30) |
| `CustomHeaders` | string? | Optional extra headers as JSON |
| `LastDeliveryAt` | DateTimeOffset? | Timestamp of last delivery attempt |

**Pattern matching logic** (in `WebhookSubscription.MatchesEvent`):
- `*` — matches all event types
- `order.*` — matches `order.created`, `order.confirmed`, etc.
- `payment.succeeded` — exact match (case-insensitive)

### WebhookDeliveryLog (`TenantEntity<Guid>`)

| Property | Type | Description |
|----------|------|-------------|
| `SubscriptionId` | Guid | Parent subscription |
| `EventType` | string | Event type delivered |
| `Payload` | string | JSON payload sent |
| `StatusCode` | int? | HTTP response status |
| `Status` | enum | Pending / Success / Failed / Retrying / Exhausted |
| `AttemptNumber` | int | Current attempt count |
| `ResponseBody` | string? | Response from target |
| `ErrorMessage` | string? | Error details on failure |

---

## Commands & Queries

| Type | Name | Description |
|------|------|-------------|
| **Command** | `CreateWebhookSubscriptionCommand` | Create subscription (auto-generates secret) |
| **Command** | `UpdateWebhookSubscriptionCommand` | Update URL, patterns, config |
| **Command** | `ActivateWebhookSubscriptionCommand` | Resume paused subscription |
| **Command** | `DeactivateWebhookSubscriptionCommand` | Pause subscription |
| **Command** | `DeleteWebhookSubscriptionCommand` | Delete subscription + logs |
| **Command** | `TestWebhookSubscriptionCommand` | Send test ping synchronously |
| **Command** | `RotateWebhookSecretCommand` | Regenerate HMAC secret |
| **Command** | `DeliverWebhookCommand` | Internal — dispatched by bridge |
| **Query** | `GetWebhookSubscriptionsQuery` | Paged list |
| **Query** | `GetWebhookSubscriptionByIdQuery` | Detail with delivery stats |
| **Query** | `GetWebhookDeliveryLogsQuery` | Delivery history (paged) |
| **Query** | `GetWebhookEventTypesQuery` | 55 event types catalog |

---

## Event Bridge

`WebhookBridgeHandler` implements `INotificationHandler<IDomainEvent>` — automatically receives all domain events via Wolverine.

**Flow:**
```
Domain Event raised
    ↓
WebhookBridgeHandler.Handle(IDomainEvent)
    ↓
1. Check Ecommerce.Webhooks feature enabled
2. Query active subscriptions for current tenant
3. For each: subscription.MatchesEvent(eventType)?
4. Build payload: { id, eventType, timestamp, apiVersion, data }
5. Sign: HMAC-SHA256(secret, payloadJson)
6. bus.PublishAsync(new DeliverWebhookCommand(...))
    ↓
DeliverWebhookCommandHandler
    ↓
HttpClient.PostAsync → target URL
    ↓ Success → log, update LastDeliveryAt
    ↓ Failure → schedule retry (exponential backoff)
```

---

## Payload Format

```json
{
  "id": "evt_01HXXXX",
  "eventType": "order.confirmed",
  "timestamp": "2026-03-08T10:00:00Z",
  "apiVersion": "2026-03",
  "data": {
    "orderId": "guid",
    "orderNumber": "ORD-001",
    "status": "Confirmed"
  }
}
```

**Request headers sent to target:**
```
Content-Type: application/json
X-Webhook-Signature: sha256=abc123def456...
X-Webhook-Event: order.confirmed
X-Webhook-Delivery: {deliveryLogId}
```

**Signature verification:**
```csharp
var expectedSig = HMACSHA256.HashData(
    Encoding.UTF8.GetBytes(subscription.Secret),
    Encoding.UTF8.GetBytes(requestBody));
var expected = "sha256=" + Convert.ToHexString(expectedSig).ToLowerInvariant();
// Use constant-time compare to prevent timing attacks
```

---

## Secret Rotation

```http
POST /api/webhooks/{id}/rotate-secret
```

Secrets are 32-byte hex strings from `RandomNumberGenerator.Fill(bytes)`. Rotation immediately invalidates the old secret. Update your verification code before rotating in production.

---

## Delivery & Retry Logic

| Attempt | Delay |
|---------|-------|
| 1 | Immediate |
| 2 | 30 seconds |
| 3 | 5 minutes |
| 4 | 1 hour |
| 5 | 4 hours |

After 5 failures → `Status = Exhausted`, subscription auto-suspended via `Suspend()`. Manual reactivation required.

**SSRF protection:** `WebhookUrlValidator` rejects private IPs (10.x, 192.168.x, 127.x, ::1, localhost).

---

## Event Types (55 Total, 16 Categories)

| Category | Sample Events |
|----------|--------------|
| Products | `product.created`, `product.published`, `product.archived` |
| Orders | `order.created`, `order.confirmed`, `order.shipped`, `order.cancelled` |
| Payments | `payment.succeeded`, `payment.failed`, `payment.refunded` |
| Customers | `customer.created`, `customer.updated`, `customer.deleted` |
| Inventory | `inventory.stock_low`, `inventory.receipt_confirmed` |
| CRM | `crm.lead_created`, `crm.lead_won`, `crm.lead_lost` |
| HR | `hr.employee_created`, `hr.employee_deactivated` |
| Reviews | `review.submitted`, `review.approved` |
| System | `webhook.test` (test ping), `subscription.suspended` |

Full catalog: `GET /api/webhooks/event-types`

---

## Testing

```http
POST /api/webhooks/{id}/test
```

Sends a synchronous `webhook.test` event with sample data. Returns the HTTP status received. Does not create a `DeliveryLog` entry.

---

## Admin API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/webhooks` | List subscriptions |
| POST | `/api/webhooks` | Create subscription |
| GET | `/api/webhooks/{id}` | Get subscription detail |
| PUT | `/api/webhooks/{id}` | Update subscription |
| DELETE | `/api/webhooks/{id}` | Delete subscription |
| POST | `/api/webhooks/{id}/activate` | Activate |
| POST | `/api/webhooks/{id}/deactivate` | Deactivate |
| POST | `/api/webhooks/{id}/test` | Send test ping |
| POST | `/api/webhooks/{id}/rotate-secret` | Rotate secret |
| GET | `/api/webhooks/{id}/delivery-logs` | Delivery history |
| GET | `/api/webhooks/event-types` | Event types catalog |

**Permission:** `webhooks:*` on `Ecommerce.Webhooks` module.

---

## File Reference

| File | Purpose |
|------|---------|
| `Domain/Entities/Webhook/WebhookSubscription.cs` | Entity, pattern matching, secret generation |
| `Domain/Entities/Webhook/WebhookDeliveryLog.cs` | Delivery tracking entity |
| `Application/Features/Webhooks/Commands/` | 8 commands |
| `Application/Features/Webhooks/Queries/` | 4 queries |
| `Infrastructure/Webhooks/WebhookBridgeHandler.cs` | Domain event → webhook dispatch |
| `Infrastructure/Webhooks/WebhookDispatcher.cs` | HTTP delivery with retry scheduling |
| `Infrastructure/Webhooks/WebhookUrlValidator.cs` | SSRF protection |
| `Web/Endpoints/WebhookEndpoints.cs` | REST endpoints |

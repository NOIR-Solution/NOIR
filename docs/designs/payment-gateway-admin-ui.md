# Payment Gateway Admin UI - Design Document

> **Status:** Implemented (January 2026)
> **Author:** Claude AI Assistant
> **Date:** January 2026
> **Related Roadmap:** Phase 6 - Vietnam Domestic Gateways

---

## 1. Overview

This document defines the frontend architecture for the Payment Gateway Admin UI, allowing tenant admins to configure, enable/disable, and manage payment gateway credentials.

### 1.1 Goals
- Tenant admins can view all available payment providers and their configuration status
- Tenant admins can configure credentials for each gateway (provider-specific fields)
- Tenant admins can enable/disable gateways without losing configuration
- Credentials are never displayed after initial entry (security)
- Test connection feature validates credentials before going live

### 1.2 Scope
| In Scope | Out of Scope |
|----------|--------------|
| Gateway list page | Customer checkout flow |
| Gateway configuration dialog | Webhook log viewer (separate page) |
| Enable/disable toggle | COD pending dashboard (separate page) |
| Test connection | Stripe/international gateways |
| Environment selector | Subscription billing |

---

## 2. Information Architecture

### 2.1 Page Location
```
/portal/admin/payment-gateways
```

### 2.2 Navigation
Add to sidebar under "Settings" section:
```
Settings
├── Tenant Settings
├── Payment Gateways  ← NEW
└── Developer Logs
```

### 2.3 Permissions
| Action | Permission Required |
|--------|---------------------|
| View gateways | `payment-gateways.read` |
| Configure/Update | `payment-gateways.manage` |
| Enable/Disable | `payment-gateways.manage` |
| Test connection | `payment-gateways.manage` |

---

## 3. Component Architecture

### 3.1 File Structure
```
src/NOIR.Web/frontend/src/
├── pages/portal/admin/payment-gateways/
│   ├── PaymentGatewaysPage.tsx          # Main page
│   └── components/
│       ├── GatewayCard.tsx              # Individual gateway card
│       ├── ConfigureGatewayDialog.tsx   # Configuration dialog
│       ├── CredentialFields.tsx         # Dynamic credential form
│       └── TestConnectionButton.tsx     # Connection test UI
├── services/
│   └── paymentGateways.ts               # API service
├── hooks/
│   └── usePaymentGateways.ts            # Data fetching hook
└── types/
│   └── payment.ts                       # TypeScript types
```

### 3.2 Component Hierarchy
```
PaymentGatewaysPage
├── PageHeader (icon, title, description)
├── GatewayGrid
│   ├── GatewayCard (VNPay)
│   │   ├── StatusBadge
│   │   ├── HealthIndicator
│   │   └── ActionButtons
│   ├── GatewayCard (MoMo)
│   ├── GatewayCard (ZaloPay)
│   └── GatewayCard (COD)
└── ConfigureGatewayDialog
    ├── EnvironmentSelector
    ├── CredentialFields (dynamic)
    ├── TestConnectionButton
    └── SaveButton
```

---

## 4. UI Design Specifications

### 4.1 Gateway Card Component

```
┌──────────────────────────────────────────────────────┐
│  ┌────┐                                              │
│  │ 🔷 │  VNPay                        [● Healthy]    │
│  └────┘  Vietnam Payment Gateway                     │
│                                                      │
│  Status: ✅ Configured                               │
│  Environment: 🧪 Sandbox                             │
│  Last Check: 2 minutes ago                           │
│                                                      │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │
│  │   Enable    │  │  Configure  │  │    Test     │  │
│  │  ┌──○       │  │     ⚙️      │  │     🔌      │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  │
└──────────────────────────────────────────────────────┘
```

**States:**
| State | Visual Indicator |
|-------|------------------|
| Not Configured | Gray border, "Configure" button primary |
| Configured (Inactive) | Default border, toggle OFF |
| Active | Green border/glow, toggle ON |
| Unhealthy | Red health dot, warning badge |

### 4.2 Configure Gateway Dialog

```
┌─────────────────────────────────────────────────────────┐
│  ┌────┐                                            [X]  │
│  │ 🔷 │  Configure VNPay                                │
│  └────┘  Enter your merchant credentials                │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Display Name                                           │
│  ┌───────────────────────────────────────────────────┐  │
│  │ VNPay                                             │  │
│  └───────────────────────────────────────────────────┘  │
│                                                         │
│  Environment                                            │
│  ┌───────────────────────────────────────────────────┐  │
│  │ ▼ Sandbox                                         │  │
│  └───────────────────────────────────────────────────┘  │
│  ⚠️ Switch to Production when ready for live payments  │
│                                                         │
│  ─────────────── Credentials ───────────────           │
│                                                         │
│  TMN Code *                                             │
│  ┌───────────────────────────────────────────────────┐  │
│  │                                                   │  │
│  └───────────────────────────────────────────────────┘  │
│                                                         │
│  Hash Secret *                                          │
│  ┌───────────────────────────────────────────────────┐  │
│  │ ••••••••••••••••••••                             │  │
│  └───────────────────────────────────────────────────┘  │
│  🔒 Credentials are encrypted at rest                   │
│                                                         │
│  ┌─────────────────────────────────────────────────┐    │
│  │  🔌 Test Connection                             │    │
│  └─────────────────────────────────────────────────┘    │
│  ✅ Connection successful                               │
│                                                         │
├─────────────────────────────────────────────────────────┤
│                            [Cancel]  [Save Configuration]│
└─────────────────────────────────────────────────────────┘
```

### 4.3 Provider-Specific Credential Fields

| Provider | Required Fields | Optional Fields |
|----------|-----------------|-----------------|
| **VNPay** | `TmnCode`, `HashSecret` | `Version`, `PaymentUrl`, `ApiUrl` |
| **MoMo** | `PartnerCode`, `AccessKey`, `SecretKey` | `ApiEndpoint` |
| **ZaloPay** | `AppId`, `Key1`, `Key2` | `Endpoint` |
| **COD** | None | `MaxCodAmount`, `CodFee` |

---

## 5. API Contract

### 5.1 Existing Endpoints (Backend Complete)

| Method | Endpoint | Purpose |
|--------|----------|---------|
| `GET` | `/api/payment-gateways` | List all gateways |
| `GET` | `/api/payment-gateways/{id}` | Get gateway details |
| `POST` | `/api/payment-gateways` | Configure new gateway |
| `PUT` | `/api/payment-gateways/{id}` | Update gateway |
| `GET` | `/api/payment-gateways/active` | Active gateways for checkout |

### 5.2 New Endpoints Required

#### 5.2.1 Get Credential Schema
```http
GET /api/payment-gateways/schemas
```

**Response:**
```json
{
  "vnpay": {
    "displayName": "VNPay",
    "description": "Vietnam Payment Gateway",
    "iconUrl": "/images/gateways/vnpay.svg",
    "fields": [
      { "key": "TmnCode", "label": "TMN Code", "type": "text", "required": true },
      { "key": "HashSecret", "label": "Hash Secret", "type": "password", "required": true },
      { "key": "Version", "label": "API Version", "type": "text", "required": false, "default": "2.1.0" },
      { "key": "PaymentUrl", "label": "Payment URL", "type": "url", "required": false },
      { "key": "ApiUrl", "label": "API URL", "type": "url", "required": false }
    ],
    "environments": {
      "sandbox": {
        "PaymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
        "ApiUrl": "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction"
      },
      "production": {
        "PaymentUrl": "https://pay.vnpay.vn/vpcpay.html",
        "ApiUrl": "https://merchant.vnpay.vn/merchant_webapi/api/transaction"
      }
    }
  },
  "momo": { ... },
  "zalopay": { ... },
  "cod": { ... }
}
```

#### 5.2.2 Test Gateway Connection
```http
POST /api/payment-gateways/{id}/test
```

**Response:**
```json
{
  "success": true,
  "message": "Connection successful",
  "responseTimeMs": 245
}
```
or
```json
{
  "success": false,
  "message": "Invalid credentials: Authentication failed",
  "errorCode": "AUTH_FAILED"
}
```

### 5.3 Enhanced List Response

Add `configuredFields` indicator (without exposing values):
```json
{
  "id": "...",
  "provider": "vnpay",
  "displayName": "VNPay",
  "isActive": true,
  "hasCredentials": true,
  "configuredFields": ["TmnCode", "HashSecret"],  // NEW
  "environment": "Sandbox",
  "healthStatus": "Healthy",
  "lastHealthCheck": "2026-01-25T10:30:00Z"
}
```

---

## 6. TypeScript Types

```typescript
// src/types/payment.ts

export type GatewayEnvironment = 'Sandbox' | 'Production'
export type GatewayHealthStatus = 'Unknown' | 'Healthy' | 'Degraded' | 'Unhealthy'
export type CredentialFieldType = 'text' | 'password' | 'url' | 'number'

export interface PaymentGateway {
  id: string
  provider: string
  displayName: string
  isActive: boolean
  sortOrder: number
  environment: GatewayEnvironment
  hasCredentials: boolean
  configuredFields?: string[]
  webhookUrl: string | null
  minAmount: number | null
  maxAmount: number | null
  supportedCurrencies: string
  lastHealthCheck: string | null
  healthStatus: GatewayHealthStatus
  createdAt: string
  modifiedAt: string | null
}

export interface CredentialField {
  key: string
  label: string
  type: CredentialFieldType
  required: boolean
  default?: string
  placeholder?: string
  helpText?: string
}

export interface GatewaySchema {
  displayName: string
  description: string
  iconUrl: string
  fields: CredentialField[]
  environments: {
    sandbox: Record<string, string>
    production: Record<string, string>
  }
}

export interface GatewaySchemas {
  [provider: string]: GatewaySchema
}

export interface ConfigureGatewayRequest {
  provider: string
  displayName: string
  environment: GatewayEnvironment
  credentials: Record<string, string>
  supportedMethods: string[]
  sortOrder: number
  isActive: boolean
}

export interface UpdateGatewayRequest {
  displayName?: string
  environment?: GatewayEnvironment
  credentials?: Record<string, string>
  supportedMethods?: string[]
  sortOrder?: number
  isActive?: boolean
}

export interface TestConnectionResult {
  success: boolean
  message: string
  responseTimeMs?: number
  errorCode?: string
}
```

---

## 7. State Management

### 7.1 usePaymentGateways Hook

```typescript
// src/hooks/usePaymentGateways.ts

export function usePaymentGateways() {
  const [gateways, setGateways] = useState<PaymentGateway[]>([])
  const [schemas, setSchemas] = useState<GatewaySchemas | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const refresh = useCallback(async () => { ... }, [])

  const toggleActive = useCallback(async (id: string, isActive: boolean) => { ... }, [])

  const testConnection = useCallback(async (id: string): Promise<TestConnectionResult> => { ... }, [])

  return {
    gateways,
    schemas,
    loading,
    error,
    refresh,
    toggleActive,
    testConnection,
  }
}
```

### 7.2 Form State (ConfigureGatewayDialog)

Use `react-hook-form` with `zod` validation:
- Dynamic schema based on selected provider
- Required field validation
- URL format validation for endpoint fields

---

## 8. User Experience Flows

### 8.1 First-Time Configuration

```
1. User opens Payment Gateways page
2. Sees 4 gateway cards, all showing "Not Configured"
3. Clicks "Configure" on VNPay card
4. Dialog opens with empty credential fields
5. User enters TmnCode and HashSecret
6. User clicks "Test Connection"
7. Success indicator shows green checkmark
8. User clicks "Save Configuration"
9. Dialog closes, card updates to "Configured" state
10. Toggle appears - user can now enable the gateway
```

### 8.2 Updating Credentials

```
1. User clicks "Configure" on already-configured gateway
2. Dialog opens with fields showing "••••••••" (masked)
3. Placeholder text: "Enter new value to update"
4. User types new secret in HashSecret field
5. User clicks "Test Connection"
6. User clicks "Save" to update
```

### 8.3 Switching to Production

```
1. User opens Configure dialog
2. Changes Environment dropdown from "Sandbox" to "Production"
3. Warning modal appears:
   "⚠️ Switching to Production Mode
    - Real transactions will be processed
    - Ensure credentials are for production environment
    [Cancel] [Confirm Switch]"
4. User confirms
5. Environment badge on card updates
```

---

## 9. Error Handling

| Scenario | UX Response |
|----------|-------------|
| API fetch fails | Toast error + retry button |
| Invalid credentials (test) | Inline error message in dialog |
| Save fails | Toast error, form stays open |
| Toggle fails | Toast error, revert toggle |
| Network timeout (test) | "Connection timed out after 10s" |

---

## 10. Accessibility

- All interactive elements have `cursor-pointer` class
- Form fields have proper labels and descriptions
- Error messages linked to fields via `aria-describedby`
- Status badges have `aria-label` for screen readers
- Toggle switches have accessible labels

---

## 11. Responsive Design

| Breakpoint | Layout |
|------------|--------|
| Mobile (<640px) | Single column card stack |
| Tablet (640-1024px) | 2-column grid |
| Desktop (>1024px) | 4-column grid (or 2x2) |

---

## 12. Implementation Checklist

### Backend (New)
- [ ] `GET /api/payment-gateways/schemas` endpoint
- [ ] `POST /api/payment-gateways/{id}/test` endpoint
- [ ] Add `configuredFields` to PaymentGatewayDto

### Frontend
- [ ] Create `payment.ts` types file
- [ ] Create `paymentGateways.ts` service
- [ ] Create `usePaymentGateways.ts` hook
- [ ] Create `PaymentGatewaysPage.tsx`
- [ ] Create `GatewayCard.tsx` component
- [ ] Create `ConfigureGatewayDialog.tsx` component
- [ ] Create `CredentialFields.tsx` component
- [ ] Create `TestConnectionButton.tsx` component
- [ ] Add route to router
- [ ] Add sidebar navigation item
- [ ] Add i18n translations

### Testing
- [ ] Unit tests for credential validation
- [ ] Integration tests for API endpoints

---

## 13. Next Steps

After design approval, use `/sc:implement` to build:
1. Backend schema endpoint
2. Backend test connection endpoint
3. Frontend components and page

---

**Approved By:** ___________________
**Date:** ___________________

# Research Report: Essential ERP/CMS Features for Business Applications

> **Date:** January 2026
> **Purpose:** Identify must-have and nice-to-have features for enterprise admin portals
> **Focus:** Tenant configuration, legal compliance, business settings

---

## Executive Summary

Based on research into enterprise frameworks (ABP Framework, Odoo, Zoho), SaaS platforms (Stripe, Shopify), and compliance requirements (GDPR, Azure Architecture), this report identifies **10 essential features** categorized by business necessity.

**NOIR Current State:**
- ✅ Already has `TenantSetting` entity with platform defaults + tenant overrides
- ✅ Has Email infrastructure (just needs UI for platform admin)
- ✅ Has Blog with `ScheduledPublishAt` support (needs UX improvement)
- ❌ Missing: Legal pages (Terms, Privacy), Business settings UI, Localization settings

---

## Research Findings

### 1. Legal/Compliance Pages (MUST HAVE)

**GDPR Requirements:**
- Privacy Policy page (mandatory for EU users)
- Terms of Service/Conditions (protects platform legally)
- Cookie consent management
- Right to data access/deletion (already have via soft-delete)

**Industry Standard Legal Pages:**

| Page | Purpose | Storage |
|------|---------|---------|
| **Terms & Conditions** | Legal agreement users must accept | TenantSetting (HTML content) |
| **Privacy Policy** | Data handling disclosure | TenantSetting (HTML content) |
| **Cookie Policy** | Cookie usage explanation | TenantSetting (HTML content) |
| **Acceptable Use Policy** | Usage rules | TenantSetting (HTML content) |
| **Refund Policy** | (For e-commerce) | TenantSetting (HTML content) |

**Implementation Pattern:**
- Store as TenantSetting with category "legal"
- Key examples: `legal.terms_and_conditions`, `legal.privacy_policy`
- DataType: "html" (rich text content)
- Allow tenant override for custom policies

### 2. Tenant/Organization Profile (MUST HAVE)

**Azure Multi-tenant Architecture Recommendations:**
- Legal/business name
- Branding (logo, colors, favicon)
- Contact information
- Data residency preferences
- Compliance requirements

**Recommended Tenant Profile Fields:**

| Field | Purpose | Already in NOIR? |
|-------|---------|------------------|
| Name | Display name | ✅ Yes |
| Identifier | URL slug | ✅ Yes |
| Description | About the tenant | ✅ Yes |
| Domain | Custom domain | ✅ Yes |
| Logo URL | Branding | ❌ No |
| Favicon URL | Branding | ❌ No |
| Primary Color | Theme | ❌ No |
| Contact Email | Support contact | ❌ No |
| Contact Phone | Support contact | ❌ No |
| Address | Business location | ❌ No |
| Timezone | Default timezone | ❌ No |
| Default Language | Localization | ❌ No |

### 3. ABP Framework Settings Pattern

ABP provides hierarchical settings:
```
Global Settings (Platform)
    └── Tenant Settings (Override)
        └── User Settings (Override)
```

**NOIR already implements this pattern** via `TenantSetting` entity with:
- `TenantId = null` → Platform default
- `TenantId = "xxx"` → Tenant override

### 4. Odoo ERP Core Modules (Reference)

Odoo provides these business modules:

| Category | Features |
|----------|----------|
| **Finance** | Invoicing, Expenses, Documents, Spreadsheets |
| **Sales** | CRM, POS, Subscriptions, Rental |
| **HR** | Employees, Recruitment, Time Off, Appraisals |
| **Marketing** | Email Marketing, SMS, Social Media, Events |
| **Services** | Projects, Timesheets, Field Service, Helpdesk |
| **Website** | Website Builder, eCommerce, Blog, Forums |

**Relevance to NOIR:** Most are domain-specific modules. For a **base template**, focus on:
- Settings management (done)
- User/Role management (done)
- Blog/CMS (done)
- Legal pages (missing)
- Branding/customization (missing)

### 5. Zoho CRM Organization Features

- Customizable layouts and page designs
- Client scripting for custom functionalities
- Portal creation for external users
- Blueprint for structured processes
- Workflow automation

**Relevance to NOIR:** These are advanced CRM features. For base template:
- Organization profile settings (missing)
- Custom branding (missing)

### 6. Stripe Platform Agreements

Stripe requires platforms to:
- Present Terms of Service to users
- Collect acceptance acknowledgment
- Store acceptance timestamp
- Cannot modify agreement type after acceptance

**Pattern for NOIR:**
- Store `TermsAcceptedAt` on User entity
- Track which version was accepted
- Require re-acceptance on significant changes

---

## Prioritized Feature List (10 Features)

### Tier 1: MUST HAVE (Almost Every Business Needs)

| # | Feature | Category | Complexity | Notes |
|---|---------|----------|------------|-------|
| **1** | **Platform SMTP Settings UI** | Platform Config | Low | You requested this - UI for email configuration |
| **2** | **Blog Scheduling UX** | Content | Low | Single save with publish date picker |
| **3** | **Legal Pages (Terms & Privacy)** | Compliance | Medium | Store in TenantSetting, render on public pages |
| **4** | **Tenant Branding Settings** | Customization | Low | Logo, favicon, primary color per tenant |
| **5** | **Tenant Contact Information** | Business | Low | Email, phone, address fields |

### Tier 2: SHOULD HAVE (Common Business Requirements)

| # | Feature | Category | Complexity | Notes |
|---|---------|----------|------------|-------|
| **6** | **Timezone & Localization Settings** | Regional | Low | Default timezone, language per tenant |
| **7** | **Platform Security Settings UI** | Security | Low | Password policy, lockout policy (backend exists) |
| **8** | **Terms Acceptance Tracking** | Compliance | Medium | Track when users accepted terms |
| **9** | **Tenant Settings Admin UI** | Admin | Medium | Generic UI to manage TenantSettings |

### Tier 3: NICE TO HAVE (Future Enhancement)

| # | Feature | Category | Complexity | Notes |
|---|---------|----------|------------|-------|
| **10** | **Custom Fields Framework** | Extensibility | High | Allow tenants to add custom fields |

---

## Detailed Feature Specifications

### Feature 1: Platform SMTP Settings UI

**Purpose:** Platform admin can configure email without editing appsettings.json

**Implementation:**
- Add UI page at `/portal/admin/platform-settings/email`
- Read/write to TenantSetting with TenantId = null
- Keys: `smtp.host`, `smtp.port`, `smtp.username`, `smtp.password` (encrypted), `smtp.enable_ssl`, `smtp.default_from_email`, `smtp.default_from_name`

**UI:**
```
┌─────────────────────────────────────────────────────────┐
│  Platform Settings > Email (SMTP)                       │
├─────────────────────────────────────────────────────────┤
│  SMTP Host:        [smtp.example.com            ]      │
│  SMTP Port:        [587                         ]      │
│  Username:         [user@example.com            ]      │
│  Password:         [••••••••••                  ]      │
│  Enable SSL:       [✓]                                 │
│  From Email:       [noreply@example.com         ]      │
│  From Name:        [NOIR Platform               ]      │
│                                                         │
│  [Test Connection]                    [💾 Save]        │
└─────────────────────────────────────────────────────────┘
```

### Feature 2: Blog Scheduling UX

**Purpose:** Single "Save" button with inline publishing options

**Current Flow:** Save draft → Separate "Publish" action
**New Flow:** Radio selection determines save behavior

**UI:**
```
┌─────────────────────────────────────────────────────────┐
│  Publishing Options                                     │
├─────────────────────────────────────────────────────────┤
│  ○ Save as Draft                                        │
│    Post will not be visible to public                   │
│                                                         │
│  ○ Publish Immediately                                  │
│    Post will be visible right away                      │
│                                                         │
│  ○ Schedule for Later                                   │
│    [📅 2026-01-25]  [🕐 09:00]                         │
│    Post will auto-publish at this date/time             │
├─────────────────────────────────────────────────────────┤
│  [💾 Save]                                              │
└─────────────────────────────────────────────────────────┘
```

### Feature 3: Legal Pages (Terms & Privacy)

**Purpose:** Every business needs Terms of Service and Privacy Policy

**Storage:** TenantSetting table
```
| Key                    | Value (HTML) | TenantId | Category |
|------------------------|--------------|----------|----------|
| legal.terms            | <html>...    | null     | legal    |
| legal.privacy          | <html>...    | null     | legal    |
| legal.terms            | <html>...    | tenant1  | legal    | ← Override
```

**Admin UI:**
- Rich text editor (TinyMCE) for editing legal pages
- Preview rendered HTML
- Option to use platform default or create tenant override

**Public Pages:**
- `/terms` → Render `legal.terms` for current tenant (or platform default)
- `/privacy` → Render `legal.privacy` for current tenant

### Feature 4: Tenant Branding Settings

**Purpose:** Allow tenants to customize their look and feel

**Fields:**
```csharp
// Stored in TenantSetting with appropriate keys
branding.logo_url         // URL to logo image
branding.favicon_url      // URL to favicon
branding.primary_color    // Hex color (e.g., #3B82F6)
branding.secondary_color  // Hex color
branding.dark_mode        // bool - default theme preference
```

**UI in Tenant Settings:**
```
┌─────────────────────────────────────────────────────────┐
│  Tenant Settings > Branding                             │
├─────────────────────────────────────────────────────────┤
│  Logo URL:        [https://...                  ] 📷   │
│  Favicon URL:     [https://...                  ] 📷   │
│  Primary Color:   [#3B82F6] [████]                     │
│  Secondary Color: [#1E40AF] [████]                     │
│  Default Theme:   ○ Light  ● Dark                      │
│                                                         │
│  [💾 Save]                                              │
└─────────────────────────────────────────────────────────┘
```

### Feature 5: Tenant Contact Information

**Purpose:** Store business contact details

**Fields:**
```csharp
contact.email           // Support email
contact.phone           // Support phone
contact.address_line1   // Street address
contact.address_line2   // Suite/Unit
contact.city
contact.state
contact.postal_code
contact.country
```

### Feature 6: Timezone & Localization

**Purpose:** Regional settings per tenant

**Fields:**
```csharp
regional.timezone       // e.g., "Asia/Ho_Chi_Minh"
regional.default_language // e.g., "vi"
regional.date_format    // e.g., "DD/MM/YYYY"
regional.time_format    // e.g., "HH:mm"
regional.currency       // e.g., "VND"
```

### Feature 7: Platform Security Settings UI

**Purpose:** UI to configure password policy (backend already exists in appsettings.json)

**Keys:**
```csharp
security.password.min_length      // int
security.password.require_digit   // bool
security.password.require_uppercase // bool
security.lockout.max_attempts     // int
security.lockout.duration_minutes // int
security.session.timeout_minutes  // int
```

### Feature 8: Terms Acceptance Tracking

**Purpose:** Legal compliance - track when users accepted terms

**Implementation:**
Add to User entity or create UserConsent entity:
```csharp
public class UserConsent : Entity<Guid>
{
    public string UserId { get; set; }
    public string ConsentType { get; set; }  // "terms", "privacy", "marketing"
    public string Version { get; set; }      // Track which version
    public DateTimeOffset AcceptedAt { get; set; }
    public string IpAddress { get; set; }
}
```

### Feature 9: Tenant Settings Admin UI

**Purpose:** Generic UI to view/edit all TenantSettings

**UI:**
```
┌─────────────────────────────────────────────────────────┐
│  Tenant Settings                        [+ Add Setting] │
├──────────────┬──────────────┬──────────┬───────────────┤
│  Key         │  Value       │  Type    │  Category     │
├──────────────┼──────────────┼──────────┼───────────────┤
│  smtp.host   │  smtp.goo... │  string  │  email        │
│  smtp.port   │  587         │  int     │  email        │
│  legal.terms │  <html>...   │  html    │  legal        │
└──────────────┴──────────────┴──────────┴───────────────┘
```

---

## Implementation Recommendation

Based on your requirements ("essential business features"), I recommend implementing in this order:

### Phase 5a: Platform & Blog (Low Effort, High Value)
1. ✅ **Platform SMTP Settings UI** - Requested by you
2. ✅ **Blog Scheduling UX** - Single save button improvement

### Phase 5b: Legal Compliance (Medium Effort, High Value)
3. ✅ **Legal Pages (Terms & Privacy)** - Almost every business needs this
4. ✅ **Terms Acceptance Tracking** - GDPR compliance

### Phase 5c: Tenant Customization (Low Effort, Medium Value)
5. ✅ **Tenant Branding** - Logo, colors
6. ✅ **Tenant Contact Info** - Business contact details
7. ✅ **Timezone & Localization** - Regional settings

### Phase 6: Admin Polish (Medium Effort, Medium Value)
8. **Platform Security Settings UI** - Password policy UI
9. **Generic Tenant Settings UI** - Admin tool for all settings

---

## Sources

- [ABP Framework Setting Management](https://abp.io/docs/latest/modules/setting-management)
- [ABP Framework Feature Management](https://abp.io/docs/latest/modules/feature-management)
- [Azure Multi-tenant Architecture - Tenant Lifecycle](https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/considerations/tenant-lifecycle)
- [GDPR Requirements](https://gdpr.eu/what-is-gdpr/)
- [Stripe Connect Service Agreements](https://docs.stripe.com/connect/service-agreement-types)
- [Shopify App Requirements](https://shopify.dev/docs/apps/store/requirements)
- [Odoo ERP Modules](https://www.odoo.com/page/all-apps)
- [Zoho CRM Features](https://www.zoho.com/crm/features.html)

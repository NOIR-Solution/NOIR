# Module: CRM (Basic)

> Priority: **Phase 3** (after PM). Complexity: Medium.
>
> Scope: Contact, Company, Lead, Pipeline Kanban. **No Activities log, no Reports, no Import/Export, no Custom Fields.** Keep it lightweight.

---

## Why This Module

Customers module (e-commerce) tracks people who **already bought**. CRM tracks people **before they buy** — who's interested, what deals are in progress.

**Strategy**: Keep Customers (e-commerce) and Contacts (CRM) as **separate entities**. When a deal is won, auto-create a Customer from the Contact.

---

## Entities

```
CrmContact (TenantAggregateRoot<Guid>)
├── Id, FirstName, LastName, Email, Phone
├── JobTitle
├── CompanyId (FK → CrmCompany, nullable)
├── OwnerId (FK → Employee or User — who manages this contact)
├── Source (Web/Referral/Social/Cold/Event/Other)
├── CustomerId (FK → Customer, nullable — linked when deal won)
├── Notes (text)
├── TenantId
└── Leads[] (one-to-many)

CrmCompany (TenantAggregateRoot<Guid>)
├── Id, Name, Domain, Industry (text)
├── Address, Phone, Website
├── OwnerId (FK → Employee or User)
├── TaxId (generic — works for any country)
├── EmployeeCount (int, nullable)
├── Notes (text)
├── TenantId
└── Contacts[] (one-to-many)

Lead (TenantAggregateRoot<Guid>)
├── Id, Title (deal name)
├── ContactId (FK → CrmContact)
├── CompanyId (FK → CrmCompany, nullable)
├── Value (decimal — estimated deal value), Currency (default from tenant)
├── OwnerId (FK → Employee or User)
├── PipelineId (FK → Pipeline)
├── StageId (FK → PipelineStage)
├── SortOrder (float — for Kanban drag ordering within stage)
├── ExpectedCloseDate (nullable)
├── WonAt, LostAt, LostReason (nullable)
├── Notes (text)
└── TenantId

Pipeline (TenantAggregateRoot<Guid>)
├── Id, Name, IsDefault (one default per tenant)
├── TenantId
└── Stages[] (one-to-many, ordered)

PipelineStage (TenantEntity)
├── Id, PipelineId (FK)
├── Name, SortOrder, Color (hex)
└── TenantId
```

**4 entities. No Activities, no custom fields, no merge logic.** Simple.

---

## Features (Commands + Queries)

### Contact
| Command/Query | Description |
|---------------|-------------|
| `CreateContactCommand` | Create contact, optionally link to company |
| `UpdateContactCommand` | Update details |
| `DeleteContactCommand` | Soft delete |
| `GetContactsQuery` | Paginated, search by name/email, filter by company/owner/source |
| `GetContactByIdQuery` | Detail with company, leads list |

### Company
| Command/Query | Description |
|---------------|-------------|
| `CreateCompanyCommand` | Create company |
| `UpdateCompanyCommand` | Update details |
| `DeleteCompanyCommand` | Soft delete (warn if has contacts) |
| `GetCompaniesQuery` | Paginated, search by name/domain |
| `GetCompanyByIdQuery` | Detail with contacts list |

### Lead / Deal
| Command/Query | Description |
|---------------|-------------|
| `CreateLeadCommand` | Create lead linked to contact, placed in pipeline stage |
| `UpdateLeadCommand` | Update title, value, dates, notes |
| `MoveLeadStageCommand` | Drag to different stage (Kanban) |
| `ReorderLeadCommand` | Change SortOrder within stage |
| `WonLeadCommand` | Mark as won → auto-create Customer if Contact.CustomerId is null |
| `LostLeadCommand` | Mark as lost with reason |
| `GetLeadsQuery` | Filter by pipeline/stage/owner |
| `GetLeadByIdQuery` | Full detail with contact, company |
| `GetPipelineViewQuery` | Stages with leads for Kanban board |

### Pipeline
| Command/Query | Description |
|---------------|-------------|
| `CreatePipelineCommand` | Create custom pipeline with stages |
| `UpdatePipelineCommand` | Update name, add/remove/reorder stages |
| `DeletePipelineCommand` | Soft delete (fail if has active leads) |
| `GetPipelinesQuery` | List pipelines |

---

## Contact → Customer Sync

```
WonLeadCommandHandler:
  1. Set Lead.WonAt = now
  2. Check Contact.CustomerId
     → Not null: already a Customer, done
     → Null: create Customer from Contact data (Name, Email, Phone)
       → Set Contact.CustomerId = new Customer.Id
  3. Fire LeadWonEvent (for webhooks/notifications)
```

Simple one-way sync. No bidirectional sync complexity.

---

## Frontend Pages

| Route | Page | Features |
|-------|------|----------|
| `/portal/crm/contacts` | Contact list | Table, search, filter by company/owner/source, create dialog |
| `/portal/crm/contacts/:id` | Contact detail | Profile, company link, leads list, link to Customer if exists |
| `/portal/crm/companies` | Company list | Table, search, create dialog |
| `/portal/crm/companies/:id` | Company detail | Info, contacts list |
| `/portal/crm/pipeline` | Pipeline Kanban | Drag-and-drop leads across stages, pipeline selector dropdown |
| `/portal/crm/pipeline/deals/:id` | Deal detail | Lead info, contact, company, won/lost actions |

### Key UI Components
- **PipelineKanban**: Stages as columns, lead cards draggable between stages (reuse `@dnd-kit` from PM)
- **LeadCard**: Compact: title, value (formatted), contact name, expected close date
- **ContactForm**: Create/edit dialog with company autocomplete
- **StageColumnHeader**: Stage name + total value of leads in stage

---

## Integration Points

| Module | Integration |
|--------|-------------|
| **Customers** | Auto-create Customer on deal won |
| **HR/Employees** | Contact/Lead owner = Employee |
| **Notifications** | Lead won/lost events |
| **Webhooks** | lead.created, lead.won, lead.lost, contact.created |
| **Activity Timeline** | Lead stage changes in audit log |

---

## Phased Implementation

### Phase 1 — Contacts + Companies + Pipeline (Full MVP)
```
Backend:
├── Domain: CrmContact, CrmCompany, Lead, Pipeline, PipelineStage
├── Application: All CRUD commands/queries listed above
├── Infrastructure: EF configs, repos, migration
├── Endpoints: CrmContactEndpoints, CrmCompanyEndpoints, LeadEndpoints, PipelineEndpoints
├── Module: CrmModuleDefinition
├── Permissions: crm:contacts:read, crm:contacts:create, crm:contacts:update, crm:contacts:delete
│             crm:companies:read, crm:companies:manage
│             crm:leads:read, crm:leads:create, crm:leads:update, crm:leads:manage
│             crm:pipeline:manage
├── Contact→Customer sync: WonLeadCommandHandler
├── Seed: Default pipeline (New, Contacted, Qualified, Proposal, Negotiation, Won, Lost)
└── Tests: Unit + integration

Frontend:
├── Pages: Contact list, Contact detail, Company list, Company detail, Pipeline Kanban, Deal detail
├── Components: PipelineKanban, LeadCard, ContactForm, CompanyForm
├── Sidebar: CRM section (Contacts, Companies, Pipeline)
├── i18n: EN + VI
└── Hooks: useContacts, useCompanies, useLeads, usePipeline, usePipelineView
```

This module is small enough to ship in **one phase**.

---

## Architecture Notes

### Module Definition
```csharp
public sealed class CrmModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Erp.Crm;
    public string DisplayNameKey => "modules.erp.crm";
    public string Icon => "Users";
    public int SortOrder => 300;
    public bool IsCore => false;
    public bool DefaultEnabled => true;
    public IReadOnlyList<FeatureDefinition> Features =>
    [
        new(ModuleNames.Erp.Crm + ".Contacts", "modules.erp.crm.contacts", "..."),
        new(ModuleNames.Erp.Crm + ".Companies", "modules.erp.crm.companies", "..."),
        new(ModuleNames.Erp.Crm + ".Pipeline", "modules.erp.crm.pipeline", "..."),
    ];
}
```

### Kanban Reuse
Pipeline Kanban shares the same drag-and-drop approach as PM Kanban:
- `@dnd-kit/core` + `@dnd-kit/sortable`
- Float-based SortOrder for insert-between
- Optimistic updates on drag

### Naming Convention
CRM entities prefixed with `Crm` to avoid collisions:
- `CrmContact` (not `Contact` — could conflict with future modules)
- `CrmCompany` (not `Company`)
- `Lead` (no prefix needed, unique enough)
- `Pipeline` / `PipelineStage` (no prefix needed)

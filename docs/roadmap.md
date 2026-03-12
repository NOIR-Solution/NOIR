# NOIR Product Roadmap

> Outcome-based roadmap using **Now / Next / Later** framework. Each item states the problem it solves, not just features.

**Last Updated:** 2026-03-01

---

## Vision

NOIR is a comprehensive, AI-built enterprise SaaS foundation — e-commerce core expanding into full ERP.

---

## At a Glance

| Horizon | Theme | Status |
|---------|-------|--------|
| **Done** | Core Foundation + E-commerce + Admin Portal + HR + CRM + PM | Shipped |
| **Now** | Calendar Module (fourth ERP module) | Design Ready |
| **Future** | Mobile, Plugin Ecosystem | Aspirational |

---

## Done

### Core Foundation

> **Outcome:** Establish a multi-tenant, secure, developer-friendly foundation that all future modules build on.

- Clean Architecture + Vertical Slice + CQRS (Wolverine)
- Multi-tenancy with Finbuckle (header + claim strategy)
- JWT auth with refresh tokens and RBAC
- Hierarchical audit logging (HTTP → command → entity)
- Email system with database-driven templates
- React 19 SPA + 98 UIKit components + Storybook (677/677 stories)
- PWA + SignalR + SSE + multi-tab session sync
- MCP Server with 55 AI-callable tools, 6 prompts, 7 resources
- 13,546 automated tests (12,715 backend + 831 frontend)

### E-commerce Platform

> **Outcome:** Full e-commerce lifecycle — from browsing to delivery — so tenants can sell products online.

- Product catalog (variants, SKUs, hierarchical categories, 13-type attributes)
- Cart → Checkout → Order → Payment → Shipping → Delivery lifecycle
- Inventory receipts (StockIn/StockOut)
- Customers, groups, reviews, wishlists, promotions
- Reports (revenue, orders, inventory, product performance)
- Webhooks + Blog CMS + Feature management (31 modules)

---

### Admin Portal Enhancement

> **Outcome:** Make the admin portal productive for daily operations — fast search, bulk actions, data import/export.

- Dashboard widgets with real-time metrics (4 feature-gated groups, recharts)
- Media Manager with drag-and-drop upload, grid/list views, bulk delete
- Global search with Cmd+K content search across 5 entity types
- Import/Export (CSV + Excel) for products, orders, customers
- Bulk actions across 6 entity list pages with shared infrastructure

### HR Module (First ERP Module)

> **Outcome:** Define "who is in the organization and where they sit" — the prerequisite for all people-dependent modules (CRM, PM).

- Employee profiles (CRUD, auto-code, hierarchy validation, User account sync)
- Department hierarchy (tree, self-referencing, manager, reorder)
- Employee tags with 7 categories (Team/Skill/Project/Location/Seniority/Employment/Custom)
- Org chart with d3-org-chart (zoom, pan, search, PNG export)
- Bulk operations (assign tags, change department)
- Import/Export (CSV import, Excel/CSV export)
- HR reports (headcount, tag distribution, employment type, status breakdown)

**Design Doc:** [module-hr.md](designs/module-hr.md) — Score: 100/100

### CRM Module (Second ERP Module)

> **Outcome:** Sales teams can manage contacts, leads, and deal pipelines — building on HR's employee/department foundation.

- Contacts, Companies, Leads with full CRUD and search
- Pipeline Kanban board with drag-and-drop stage progression
- Activities (calls, meetings, emails, tasks) per contact/company/lead
- CRM dashboard widgets (contacts, active pipeline, conversion rate)
- Domain events for webhook integration
- Default pipeline seeding for new tenants

**Design Doc:** [module-crm.md](designs/module-crm.md) — Score: 100/100

### PM Module (Third ERP Module)

> **Outcome:** Teams can manage projects, track tasks, and collaborate — building on HR's employee foundation.

- Projects with auto-code (PRJ-YYYYMMDD-NNNNNN), status lifecycle, member management
- Kanban board with columns, WIP limits, drag-and-drop
- Tasks with priority, labels, comments, subtasks (depth ≤ 3), attachments
- Task list view, project member avatars, column settings, label manager
- Domain events for webhook integration (ProjectCreated, TaskMoved)

**Design Doc:** [module-pm.md](designs/module-pm.md) — Score: 100/100

---

## Now — Calendar Module (Fourth ERP Module)

> **Outcome:** Teams need event scheduling, resource booking, and shared calendars.

**Design Doc:** [module-calendar.md](designs/module-calendar.md) — Design Ready.

---

### Dependency Graph

```
HR (Done) ──────────┬──→ CRM (Done)
                    └──→ PM (Done)

Standalone ─────────────→ Calendar (Now)
```

### Design Status Legend

| Status | Meaning |
|--------|---------|
| **Ready** | Complete — DTOs, flows, NOIR patterns, edge cases. Implementable without ambiguity. |
| **Draft** | Initial concept — entities and features listed. Needs NOIR pattern review, DTOs, edge cases. |

---

## Future — Platform Ecosystem

> **Outcome:** Transform from a single product into a platform — third-party extensions and mobile apps.

| Module | Problem It Solves | Design |
|--------|-------------------|--------|
| Plugin ecosystem | Tenants need custom extensions without forking | Not Started |
| Third-party integrations | Connect to Zapier, external APIs, and webhooks | Not Started |
| White-label | Tenants need custom branding and domain mapping | Not Started |
| Mobile apps | Users need native mobile experience (React Native) | Not Started |
| BI dashboards | Management needs advanced analytics and reporting | Not Started |

---

## Technical Debt (Ongoing)

Addressed continuously across all horizons:

- [ ] Validation unification (FluentValidation ↔ Zod sync) — [Research](backend/research/validation-unification-plan.md)
- [ ] Vietnam shipping provider integration — [Research](backend/research/vietnam-shipping-integration-2026.md)
- [ ] Cache busting strategy refinement — [Research](backend/research/cache-busting-best-practices.md)
- [ ] SEO meta optimization — [Research](backend/research/seo-meta-and-hint-text-best-practices.md)

---

## How This Roadmap Works

- **Now** = actively in development, high confidence, scoped with design docs
- **Next** = committed, design complete, implementation starts when "Now" ships
- **Later** = planned order based on dependencies, designs in progress
- **Future** = aspirational, no commitments, shaped by market needs
- Items move left as they become clearer: Future → Later → Next → Now → Done
- Each module design doc lives in `docs/designs/module-*.md`
- All modules use the feature management system for tenant-level enable/disable

---

*100% AI-coded using Claude Code. All architecture, implementation, testing, and documentation generated through AI-assisted development.*

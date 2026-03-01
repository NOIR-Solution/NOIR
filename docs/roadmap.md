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
| **Done** | Core Foundation + E-commerce + Admin Portal | Shipped |
| **Now** | HR Module (first ERP module) | Design Ready |
| **Next** | CRM Module | Design Draft |
| **Later** | CRM → PM → Calendar → Support Center | Designs in Draft |
| **Future** | Marketplace, Chat, Mobile, Plugin Ecosystem | Aspirational |

---

## Done

### Core Foundation

> **Outcome:** Establish a multi-tenant, secure, developer-friendly foundation that all future modules build on.

- Clean Architecture + Vertical Slice + CQRS (Wolverine)
- Multi-tenancy with Finbuckle (header + claim strategy)
- JWT auth with refresh tokens and RBAC
- Hierarchical audit logging (HTTP → command → entity)
- Email system with database-driven templates
- React 19 SPA + 98 UIKit components + Storybook (674/674 stories)
- PWA + SignalR + SSE + multi-tab session sync
- 11,341+ automated tests

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

**Design Docs:** [Enhancement Plan](designs/admin-portal-enhancement-v28-feb.md) | [Design Spec](designs/admin-portal-enhancement-v28-feb-design.md)

---

## Now — HR Module (First ERP Module)

> **Outcome:** Define "who is in the organization and where they sit" — the prerequisite for all people-dependent modules (CRM, PM, Support Center).

| Deliverable | Design | Impl |
|-------------|--------|------|
| Employee profiles (CRUD, auto-code, User account sync) | Ready | **Phase 1 Done** |
| Department hierarchy (tree, self-referencing, manager) | Ready | **Phase 1 Done** |
| Employee tags with categories (Team/Skill/Project/etc.) | Ready | **Phase 2 Done** |
| Org chart (d3-org-chart), bulk ops, import/export, reports | Ready | **Phase 3 Done** |

**Design Doc:** [module-hr.md](designs/module-hr.md) — 100% complete (DTOs, sequence diagrams, NOIR pattern compliance, edge cases).

---

## Later — ERP Module Expansion

> **Outcome:** Expand from e-commerce into full enterprise operations — customer relationships, project management, scheduling, and support.

### Module Pipeline

| # | Module | Problem It Solves | Depends On | Design | Design Doc |
|---|--------|-------------------|------------|--------|------------|
| 2 | **CRM** | Sales teams need to manage contacts, leads, and deal pipelines | HR | Ready | [module-crm.md](designs/module-crm.md) |
| 3 | **Project Management** | Teams need to track tasks, milestones, and deadlines | HR | Ready | [module-pm.md](designs/module-pm.md) |
| 4 | **Calendar** | Teams need event scheduling and resource booking | Standalone | Draft | [module-calendar.md](designs/module-calendar.md) |
| 5 | **Support Center** | Unified helpdesk ticketing + knowledge base for self-service and support | HR, Customers | Draft | [module-support-center.md](designs/module-support-center.md) |

### Dependency Graph

```
HR (Next) ─────────┬──→ CRM
                   ├──→ Project Management
                   └──→ Support Center (+ Customers)

Standalone ────────┴──→ Calendar
```

### Design Status Legend

| Status | Meaning |
|--------|---------|
| **Ready** | Complete — DTOs, flows, NOIR patterns, edge cases. Implementable without ambiguity. |
| **Draft** | Initial concept — entities and features listed. Needs NOIR pattern review, DTOs, edge cases. |

---

## Future — Platform Ecosystem

> **Outcome:** Transform from a single product into a platform — third-party extensions, multi-vendor marketplace, and mobile apps.

| Module | Problem It Solves | Design |
|--------|-------------------|--------|
| **Chat** | Internal teams need real-time messaging and channels | [Draft](designs/module-chat.md) |
| **Marketplace** | Vendors need multi-store management with commission tracking | [Draft](designs/module-marketplace.md) |
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
- All modules use the [feature management system](designs/admin-portal-gaps.md) for tenant-level enable/disable

---

*100% AI-coded using Claude Code. All architecture, implementation, testing, and documentation generated through AI-assisted development.*

# Regression Flow — P0 + P1 Cases (~1-2h)

> 438 cases (50 P0 + 388 P1) | Dependencies first, then by domain
> Last updated: 2026-04-03

## Execution Order

### Phase 1: Auth & Security (prerequisite)
All TC-AUTH P0+P1 cases → must pass before other domains

### Phase 2: Core Infrastructure
- TC-SET P0+P1 cases (settings, users, roles, tenants, feature management)
- TC-DSH P0+P1 cases (dashboard, reports, welcome)

### Phase 3: E-commerce Core
- TC-CAT P0+P1 cases (products, categories, attributes, brands, inventory)
- TC-ORD P0+P1 cases (orders, payments, shipping)

### Phase 4: Customer Engagement
- TC-CUS P0+P1 cases (customers, groups, promotions, reviews, wishlists)
- TC-CON P0+P1 cases (blog posts, categories, tags, media library)

### Phase 5: ERP Modules
- TC-HR P0+P1 cases (employees, departments, tags, org chart, reports)
- TC-CRM P0+P1 cases (contacts, companies, pipeline, deals)
- TC-PM P0+P1 cases (projects, kanban, tasks)

## Case Counts by Domain

| Domain | P0 | P1 | Total |
|--------|----|----|-------|
| Auth | 2 | 19 | 21 |
| Dashboard | 3 | 29 | 32 |
| Settings | 4 | 54 | 58 |
| Catalog | 9 | 48 | 57 |
| Orders | 10 | 48 | 58 |
| Customers | 3 | 45 | 48 |
| Content | 6 | 38 | 44 |
| HR | 4 | 34 | 38 |
| CRM | 5 | 41 | 46 |
| PM | 4 | 32 | 36 |
| **Total** | **50** | **388** | **438** |

## Regression-Tagged Cases (Known Bug Prevention)

Extract from all files with [regression] tag — execute these even if domain appears unaffected:
- Auth: audit description translation (TC-AUTH regression cases)
- Dashboard: Quick Action label truncation, CRM widget Vietnamese (TC-DSH regression cases)
- Settings: English in Vietnamese translations (TC-SET regression cases)
- All domains: date formatting consistency, empty state components

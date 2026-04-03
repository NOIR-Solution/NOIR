# Full Flow — ALL Priorities (~3-4h)

> 801 cases total | P0: 50 | P1: 388 | P2: 314 | P3: 49
> Last updated: 2026-04-03

## Execution Order

Same phased order as regression.md, but includes P2 + P3:

### Phase 1: Auth & Security — 48 cases
All TC-AUTH cases (P0→P1→P2→P3)

### Phase 2: Core Infrastructure — 178 cases
- TC-SET: 112 cases
- TC-DSH: 66 cases (includes TC-RPT + welcome)

### Phase 3: E-commerce Core — 193 cases
- TC-CAT: 98 cases
- TC-ORD: 95 cases

### Phase 4: Customer Engagement — 160 cases
- TC-CUS: 78 cases
- TC-CON: 82 cases

### Phase 5: ERP Modules — 222 cases
- TC-HR: 78 cases
- TC-CRM: 82 cases
- TC-PM: 62 cases

## Edge Cases (P2) — 314 cases
Boundary values, concurrent ops, invalid URLs, permission boundaries, state transitions

## Cosmetic (P3) — 49 cases
Typography, spacing, animation consistency, minor visual polish

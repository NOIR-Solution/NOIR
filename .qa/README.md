# NOIR QA System

> Managed by `/noir-qa` orchestrator. Do not edit manually unless you know what you're doing.

## Structure

```
.qa/
├── cases/           # Test cases by feature domain (committed)
│   ├── auth.md      # Login, profile, password reset
│   ├── dashboard.md # Dashboard widgets, metrics
│   ├── orders.md    # Order CRUD, status transitions
│   └── ...          # One file per feature domain
├── flows/           # Test suites with execution order (committed)
│   ├── smoke.md     # P0 critical path (~15 min)
│   ├── regression.md# Full regression (~2h)
│   └── cross-feature.md # Multi-feature flows
├── results/         # Execution results (gitignored)
│   └── latest.md    # Most recent run
├── state.json       # Last checked git commit (gitignored)
└── README.md        # This file
```

## Commands

| Command | What it does |
|---------|-------------|
| `/noir-qa` | Full QA: update cases → organize flows → execute all → report |
| `/noir-qa test <feature>` | Targeted: filter cases/flows by feature keyword → execute |
| `/noir-qa update` | Git diff → update/add test cases only (no execution) |
| `/noir-qa execute` | Execute existing cases/flows (no update) |
| `/noir-qa fix` | Read latest results → fix all issues → re-execute failed cases |

## Test Case Format

Each `.qa/cases/{feature}.md` follows this structure:

```markdown
# {Feature} — Test Cases
> Pages: /portal/... | Last updated: YYYY-MM-DD | Git ref: abc1234 | Total: N cases

## Page: {Page Name} (`/portal/path`)

### Happy Path
#### TC-{FTR}-001: {Title} [P1] [smoke]
- **Pre**: {Precondition}
- **Steps**: {Numbered steps}
- **Expected**: {What should happen}
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Count correct | ☐ Sort works | ☐ Search works

### Edge Cases
#### TC-{FTR}-020: {Title} [P2]
...

### Regression
#### TC-{FTR}-040: {Title} [P2]
...
```

## Priority Levels

| Priority | Meaning | When to run |
|----------|---------|-------------|
| P0 | Critical path — app unusable if broken | Every commit (smoke) |
| P1 | Core functionality — feature broken | Every PR (regression) |
| P2 | Edge cases — non-obvious scenarios | Weekly / pre-release |
| P3 | Cosmetic / nice-to-have | Monthly / manual |

## Tags

`[smoke]` `[regression]` `[edge-case]` `[visual]` `[dark-mode]` `[i18n]` `[responsive]` `[data-consistency]` `[cross-feature]` `[security]` `[performance]`

# NOIR — Documentation Audit & Maintenance

## Objective

Audit ALL documentation in the project. Every doc file must earn its place.
After this task, the docs should be accurate, current, and useful — zero stale content.

## Execution: See `.claude/rules/team-coordination.md`

### Team Roles

| Role | Responsibility |
|------|---------------|
| `docs-auditor` | Inventory all docs, cross-reference with codebase, classify each file |
| `docs-writer` | Update outdated docs, remove obsolete ones, create missing ones |
| `claude-md-updater` | (optional 3rd) Focus specifically on CLAUDE.md + README.md |

**Note:** If `docs-auditor` scope is too large (50+ doc files), split into `backend-docs-auditor` + `frontend-docs-auditor`.

---

## Decision Framework (Apply to EVERY Doc File)

```
1. Does this doc describe something that STILL EXISTS in the codebase?
   NO  → Is it a decision record worth preserving?
         NO  → DELETE the file
         YES → Mark as "historical" or move to docs/decisions/

2. Is the content ACCURATE and CURRENT?
   NO  → UPDATE it to match current codebase reality
   YES → KEEP as-is

3. Is there important knowledge NOT documented?
   YES → CREATE the missing doc
```

---

## Scope: What to Audit

### 1. `docs/` folder (ALL files recursively)
- `docs/DOCUMENTATION_INDEX.md` — Does it list all current docs? Any dead links?
- `docs/KNOWLEDGE_BASE.md` — Is every entry still accurate?
- `docs/PROJECT_INDEX.md` — Does the file tree match actual project structure?
- `docs/FEATURE_CATALOG.md` — Does it list all current features? Missing any?
- `docs/TECH_STACK.md` — Are versions correct? Any added/removed dependencies?
- `docs/backend/patterns/*.md` — Does each pattern doc match current code?
- `docs/backend/research/*.md` — Still relevant or task already done?
- `docs/frontend/*.md` — Does frontend architecture doc match current code?
- `docs/decisions/*.md` — ADRs are historical records — keep but verify accuracy

### 2. `CLAUDE.md` (root)
- Critical rules still accurate? Any new rules needed?
- Build/test/migration commands still work?
- Project structure tree matches actual?
- Code examples match current implementation?
- Counts (test count, story count) verified?

### 3. `README.md`, `AGENTS.md`, `.claude/` folder
- Setup instructions work? Feature list current? Tech stack versions correct?
- Agent instructions accurate? Referenced file paths still exist?
- Routing rules current? Skill names match available skills?

---

## Process

### Phase 0: Inventory & Classify

**docs-auditor produces:**
```
| File Path | Status | Action | Reason |
|-----------|--------|--------|--------|
| docs/backend/patterns/xyz.md | Outdated | UPDATE | References old handler pattern |
```

Cross-reference: verify file paths, class/method signatures, version numbers, and patterns.

### Phase 1: Execute Changes

**docs-writer:** DELETE obsolete, UPDATE outdated, CREATE missing, update DOCUMENTATION_INDEX.md.
**claude-md-updater (parallel):** Verify CLAUDE.md, README.md, bump changelog.

### Phase 2: Verification

Review all changes, verify no broken cross-references, run quality gates.

---

## Rules

- Follow ALL rules in CLAUDE.md
- Don't create docs for the sake of having docs — every file must be useful
- Prefer updating over deleting when the topic is still relevant
- ADRs are historical — update status but don't delete
- No empty/placeholder docs — if creating, write real content

## Done Criteria

```
✅ Every file in docs/ is classified (KEEP/UPDATE/DELETE/CREATE)
✅ Zero outdated docs — all content matches current codebase
✅ Zero dead links in DOCUMENTATION_INDEX.md
✅ CLAUDE.md + README.md accurate, changelog bumped
✅ All quality gates pass
```

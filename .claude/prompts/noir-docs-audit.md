# NOIR — Documentation Audit & Maintenance

## Objective

Audit ALL documentation in the project. Every doc file must earn its place.
After this task, the docs should be accurate, current, and useful — zero stale content.

## Execution Method: Agent Team (NON-NEGOTIABLE)

**HARD RULE: You MUST use TeamCreate + Task tool to spawn agent teammates.**
- Minimum 2 teammates. You are the **Team Lead / Coordinator**.
- You do NOT read/edit docs yourself. Delegate to teammates, review their output.

### Team

| Role | Responsibility |
|------|---------------|
| `docs-auditor` | Inventory all docs, cross-reference with codebase, classify each file |
| `docs-writer` | Update outdated docs, remove obsolete ones, create missing ones |
| `claude-md-updater` | (optional 3rd) Focus specifically on CLAUDE.md + README.md |

**Note:** If `docs-auditor` scope is too large (50+ doc files), split into `backend-docs-auditor` + `frontend-docs-auditor`.

### Team Lifecycle (Every Phase)

```
1. TeamCreate → create team for this task
2. Task tool → spawn teammates with clear, scoped assignments
3. Monitor → read task list, respond to teammate messages
4. Quality Gate → run build + test + frontend build
5. Shutdown teammates → SendMessage type: "shutdown_request"
6. TeamDelete → clean up
```

---

## Decision Framework (Apply to EVERY Doc File)

For each documentation file, ask these 3 questions in order:

```
1. Does this doc describe something that STILL EXISTS in the codebase?
   NO  → Is it a decision record or architectural knowledge worth preserving?
         NO  → DELETE the file
         YES → Mark as "historical" or move to docs/decisions/

2. Is the content ACCURATE and CURRENT?
   NO  → UPDATE it to match current codebase reality
   YES → KEEP as-is

3. Is there important knowledge NOT documented?
   YES → CREATE the missing doc
```

### Classification Labels

| Label | Action | Examples |
|-------|--------|---------|
| **KEEP** | No changes needed | Accurate, current, useful |
| **UPDATE** | Content outdated but topic still relevant | Wrong file paths, old API signatures, outdated patterns |
| **DELETE** | Topic no longer exists or task completed | Plans for completed features, removed feature docs, obsolete research |
| **CREATE** | Important knowledge gap | Undocumented patterns, missing architecture docs, tribal knowledge |

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
- **Critical rules** — Are all 23 rules still accurate? Any new rules needed?
- **Quick Reference** — Do build/test/migration commands still work?
- **Project Structure** — Does the tree match actual directory structure?
- **Code Patterns** — Do code examples match current implementation?
- **Frontend Rules** — Do patterns match current React/TypeScript conventions?
- **E-commerce Patterns** — Do they reflect current feature state?
- **Changelog** — Add new version entry for this audit
- **Counts** — Test count, UIKit component count, story count — verify all numbers

### 3. `README.md` (root)
- Professional presentation: badges, screenshots, quick-start
- Accurate setup instructions (do they actually work?)
- Feature list matches what's actually implemented
- Tech stack matches actual dependencies and versions
- Contributing guide (if exists) is current

### 4. `AGENTS.md` (root)
- Are agent instructions still accurate?
- Do referenced file paths, patterns, and tools still exist?

### 5. `.claude/` folder
- `.claude/rules/*.md` — Are routing rules current? Do skill names match available skills?
- `.claude/prompts/*.md` — Do prompts reference correct file paths, commands, and patterns?
  Verify quality gate commands still work. Verify referenced pages/components still exist.

---

## Process

### Phase 0: Inventory & Classify

**docs-auditor produces:**
```
| File Path | Status | Action | Reason |
|-----------|--------|--------|--------|
| docs/backend/patterns/xyz.md | Outdated | UPDATE | References old handler pattern |
| docs/backend/research/abc.md | Obsolete | DELETE | Feature already implemented |
| docs/frontend/architecture.md | Current | KEEP | Accurate |
| (missing) | Gap | CREATE | No doc for inventory receipt pattern |
```

Cross-reference method:
- For each doc mentioning a file path → verify path exists
- For each doc mentioning a class/method → verify it exists with that signature
- For each doc mentioning a version number → verify against package.json / .csproj
- For each doc describing a pattern → verify code still follows that pattern

### Phase 1: Execute Changes

**docs-writer executes** based on auditor's report:
- DELETE obsolete files (remove from git, update any indexes that referenced them)
- UPDATE outdated files (fix paths, signatures, versions, patterns, examples)
- CREATE missing docs (follow existing doc style and structure)
- Update `docs/DOCUMENTATION_INDEX.md` to reflect all changes

**claude-md-updater** (in parallel):
- Verify and update CLAUDE.md (rules, patterns, counts, structure tree)
- Verify and update README.md (setup instructions, feature list, tech stack)
- Bump CLAUDE.md changelog version

### Phase 2: Verification

**You (Team Lead):**
- Review all changes
- Verify no broken cross-references between docs
- Verify DOCUMENTATION_INDEX.md is complete and all links work
- Run quality gates:

```bash
dotnet build src/NOIR.sln                              # Verify code examples in docs match reality
dotnet test src/NOIR.sln                               # All pass (nothing broken)
cd src/NOIR.Web/frontend && pnpm run build             # Frontend still builds
cd src/NOIR.Web/frontend && pnpm build-storybook       # Storybook still builds (if docs reference it)
```

---

## Rules

- Follow ALL rules in CLAUDE.md
- Don't create docs for the sake of having docs — every file must be useful
- Prefer updating over deleting when the topic is still relevant
- ADRs (Architecture Decision Records) are historical — update status but don't delete
- Keep doc style consistent: use existing formatting patterns
- No empty/placeholder docs — if creating, write real content

---

## Done Criteria

```
✅ Every file in docs/ is classified (KEEP/UPDATE/DELETE/CREATE)
✅ Zero outdated docs remaining — all content matches current codebase
✅ Zero obsolete docs remaining — completed task docs removed
✅ Zero dead links in DOCUMENTATION_INDEX.md
✅ CLAUDE.md accurate: rules, patterns, counts, structure tree, changelog bumped
✅ README.md accurate: setup instructions work, feature list current, versions correct
✅ All quality gates pass
```

---

## Reminder: You Are The Coordinator

```
❌ WRONG: Reading docs yourself, editing files directly
✅ RIGHT: Spawning teammates, reviewing their classification report, approving changes

The only commands you run directly are quality gate checks.
```

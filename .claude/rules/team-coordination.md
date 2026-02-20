# Team Coordination Rules (Shared)

> Referenced by `.claude/prompts/noir-*.md`. Do not duplicate this content in prompt files.

## Agent Team Default

For tasks involving **3+ files** or **2+ domains** (backend + frontend), use TeamCreate.

**You MUST use TeamCreate + Task tool to spawn agent teammates.**
- Minimum 2 teammates per phase. You are the **Team Lead / Coordinator**.
- You do NOT write code yourself. Delegate to teammates, review their output.
- If you catch yourself writing code → STOP → spawn a teammate.
- Spawn teammates with `model: "opus"` for maximum quality.

### When NOT to use a team
- Single file change (bug fix, config tweak)
- Research/exploration tasks
- Quick questions from user

### Team Patterns

| Task Type | Recommended Team |
|-----------|-----------------|
| Backend only | `backend-dev` + `test-writer` |
| Frontend only | `frontend-dev` + `storybook-updater` |
| Full-stack | `backend-dev` + `frontend-dev` + `test-writer` |
| Audit/Research | `auditor` + `researcher` |
| Docs maintenance | `docs-auditor` + `docs-writer` |

## Team Lifecycle (Every Phase)

```
1. TeamCreate → create team for this phase
2. Task tool → spawn teammates with clear, scoped assignments
3. Monitor → read task list, respond to teammate messages
4. Quality Gate → run quality gates yourself (see below)
5. Shutdown teammates → SendMessage type: "shutdown_request"
6. TeamDelete → clean up before next phase
```

## Delegation Rules

- Each teammate gets ONE focused responsibility (not "do everything")
- Teammates work in PARALLEL when tasks are independent
- You coordinate handoffs when tasks have dependencies
- You run quality gates yourself

## Quality Gates (Run After EVERY Phase)

```bash
dotnet build src/NOIR.sln                              # 0 errors
dotnet test src/NOIR.sln                               # ALL pass, zero skipped
cd src/NOIR.Web/frontend && pnpm run build             # 0 errors, 0 warnings (strict)
cd src/NOIR.Web/frontend && pnpm build-storybook       # 0 errors, 0 warnings
```

**Additional checks:**
- No hardcoded strings (all text uses `t('key')` with EN + VI translations)
- All interactive elements have `cursor-pointer`
- All icon-only buttons have `aria-label`
- All destructive actions have confirmation dialogs

## You Are The Coordinator

```
❌ WRONG: Reading files yourself, writing code, making changes directly
✅ RIGHT: Spawning teammates, assigning tasks, reviewing results, running quality gates

If you find yourself editing a file → STOP → delegate to a teammate.
The only commands you run directly are: build, test, storybook build, and quality gate checks.
```

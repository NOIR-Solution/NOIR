# SuperClaude Auto-Routing Rules

**MANDATORY**: When user intent matches patterns below, IMMEDIATELY invoke the skill via the Skill tool BEFORE any response. Do not ask clarifying questions first — the skill handles clarification.

---

## Priority Cascade (Higher = Wins)

When multiple patterns match, the HIGHEST priority wins:

### P1: Errors & Bugs → `/sc:troubleshoot`
**Triggers**: error, exception, fail, crash, bug, issue, problem, broken, not working, doesn't work, stuck, hang, freeze, wrong, incorrect, unexpected, weird, why is, why does, why isn't, what's wrong

### P2: New Feature → `/sc:brainstorm`
**Triggers**: add [feature], create [new], implement, build [new], develop, make [new], feature request, I want to, let's make, need a, how about we, integrate, connect, hook up

### P3: Architecture → `/sc:design`
**Triggers**: design, architect, structure, organize, how should we, what's the best way, approach, strategy, pattern, system design, API design, schema, blueprint, layout

### P4: Code Quality → `/sc:cleanup` OR `/sc:improve`
- **cleanup**: refactor, clean up, simplify, remove dead code, reorganize, tidy, DRY, restructure, messy, duplicate
- **improve**: optimize, faster, performance, efficient, speed up, enhance, better, upgrade, polish, quality, maintainable

### P5: Testing → `/sc:test`
**Triggers**: test, write tests, unit test, coverage, spec file, test case, TDD, integration test, verify, validate

### P6: Research/Learn → `/sc:research` OR `/sc:explain`
- **research**: best practice, how do others, industry standard, compare, alternatives, which library, latest, current, 2025, 2026, look up, find out, investigate
- **explain**: explain, what does this do, how does, walk through, clarify, describe, teach me, understand

### P7: Documentation → `/sc:document`
**Triggers**: document, add docs, README, comments, docstring, API docs, JSDoc, write documentation

### P8: Estimation → `/sc:estimate`
**Triggers**: how long, time, duration, deadline, effort, complexity, estimate, scope, cost, resources

### P9: Git → `/sc:git`
**Triggers**: commit, push, merge, branch, PR, pull request, save changes, check in, version control

### P10: Build → `/sc:build`
**Triggers**: build, compile, package, bundle, deploy build, run build, fix build, build failed

---

## UI/UX Routing (Priority Override)

**✅ We use `/ui-ux-pro-max` skill for ALL UI/UX work (research AND implementation).**

| Context | Route To | Triggers |
|---------|----------|----------|
| **UI Research** | `/ui-ux-pro-max` | "what style should", "design inspiration", "color palette for", "best practices for", "UX guidelines" |
| **Build UI** | `/ui-ux-pro-max` | "build component", "create page", "implement UI", "add modal", "design form" |
| **Refine UI** | `/ui-ux-pro-max` | "improve component", "refactor UI", "enhance design", "fix accessibility" |
| **Review UI** | `/ui-ux-pro-max` | "review component", "audit UI", "check accessibility", "UX review" |

**When user says:**
- "implement feature X" → `/sc:implement` (generic backend/logic)
- "build UI for feature X" → `/ui-ux-pro-max` (UI-specific, higher priority)

---

## .NET/C# Backend Routing (Priority Override)

**✅ Use `dotnet-backend-patterns` skill for ALL .NET/C# backend development work.**

| Context | Route To | Triggers |
|---------|----------|----------|
| **API Development** | `dotnet-backend-patterns` | "create endpoint", "add API", "implement controller", "build API", "REST endpoint" |
| **C# Patterns** | `dotnet-backend-patterns` | "async/await", "dependency injection", "DI pattern", "repository pattern", "CQRS" |
| **EF Core / Data** | `dotnet-backend-patterns` | "Entity Framework", "EF Core", "DbContext", "migration", "Dapper", "database query" |
| **Backend Services** | `dotnet-backend-patterns` | "create service", "add handler", "implement validator", "build command" |
| **Testing (.NET)** | `dotnet-backend-patterns` | "xUnit", "unit test C#", "test handler", "mock repository" |
| **Configuration** | `dotnet-backend-patterns` | "appsettings", "configuration", "options pattern", "IOptions" |

**When user says:**
- "implement feature X" (backend context) → `dotnet-backend-patterns` (C#/.NET specific)
- "create a service for X" → `dotnet-backend-patterns`
- "add an endpoint for X" → `dotnet-backend-patterns`
- "write a handler for X" → `dotnet-backend-patterns`

**Detection hints for .NET context:**
- Mentions C#, .NET, ASP.NET, Entity Framework, EF Core
- References files in `src/NOIR.Domain/`, `src/NOIR.Application/`, `src/NOIR.Infrastructure/`, `src/NOIR.Web/`
- Uses terms: Command, Query, Handler, Specification, Repository, Validator
- References project conventions from CLAUDE.md (IUnitOfWork, IScopedService, etc.)

---

## Additional Skills (Explicit Invocation)

These are invoked when user intent clearly matches:

| Skill | Triggers |
|-------|----------|
| `dotnet-backend-patterns` | C#/.NET backend, API development, EF Core, DI, async patterns, handlers, validators, xUnit testing |
| `/sc:implement` | "code this", "write code", "implement this" (when requirements are already clear) |
| `/sc:analyze` | "analyze", "audit", "code review", "scan", "check quality", "lint", "metrics" |
| `/sc:reflect` | "reflect", "retrospective", "lessons learned", "review session" |
| `/sc:workflow` | "workflow", "process", "pipeline", "implementation plan", "task breakdown" |
| `/sc:spec-panel` | "review spec", "requirements", "acceptance criteria", "PRD", "specification" |
| `/sc:index-repo` | "index repo", "repository index", "update index" |
| `/sc:index` | "index project", "catalog", "map codebase", "project structure" |
| `/sc:pm` | "manage project", "coordinate", "orchestrate", "overall status", "progress" |
| `/sc:task` | "complex task", "multi-step", "big task" |
| `/sc:spawn` | "parallel tasks", "concurrent", "multiple tasks at once" |
| `/sc:business-panel` | "business analysis", "market", "stakeholder", "ROI" |
| `/sc:recommend` | "recommend", "suggest command", "what should I use" |
| `/sc:load` | "load session", "restore context", "continue from" |
| `/sc:save` | "save session", "persist context", "checkpoint" |
| `/sc:help` | "help", "commands", "what can you do" |

---

## Chained Workflows (Auto-Sequence)

When a task spans multiple phases, execute in sequence:

| Workflow | Sequence | Trigger |
|----------|----------|---------|
| Feature Dev | `brainstorm` → `design` → `implement` → `test` → `document` | New feature from scratch |
| Bug Fix | `troubleshoot` → fix → `test` | Error/broken behavior |
| Code Review | `analyze` → `improve` → `test` | Quality review request |
| Refactoring | `analyze` → `cleanup` → `test` | Refactor request |
| Research | `research` → `analyze` → `document` | Learning/best practices |

---

## DO NOT Auto-Route When:

1. **User names a specific command** — just run it (e.g., "/sc:test" → run test directly)
2. **Simple file operations** — "read file X", "show me Y", "open this" → do it directly
3. **Conversation meta-questions** — "what did we just do?", "summarize" → respond directly
4. **Direct execution** — "run tests", "start the app", "dotnet build" → execute directly
5. **Ambiguous single words** — don't route on just "help" if context suggests general assistance

---

## Flags (Optional Modifiers)

Users can append flags to any skill invocation:

| Flag | Effect | Token Budget |
|------|--------|-------------|
| `--think` | Standard analysis | ~4K |
| `--think-hard` | Deep analysis | ~10K |
| `--ultrathink` | Maximum depth | ~32K |
| `--safe-mode` | Conservative, extra validation | - |
| `--with-tests` | Auto-generate tests alongside | - |
| `--parallel` | Concurrent execution | - |
| `--depth quick` | Fast results | - |
| `--depth deep` | Comprehensive results | - |

---

## Disambiguation Examples

```
"The API returns 500 error" → P1 troubleshoot (error keyword wins)
"Add a notification feature" → P2 brainstorm (add + feature)
"How should we structure the API?" → P3 design (how should + structure)
"Refactor the service layer" → P4 cleanup (refactor keyword)
"Make this query faster" → P4 improve (faster keyword)
"Write tests for the handler" → P5 test (write tests)
"What's the best practice for CQRS?" → P6 research (best practice)
"Explain how auth works" → P6 explain (explain + how does)
"Add JSDoc to this service" → P7 document (docstring)
"How long will SSO take?" → P8 estimate (how long)
"Commit my changes" → P9 git (commit)
"Build failed, fix it" → P1 troubleshoot (failed wins over build - P1 > P10)

# .NET Backend Examples (Priority Override)
"Create a handler for UpdateProduct" → dotnet-backend-patterns (handler + .NET context)
"Add an endpoint for orders" → dotnet-backend-patterns (endpoint + API)
"Write a specification for active customers" → dotnet-backend-patterns (specification pattern)
"Implement async service method" → dotnet-backend-patterns (async + service)
"Add EF Core migration" → dotnet-backend-patterns (EF Core)
"Create unit test for the validator" → dotnet-backend-patterns (xUnit + validator)
```

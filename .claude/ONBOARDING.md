# Claude Code Onboarding for NOIR

**Goal:** a new contributor clones the repo and gets an identical AI-assisted development environment in under 10 minutes.

---

## TL;DR — first 3 commands

```bash
git clone <repo-url> && cd NOIR
./setup-claude.sh        # or .\setup-claude.ps1 on native Windows PowerShell
claude                   # opens Claude Code, prompts to install plugins — accept all
```

That's it. Details below.

---

## What's automatic vs manual

This project commits `.claude/settings.json` with declared plugins and `.claude/skills/` with NOIR-specific scaffolding skills. When you run `claude` in the repo for the first time, it auto-registers the marketplaces and prompts to install the plugins.

Everything else is system tooling — covered in [SETUP.md](../SETUP.md) — plus a few personal Claude Code preferences that stay per-user.

### ✅ Fully automatic (no action required)

| Thing | How it works |
|---|---|
| `.claude/rules/*.md` loaded into every conversation | Claude Code auto-detects project rules |
| `CLAUDE.md` + `AGENTS.md` in context | Auto-loaded |
| Project skills (`noir-qa`, `noir-feature-add`, `noir-migration`, `noir-form-scaffold`, `noir-mcp-tool-add`, `noir-datatable-page`, `noir-qa-run`, `noir-test-flow`, `ui-audit`) | Auto-detected from `.claude/skills/` |
| Marketplace registration | Auto-registered from `extraKnownMarketplaces` in `.claude/settings.json` |
| Plugin install prompt on first `claude` session | Triggered by `enabledPlugins` — you just accept |

### 🤖 Automated by script (run `./setup-claude.sh`)

| Step | What the script does |
|---|---|
| Verify Claude Code CLI installed | `claude --version` check, exits with install hint if missing |
| Verify .NET SDK matches `global.json` (10.0.101+) | `dotnet --version` + `global.json` cross-check |
| Verify Node.js ≥ 20 | `node --version` check |
| Verify pnpm installed at expected version (10.28.1 per `package.json`) | `pnpm --version` check |
| Verify SQL Server reachable | `sqlcmd` / connection string test (skipped if no `sqlcmd`) |
| Verify `.claude/settings.json` present | File existence check |
| List declared plugins vs installed | Diff output — shows missing plugins |
| Restore dependencies | `dotnet restore` + `pnpm install` |
| Build verification | `dotnet build` (optional) |
| Print next-step guidance | Points to `claude` + `./start-dev.sh` |

### 🛠️ Manual (one-time, per developer)

These can't be automated because they involve personal choices, secrets, or interactive prompts.

| Step | Where | Why manual |
|---|---|---|
| **1. Install Claude Code CLI** | [docs.claude.com/claude-code](https://docs.claude.com/claude-code) | User must choose install method (npm global, curl installer, IDE extension, or Desktop app) |
| **2. Authenticate Claude Code** | `claude` (first run, OAuth flow) | OAuth with user's Anthropic account |
| **3. Accept plugin install prompts** | First `claude` session in the repo | Claude Code prompts for each plugin — requires user consent |
| **4. Install .NET SDK 10 / Node.js 20+ / pnpm / SQL Server** | See [SETUP.md](../SETUP.md) | System installers, OS-specific |
| **5. (Optional) User-scoped MCP servers** (e.g. `ms365`, Gmail, Slack) | `claude mcp add ...` | OAuth-tied to your accounts, NOT shared across team. Safe to skip unless you want those integrations |
| **6. (Optional) Personal `~/.claude/settings.json`** (theme, status line, keybindings) | User Claude Code settings | Personal preferences |
| **7. (Optional) Configure git user** | `git config user.name/email` | Per-contributor identity |

### 🚫 NOT shared across team (intentionally per-user)

| Thing | Why not shared |
|---|---|
| `.claude/settings.local.json` | Gitignored — per-dev permission allowlist |
| `~/.claude/projects/d--GIT-TOP-NOIR/memory/` | Per-user observations & feedback from personal Claude sessions |
| `~/.claude/.mcp.json` (user-scope MCP servers) | OAuth tokens bound to individual accounts |
| `~/.claude/statusLine`, theme, keybindings | Personal preferences |

---

## The committed plugin list (what gets installed)

From `.claude/settings.json` → `enabledPlugins`. All 13:

| Plugin | Marketplace | What it gives you |
|---|---|---|
| `dotnet-skills` | `Aaronontheweb/dotnet-skills` | 30+ .NET skills: Akka.NET, EF Core, Aspire, performance, testing |
| `context7` | `anthropics/claude-plugins-official` | Live docs fetcher — React, EF Core, Radix, etc. (MCP-based) |
| `serena` | official | Semantic code navigation (MCP-based) |
| `csharp-lsp` | official | C# language server for symbol lookup |
| `playwright` | official | Browser automation for QA + E2E (MCP-based) |
| `frontend-design` | official | Design-driven UI generation skill |
| `claude-md-management` | official | CLAUDE.md auditing + improvement skill |
| `skill-creator` | official | Helper for creating / editing / benchmarking skills |
| `claude-code-setup` | official | Automation recommendations for Claude Code setup |
| `accessibility-compliance` | `wshobson/agents` | WCAG audit + screen reader testing skills |
| `full-stack-orchestration` | `wshobson/agents` | Multi-agent feature orchestration (deployment/perf/security/test) |
| `document-skills` | `anthropics/skills` | PDF / XLSX / PPTX / DOCX authoring |
| `ui-ux-pro-max` | `nextlevelbuilder/ui-ux-pro-max-skill` | 161 palettes, 57 font pairings, 25 chart types |

**To verify after setup:** `claude` → `/plugin` → the list should show all 13 as installed.

---

## Project-specific skills (already in repo, auto-loaded)

Located in `.claude/skills/`:

| Skill | Invoke when you | Reference |
|---|---|---|
| `noir-feature-add` | Add a new feature/module | `.claude/rules/feature-registry-sync.md` |
| `noir-migration` | Add an EF Core migration | CLAUDE.md Rule 23 |
| `noir-form-scaffold` | Build a form/dialog | `.claude/rules/form-validation-standard.md` |
| `noir-mcp-tool-add` | Expose a feature to AI agents | CLAUDE.md Rules 25-30 |
| `noir-datatable-page` | Create a list page | `.claude/rules/datatable-standard.md` |
| `noir-qa`, `noir-qa-run`, `noir-test-flow` | Run QA workflows | `.qa/README.md` |
| `ui-audit` | Run UI/UX audit | CLAUDE.md Rule 31 |

Claude picks these up automatically based on the conversation — you don't type their names. Just describe what you're doing.

---

## Troubleshooting

**Plugins don't auto-prompt on first `claude` session:**
```bash
claude
> /plugin                  # Manually open plugin manager
> install <plugin-name>    # Install each missing one
```

**A plugin is stuck / outdated:**
```bash
claude
> /plugin update <plugin-name>
```

**Marketplaces not registered:**
```bash
claude
> /plugin marketplace add anthropics/claude-plugins-official
> /plugin marketplace add wshobson/agents
> /plugin marketplace add anthropics/skills
> /plugin marketplace add Aaronontheweb/dotnet-skills
> /plugin marketplace add nextlevelbuilder/ui-ux-pro-max-skill
```

**Project skills not detected:**
- Ensure you opened `claude` from the repo root — skill discovery is project-scoped
- Check `.claude/skills/{name}/SKILL.md` exists and has valid frontmatter

**Permission prompts on every command:**
- Copy `.claude/settings.local.json.example` (if present) to `.claude/settings.local.json` and customize
- Or use `/permissions` inside Claude Code to build your own allowlist

---

## Verifying 100% match

Run the verification script at any time:
```bash
./setup-claude.sh --verify
```

It will compare declared vs installed and report any drift. For manual verification:
```bash
# Inside claude session
> /plugin list             # should show 13 installed plugins
> /help                    # project skills appear at the bottom of the skill list
```

---

## When this drifts (maintenance)

When someone adds/removes a plugin:
1. Update `.claude/settings.json` → `enabledPlugins`
2. Update the plugin table in this file
3. Update the plugin description in `CONTRIBUTING.md`
4. Notify the team in PR description

When someone adds a new project skill:
1. Create `.claude/skills/{name}/SKILL.md` with frontmatter
2. Add a row in the "Project-specific skills" table above
3. Reference the related rule file in `.claude/rules/` if applicable

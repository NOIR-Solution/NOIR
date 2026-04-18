#!/bin/bash
#===============================================================================
#  NOIR — Claude Code Setup Verification
#  Verifies the new clone has everything needed for identical AI tooling.
#  Cross-platform: macOS, Linux, Windows (Git Bash/MSYS2/WSL)
#
#  Usage:
#    ./setup-claude.sh            # Full setup: verify + restore deps
#    ./setup-claude.sh --verify   # Verify only, no restore
#    ./setup-claude.sh --help
#===============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

MODE="full"
[[ "${1:-}" == "--verify" ]] && MODE="verify"
[[ "${1:-}" == "--help" ]] && { head -12 "$0" | tail -10 | sed 's/^#//'; exit 0; }

#-------------------------------------------------------------------------------
# Colors
#-------------------------------------------------------------------------------
if [[ -t 1 ]]; then
    RED=$'\e[31m'; GREEN=$'\e[32m'; YELLOW=$'\e[33m'
    BLUE=$'\e[34m'; CYAN=$'\e[36m'; BOLD=$'\e[1m'; DIM=$'\e[2m'; NC=$'\e[0m'
else
    RED=""; GREEN=""; YELLOW=""; BLUE=""; CYAN=""; BOLD=""; DIM=""; NC=""
fi

ok()   { echo "  ${GREEN}✓${NC} $*"; }
warn() { echo "  ${YELLOW}!${NC} $*"; }
err()  { echo "  ${RED}✗${NC} $*"; }
info() { echo "  ${CYAN}ℹ${NC} $*"; }
section() { echo ""; echo "${BOLD}${BLUE}━━ $* ━━${NC}"; }

FAILED=0
fail() { err "$*"; FAILED=$((FAILED+1)); }

#-------------------------------------------------------------------------------
# Expected versions (keep in sync with global.json / package.json)
#-------------------------------------------------------------------------------
DOTNET_MIN="10.0"
NODE_MIN="20"
PNPM_EXPECTED="10.28.1"

EXPECTED_PLUGINS=(
    "context7@claude-plugins-official"
    "serena@claude-plugins-official"
    "csharp-lsp@claude-plugins-official"
    "playwright@claude-plugins-official"
    "frontend-design@claude-plugins-official"
    "claude-md-management@claude-plugins-official"
    "skill-creator@claude-plugins-official"
    "claude-code-setup@claude-plugins-official"
    "accessibility-compliance@claude-code-workflows"
    "full-stack-orchestration@claude-code-workflows"
    "document-skills@anthropic-agent-skills"
    "dotnet-skills@dotnet-skills"
    "ui-ux-pro-max@ui-ux-pro-max-skill"
)

EXPECTED_SKILLS=(
    "noir-qa"
    "noir-qa-run"
    "noir-test-flow"
    "ui-audit"
    "noir-feature-add"
    "noir-migration"
    "noir-form-scaffold"
    "noir-mcp-tool-add"
    "noir-datatable-page"
)

#-------------------------------------------------------------------------------
# Banner
#-------------------------------------------------------------------------------
echo ""
echo "${BOLD}${CYAN}┌─────────────────────────────────────────────┐${NC}"
echo "${BOLD}${CYAN}│  NOIR — Claude Code Setup Verification      │${NC}"
echo "${BOLD}${CYAN}└─────────────────────────────────────────────┘${NC}"
echo "${DIM}Mode: $MODE${NC}"

#-------------------------------------------------------------------------------
# 1. Claude Code CLI
#-------------------------------------------------------------------------------
section "1/6 — Claude Code CLI"

if command -v claude >/dev/null 2>&1; then
    CLAUDE_VERSION=$(claude --version 2>/dev/null | head -1)
    ok "claude installed: ${CLAUDE_VERSION}"
else
    fail "claude CLI not found"
    info "Install: https://docs.claude.com/claude-code"
    info "npm:    npm install -g @anthropic-ai/claude-code"
    info "curl:   curl -fsSL https://claude.ai/install.sh | bash"
fi

#-------------------------------------------------------------------------------
# 2. .NET SDK (must match global.json)
#-------------------------------------------------------------------------------
section "2/6 — .NET SDK"

if command -v dotnet >/dev/null 2>&1; then
    DOTNET_VERSION=$(dotnet --version 2>/dev/null)
    DOTNET_MAJOR=$(echo "$DOTNET_VERSION" | cut -d. -f1)
    if [[ "$DOTNET_MAJOR" -ge 10 ]]; then
        ok ".NET SDK $DOTNET_VERSION (>= $DOTNET_MIN required)"
    else
        fail ".NET SDK $DOTNET_VERSION is too old (need >= $DOTNET_MIN)"
        info "See global.json — project pins SDK 10.0.101+"
    fi
else
    fail ".NET SDK not found"
    info "Install: https://dotnet.microsoft.com/download"
fi

#-------------------------------------------------------------------------------
# 3. Node.js + pnpm
#-------------------------------------------------------------------------------
section "3/6 — Node.js + pnpm"

if command -v node >/dev/null 2>&1; then
    NODE_VERSION=$(node --version 2>/dev/null | sed 's/v//')
    NODE_MAJOR=$(echo "$NODE_VERSION" | cut -d. -f1)
    if [[ "$NODE_MAJOR" -ge "$NODE_MIN" ]]; then
        ok "Node.js $NODE_VERSION (>= $NODE_MIN required)"
    else
        fail "Node.js $NODE_VERSION is too old (need >= $NODE_MIN)"
    fi
else
    fail "Node.js not found"
    info "Install: https://nodejs.org (LTS)"
fi

if command -v pnpm >/dev/null 2>&1; then
    PNPM_VERSION=$(pnpm --version 2>/dev/null)
    if [[ "$PNPM_VERSION" == "$PNPM_EXPECTED" ]]; then
        ok "pnpm $PNPM_VERSION (exact match)"
    else
        warn "pnpm $PNPM_VERSION (expected $PNPM_EXPECTED — may still work)"
        info "To match exactly: npm install -g pnpm@$PNPM_EXPECTED"
    fi
else
    fail "pnpm not found"
    info "Install: npm install -g pnpm@$PNPM_EXPECTED"
fi

#-------------------------------------------------------------------------------
# 4. SQL Server connectivity (soft check — can't auto-install)
#-------------------------------------------------------------------------------
section "4/6 — SQL Server"

if command -v sqlcmd >/dev/null 2>&1; then
    ok "sqlcmd available — verify connection string in appsettings.Development.json"
elif command -v sqllocaldb >/dev/null 2>&1; then
    ok "SqlLocalDB available (Windows)"
else
    warn "No SQL client detected in PATH — SETUP.md covers install options"
    info "LocalDB (Windows), SQL Server Express, or full SQL Server all work"
fi

#-------------------------------------------------------------------------------
# 5. Claude project config
#-------------------------------------------------------------------------------
section "5/6 — Project Claude config"

if [[ -f "$SCRIPT_DIR/.claude/settings.json" ]]; then
    ok ".claude/settings.json present"
else
    fail ".claude/settings.json missing — repo may be incomplete"
fi

if [[ -d "$SCRIPT_DIR/.claude/rules" ]]; then
    RULE_COUNT=$(find "$SCRIPT_DIR/.claude/rules" -name "*.md" | wc -l | tr -d ' ')
    ok ".claude/rules/ — $RULE_COUNT rule files"
else
    fail ".claude/rules/ missing"
fi

echo ""
echo "  ${CYAN}Project skills:${NC}"
for skill in "${EXPECTED_SKILLS[@]}"; do
    if [[ -f "$SCRIPT_DIR/.claude/skills/$skill/SKILL.md" ]]; then
        ok "$skill"
    else
        fail "$skill — missing SKILL.md"
    fi
done

#-------------------------------------------------------------------------------
# 6. Installed plugins (user-scope, via ~/.claude)
#-------------------------------------------------------------------------------
section "6/6 — User Claude Code plugins"

# Resolve user home across platforms
USER_HOME="${HOME:-$USERPROFILE}"
USER_HOME="${USER_HOME//\\//}"  # Windows path normalize
INSTALLED_FILE="$USER_HOME/.claude/plugins/installed_plugins.json"

if [[ -f "$INSTALLED_FILE" ]]; then
    MISSING=()
    for plugin in "${EXPECTED_PLUGINS[@]}"; do
        # grep for exact plugin key (JSON)
        if grep -q "\"$plugin\"" "$INSTALLED_FILE" 2>/dev/null; then
            ok "$plugin"
        else
            err "$plugin — NOT installed"
            MISSING+=("$plugin")
        fi
    done

    if [[ ${#MISSING[@]} -gt 0 ]]; then
        echo ""
        warn "${#MISSING[@]} plugin(s) missing. To install:"
        info "Run: ${BOLD}claude${NC}  (auto-prompts from .claude/settings.json)"
        info "Or:  claude then type /plugin → install each one"
    fi
else
    warn "No Claude Code plugin install record found at $INSTALLED_FILE"
    info "Run ${BOLD}claude${NC} in this directory — it will prompt to install the declared plugins"
fi

#-------------------------------------------------------------------------------
# Restore dependencies (full mode only)
#-------------------------------------------------------------------------------
if [[ "$MODE" == "full" ]] && [[ $FAILED -eq 0 ]]; then
    section "Restoring project dependencies"

    if command -v dotnet >/dev/null 2>&1; then
        info "dotnet restore..."
        if dotnet restore src/NOIR.sln > /dev/null 2>&1; then
            ok "dotnet restore complete"
        else
            warn "dotnet restore had warnings — run 'dotnet restore src/NOIR.sln' manually to see details"
        fi
    fi

    if command -v pnpm >/dev/null 2>&1 && [[ -d src/NOIR.Web/frontend ]]; then
        info "pnpm install..."
        if (cd src/NOIR.Web/frontend && pnpm install --prefer-offline > /dev/null 2>&1); then
            ok "pnpm install complete"
        else
            warn "pnpm install had warnings — run 'cd src/NOIR.Web/frontend && pnpm install' manually"
        fi
    fi
fi

#-------------------------------------------------------------------------------
# Summary
#-------------------------------------------------------------------------------
echo ""
echo "${BOLD}${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

if [[ $FAILED -eq 0 ]]; then
    echo "${BOLD}${GREEN}✓ All required checks passed${NC}"
    echo ""
    echo "Next steps:"
    echo "  ${CYAN}1.${NC} Start the dev environment:  ${BOLD}./start-dev.sh${NC}"
    echo "  ${CYAN}2.${NC} Launch Claude Code:          ${BOLD}claude${NC}"
    echo "  ${CYAN}3.${NC} Accept plugin install prompts (if this is the first run)"
    echo ""
    echo "Full onboarding guide: ${DIM}.claude/ONBOARDING.md${NC}"
    exit 0
else
    echo "${BOLD}${RED}✗ $FAILED check(s) failed — see messages above${NC}"
    echo ""
    echo "Troubleshooting: ${DIM}.claude/ONBOARDING.md${NC}"
    echo "System setup:    ${DIM}SETUP.md${NC}"
    exit 1
fi

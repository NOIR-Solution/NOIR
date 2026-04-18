#Requires -Version 5.0
<#
.SYNOPSIS
    NOIR - Claude Code Setup Verification (Windows PowerShell)
.DESCRIPTION
    Verifies the new clone has everything needed for identical AI tooling.
    Windows-native PowerShell version of setup-claude.sh.
.PARAMETER Verify
    Only verify — do not restore dependencies.
.EXAMPLE
    .\setup-claude.ps1
    .\setup-claude.ps1 -Verify
#>
param(
    [switch]$Verify
)

$ErrorActionPreference = 'Continue'
$script:Failed = 0

$Mode = if ($Verify) { 'verify' } else { 'full' }

#-------------------------------------------------------------------------------
# Helpers
#-------------------------------------------------------------------------------
function Write-Ok    ($msg) { Write-Host "  " -NoNewline; Write-Host "✓" -ForegroundColor Green -NoNewline; Write-Host " $msg" }
function Write-Warn  ($msg) { Write-Host "  " -NoNewline; Write-Host "!" -ForegroundColor Yellow -NoNewline; Write-Host " $msg" }
function Write-Err   ($msg) { Write-Host "  " -NoNewline; Write-Host "✗" -ForegroundColor Red -NoNewline; Write-Host " $msg" }
function Write-Info  ($msg) { Write-Host "  " -NoNewline; Write-Host "ℹ" -ForegroundColor Cyan -NoNewline; Write-Host " $msg" }
function Write-Section ($title) {
    Write-Host ""
    Write-Host "━━ $title ━━" -ForegroundColor Blue
}
function Fail ($msg) { Write-Err $msg; $script:Failed++ }

function Test-Command ($cmd) { return [bool](Get-Command $cmd -ErrorAction SilentlyContinue) }

#-------------------------------------------------------------------------------
# Expected versions / plugins
#-------------------------------------------------------------------------------
$DotnetMin = 10
$NodeMin = 20
$PnpmExpected = '10.28.1'

$ExpectedPlugins = @(
    'context7@claude-plugins-official',
    'serena@claude-plugins-official',
    'csharp-lsp@claude-plugins-official',
    'playwright@claude-plugins-official',
    'frontend-design@claude-plugins-official',
    'claude-md-management@claude-plugins-official',
    'skill-creator@claude-plugins-official',
    'claude-code-setup@claude-plugins-official',
    'accessibility-compliance@claude-code-workflows',
    'full-stack-orchestration@claude-code-workflows',
    'document-skills@anthropic-agent-skills',
    'dotnet-skills@dotnet-skills',
    'ui-ux-pro-max@ui-ux-pro-max-skill',
    'marketing-skills@marketingskills'
)

$ExpectedSkills = @(
    'noir-qa',
    'noir-qa-run',
    'noir-test-flow',
    'ui-audit',
    'noir-feature-add',
    'noir-migration',
    'noir-form-scaffold',
    'noir-mcp-tool-add',
    'noir-datatable-page',
    'noir-seo-check'
)

$RepoRoot = $PSScriptRoot

#-------------------------------------------------------------------------------
# Banner
#-------------------------------------------------------------------------------
Write-Host ""
Write-Host "┌─────────────────────────────────────────────┐" -ForegroundColor Cyan
Write-Host "│  NOIR — Claude Code Setup Verification      │" -ForegroundColor Cyan
Write-Host "└─────────────────────────────────────────────┘" -ForegroundColor Cyan
Write-Host "Mode: $Mode" -ForegroundColor DarkGray

#-------------------------------------------------------------------------------
# 1. Claude Code CLI
#-------------------------------------------------------------------------------
Write-Section "1/6 - Claude Code CLI"

if (Test-Command 'claude') {
    $ver = (claude --version 2>$null | Select-Object -First 1)
    Write-Ok "claude installed: $ver"
} else {
    Fail "claude CLI not found"
    Write-Info "Install: https://docs.claude.com/claude-code"
    Write-Info "npm:    npm install -g @anthropic-ai/claude-code"
}

#-------------------------------------------------------------------------------
# 2. .NET SDK
#-------------------------------------------------------------------------------
Write-Section "2/6 - .NET SDK"

if (Test-Command 'dotnet') {
    $dotnetVer = (dotnet --version 2>$null)
    $dotnetMajor = [int]($dotnetVer -split '\.')[0]
    if ($dotnetMajor -ge $DotnetMin) {
        Write-Ok ".NET SDK $dotnetVer (>= $DotnetMin required)"
    } else {
        Fail ".NET SDK $dotnetVer is too old (need >= $DotnetMin)"
        Write-Info "global.json pins SDK 10.0.101+"
    }
} else {
    Fail ".NET SDK not found"
    Write-Info "Install: https://dotnet.microsoft.com/download"
}

#-------------------------------------------------------------------------------
# 3. Node.js + pnpm
#-------------------------------------------------------------------------------
Write-Section "3/6 - Node.js + pnpm"

if (Test-Command 'node') {
    $nodeVer = (node --version 2>$null) -replace '^v',''
    $nodeMajor = [int]($nodeVer -split '\.')[0]
    if ($nodeMajor -ge $NodeMin) {
        Write-Ok "Node.js $nodeVer (>= $NodeMin required)"
    } else {
        Fail "Node.js $nodeVer is too old (need >= $NodeMin)"
    }
} else {
    Fail "Node.js not found"
    Write-Info "Install: https://nodejs.org (LTS)"
}

if (Test-Command 'pnpm') {
    $pnpmVer = (pnpm --version 2>$null)
    if ($pnpmVer -eq $PnpmExpected) {
        Write-Ok "pnpm $pnpmVer (exact match)"
    } else {
        Write-Warn "pnpm $pnpmVer (expected $PnpmExpected - may still work)"
        Write-Info "To match exactly: npm install -g pnpm@$PnpmExpected"
    }
} else {
    Fail "pnpm not found"
    Write-Info "Install: npm install -g pnpm@$PnpmExpected"
}

#-------------------------------------------------------------------------------
# 4. SQL Server
#-------------------------------------------------------------------------------
Write-Section "4/6 - SQL Server"

if (Test-Command 'sqlcmd') {
    Write-Ok "sqlcmd available - verify connection string in appsettings.Development.json"
} elseif (Test-Command 'SqlLocalDB') {
    Write-Ok "SqlLocalDB available (Windows)"
} else {
    Write-Warn "No SQL client detected in PATH - SETUP.md covers install options"
    Write-Info "LocalDB (Windows), SQL Server Express, or full SQL Server all work"
}

#-------------------------------------------------------------------------------
# 5. Project Claude config
#-------------------------------------------------------------------------------
Write-Section "5/6 - Project Claude config"

if (Test-Path "$RepoRoot\.claude\settings.json") {
    Write-Ok ".claude/settings.json present"
} else {
    Fail ".claude/settings.json missing - repo may be incomplete"
}

$rulesDir = "$RepoRoot\.claude\rules"
if (Test-Path $rulesDir) {
    $ruleCount = (Get-ChildItem -Path $rulesDir -Filter '*.md').Count
    Write-Ok ".claude/rules/ - $ruleCount rule files"
} else {
    Fail ".claude/rules/ missing"
}

Write-Host ""
Write-Host "  Project skills:" -ForegroundColor Cyan
foreach ($skill in $ExpectedSkills) {
    $skillPath = "$RepoRoot\.claude\skills\$skill\SKILL.md"
    if (Test-Path $skillPath) {
        Write-Ok $skill
    } else {
        Fail "$skill - missing SKILL.md"
    }
}

#-------------------------------------------------------------------------------
# 6. Installed plugins
#-------------------------------------------------------------------------------
Write-Section "6/6 - User Claude Code plugins"

$installedFile = "$env:USERPROFILE\.claude\plugins\installed_plugins.json"

if (Test-Path $installedFile) {
    $content = Get-Content -Raw -Path $installedFile -ErrorAction SilentlyContinue
    $missing = @()
    foreach ($plugin in $ExpectedPlugins) {
        if ($content -match [regex]::Escape("`"$plugin`"")) {
            Write-Ok $plugin
        } else {
            Write-Err "$plugin - NOT installed"
            $missing += $plugin
        }
    }

    if ($missing.Count -gt 0) {
        Write-Host ""
        Write-Warn "$($missing.Count) plugin(s) missing. To install:"
        Write-Info "Run: claude  (auto-prompts from .claude/settings.json)"
        Write-Info "Or:  claude then type /plugin -> install each one"
    }
} else {
    Write-Warn "No Claude Code plugin install record found at $installedFile"
    Write-Info "Run 'claude' in this directory - it will prompt to install the declared plugins"
}

#-------------------------------------------------------------------------------
# Restore dependencies
#-------------------------------------------------------------------------------
if ($Mode -eq 'full' -and $script:Failed -eq 0) {
    Write-Section "Restoring project dependencies"

    if (Test-Command 'dotnet') {
        Write-Info "dotnet restore..."
        dotnet restore "$RepoRoot\src\NOIR.sln" *> $null
        if ($LASTEXITCODE -eq 0) {
            Write-Ok "dotnet restore complete"
        } else {
            Write-Warn "dotnet restore had warnings - run manually: dotnet restore src\NOIR.sln"
        }
    }

    $frontendDir = "$RepoRoot\src\NOIR.Web\frontend"
    if ((Test-Command 'pnpm') -and (Test-Path $frontendDir)) {
        Write-Info "pnpm install..."
        Push-Location $frontendDir
        try {
            pnpm install --prefer-offline *> $null
            if ($LASTEXITCODE -eq 0) {
                Write-Ok "pnpm install complete"
            } else {
                Write-Warn "pnpm install had warnings - run manually: cd $frontendDir; pnpm install"
            }
        } finally {
            Pop-Location
        }
    }
}

#-------------------------------------------------------------------------------
# Summary
#-------------------------------------------------------------------------------
Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan

if ($script:Failed -eq 0) {
    Write-Host "✓ All required checks passed" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:"
    Write-Host "  1. Start the dev environment:  ./start-dev.sh"
    Write-Host "  2. Launch Claude Code:          claude"
    Write-Host "  3. Accept plugin install prompts (if this is the first run)"
    Write-Host ""
    Write-Host "Full onboarding guide: .claude/ONBOARDING.md" -ForegroundColor DarkGray
    exit 0
} else {
    Write-Host "✗ $($script:Failed) check(s) failed - see messages above" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting: .claude/ONBOARDING.md" -ForegroundColor DarkGray
    Write-Host "System setup:    SETUP.md" -ForegroundColor DarkGray
    exit 1
}

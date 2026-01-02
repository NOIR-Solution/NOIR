# Frontend UI Stack Decision

**Date:** 2026-01-03
**Status:** Decided
**Decision:** React + 21st.dev + Tailwind CSS

---

## Context

NOIR needs a modern frontend for:
1. Admin login page (cookie auth for Hangfire dashboard)
2. Future React SPA frontend
3. AI-assisted development workflow

## Decision

| Technology | Purpose |
|------------|---------|
| **React** | UI framework |
| **21st.dev** | Component marketplace |
| **Tailwind CSS** | Utility-first CSS |

## Why 21st.dev?

### Key Features
- **730+ components** - Production-ready, community-contributed
- **MCP Integration** - Magic MCP for AI-assisted development
- **AI-first workflow** - `/ui` command in Cursor/Claude/Windsurf
- **Copy-paste model** - You own the code, no lock-in

### Statistics
- GitHub Stars: 5,000+
- Components: 730+
- MCP Support: Yes (Magic MCP)
- License: MIT

## MCP Configuration

### 21st.dev Magic MCP

Add to `.cursor/mcp.json` or Claude Code config:

```json
{
  "mcpServers": {
    "@21st-dev/magic": {
      "command": "npx",
      "args": ["-y", "@21st-dev/magic@latest", "API_KEY=\"your-api-key\""]
    }
  }
}
```

### Usage
```
/ui create a modern login form with email and password
/ui create an admin dashboard sidebar
/ui create a data table with pagination
```

## Implementation Plan

### Phase 1: Admin Login
```
/admin/login → Cookie authentication → /hangfire dashboard
```

### Phase 2: React SPA
```bash
# Initialize React app
npx create-next-app@latest client --typescript --tailwind
cd client

# Install 21st.dev Magic MCP
npx @21st-dev/cli@latest install cursor --api-key <key>
```

## References

- [21st.dev](https://21st.dev) - Component marketplace
- [21st.dev GitHub](https://github.com/serafimcloud/21st)
- [21st.dev Magic MCP](https://github.com/21st-dev/magic-mcp)
- [Tailwind CSS](https://tailwindcss.com)

# Vibe Kanban Integration Guide

## Overview

NOIR uses [Vibe Kanban](https://vibekanban.com) for task management and sprint tracking. The project includes the `vibe-kanban-web-companion` package which provides click-to-edit functionality in the React frontend during development.

## Features

### Click-to-Edit Components
When running in development mode, you can:
- Click on any React component in the browser to jump directly to its source code
- Navigate the codebase visually through the UI
- Quickly locate component definitions for editing

### Dev Server Integration
The project includes tooling to start the full-stack development environment directly from Vibe Kanban.

## Setup

### 1. Browser Companion Installation

The browser companion is already configured in the project. It's automatically included in development builds via `main.tsx`:

```tsx
{import.meta.env.DEV && <VibeKanbanWebCompanion />}
```

This ensures the companion is:
- ✅ Only loaded in development mode
- ✅ Tree-shaken out of production builds
- ✅ Available for all React components

### 2. Dev Server Configuration

#### Windows (Recommended Method)

Use the provided batch file in Vibe Kanban's "Edit Dev Script" dialog:

```batch
src\NOIR.Web\frontend\start-dev.bat
```

**Or use absolute path:**
```batch
D:\TOP\GIT\NOIR\src\NOIR.Web\frontend\start-dev.bat
```

**Why use the batch file?**
- Handles directory navigation automatically
- Works reliably with Vibe Kanban's command launcher
- Avoids Windows PowerShell execution policy issues
- Provides clear error messages

#### macOS/Linux

Use the direct command:

```bash
cd src/NOIR.Web/frontend && npm run dev:full
```

#### VSCode Task (Alternative)

Run the "Start Dev Server" task:
1. Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on macOS)
2. Type "Tasks: Run Task"
3. Select "Start Dev Server"

This uses the configuration in `.vscode/tasks.json`.

## Dev Server Startup Process

When you start the dev server (via `start-dev.bat` or `npm run dev:full`), it:

1. **Checks dependencies** - Installs `node_modules` if missing
2. **Detects running backend** - Reuses existing backend if available
3. **Builds backend** - Runs `dotnet build src/NOIR.sln` if needed
4. **Starts backend** - Launches .NET backend on `localhost:4000`
5. **Waits for health check** - Polls `localhost:4000/api/health`
6. **Generates API types** - Creates TypeScript types from OpenAPI spec
7. **Starts frontend** - Launches Vite dev server on `localhost:3000`

**Startup time:** ~10-30 seconds (faster if backend already running)

## URLs After Startup

| URL | Description |
|-----|-------------|
| http://localhost:3000 | Frontend (Vite + React) with Vibe Kanban companion |
| http://localhost:4000 | Backend API (ASP.NET Core) |
| http://localhost:4000/api/docs | API Documentation (Scalar UI) |
| http://localhost:8025 | MailHog (email testing, if using Docker) |

## Troubleshooting

### "The system cannot find the path specified"

**Problem:** Vibe Kanban can't execute the dev command.

**Solution:** Use the batch file instead of a multi-line command:
```batch
src\NOIR.Web\frontend\start-dev.bat
```

### "Port 3000 is already in use"

**Problem:** Vite can't start because port 3000 is occupied.

**Solution:** Kill the process and restart:
```cmd
netstat -ano | findstr :3000
taskkill /PID <PID> /F
```

Then restart the dev server.

### PowerShell Execution Policy Error

**Problem:** PowerShell blocks npm from running scripts.

**Solution 1 (Easiest):** Use Command Prompt instead of PowerShell

**Solution 2:** Use the batch file (already configured to work around this)

**Solution 3:** Temporarily bypass in PowerShell:
```powershell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process
```

### Vibe Kanban Click-to-Edit Not Working

**Checklist:**
1. ✅ Dev server is running on `localhost:3000`
2. ✅ `import.meta.env.DEV` is true (check browser console)
3. ✅ `VibeKanbanWebCompanion` is rendered in React tree
4. ✅ Browser extension is installed and enabled

**Quick test:**
- Open browser console
- Check for Vibe Kanban logs or errors
- Verify the companion component is mounted

## File Structure

```
src/NOIR.Web/frontend/
├── start-dev.bat              # Dev server launcher for Vibe Kanban (Windows)
├── scripts/
│   └── dev.mjs                # Full-stack orchestration script
├── src/
│   └── main.tsx               # Vibe Kanban companion integration
└── package.json               # dev:full script definition

.vscode/
└── tasks.json                 # VSCode task for dev server
```

## Advanced Configuration

### Customizing Dev Server Behavior

The dev server script (`scripts/dev.mjs`) can be customized:

- **Backend URL:** Change `BACKEND_URL` constant (default: `http://localhost:4000`)
- **Frontend port:** Modify `vite.config.ts` server port (default: `3000`)
- **Health check timeout:** Adjust `waitForBackend()` max attempts (default: 60)

### Environment-Specific Setup

The Vibe Kanban companion automatically detects the environment:
- **Development:** Loaded via `import.meta.env.DEV` check
- **Production:** Tree-shaken out by Vite (zero bundle impact)

No additional configuration needed for different environments.

## Related Documentation

- [SETUP.md](../../SETUP.md) - Full project setup guide
- [Frontend Guide](./README.md) - Frontend architecture overview
- [Vite Configuration](../../src/NOIR.Web/frontend/vite.config.ts) - Vite dev server settings
- [Dev Script](../../src/NOIR.Web/frontend/scripts/dev.mjs) - Full-stack orchestration

## Support

If you encounter issues with Vibe Kanban integration:

1. Check this guide's troubleshooting section
2. Verify all URLs are accessible after startup
3. Review `scripts/dev.mjs` console output for errors
4. Ensure backend is healthy at `localhost:4000/api/health`

For Vibe Kanban-specific questions, refer to the [official documentation](https://vibekanban.com/docs).

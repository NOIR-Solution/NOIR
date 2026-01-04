@echo off
REM Vibe Kanban Dev Server Launcher
REM ================================
REM This batch file provides a reliable way to start the NOIR full-stack dev server
REM from Vibe Kanban's "Start Dev Server" button on Windows.
REM
REM What it does:
REM   1. Changes to the frontend directory (where this script is located)
REM   2. Runs npm run dev:full which orchestrates:
REM      - Backend build and startup (dotnet run)
REM      - API type generation from OpenAPI
REM      - Frontend Vite dev server startup
REM
REM Usage:
REM   - Double-click to run manually
REM   - Use in Vibe Kanban's "Edit Dev Script" dialog:
REM     src\NOIR.Web\frontend\start-dev.bat
REM
REM Expected URLs after successful startup:
REM   Frontend: http://localhost:3000
REM   Backend:  http://localhost:4000
REM   API Docs: http://localhost:4000/api/docs
REM

cd /d "%~dp0"
npm run dev:full

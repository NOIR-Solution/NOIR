@echo off
REM NOIR Development Startup Script (Windows)
REM Starts both backend (.NET) and frontend (React/Vite)

setlocal enabledelayedexpansion

echo ==========================================
echo   NOIR Development Environment Startup
echo ==========================================
echo.

set SCRIPT_DIR=%~dp0
set FRONTEND_DIR=%SCRIPT_DIR%src\NOIR.Web\frontend
set BACKEND_DIR=%SCRIPT_DIR%src\NOIR.Web

REM Check if ports are in use and kill processes
echo Checking ports...

REM Kill process on port 4000
for /f "tokens=5" %%a in ('netstat -ano ^| findstr :4000 ^| findstr LISTENING 2^>nul') do (
    echo Port 4000 is in use. Killing process %%a...
    taskkill /F /PID %%a >nul 2>&1
)

REM Kill process on port 3000
for /f "tokens=5" %%a in ('netstat -ano ^| findstr :3000 ^| findstr LISTENING 2^>nul') do (
    echo Port 3000 is in use. Killing process %%a...
    taskkill /F /PID %%a >nul 2>&1
)

timeout /t 1 >nul

REM Install frontend dependencies
echo.
echo Installing frontend dependencies...
cd /d "%FRONTEND_DIR%"
call npm install
if errorlevel 1 (
    echo ERROR: Failed to install frontend dependencies
    pause
    exit /b 1
)

REM Start backend in new window
echo.
echo Starting backend on port 4000...
cd /d "%BACKEND_DIR%"
start "NOIR Backend" cmd /c "dotnet run --no-build"

REM Wait for backend to start
echo Waiting for backend to start...
:wait_backend
timeout /t 2 >nul
curl -s http://localhost:4000/ >nul 2>&1
if errorlevel 1 goto wait_backend
echo Backend is ready!

REM Start frontend in new window
echo.
echo Starting frontend on port 3000...
cd /d "%FRONTEND_DIR%"
start "NOIR Frontend" cmd /c "npm run dev"

timeout /t 3 >nul

echo.
echo ==========================================
echo   NOIR is running!
echo ==========================================
echo.
echo   Frontend: http://localhost:3000
echo   Backend:  http://localhost:4000
echo.
echo   Login: admin@noir.local / 123qwe
echo.
echo   Close the terminal windows to stop services
echo ==========================================
echo.

REM Open browser
start http://localhost:3000

pause

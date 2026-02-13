# NOIR - Development Setup Guide

Complete setup instructions for Windows, macOS, and Linux development environments, plus production deployment guidance.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Vibe Kanban Integration](#vibe-kanban-integration)
- [Windows Setup](#windows-setup)
- [macOS Setup](#macos-setup)
- [Linux Setup](#linux-setup)
- [Running the Application](#running-the-application)
- [Running Tests](#running-tests)
- [Troubleshooting](#troubleshooting)
- [Production Deployment](#production-deployment)
- [Claude AI Setup Assistance](#claude-ai-setup-assistance)

---

## Prerequisites

### Required Software

| Software | Version | Purpose |
|----------|---------|---------|
| .NET SDK | 10.0+ | Runtime and build tools |
| SQL Server | Any edition | Database (LocalDB, Express, or full) |
| Git | Latest | Version control |

### Optional Software

| Software | Purpose |
|----------|---------|
| Docker Desktop | SQL Server on macOS/Linux |
| Azure Data Studio | Cross-platform SQL management |
| Visual Studio 2022 | Windows IDE with full .NET support |
| VS Code | Cross-platform editor |
| Rider | Cross-platform .NET IDE |

---

## Quick Start

For experienced developers who want to get running quickly:

```bash
# Clone repository
git clone https://github.com/NOIR-Solution/NOIR.git
cd NOIR

# Start infrastructure (SQL Server + MailHog)
docker-compose up -d

# Restore and build
dotnet restore src/NOIR.sln
dotnet build src/NOIR.sln

# Terminal 1: Start backend
dotnet run --project src/NOIR.Web --environment Development

# Terminal 2: Start frontend (REQUIRED for development)
cd src/NOIR.Web/frontend
pnpm install
ppnpm run dev

# Access application at: http://localhost:3000
# Admin credentials: admin@noir.local / 123qwe
# MailHog (email testing): http://localhost:8025
```

> **IMPORTANT:** You must run both backend AND frontend separately during development:
> - Backend runs on port 4000 (API server)
> - Frontend runs on port 3000 (Vite dev server with HMR)
> - Always access the app via **http://localhost:3000** for full functionality

> **Alternative - Single command (recommended):**
> ```bash
> cd src/NOIR.Web/frontend
> pnpm install
> ppnpm run dev:full
> ```
> This starts both backend and frontend with a single command, auto-generates API types, and handles graceful shutdown.

> **Alternative - Production-like mode (single terminal):**
> ```bash
> dotnet build src/NOIR.sln -c Release  # Auto-builds frontend
> dotnet run --project src/NOIR.Web -c Release
> # Access: http://localhost:4000
> ```

**Note:** This assumes Docker is available for SQL Server and MailHog. For Windows LocalDB, see platform-specific setup below.

---

## Vibe Kanban Integration

This project uses **Vibe Kanban** for task management and sprint tracking. When using Vibe Kanban to manage this project, you have two options for starting the dev server:

### Option 1: Batch File (Recommended for Windows)

**Command:**
```
D:\TOP\GIT\NOIR\src\NOIR.Web\frontend\start-dev.bat
```

Or use the relative path in Vibe Kanban's "Edit Dev Script" dialog:
```
src\NOIR.Web\frontend\start-dev.bat
```

The `start-dev.bat` file is a convenience wrapper that:
- Automatically changes to the correct directory (`%~dp0` = script's directory)
- Runs `ppnpm run dev:full` in the frontend folder
- Works reliably with Vibe Kanban's dev server launcher

### Option 2: Direct Command

**Command:**
```bash
cd NOIR/src/NOIR.Web/frontend && pnpm run dev:full
```

**Note:** This may not work properly in Vibe Kanban's dev script UI on Windows due to command parsing issues. Use the batch file instead.

**What it does:**
1. Installs frontend dependencies (`pnpm install`) if `node_modules` is missing
2. Checks if backend is already running (reuses existing instance)
3. Builds backend (`dotnet build`) if backend not running
4. Starts .NET backend (`dotnet run`)
5. Waits for backend health check at `localhost:4000/api/health`
6. Generates TypeScript API types from OpenAPI spec
7. Starts Vite dev server on `localhost:3000`
8. Handles graceful shutdown with Ctrl+C

**Output URLs:**
| URL | Purpose |
|-----|---------|
| http://localhost:3000 | Frontend (Vite dev server) |
| http://localhost:4000 | Backend API |
| http://localhost:4000/api/docs | API Documentation (Scalar) |
| http://localhost:8025 | MailHog (email testing) |

### Prerequisites for Dev Script

Before running the dev script, ensure:
1. Docker containers are running: `docker-compose up -d`
2. Node.js installed (LTS version)
3. .NET SDK 10.0+ installed

> **Note:** The script auto-installs pnpm packages if `node_modules` is missing, and auto-builds the backend before starting. No manual `pnpm install` or `dotnet build` needed!

---

## Windows Setup

### Step 1: Install .NET 10 SDK

1. Download from https://dotnet.microsoft.com/download/dotnet/10.0
2. Run the installer
3. Verify installation:
   ```powershell
   dotnet --version
   # Should output: 10.0.x
   ```

### Step 2: Install SQL Server

**Option A: SQL Server LocalDB (Recommended for Development)**

LocalDB is included with Visual Studio. To install standalone:

1. Download SQL Server Express with LocalDB from https://www.microsoft.com/en-us/sql-server/sql-server-downloads
2. Run installer, select "Download Media" > "LocalDB"
3. Install the LocalDB MSI package
4. Verify installation:
   ```powershell
   sqllocaldb info
   # Should show MSSQLLocalDB instance
   ```

**Option B: SQL Server Express**

1. Download from https://www.microsoft.com/en-us/sql-server/sql-server-downloads
2. Run Express edition installer
3. Note your instance name (default: `.\SQLEXPRESS`)
4. Update connection string in `appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=.\\SQLEXPRESS;Database=NOIR;Trusted_Connection=True;TrustServerCertificate=True"
     }
   }
   ```

**Option C: SQL Server Developer Edition**

Free full-featured edition for development:
1. Download from https://www.microsoft.com/en-us/sql-server/sql-server-downloads
2. Install Developer edition
3. Configure connection string as needed

### Step 3: Clone and Run

```powershell
# Clone repository
git clone https://github.com/yourusername/noir.git
cd noir

# Restore dependencies
dotnet restore src/NOIR.sln

# Build
dotnet build src/NOIR.sln

# Run
dotnet run --project src/NOIR.Web
```

### Step 4: Verify Setup

1. Open browser to http://localhost:4000/api/health
   - Should return healthy status
2. Open http://localhost:4000/api/docs
   - Should show Scalar API documentation
3. Login at POST `/api/auth/login`:
   ```json
   {
     "email": "admin@noir.local",
     "password": "123qwe"
   }
   ```

---

## macOS Setup

### Step 1: Install .NET 10 SDK

**Option A: Direct Download**
```bash
# Download and install from
# https://dotnet.microsoft.com/download/dotnet/10.0

# Verify
dotnet --version
```

**Option B: Using Homebrew**
```bash
brew install --cask dotnet-sdk

# Verify
dotnet --version
```

### Step 2: Install Docker Desktop

SQL Server requires Docker on macOS (no native version available).

1. Download Docker Desktop from https://www.docker.com/products/docker-desktop/
2. Install and start Docker Desktop
3. Verify Docker is running:
   ```bash
   docker --version
   docker ps
   ```

### Step 3: Start SQL Server Container

**Recommended: Use Docker Compose**
```bash
# Start SQL Server (uses Azure SQL Edge for ARM64/M1 Mac compatibility)
docker-compose up -d sqlserver

# Verify container is running
docker-compose ps
```

**Alternative: Manual Docker Run**
```bash
# Azure SQL Edge (recommended for ARM64/M1 Macs)
docker run -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=coffee123@@" \
  -p 1433:1433 \
  --name noir-sqlserver \
  -d mcr.microsoft.com/azure-sql-edge:latest

# Start MailHog for email testing
docker run -d \
  -p 1025:1025 \
  -p 8025:8025 \
  --name noir-mailhog \
  mailhog/mailhog

# Verify containers are running
docker ps

# View logs if needed
docker logs noir-sqlserver
```

**Important:** The password `coffee123@@` matches `appsettings.Development.json`. Change both if you use a different password.

### Step 4: Clone and Run

```bash
# Clone repository
git clone https://github.com/yourusername/noir.git
cd noir

# Restore dependencies
dotnet restore src/NOIR.sln

# Build
dotnet build src/NOIR.sln

# Terminal 1: Start backend (uses appsettings.Development.json with Docker connection string)
dotnet run --project src/NOIR.Web --environment Development

# Terminal 2: Start frontend (REQUIRED - open a new terminal)
cd src/NOIR.Web/frontend
pnpm install
ppnpm run dev
```

### Step 5: Access the Application

| URL | Purpose |
|-----|---------|
| http://localhost:3000 | **Main application** (use this!) |
| http://localhost:4000 | Backend API only |
| http://localhost:8025 | MailHog - view sent emails |

**Login:** `admin@noir.local` / `123qwe`

### Step 6: Daily Development

```bash
# Start infrastructure (if stopped)
docker start noir-sqlserver noir-mailhog

# Terminal 1: Run backend with hot reload
dotnet watch --project src/NOIR.Web

# Terminal 2: Run frontend with hot reload
cd src/NOIR.Web/frontend && pnpm run dev
```

### Optional: Azure Data Studio

For database management on macOS:
1. Download from https://docs.microsoft.com/en-us/sql/azure-data-studio/
2. Connect to `localhost,1433` with user `sa` and password `coffee123@@`

---

## Linux Setup

### Step 1: Install .NET 10 SDK

**Ubuntu/Debian:**
```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install .NET SDK
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0

# Verify
dotnet --version
```

**Fedora/RHEL:**
```bash
sudo dnf install dotnet-sdk-10.0

# Verify
dotnet --version
```

**Arch Linux:**
```bash
sudo pacman -S dotnet-sdk

# Verify
dotnet --version
```

### Step 2: Install Docker

**Ubuntu/Debian:**
```bash
# Install Docker
sudo apt-get update
sudo apt-get install -y docker.io docker-compose

# Add user to docker group
sudo usermod -aG docker $USER

# Log out and back in, then verify
docker --version
```

**Fedora:**
```bash
sudo dnf install docker docker-compose
sudo systemctl start docker
sudo systemctl enable docker
sudo usermod -aG docker $USER
```

### Step 3: Start SQL Server and MailHog Containers

**Recommended: Use Docker Compose**
```bash
# Start SQL Server and MailHog together
docker-compose up -d

# Verify containers are running
docker-compose ps
```

**Alternative: Manual Docker Run**
```bash
# For ARM64 (Apple Silicon, Raspberry Pi, etc.)
docker run -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=coffee123@@" \
  -p 1433:1433 \
  --name noir-sqlserver \
  -d mcr.microsoft.com/azure-sql-edge:latest

# For x64 (Intel/AMD)
docker run -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=coffee123@@" \
  -p 1433:1433 \
  --name noir-sqlserver \
  -d mcr.microsoft.com/mssql/mssql-server:2022-latest

# Start MailHog for email testing
docker run -d \
  -p 1025:1025 \
  -p 8025:8025 \
  --name noir-mailhog \
  mailhog/mailhog

# Verify containers are running
docker ps
```

**Important:** The password `coffee123@@` matches `appsettings.Development.json`. Change both if you use a different password.

### Step 4: Clone and Run

```bash
# Clone repository
git clone https://github.com/yourusername/noir.git
cd noir

# Restore and build
dotnet restore src/NOIR.sln
dotnet build src/NOIR.sln

# Terminal 1: Start backend (uses appsettings.Development.json with Docker connection string)
dotnet run --project src/NOIR.Web --environment Development

# Terminal 2: Start frontend (REQUIRED - open a new terminal)
cd src/NOIR.Web/frontend
pnpm install
ppnpm run dev
```

### Step 5: Access the Application

| URL | Purpose |
|-----|---------|
| http://localhost:3000 | **Main application** (use this!) |
| http://localhost:4000 | Backend API only |
| http://localhost:8025 | MailHog - view sent emails |

**Login:** `admin@noir.local` / `123qwe`

### Step 6: Daily Development

```bash
# Start infrastructure (if stopped)
docker start noir-sqlserver noir-mailhog

# Terminal 1: Run backend with hot reload
dotnet watch --project src/NOIR.Web

# Terminal 2: Run frontend with hot reload
cd src/NOIR.Web/frontend && pnpm run dev
```

---

## Running the Application

### Development Mode

```bash
# Standard run
dotnet run --project src/NOIR.Web

# With hot reload (recommended for development)
dotnet watch --project src/NOIR.Web

# Explicit environment
dotnet run --project src/NOIR.Web --environment Development
```

### Application URLs

**Development (use port 3000):**
| URL | Purpose |
|-----|---------|
| http://localhost:3000 | Application (frontend + API via proxy) |
| http://localhost:3000/api/docs | API documentation (Scalar) |
| http://localhost:3000/api/health | Health check |
| http://localhost:3000/hangfire | Background jobs dashboard (Admin only) |

> **Note:** Port 4000 serves the .NET backend directly (API + embedded static files). Use for production-like testing.

### Default Credentials

| User | Password | Role |
|------|----------|------|
| admin@noir.local | 123qwe | Admin |

---

## Running Tests

### All Tests

```bash
# Run all 1,808+ tests
dotnet test src/NOIR.sln
```

### By Project

```bash
# Unit tests only (fast, no database)
dotnet test tests/NOIR.Domain.UnitTests
dotnet test tests/NOIR.Application.UnitTests

# Integration tests (requires SQL Server)
dotnet test tests/NOIR.IntegrationTests

# Architecture tests
dotnet test tests/NOIR.ArchitectureTests
```

### Cross-Platform Test Configuration

Tests automatically detect the platform and use appropriate database:

| Platform | Default Database | Override |
|----------|------------------|----------|
| Windows | SQL Server LocalDB | Set `NOIR_USE_LOCALDB=false` for Docker |
| macOS | Docker SQL Server | Set `NOIR_TEST_SQL_CONNECTION` for custom |
| Linux | Docker SQL Server | Set `NOIR_TEST_SQL_CONNECTION` for custom |

**Custom SQL Server for Tests:**
```bash
# Use custom SQL Server connection
export NOIR_TEST_SQL_CONNECTION="Server=myserver;User Id=myuser;Password=mypass;TrustServerCertificate=True"
dotnet test src/NOIR.sln
```

**Force LocalDB on Windows with Docker:**
```bash
# If you have both LocalDB and Docker, force LocalDB
set NOIR_USE_LOCALDB=true
dotnet test src/NOIR.sln
```

### Test Coverage

```bash
# Run tests with coverage
dotnet test src/NOIR.sln --collect:"XPlat Code Coverage"

# Generate HTML report (requires reportgenerator tool)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"TestResults/CoverageReport" -reporttypes:Html

# Open report
open TestResults/CoverageReport/index.html  # macOS
start TestResults/CoverageReport/index.html  # Windows
xdg-open TestResults/CoverageReport/index.html  # Linux
```

---

## Troubleshooting

### Common Issues

#### "Could not connect to SQL Server"

**Windows (LocalDB):**
```powershell
# Check if LocalDB is installed
sqllocaldb info

# Start the instance
sqllocaldb start MSSQLLocalDB

# Check instance status
sqllocaldb info MSSQLLocalDB
```

**macOS/Linux (Docker):**
```bash
# Check if container is running
docker ps | grep noir-sqlserver

# Start container if stopped
docker start noir-sqlserver

# Check container logs for errors
docker logs noir-sqlserver

# Restart container
docker restart noir-sqlserver
```

#### "Port 1433 already in use"

```bash
# Find process using port 1433
lsof -i :1433  # macOS/Linux
netstat -ano | findstr :1433  # Windows

# Kill the process or use different port
docker run ... -p 1434:1433 ...  # Map to different host port
# Then update connection string to use localhost,1434
```

#### "Login failed for user 'sa'"

Check that the SA password meets SQL Server requirements:
- At least 8 characters
- Contains uppercase, lowercase, numbers, and symbols

```bash
# Reset password in running container (Azure SQL Edge uses mssql-tools18)
docker exec -it noir-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U SA -P 'OldPassword' -C \
  -Q "ALTER LOGIN SA WITH PASSWORD = 'NewPassword123!'"
```

#### ".NET SDK not found"

Ensure the SDK is in your PATH:
```bash
# Check .NET location
which dotnet  # macOS/Linux
where dotnet  # Windows

# Add to PATH if needed (example for .bashrc/.zshrc)
export PATH="$PATH:$HOME/.dotnet"
export DOTNET_ROOT="$HOME/.dotnet"
```

#### "The certificate chain was issued by an authority that is not trusted"

Add `TrustServerCertificate=True` to your connection string (already set in default configs).

---

## Production Deployment

### Windows Server

#### Prerequisites
- Windows Server 2019 or later
- IIS 10 with ASP.NET Core Module
- SQL Server (Express, Standard, or Enterprise)

#### Installation Steps

1. **Install .NET 10 Hosting Bundle:**
   ```powershell
   # Download from https://dotnet.microsoft.com/download/dotnet/10.0
   # Run the Hosting Bundle installer (not just SDK)
   ```

2. **Install SQL Server:**
   - Install SQL Server Express/Standard
   - Create database: `NOIR`
   - Create SQL login or use Windows Authentication

3. **Configure IIS:**
   ```powershell
   # Install IIS and ASP.NET Core Module
   Install-WindowsFeature -name Web-Server -IncludeManagementTools
   # ASP.NET Core Module is included in Hosting Bundle
   ```

4. **Publish Application:**
   ```bash
   # From development machine
   dotnet publish src/NOIR.Web -c Release -o ./publish

   # Copy ./publish folder to server (e.g., C:\inetpub\noir)
   ```

5. **Create IIS Site:**
   - Open IIS Manager
   - Add Website: `NOIR`
   - Physical path: `C:\inetpub\noir`
   - Application Pool: Create new, No Managed Code

6. **Configure appsettings.Production.json:**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=.;Database=NOIR;Trusted_Connection=True;TrustServerCertificate=True"
     },
     "JwtSettings": {
       "Secret": "YOUR-PRODUCTION-SECRET-KEY-AT-LEAST-32-CHARS!"
     }
   }
   ```

7. **Set Environment Variable:**
   - In IIS > Application Pool > Advanced Settings
   - Or system-wide: `ASPNETCORE_ENVIRONMENT=Production`

### Linux Server (Ubuntu/Debian)

#### Prerequisites
- Ubuntu 20.04+ or Debian 11+
- .NET 10 Runtime
- SQL Server (Docker or remote)
- Nginx or Apache (reverse proxy)

#### Installation Steps

1. **Install .NET Runtime:**
   ```bash
   sudo apt-get update
   sudo apt-get install -y aspnetcore-runtime-10.0
   ```

2. **Setup SQL Server:**
   ```bash
   # Option A: Docker
   docker run -e "ACCEPT_EULA=Y" \
     -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
     -p 1433:1433 \
     --name noir-sqlserver \
     --restart unless-stopped \
     -d mcr.microsoft.com/mssql/mssql-server:2022-latest

   # Option B: Use remote SQL Server
   # Update connection string accordingly
   ```

3. **Publish and Deploy:**
   ```bash
   # On development machine
   dotnet publish src/NOIR.Web -c Release -o ./publish

   # Copy to server
   scp -r ./publish user@server:/var/www/noir
   ```

4. **Create systemd Service:**
   ```bash
   sudo nano /etc/systemd/system/noir.service
   ```

   ```ini
   [Unit]
   Description=NOIR Web Application
   After=network.target

   [Service]
   WorkingDirectory=/var/www/noir
   ExecStart=/usr/bin/dotnet /var/www/noir/NOIR.Web.dll
   Restart=always
   RestartSec=10
   SyslogIdentifier=noir
   User=www-data
   Environment=ASPNETCORE_ENVIRONMENT=Production
   Environment=ASPNETCORE_URLS=http://localhost:4000

   [Install]
   WantedBy=multi-user.target
   ```

5. **Enable and Start Service:**
   ```bash
   sudo systemctl enable noir
   sudo systemctl start noir
   sudo systemctl status noir
   ```

6. **Configure Nginx Reverse Proxy:**
   ```bash
   sudo nano /etc/nginx/sites-available/noir
   ```

   ```nginx
   server {
       listen 80;
       server_name your-domain.com;

       location / {
           proxy_pass http://localhost:4000;
           proxy_http_version 1.1;
           proxy_set_header Upgrade $http_upgrade;
           proxy_set_header Connection keep-alive;
           proxy_set_header Host $host;
           proxy_cache_bypass $http_upgrade;
           proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
           proxy_set_header X-Forwarded-Proto $scheme;
       }
   }
   ```

   ```bash
   sudo ln -s /etc/nginx/sites-available/noir /etc/nginx/sites-enabled/
   sudo nginx -t
   sudo systemctl reload nginx
   ```

7. **SSL with Certbot:**
   ```bash
   sudo apt-get install certbot python3-certbot-nginx
   sudo certbot --nginx -d your-domain.com
   ```

### Docker Deployment

The repository includes production-ready Docker configuration:

- **Dockerfile** - Multi-stage build with non-root user, health checks
- **docker-compose.yml** - SQL Server + MailHog for development

#### Development with Docker Compose

```bash
# Start SQL Server and MailHog
docker-compose up -d

# Check services are running
docker-compose ps

# View logs
docker-compose logs -f sqlserver

# Stop services
docker-compose down
```

Services:
- SQL Server: `localhost:1433` (sa / coffee123@@)
- MailHog Web UI: http://localhost:8025
- MailHog SMTP: `localhost:1025`

#### Production Docker Build

```bash
# Build image
docker build -t noir-api:latest .

# Run with external SQL Server
docker run -d \
  -p 8080:8080 \
  -e "ConnectionStrings__DefaultConnection=Server=your-sql-server;Database=NOIR;User Id=sa;Password=YourPassword;TrustServerCertificate=True" \
  -e "JwtSettings__Secret=your-production-secret-at-least-32-chars" \
  --name noir-api \
  noir-api:latest
```

#### Full Production Stack

To run both API and SQL Server in Docker:

```bash
# Uncomment the api service in docker-compose.yml, then:
docker-compose up -d

# Or create docker-compose.prod.yml for production
```

---

## Claude AI Setup Assistance

When you clone this repository on a new machine and want Claude AI to help with setup:

### Prompt Template

Copy and paste this to Claude:

```
I just cloned the NOIR repository. Please help me set up the development environment.

My system:
- Operating System: [Windows 11 / macOS Sonoma / Ubuntu 22.04]
- .NET SDK installed: [Yes/No, version if yes]
- SQL Server: [None / LocalDB / Docker / Express]
- Docker installed: [Yes/No]

Please:
1. Check if I have the prerequisites installed
2. Guide me through the setup for my platform
3. Verify the application runs correctly
4. Help me run the tests

Start by reading the SETUP.md file for the current documentation.
```

### What Claude Can Help With

Claude has access to:
- `SETUP.md` - This comprehensive setup guide
- `CLAUDE.md` - Development patterns and conventions
- `README.md` - Project overview and features
- All source code for troubleshooting

Claude can:
- Diagnose connection issues
- Verify .NET and SQL Server installations
- Run build and test commands
- Explain error messages
- Guide through platform-specific setup

---

## Environment Configuration Reference

### appsettings.json (Base Configuration)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=NOIR;..."
  },
  "JwtSettings": {
    "Secret": "minimum-32-character-secret-key",
    "Issuer": "NOIR.API",
    "Audience": "NOIR.Client",
    "ExpirationInMinutes": 60,
    "RefreshTokenExpirationInDays": 7
  },
  "Identity": {
    "Password": {
      "RequireDigit": true,
      "RequireLowercase": true,
      "RequireUppercase": true,
      "RequireNonAlphanumeric": true,
      "RequiredLength": 12
    }
  }
}
```

### appsettings.Development.json (Development Overrides)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=NOIR;User Id=sa;Password=coffee123@@;TrustServerCertificate=True"
  },
  "Email": {
    "SmtpHost": "localhost",
    "SmtpPort": 1025,
    "EnableSsl": false
  },
  "Identity": {
    "Password": {
      "RequireDigit": false,
      "RequiredLength": 6
    }
  }
}
```

### Environment Variables

| Variable | Purpose | Example |
|----------|---------|---------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Development`, `Production` |
| `NOIR_TEST_SQL_CONNECTION` | Override test database | `Server=...;Database=...` |
| `NOIR_USE_LOCALDB` | Force LocalDB for tests | `true`, `false` |

---

## Version History

| Date | Version | Changes |
|------|---------|---------|
| 2025-01-02 | 1.0 | Initial comprehensive setup guide |

# NOIR Suggested Shell Commands

## Build & Run

### Build Solution
```bash
dotnet build src/NOIR.sln
```

### Run with Hot Reload
```bash
dotnet watch --project src/NOIR.Web
```

### Run Without Hot Reload
```bash
dotnet run --project src/NOIR.Web
```

## Testing

### Run All Tests
```bash
dotnet test src/NOIR.sln
```

### Run Specific Test Project
```bash
dotnet test tests/NOIR.Domain.Tests
```

### Run with Verbosity
```bash
dotnet test src/NOIR.sln --verbosity normal
```

## Database

### Create Migration
```bash
dotnet ef migrations add MigrationName \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web
```

### Apply Migrations
```bash
dotnet ef database update \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web
```

### Remove Last Migration
```bash
dotnet ef migrations remove \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web
```

## Frontend

### Install Dependencies
```bash
cd src/NOIR.Web/frontend && pnpm install
```

### Dev Server
```bash
cd src/NOIR.Web/frontend && pnpm run dev
```

### Build
```bash
cd src/NOIR.Web/frontend && pnpm run build
```

### Generate API Types
```bash
cd src/NOIR.Web/frontend && pnpm run generate:api
```

## Code Quality

### Format
```bash
dotnet format src/NOIR.sln
```

### Lint Frontend
```bash
cd src/NOIR.Web/frontend && pnpm run lint
```

## Package Management

### Add Package
```bash
dotnet add src/NOIR.Web package PackageName
```

### Restore Packages
```bash
dotnet restore src/NOIR.sln
```

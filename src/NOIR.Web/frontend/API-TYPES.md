# API Type Generation

This document explains how to keep frontend TypeScript types synchronized with backend API definitions.

## Overview

We use **openapi-typescript** to generate TypeScript types from the .NET backend's OpenAPI specification. This ensures 100% type safety between frontend and backend.

## Quick Start

```bash
# 1. Start the backend (in separate terminal)
cd src/NOIR.Web
dotnet run

# 2. Generate types from running backend
cd frontend
npm run generate:api
```

## Available Commands

| Command | Description |
|---------|-------------|
| `npm run generate:api` | Generate types from running backend at http://localhost:4000 |
| `npm run generate:api:file` | Generate types from exported openapi.json file |
| `node scripts/generate-api-types.mjs --help` | Show available options |

## Generated Output

Types are generated to `src/types/api.generated.ts`. This file includes:

- **Path types**: Request/response types for each API endpoint
- **Component schemas**: DTOs matching backend C# models
- **Operation types**: Parameters, request bodies, and responses

## Usage Example

```typescript
import type { paths, components } from '@/types/api.generated'

// Get response type for an endpoint
type LoginResponse = paths['/api/auth/login']['post']['responses']['200']['content']['application/json']

// Get a component/DTO type
type UserDto = components['schemas']['CurrentUserDto']

// Use in API calls
async function login(email: string, password: string): Promise<LoginResponse> {
  const response = await fetch('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password })
  })
  return response.json()
}
```

## Workflow

### When to Regenerate

Regenerate types whenever the backend API changes:

1. New endpoints added
2. Request/response DTOs modified
3. Validation rules changed
4. API routes renamed

### Recommended Workflow

```bash
# After pulling changes or modifying backend
dotnet build src/NOIR.sln

# Start backend
dotnet run --project src/NOIR.Web &

# Wait for startup, then generate
sleep 5
cd src/NOIR.Web/frontend
npm run generate:api

# Stop backend if running in background
kill %1
```

### CI/CD Integration

For CI pipelines, export the OpenAPI spec during build:

```bash
# Option 1: Run backend temporarily
dotnet run --project src/NOIR.Web &
sleep 10
curl http://localhost:4000/api/openapi/v1.json > frontend/openapi.json
kill %1
npm run generate:api:file --prefix frontend

# Option 2: Use Microsoft.Extensions.ApiDescription.Server (future)
# This generates spec at build time without running the app
```

## Type Organization

The project uses two types of API types:

### 1. Generated Types (`api.generated.ts`)

Auto-generated from OpenAPI spec. **Never edit this file manually.**

```typescript
// ❌ Don't import directly in components
import type { components } from '@/types/api.generated'

// ✅ Re-export through domain type files
// types/auth.ts
export type AuthResponse = components['schemas']['AuthResponse']
```

### 2. Domain Types (`auth.ts`, `api.ts`, etc.)

Hand-written types that re-export or extend generated types:

```typescript
// types/auth.ts
import type { components } from './api.generated'

// Re-export with cleaner names
export type AuthResponse = components['schemas']['AuthResponse']
export type CurrentUser = components['schemas']['CurrentUserDto']

// Add frontend-only types
export interface LoginFormData {
  email: string
  password: string
  rememberMe: boolean
}
```

## Troubleshooting

### "Connection refused" error

Make sure the backend is running:
```bash
dotnet run --project src/NOIR.Web
```

### Types don't match expected schema

1. Verify you're hitting the correct backend version
2. Check if the endpoint has authentication requirements
3. Regenerate after a fresh `dotnet build`

### Generated file is empty or malformed

1. Check the OpenAPI spec is valid: `curl http://localhost:4000/api/openapi/v1.json | jq .`
2. Ensure openapi-typescript is installed: `npm install`

## Comparison with NSwag

| Feature | openapi-typescript | NSwag |
|---------|-------------------|-------|
| Output | Types only | Types + HTTP client |
| Bundle size | Zero (types stripped) | Includes runtime code |
| Flexibility | Use any HTTP library | Uses generated client |
| Setup | Simple npm script | More configuration |

We chose openapi-typescript because:
- **Zero runtime overhead** - types are erased at build
- **Works with any HTTP library** - not locked into generated client
- **Simpler maintenance** - just types, no client code to debug
- **Modern TypeScript** - leverages latest TS features

If you need a full generated client, consider adding [@hey-api/openapi-ts](https://github.com/hey-api/openapi-ts) or [orval](https://orval.dev).

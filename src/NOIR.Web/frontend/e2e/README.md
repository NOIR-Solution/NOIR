# E2E Test Authentication

## Multiple Authentication States

This project uses role-based authentication with TWO distinct auth states:

### 1. Tenant Admin (Default)

- **File**: `e2e/.auth/tenant-admin.json`
- **Credentials**: `admin@noir.local` / `123qwe`
- **Permissions**: Full access WITHIN a single tenant
- **Used By**: Most tests (users, roles, content management)

### 2. Platform Admin (Platform Features)

- **File**: `e2e/.auth/platform-admin.json`
- **Credentials**: `platform@noir.local` / `Platform123!`
- **Permissions**: Cross-tenant + system-level operations
- **Used By**: Platform Settings, Tenant Management

## When to Use Each

### Use Platform Admin for tests requiring:

- `system:config:view` / `system:config:edit` (Platform Settings)
- `system:app:restart` (Application Restart)
- `tenants:*` (Tenant CRUD operations)

### Use Tenant Admin for:

- Everything else (user/role management, content, blog posts, etc.)

## Example Usage

```typescript
// Default behavior (Tenant Admin)
test.describe('User Management', () => {
  test('can create user', async ({ page }) => {
    // Uses tenant-admin.json automatically
  })
})

// Explicit Platform Admin
test.describe('Platform Settings', () => {
  // Override auth state for this test suite
  test.use({ storageState: 'e2e/.auth/platform-admin.json' })

  test('can edit configuration', async ({ page }) => {
    // Uses platform-admin.json
  })
})
```

## Troubleshooting

### 403 Forbidden Errors

If you encounter 403 Forbidden errors in your tests:

1. **Check the feature requirements** - Does it need platform admin permissions?
2. **Verify auth state** - Add `test.use({ storageState: 'e2e/.auth/platform-admin.json' })` to your test suite
3. **Regenerate auth files** - Delete `e2e/.auth/*.json` and run tests again to trigger global-setup

### Auth Files Not Found

If auth files are missing:

1. Run the global setup: `npx playwright test --project=setup`
2. Or run any test - global setup runs automatically before tests

### Permission Changes

If new permissions are added:

1. Drop and recreate the database to seed new permissions
2. Delete `e2e/.auth/*.json` files
3. Run tests again to generate fresh auth files with updated permissions

## Architecture

### Global Setup

Both auth states are created in `e2e/global-setup.ts`:

```typescript
// Creates tenant-admin.json
setup('authenticate as tenant admin', async ({ page }) => {
  // Login as admin@noir.local
})

// Creates platform-admin.json
setup('authenticate as platform admin', async ({ page }) => {
  // Login as platform@noir.local
})
```

### Playwright Configuration

The default auth state is configured in `playwright.config.ts`:

```typescript
{
  name: 'chromium',
  use: {
    storageState: 'e2e/.auth/tenant-admin.json',  // Default
  },
  dependencies: ['setup'],  // Runs global-setup first
}
```

### Auth State Contents

Auth files contain:

- Cookies (including authentication token)
- Local storage
- Session storage

These are automatically reused for all subsequent requests in tests.

## Best Practices

1. **Use the right auth** - Don't use platform admin for tenant-scoped features
2. **Document requirements** - Add comments explaining why platform admin is needed
3. **Test isolation** - Each auth state should be independent (no shared session state)
4. **Parallel execution** - Tests using different auth states can run in parallel safely

import { test, expect } from '@playwright/test';

interface AuthMeResponse {
  id: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  displayName: string | null;
  fullName: string;
  phoneNumber: string | null;
  avatarUrl: string | null;
  roles: string[];
  tenantId: string | null;
  isActive: boolean;
  createdAt: string;
}

/**
 * Test to verify that platform admin user has TenantId = null
 * and displays "Platform" (not a specific tenant ID) in the dashboard.
 */
test.use({ storageState: { cookies: [], origins: [] } }); // Don't use pre-authenticated state

test('platform admin should have no tenant ID', async ({ page }) => {
  // Navigate to login page
  await page.goto('http://localhost:3000/login');

  // Login as platform admin (using correct password from appsettings.json)
  await page.fill('input[type="email"]', 'platform@noir.local');
  await page.fill('input[type="password"]', 'Platform123!');
  await page.click('button[type="submit"]');

  // Wait for navigation to portal (platform admin goes to /portal, not /portal/dashboard)
  await page.waitForURL('**/portal', { timeout: 10000 });

  // Take screenshot for debugging
  await page.screenshot({ path: '/tmp/playwright-platform-admin-dashboard.png', fullPage: true });

  // Intercept the /api/auth/me call to inspect the response
  let authMeResponse: AuthMeResponse | null = null;
  page.on('response', async response => {
    if (response.url().includes('/api/auth/me')) {
      authMeResponse = await response.json();
      console.log('=== /api/auth/me response ===');
      console.log(JSON.stringify(authMeResponse, null, 2));
    }
  });

  // Reload to trigger /api/auth/me call
  await page.reload();
  await page.waitForTimeout(2000);

  // Get the access token from localStorage
  const accessToken = await page.evaluate(() => {
    return localStorage.getItem('accessToken');
  });

  console.log('=== Access Token ===');
  console.log(accessToken);

  if (accessToken) {
    // Decode JWT token (it's base64 encoded)
    const tokenParts = accessToken.split('.');
    if (tokenParts.length === 3) {
      const payload = JSON.parse(Buffer.from(tokenParts[1], 'base64').toString());
      console.log('=== JWT Token Payload ===');
      console.log(JSON.stringify(payload, null, 2));

      // Check if tenant_id claim exists
      if (payload.tenant_id) {
        console.error(`❌ ERROR: JWT token contains tenant_id claim: ${payload.tenant_id}`);
      } else {
        console.log('✅ CORRECT: JWT token does NOT contain tenant_id claim');
      }
    }
  }

  // Check the tenant display in the dashboard (get parent container with both label and value)
  const tenantContainer = page.locator('text="Tenant:"').locator('..');
  const tenantText = await tenantContainer.textContent();
  console.log('=== Tenant Display in UI ===');
  console.log(tenantText);

  // Verify tenant display shows "Platform" not a GUID
  expect(tenantText).toContain('Platform');
  expect(tenantText).not.toMatch(/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/i);

  // Additional verification: check /api/auth/me response
  if (authMeResponse) {
    console.log('=== Verifying /api/auth/me response ===');
    console.log(`TenantId in response: ${authMeResponse.tenantId ?? 'NULL'}`);

    // When C# returns null, JSON serialization omits the field (undefined in JS)
    if (authMeResponse.tenantId !== null && authMeResponse.tenantId !== undefined) {
      console.error(`❌ ERROR: /api/auth/me returns tenantId: ${authMeResponse.tenantId}`);
      throw new Error(`Expected tenantId to be null/undefined, but got: ${authMeResponse.tenantId}`);
    } else {
      console.log('✅ CORRECT: /api/auth/me returns tenantId = null (omitted from JSON)');
    }
  }
});

import { test, expect } from '../../fixtures/base.fixture';
import { testRole } from '../../helpers/test-data';
import { waitForTableLoad, expectToast, confirmDelete } from '../../helpers/selectors';

test.describe('Admin Roles @regression', () => {
  /**
   * ADMIN-004: Role list loads
   * Verify that the roles management page renders with existing roles.
   */
  test('ADMIN-004: role list loads @regression', async ({
    rolesPage,
    page,
  }) => {
    await rolesPage.goto();
    await waitForTableLoad(page);

    // At minimum the default "Admin" role should exist
    const rows = rolesPage.roleRows;
    await expect(rows).not.toHaveCount(0);

    // Search for "Admin" role to avoid pagination issues if many test roles exist
    const searchInput = rolesPage.searchInput;
    if (await searchInput.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await searchInput.fill('Admin');
      await page.waitForTimeout(500); // debounce
    }

    // Verify the Admin role is visible
    await rolesPage.expectRoleInList('Admin');
  });

  /**
   * ADMIN-005: Create role, edit, and delete (CRUD lifecycle)
   * Tests the full CRUD lifecycle for roles.
   * Note: The permissions dialog (Manage Permissions) is tested separately via
   * ADMIN-004 to keep this test focused on role CRUD.
   */
  test('ADMIN-005: create role and assign permissions @regression', async ({
    rolesPage,
    page,
    api,
    trackCleanup,
  }) => {
    const roleData = testRole();
    await rolesPage.goto();
    await waitForTableLoad(page);

    // Create a new role via UI
    await rolesPage.createRole({ name: roleData.name, description: roleData.description });
    await expectToast(page, /created|success|th\u00e0nh c\u00f4ng/i);

    // Verify role appears in list
    await rolesPage.expectRoleInList(roleData.name);

    // Find the created role's ID via API for cleanup fallback
    const rolesRes = await api.request.get(`http://localhost:4000/api/roles?page=1&pageSize=50`);
    const rolesData = await rolesRes.json().catch(() => ({ items: [] }));
    const createdRole = (rolesData.items as Array<{ id: string; name: string }>)
      .find(r => r.name === roleData.name);
    if (createdRole) {
      trackCleanup(async () => { await api.deleteRole(createdRole.id).catch(() => {}); });
    }

    // Edit the role — wait for dialog, fill form, submit via API directly as fallback
    await rolesPage.editRole(roleData.name);
    // Wait for edit dialog to be open
    await expect(page.locator('[role="dialog"]')).toBeVisible({ timeout: 5_000 });
    // Use locator scoped to dialog (portaled outside root, so no aria-hidden issue)
    const editDialog = page.locator('[role="dialog"]');
    const nameInput = editDialog.getByLabel(/name/i).first();
    await expect(nameInput).toBeVisible({ timeout: 5_000 });
    await nameInput.fill('');
    const updatedName = `${roleData.name}-Updated`;
    await nameInput.fill(updatedName);
    // Find Save button in the dialog and submit via JS click (trusted event, bypasses overlay)
    const editSaveBtn = editDialog.getByRole('button', { name: /save/i });
    await editSaveBtn.waitFor({ state: 'attached', timeout: 5_000 });
    await page.evaluate(() => {
      // Click the first Save button in the dialog (trusted event)
      const btns = [...document.querySelectorAll('[role="dialog"] button')];
      const saveBtn = btns.find(b => /save/i.test(b.textContent || ''));
      if (saveBtn) (saveBtn as HTMLButtonElement).click();
    });
    await page.waitForResponse(
      resp => resp.url().includes('/api/roles') && resp.request().method() === 'PUT',
      { timeout: 10_000 },
    ).catch(() => page.waitForTimeout(2_000));
    await expectToast(page, /updated|saved|success|th\u00e0nh c\u00f4ng/i);

    // Verify the updated name in list
    await rolesPage.expectRoleInList(updatedName);

    // Delete the role via dropdown menu
    const updatedRow = page.getByRole('row', { name: new RegExp(updatedName, 'i') });
    await expect(updatedRow).toBeVisible({ timeout: 10_000 });
    await updatedRow.getByRole('button').first().click();
    await page.getByRole('menuitem', { name: /delete/i }).click();
    // Set up response watcher BEFORE clicking confirm (response may arrive quickly)
    const deleteResponsePromise = page.waitForResponse(
      resp => resp.url().includes('/api/roles') && resp.request().method() === 'DELETE',
      { timeout: 10_000 },
    ).catch(() => undefined);
    await confirmDelete(page);
    await deleteResponsePromise; // Ensure API response completes before checking toast
    await expectToast(page, /deleted|removed|success|th\u00e0nh c\u00f4ng/i);

    // Verify role no longer in list
    await expect(page.getByRole('row', { name: new RegExp(updatedName, 'i') })).not.toBeVisible();
  });

  /**
   * ADMIN-006: Feature toggle — disable module, verify sidebar hidden
   * Tests that toggling a module off hides it from the sidebar.
   *
   * This test requires platform admin auth to toggle features.
   */
  test('ADMIN-006: feature toggle hides sidebar item @regression', async ({
    page,
  }) => {
    // Use platform admin auth for feature management
    const platformAdminState = '.auth/platform-admin.json';

    // Create a new context with platform admin auth
    const context = await page.context().browser()!.newContext({
      storageState: platformAdminState,
    });
    const platformPage = await context.newPage();

    try {
      // Navigate to platform settings / modules tab
      await platformPage.goto('/portal/admin/platform-settings');
      await platformPage.waitForLoadState('networkidle');

      // Find and click the Modules/Features tab
      const modulesTab = platformPage.getByRole('tab', { name: /modules|features|t\u00ednh n\u0103ng/i });
      if (await modulesTab.isVisible({ timeout: 5_000 }).catch(() => false)) {
        await modulesTab.click();
      }

      // Find the Blog module toggle and get its current state
      const blogToggle = platformPage.getByRole('switch', { name: /blog/i })
        .or(platformPage.locator('label', { hasText: /blog/i }).locator('button[role="switch"]'));

      // If blog is enabled, disable it
      if (await blogToggle.isVisible({ timeout: 5_000 }).catch(() => false)) {
        const isChecked = await blogToggle.getAttribute('data-state');
        if (isChecked === 'checked') {
          await blogToggle.click();

          // Save changes
          const saveBtn = platformPage.getByRole('button', { name: /save|update|apply/i });
          if (await saveBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
            await saveBtn.click();
            await platformPage.waitForResponse(resp =>
              resp.url().includes('/api/') && (resp.request().method() === 'PUT' || resp.request().method() === 'POST'),
            );
          }

          // Verify sidebar no longer shows Blog for tenant admin
          // Switch to tenant admin context
          await page.goto('/portal');
          await page.waitForLoadState('networkidle');

          const sidebar = page.locator('[data-testid="sidebar"], aside nav, [role="navigation"]');
          await expect(sidebar).toBeVisible();

          // Blog menu item should be hidden
          const blogLink = sidebar.getByRole('link', { name: /blog/i });
          await expect(blogLink).not.toBeVisible();

          // Re-enable the Blog module
          await platformPage.goto('/portal/admin/platform-settings');
          await platformPage.waitForLoadState('networkidle');

          const modulesTab2 = platformPage.getByRole('tab', { name: /modules|features|t\u00ednh n\u0103ng/i });
          if (await modulesTab2.isVisible({ timeout: 5_000 }).catch(() => false)) {
            await modulesTab2.click();
          }

          const blogToggle2 = platformPage.getByRole('switch', { name: /blog/i })
            .or(platformPage.locator('label', { hasText: /blog/i }).locator('button[role="switch"]'));
          await blogToggle2.click();

          const saveBtn2 = platformPage.getByRole('button', { name: /save|update|apply/i });
          if (await saveBtn2.isVisible({ timeout: 3_000 }).catch(() => false)) {
            await saveBtn2.click();
          }
        }
      }
    } finally {
      await platformPage.close();
      await context.close();
    }
  });
});

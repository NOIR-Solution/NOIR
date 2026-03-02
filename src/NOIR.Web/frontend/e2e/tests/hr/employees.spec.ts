import { test, expect } from '../../fixtures/base.fixture';
import { testEmployee } from '../../helpers/test-data';
import { expectToast } from '../../helpers/selectors';

const API_URL = process.env.API_URL ?? 'http://localhost:4000';

/**
 * HR Employees E2E Tests
 *
 * Covers: HR-001 (Employee CRUD), HR-002 (Validation errors),
 *         HR-003 (Edit employee + assign department), HR-004 (Deactivate employee)
 *
 * Notes:
 * - Employees are soft-deleted (deactivated), not hard-deleted via UI.
 * - Editing from the list page opens a form that requires re-selecting department
 *   (EmployeeListDto doesn't include departmentId). Tests use the detail page
 *   to access the full EmployeeDto for reliable edit testing.
 */

test.describe('HR Employees @regression', () => {
  async function createTestDepartment(api: any): Promise<{ id: string; name: string; code: string }> {
    const suffix = Math.random().toString(36).substring(2, 8);
    const res = await api.request.post(`${API_URL}/api/hr/departments`, {
      data: { name: `E2E Dept ${suffix}`, code: `E2E-${suffix.toUpperCase()}` },
    });
    return res.json();
  }

  async function deleteDepartment(api: any, id: string) {
    await api.request.delete(`${API_URL}/api/hr/departments/${id}`).catch(() => {});
  }

  // ─── HR-001: Employee CRUD ───────────────────────────────────

  test.describe('HR-001: Employee CRUD @smoke', () => {
    let employeeId: string;
    let departmentId: string;

    test.afterEach(async ({ api }) => {
      if (employeeId) await api.deleteEmployee(employeeId).catch(() => {});
      if (departmentId) await deleteDepartment(api, departmentId);
    });

    test('should load employee list page', async ({ employeesPage }) => {
      await employeesPage.goto();
      await expect(employeesPage.employeeTable).toBeVisible({ timeout: 15_000 });
    });

    test('should create a new employee', async ({ employeesPage, api, page }) => {
      const dept = await createTestDepartment(api);
      departmentId = dept.id;

      const data = testEmployee();
      await employeesPage.goto();
      await expect(employeesPage.employeeTable).toBeVisible({ timeout: 15_000 });
      await employeesPage.createEmployee({
        firstName: data.firstName,
        lastName: data.lastName,
        email: data.email,
        departmentId: dept.name,
      });

      await expectToast(page, /created|success/i);
      await employeesPage.expectEmployeeInList(data.lastName);

      const searchRes = await api.request.get(
        `${API_URL}/api/hr/employees?search=${encodeURIComponent(data.email)}`,
      );
      if (searchRes.ok()) {
        const body = await searchRes.json();
        const match = (body?.items ?? []).find((e: { email?: string }) => e.email === data.email);
        if (match) employeeId = match.id;
      }
    });

    test('should edit an existing employee', async ({ api, page }) => {
      const dept = await createTestDepartment(api);
      departmentId = dept.id;

      const data = testEmployee({ departmentId: dept.id });
      const created = await api.createEmployee(data);
      employeeId = created.id;

      // Use detail page — loads full EmployeeDto with departmentId so form pre-fills correctly
      await page.goto(`/portal/hr/employees/${created.id}`);
      await page.waitForLoadState('networkidle');
      const editBtn = page.getByRole('button', { name: /edit/i }).first();
      await expect(editBtn).toBeVisible({ timeout: 15_000 });
      await editBtn.click();

      await page.locator('[role="dialog"]').waitFor({ state: 'visible', timeout: 5_000 });

      const lastNameInput = page.getByLabel(/last name/i);
      await lastNameInput.clear();
      await lastNameInput.fill(`${data.lastName}Updated`);

      // Submit via evaluate() — CredenzaFooter button may be outside viewport
      await page.evaluate(() => {
        const btns = [...document.querySelectorAll('[role="dialog"] button')];
        const saveBtn = btns.find(b => /save|update|submit/i.test(b.textContent || ''));
        if (saveBtn) (saveBtn as HTMLButtonElement).click();
      });

      await page.waitForResponse(
        resp => resp.url().includes('/api/hr/employees') && resp.request().method() === 'PUT',
        { timeout: 10_000 },
      ).catch(() => page.waitForTimeout(2_000));
      await expectToast(page, /updated|success/i);
    });

    test('should show employee with auto-generated code', async ({ employeesPage, api, page }) => {
      const dept = await createTestDepartment(api);
      departmentId = dept.id;

      const data = testEmployee({ departmentId: dept.id });
      const created = await api.createEmployee(data);
      employeeId = created.id;

      await employeesPage.goto();
      await expect(employeesPage.employeeTable).toBeVisible({ timeout: 15_000 });
      await employeesPage.expectEmployeeInList(data.lastName);

      const row = page.getByRole('row', { name: new RegExp(data.lastName, 'i') });
      await expect(row).toContainText(/EMP-\d+/i);
    });

    test('should deactivate an employee (destructive action from list)', async ({
      employeesPage,
      api,
      page,
    }) => {
      // Employees are soft-deleted via Deactivate, not hard-deleted.
      // List dropdown provides: View | Edit | Deactivate
      const dept = await createTestDepartment(api);
      departmentId = dept.id;

      const data = testEmployee({ departmentId: dept.id });
      const created = await api.createEmployee(data);
      employeeId = created.id;

      await employeesPage.goto();
      await expect(employeesPage.employeeTable).toBeVisible({ timeout: 15_000 });

      await employeesPage.openEmployeeMenu(data.lastName);
      await page.getByRole('menuitem', { name: /deactivate/i }).click();

      // Confirm deactivation dialog — button text is "Deactivate Employee"
      await page.getByRole('dialog').waitFor({ state: 'visible', timeout: 5_000 });
      await page.waitForTimeout(500); // let dialog animation fully settle

      // Use page.evaluate() to find and trigger click on the confirm button
      // This avoids Playwright intercept issues with dialogs that have CSS transforms
      await page.evaluate(() => {
        const dialog = document.querySelector('[role="dialog"]');
        if (!dialog) return;
        const btns = Array.from(dialog.querySelectorAll('button'));
        const confirmBtn = btns.find(b => {
          const text = b.textContent?.trim() ?? '';
          return /deactivate employee/i.test(text);
        });
        if (confirmBtn) (confirmBtn as HTMLButtonElement).click();
      });

      // Wait for deactivate API response to confirm the mutation fired
      await page.waitForResponse(
        resp => resp.url().includes('/api/hr/employees') && resp.url().includes('deactivate'),
        { timeout: 5_000 },
      ).catch(() => {});

      await expectToast(page, /deactivated|success/i);

      // Reactivate via API so afterEach cleanup can delete the employee
      await api.request.post(`${API_URL}/api/hr/employees/${created.id}/reactivate`).catch(() => {});
    });
  });

  // ─── HR-002: Employee validation errors ────────────────────────

  test.describe('HR-002: Employee validation errors @regression', () => {
    test('should show validation errors for empty required fields', async ({
      employeesPage,
      page,
    }) => {
      await employeesPage.goto();
      await expect(employeesPage.employeeTable).toBeVisible({ timeout: 15_000 });
      await employeesPage.createButton.click();
      await page.locator('[role="dialog"]').waitFor({ state: 'visible', timeout: 5_000 });

      // Submit empty form — use evaluate() for Credenza footer button
      await page.evaluate(() => {
        const btns = [...document.querySelectorAll('[role="dialog"] button')];
        const saveBtn = btns.find(b => /save|create|submit/i.test(b.textContent || ''));
        if (saveBtn) (saveBtn as HTMLButtonElement).click();
      });

      // Expect at least one validation error message in the dialog
      await expect(
        page.locator('[role="dialog"]').getByText(/required/i).first(),
      ).toBeVisible({ timeout: 5_000 });
    });

    test('should show validation error for invalid email', async ({
      employeesPage,
      page,
    }) => {
      await employeesPage.goto();
      await expect(employeesPage.employeeTable).toBeVisible({ timeout: 15_000 });
      await employeesPage.createButton.click();
      await page.locator('[role="dialog"]').waitFor({ state: 'visible', timeout: 5_000 });

      await page.getByLabel(/first name/i).fill('Test');
      await page.getByLabel(/last name/i).fill('Employee');
      await page.getByLabel(/email/i).fill('invalid-email');
      await page.getByLabel(/first name/i).focus(); // trigger onBlur validation

      await expect(
        page.getByText(/valid email|invalid email|email.*invalid/i),
      ).toBeVisible({ timeout: 5_000 });
    });

    test('should show validation error for duplicate email', async ({
      employeesPage,
      api,
      page,
    }) => {
      const dept = await createTestDepartment(api);
      const data = testEmployee({ departmentId: dept.id });
      const created = await api.createEmployee(data);

      try {
        await employeesPage.goto();
        await expect(employeesPage.employeeTable).toBeVisible({ timeout: 15_000 });
        // Must pass departmentId (as dept name) — Department is required by frontend validator
        // Without it, Zod validation fails before reaching the backend duplicate check
        await employeesPage.createEmployee({
          firstName: 'Duplicate',
          lastName: 'EmailTest',
          email: data.email,
          departmentId: dept.name,
        });

        await expectToast(page, /already exists|duplicate|conflict/i, 'error');
      } finally {
        await api.deleteEmployee(created.id).catch(() => {});
        await deleteDepartment(api, dept.id).catch(() => {});
      }
    });
  });

  // ─── HR-003: Edit employee + assign department ─────────────────

  test.describe('HR-003: Edit employee + assign department @regression', () => {
    test('should change employee department', async ({ api, page }) => {
      const dept1 = await createTestDepartment(api);
      const dept2 = await createTestDepartment(api);

      const data = testEmployee({ departmentId: dept1.id });
      const created = await api.createEmployee(data);

      try {
        // Navigate to detail page — full EmployeeDto so department pre-fills correctly
        await page.goto(`/portal/hr/employees/${created.id}`);
        await page.waitForLoadState('networkidle');

        const editBtn = page.getByRole('button', { name: /edit/i }).first();
        await expect(editBtn).toBeVisible({ timeout: 15_000 });
        await editBtn.click();
        await page.locator('[role="dialog"]').waitFor({ state: 'visible', timeout: 5_000 });

        // Change department (first combobox in dialog is the department selector)
        const deptCombobox = page.locator('[role="dialog"]').getByRole('combobox').first();
        await deptCombobox.click();
        await page.getByRole('option', { name: new RegExp(dept2.name, 'i') }).click();

        // Save via evaluate()
        await page.evaluate(() => {
          const btns = [...document.querySelectorAll('[role="dialog"] button')];
          const saveBtn = btns.find(b => /save|update|submit/i.test(b.textContent || ''));
          if (saveBtn) (saveBtn as HTMLButtonElement).click();
        });
        await page.waitForResponse(
          resp => resp.url().includes('/api/hr/employees') && resp.request().method() === 'PUT',
          { timeout: 10_000 },
        ).catch(() => page.waitForTimeout(2_000));
        await expectToast(page, /updated|success/i);

        // Verify: reload and check new department is shown
        await page.reload();
        await page.waitForLoadState('networkidle');
        // Department name may appear multiple times (header + field) — use .first()
        await expect(page.getByText(new RegExp(dept2.name, 'i')).first()).toBeVisible({ timeout: 10_000 });
      } finally {
        await api.deleteEmployee(created.id).catch(() => {});
        await deleteDepartment(api, dept1.id).catch(() => {});
        await deleteDepartment(api, dept2.id).catch(() => {});
      }
    });
  });

  // ─── HR-004: Deactivate employee ──────────────────────────────

  test.describe('HR-004: Deactivate employee @regression', () => {
    test('should deactivate an active employee', async ({ api, page }) => {
      const dept = await createTestDepartment(api);
      const data = testEmployee({ departmentId: dept.id });
      const created = await api.createEmployee(data);

      try {
        await page.goto(`/portal/hr/employees/${created.id}`);
        await page.waitForLoadState('networkidle');
        await page.waitForSelector('h1, h2, button', { timeout: 15_000 }).catch(() => {});

        const deactivateBtn = page.getByRole('button', { name: /deactivate|resign|terminate/i }).first();
        if (await deactivateBtn.isVisible({ timeout: 5_000 }).catch(() => false)) {
          await deactivateBtn.click();

          // Wait for confirmation dialog and click "Deactivate Employee" confirm button
          await page.locator('[role="dialog"]').waitFor({ state: 'visible', timeout: 5_000 }).catch(() => {});
          await page.waitForTimeout(500); // let dialog animation fully settle
          // Use page.evaluate() to fire trusted click on the confirm button
          await page.evaluate(() => {
            const dialog = document.querySelector('[role="dialog"]');
            if (!dialog) return;
            const btns = Array.from(dialog.querySelectorAll('button'));
            const confirmBtn = btns.find(b => {
              const text = b.textContent?.trim() ?? '';
              return /deactivate employee/i.test(text);
            });
            if (confirmBtn) (confirmBtn as HTMLButtonElement).click();
          });
          // Wait for deactivate API response
          await page.waitForResponse(
            resp => resp.url().includes('/api/hr/employees') && resp.url().includes('deactivate'),
            { timeout: 5_000 },
          ).catch(() => {});

          await expectToast(page, /deactivated|updated|success/i);
          await expect(
            page.getByText(/resigned|inactive|terminated|suspended/i).first(),
          ).toBeVisible({ timeout: 5_000 });
        } else {
          // Fallback: deactivate via API
          const res = await api.request.post(
            `${API_URL}/api/hr/employees/${created.id}/deactivate`,
            { data: { status: 'Resigned' } },
          );
          expect(res.ok()).toBeTruthy();
          await page.reload();
          await page.waitForLoadState('networkidle');
          await expect(page.getByText(/resigned|inactive/i).first()).toBeVisible({ timeout: 5_000 });
        }
      } finally {
        await api.deleteEmployee(created.id).catch(() => {});
        await deleteDepartment(api, dept.id).catch(() => {});
      }
    });

    test('should be able to reactivate a deactivated employee', async ({ api, page }) => {
      const dept = await createTestDepartment(api);
      const data = testEmployee({ departmentId: dept.id });
      const created = await api.createEmployee(data);

      await api.request.post(
        `${API_URL}/api/hr/employees/${created.id}/deactivate`,
        { data: { status: 'Resigned' } },
      );

      try {
        await page.goto(`/portal/hr/employees/${created.id}`);
        await page.waitForLoadState('networkidle');
        await page.waitForSelector('h1, h2, button', { timeout: 15_000 }).catch(() => {});

        const reactivateBtn = page.getByRole('button', { name: /reactivate|activate/i });
        if (await reactivateBtn.isVisible({ timeout: 5_000 }).catch(() => false)) {
          await reactivateBtn.click();

          const confirmBtn = page.getByRole('button', { name: /confirm|yes|ok/i });
          if (await confirmBtn.isVisible({ timeout: 2_000 }).catch(() => false)) {
            await confirmBtn.click();
          }

          await expectToast(page, /reactivated|updated|success/i);
          await expect(page.getByText(/active/i).first()).toBeVisible({ timeout: 5_000 });
        } else {
          const res = await api.request.post(`${API_URL}/api/hr/employees/${created.id}/reactivate`);
          expect(res.ok()).toBeTruthy();
        }
      } finally {
        await api.deleteEmployee(created.id).catch(() => {});
        await deleteDepartment(api, dept.id).catch(() => {});
      }
    });
  });
});

import { type Page, type Locator, expect } from '@playwright/test';

export class EmployeesPage {
  readonly createButton: Locator;
  readonly employeeTable: Locator;
  readonly searchInput: Locator;
  readonly employeeRows: Locator;

  constructor(private page: Page) {
    this.createButton = page.getByRole('button', { name: /create|add|new/i }).first();
    this.employeeTable = page.getByRole('table');
    this.searchInput = page.getByPlaceholder(/search/i);
    this.employeeRows = page.getByRole('table').getByRole('row');
  }

  async goto() {
    await this.page.goto('/portal/hr/employees');
    // Wait for either the table (with data) or empty state to appear
    // This is more robust than waitForLoadState('networkidle') for SPAs
    await Promise.race([
      this.page.waitForSelector('table', { timeout: 15_000 }),
      this.page.waitForSelector('[data-testid="empty-state"], [class*="EmptyState"]', { timeout: 15_000 }),
      this.page.waitForLoadState('networkidle').then(() =>
        this.page.waitForTimeout(2_000)
      ),
    ]).catch(() => {});
    // Give React time to finish rendering after data loads
    await this.page.waitForTimeout(500);
  }

  async createEmployee(data: {
    firstName: string;
    lastName: string;
    email: string;
    departmentId?: string;
  }) {
    await this.createButton.click();
    // Wait for the dialog to open
    await this.page.locator('[role="dialog"]').waitFor({ state: 'visible', timeout: 5_000 });
    await this.page.getByLabel(/first name/i).fill(data.firstName);
    await this.page.getByLabel(/last name/i).fill(data.lastName);
    await this.page.getByLabel(/email/i).fill(data.email);
    if (data.departmentId) {
      // The departmentId here is actually the department NAME to select
      const deptCombobox = this.page.getByRole('combobox', { name: /department/i });
      await deptCombobox.click();
      await this.page
        .getByRole('option')
        .filter({ hasText: new RegExp(data.departmentId, 'i') })
        .first()
        .click();
    }
    // Use evaluate() to generate trusted click event for CredenzaFooter button
    await this.page.evaluate(() => {
      const btns = [...document.querySelectorAll('[role="dialog"] button')];
      const saveBtn = btns.find(b => /save|create|submit/i.test(b.textContent || ''));
      if (saveBtn) (saveBtn as HTMLButtonElement).click();
    });
    await this.page.waitForResponse(resp =>
      resp.url().includes('/api/hr/employees') && resp.request().method() === 'POST',
      { timeout: 10_000 },
    ).catch(() => this.page.waitForTimeout(2_000));
  }

  async openEmployeeMenu(lastName: string) {
    // The employees table uses DropdownMenu (EllipsisVertical) for row actions
    const row = this.page.getByRole('row', { name: new RegExp(lastName, 'i') });
    await expect(row).toBeVisible({ timeout: 10_000 });
    // The first button in each row is the EllipsisVertical dropdown trigger
    const triggerBtn = row.getByRole('button').first();
    await expect(triggerBtn).toBeVisible({ timeout: 5_000 });
    await triggerBtn.click();
  }

  async editEmployee(lastName: string) {
    await this.openEmployeeMenu(lastName);
    await this.page.getByRole('menuitem', { name: /edit/i }).click();
    await this.page.locator('[role="dialog"]').waitFor({ state: 'visible', timeout: 5_000 });
  }

  async deleteEmployee(lastName: string) {
    await this.openEmployeeMenu(lastName);
    await this.page.getByRole('menuitem', { name: /delete/i }).click();
  }

  async searchEmployee(query: string) {
    await this.searchInput.fill(query);
    await this.page.waitForResponse(resp =>
      resp.url().includes('/api/hr/employees') && resp.status() === 200,
    );
  }

  async expectEmployeeInList(name: string) {
    await expect(this.page.getByRole('row', { name: new RegExp(name, 'i') })).toBeVisible({ timeout: 10_000 });
  }
}

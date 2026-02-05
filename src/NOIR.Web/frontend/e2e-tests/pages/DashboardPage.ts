import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './BasePage';

/**
 * DashboardPage - Page Object for the main dashboard
 */
export class DashboardPage extends BasePage {
  readonly welcomeMessage: Locator;
  readonly statsCards: Locator;
  readonly recentActivity: Locator;
  readonly quickActions: Locator;

  constructor(page: Page) {
    super(page);

    this.welcomeMessage = page.locator('[data-testid="welcome-message"], h1, .welcome');
    this.statsCards = page.locator('[data-testid="stats-card"], .stat-card, .dashboard-stat');
    this.recentActivity = page.locator('[data-testid="recent-activity"], .recent-activity');
    this.quickActions = page.locator('[data-testid="quick-actions"], .quick-actions');
  }

  /**
   * Navigate to dashboard
   */
  async navigate(): Promise<void> {
    await this.goto('/portal');
  }

  /**
   * Verify dashboard loaded correctly
   */
  async expectDashboardLoaded(): Promise<void> {
    await expect(this.sidebar).toBeVisible({ timeout: 10000 });
    // Dashboard should show some content
    const content = this.page.locator('main, [data-testid="dashboard-content"], .dashboard');
    await expect(content.first()).toBeVisible({ timeout: 10000 });
  }

  /**
   * Wait for sidebar to fully load with all permission-based menu items
   * This waits for the E-commerce section links to appear
   */
  async waitForSidebarReady(): Promise<void> {
    // Wait for sidebar to be visible first
    await expect(this.sidebar).toBeVisible({ timeout: 10000 });

    // Wait for page to fully load (permissions are fetched async)
    await this.page.waitForLoadState('networkidle', { timeout: 30000 }).catch(() => {
      // Network might not go idle if there are long-polling connections
    });

    // Wait for permission-based links to render (e.g., Products link)
    // The sidebar loads menu items based on permissions fetched async
    const ecommerceLink = this.page.locator(
      'a[href="/portal/ecommerce/products"], ' +
      'a[href="/portal/ecommerce/categories"], ' +
      'a[href="/portal/admin/users"]'
    );
    await expect(ecommerceLink.first()).toBeVisible({ timeout: 30000 });
  }

  /**
   * Get stat card value by title
   */
  async getStatValue(title: string): Promise<string> {
    const card = this.page.locator(`[data-testid="stats-card"]:has-text("${title}"), .stat-card:has-text("${title}")`);
    const value = card.locator('.stat-value, [data-testid="stat-value"], .text-2xl, .text-3xl');
    return await value.textContent() || '';
  }

  /**
   * Click quick action button
   */
  async clickQuickAction(actionText: string): Promise<void> {
    const action = this.quickActions.locator(`button:has-text("${actionText}"), a:has-text("${actionText}")`);
    await action.click();
    await this.waitForPageLoad();
  }

  /**
   * Navigate to products via sidebar
   */
  async navigateToProducts(): Promise<void> {
    await this.navigateToMenu('Products');
  }

  /**
   * Navigate to categories via sidebar
   */
  async navigateToCategories(): Promise<void> {
    await this.navigateToMenu('Categories');
  }

  /**
   * Navigate to users via sidebar
   */
  async navigateToUsers(): Promise<void> {
    await this.navigateToMenu('Users');
  }

  /**
   * Navigate to roles via sidebar
   */
  async navigateToRoles(): Promise<void> {
    await this.navigateToMenu('Roles');
  }

  /**
   * Navigate to settings via sidebar
   */
  async navigateToSettings(): Promise<void> {
    await this.navigateToMenu('Settings');
  }
}

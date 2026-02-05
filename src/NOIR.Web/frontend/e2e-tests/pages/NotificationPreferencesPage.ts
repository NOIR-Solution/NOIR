import { Page, Locator, expect } from '@playwright/test';
import { BasePage, Timeouts } from './BasePage';

/**
 * Notification category types matching the backend
 */
export type NotificationCategory = 'system' | 'userAction' | 'workflow' | 'security' | 'integration';

/**
 * Email frequency options
 */
export type EmailFrequency = 'none' | 'immediate' | 'daily' | 'weekly';

/**
 * NotificationPreferencesPage - Page Object for Notification Preferences
 *
 * Based on: src/pages/portal/NotificationPreferences.tsx
 * - Manage notification preferences per category
 * - Each category has in-app toggle and email frequency options
 * - Categories: System, User Actions, Workflow, Security, Integration
 */
export class NotificationPreferencesPage extends BasePage {
  // Page header
  readonly pageHeader: Locator;
  readonly pageDescription: Locator;
  readonly backButton: Locator;
  readonly saveButton: Locator;

  // Category cards
  readonly categoryCards: Locator;
  readonly systemCard: Locator;
  readonly userActionCard: Locator;
  readonly workflowCard: Locator;
  readonly securityCard: Locator;
  readonly integrationCard: Locator;

  // Info text
  readonly infoText: Locator;

  // Loading state
  readonly skeleton: Locator;

  constructor(page: Page) {
    super(page);

    // Page header
    this.pageHeader = page.locator('h1:has-text("Notification Preferences")');
    this.pageDescription = page.locator('p.text-muted-foreground').filter({ hasText: 'Choose how' }).first();
    this.backButton = page.locator('a[href="/portal/notifications"], button:has-text("Back")').first();
    this.saveButton = page.locator('button:has-text("Save Changes"), button:has-text("Save")').first();

    // Category cards - identified by their title
    this.categoryCards = page.locator('.shadow-sm.hover\\:shadow-lg, [data-testid="category-card"]');
    this.systemCard = page.locator('div.rounded-lg').filter({ hasText: 'System' }).first();
    this.userActionCard = page.locator('div.rounded-lg').filter({ hasText: 'User Actions' }).first();
    this.workflowCard = page.locator('div.rounded-lg').filter({ hasText: 'Workflow' }).first();
    this.securityCard = page.locator('div.rounded-lg').filter({ hasText: 'Security' }).first();
    this.integrationCard = page.locator('div.rounded-lg').filter({ hasText: 'Integration' }).first();

    // Info text
    this.infoText = page.locator('p.text-sm.text-muted-foreground').filter({ hasText: 'Security notifications' });

    // Loading state
    this.skeleton = page.locator('.animate-pulse').first();
  }

  /**
   * Navigate to notification preferences page
   */
  async navigate(): Promise<void> {
    await this.goto('/portal/settings/notifications');
    await this.page.waitForLoadState('domcontentloaded');
  }

  /**
   * Verify page loaded using sequential wait pattern
   */
  async expectPageLoaded(): Promise<void> {
    // Wait for skeleton to disappear first
    await this.skeleton.waitFor({ state: 'hidden', timeout: Timeouts.API_RESPONSE }).catch(() => {});
    await expect(this.pageHeader).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await expect(this.saveButton).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Get the card locator for a specific category
   */
  private getCategoryCard(category: NotificationCategory): Locator {
    switch (category) {
      case 'system':
        return this.systemCard;
      case 'userAction':
        return this.userActionCard;
      case 'workflow':
        return this.workflowCard;
      case 'security':
        return this.securityCard;
      case 'integration':
        return this.integrationCard;
    }
  }

  /**
   * Get the in-app toggle for a category
   */
  private getInAppToggle(category: NotificationCategory): Locator {
    return this.page.locator(`button#inapp-${category}[role="switch"]`);
  }

  /**
   * Get email frequency button for a category
   */
  private getEmailFrequencyButton(category: NotificationCategory, frequency: EmailFrequency): Locator {
    const card = this.getCategoryCard(category);
    const frequencyLabels: Record<EmailFrequency, string> = {
      none: 'Never',
      immediate: 'Immediate',
      daily: 'Daily digest',
      weekly: 'Weekly digest',
    };
    return card.locator(`button:has-text("${frequencyLabels[frequency]}")`);
  }

  /**
   * Toggle in-app notifications for a category
   */
  async toggleInApp(category: NotificationCategory): Promise<void> {
    const toggle = this.getInAppToggle(category);
    await toggle.click();
  }

  /**
   * Enable in-app notifications for a category
   */
  async enableInApp(category: NotificationCategory): Promise<void> {
    const toggle = this.getInAppToggle(category);
    const isEnabled = await toggle.getAttribute('aria-checked') === 'true';
    if (!isEnabled) {
      await toggle.click();
    }
  }

  /**
   * Disable in-app notifications for a category
   */
  async disableInApp(category: NotificationCategory): Promise<void> {
    const toggle = this.getInAppToggle(category);
    const isEnabled = await toggle.getAttribute('aria-checked') === 'true';
    if (isEnabled) {
      await toggle.click();
    }
  }

  /**
   * Check if in-app notifications are enabled for a category
   */
  async isInAppEnabled(category: NotificationCategory): Promise<boolean> {
    const toggle = this.getInAppToggle(category);
    return await toggle.getAttribute('aria-checked') === 'true';
  }

  /**
   * Set email frequency for a category
   */
  async setEmailFrequency(category: NotificationCategory, frequency: EmailFrequency): Promise<void> {
    const button = this.getEmailFrequencyButton(category, frequency);
    await button.click();
  }

  /**
   * Get current email frequency for a category
   */
  async getEmailFrequency(category: NotificationCategory): Promise<EmailFrequency> {
    const card = this.getCategoryCard(category);
    const frequencyMap: Record<string, EmailFrequency> = {
      'Never': 'none',
      'Immediate': 'immediate',
      'Daily digest': 'daily',
      'Weekly digest': 'weekly',
    };

    for (const [label, freq] of Object.entries(frequencyMap)) {
      const button = card.locator(`button:has-text("${label}")`);
      const classes = await button.getAttribute('class') || '';
      // Active button has primary background
      if (classes.includes('bg-primary')) {
        return freq;
      }
    }
    return 'none';
  }

  /**
   * Save preferences
   */
  async save(): Promise<void> {
    await this.saveButton.click();
    await this.expectSuccessToast();
  }

  /**
   * Check if save button is enabled (has unsaved changes)
   */
  async hasUnsavedChanges(): Promise<boolean> {
    const isDisabled = await this.saveButton.isDisabled();
    return !isDisabled;
  }

  /**
   * Go back to notifications page
   */
  async goBack(): Promise<void> {
    await this.backButton.click();
    await this.page.waitForURL('**/notifications');
  }

  /**
   * Get all preferences as an object
   */
  async getAllPreferences(): Promise<Record<NotificationCategory, { inAppEnabled: boolean; emailFrequency: EmailFrequency }>> {
    const categories: NotificationCategory[] = ['system', 'userAction', 'workflow', 'security', 'integration'];
    const preferences: Record<NotificationCategory, { inAppEnabled: boolean; emailFrequency: EmailFrequency }> = {} as any;

    for (const category of categories) {
      preferences[category] = {
        inAppEnabled: await this.isInAppEnabled(category),
        emailFrequency: await this.getEmailFrequency(category),
      };
    }

    return preferences;
  }

  /**
   * Set preferences for a category
   */
  async setPreferences(
    category: NotificationCategory,
    options: { inAppEnabled?: boolean; emailFrequency?: EmailFrequency }
  ): Promise<void> {
    if (options.inAppEnabled !== undefined) {
      const currentEnabled = await this.isInAppEnabled(category);
      if (currentEnabled !== options.inAppEnabled) {
        await this.toggleInApp(category);
      }
    }

    if (options.emailFrequency) {
      await this.setEmailFrequency(category, options.emailFrequency);
    }
  }

  /**
   * Get count of category cards
   */
  async getCategoryCount(): Promise<number> {
    return await this.categoryCards.count();
  }

  /**
   * Verify category card is visible
   */
  async expectCategoryVisible(category: NotificationCategory): Promise<void> {
    const card = this.getCategoryCard(category);
    await expect(card).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify all category cards are visible
   */
  async expectAllCategoriesVisible(): Promise<void> {
    const categories: NotificationCategory[] = ['system', 'userAction', 'workflow', 'security', 'integration'];
    for (const category of categories) {
      await this.expectCategoryVisible(category);
    }
  }

  /**
   * Verify info text is visible
   */
  async expectInfoTextVisible(): Promise<void> {
    await expect(this.infoText).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Check if page is in loading state
   */
  async isLoading(): Promise<boolean> {
    return await this.skeleton.isVisible();
  }

  /**
   * Wait for page to finish loading
   */
  async waitForLoaded(): Promise<void> {
    await this.skeleton.waitFor({ state: 'hidden', timeout: Timeouts.API_RESPONSE }).catch(() => {});
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }
}

import { Page, Locator, expect } from '@playwright/test';
import { BasePage, Timeouts } from './BasePage';

/**
 * CommandPalettePage - Page Object for the Command Palette (Cmd+K / Ctrl+K)
 *
 * The command palette provides:
 * - Quick navigation to any page
 * - Quick actions (theme toggle, create new product/post)
 * - Keyboard navigation (arrow keys, Enter, Escape)
 */
export class CommandPalettePage extends BasePage {
  readonly dialog: Locator;
  readonly searchInput: Locator;
  readonly navigationGroup: Locator;
  readonly actionsGroup: Locator;
  readonly noResults: Locator;
  readonly backdrop: Locator;
  readonly footer: Locator;
  readonly escHint: Locator;

  constructor(page: Page) {
    super(page);

    // The command palette is rendered as a fixed overlay with cmdk Command component
    this.dialog = page.locator('[cmdk-root]');
    this.searchInput = page.locator('[cmdk-input]');
    this.navigationGroup = page.locator('[cmdk-group]:has([cmdk-group-heading]:has-text("Navigation"))');
    this.actionsGroup = page.locator('[cmdk-group]:has([cmdk-group-heading]:has-text("Quick Actions"))');
    this.noResults = page.locator('[cmdk-empty]');
    this.backdrop = page.locator('.fixed.inset-0 > .absolute.inset-0');
    this.footer = page.locator('[cmdk-root] .border-t');
    this.escHint = page.locator('kbd:has-text("Esc")');
  }

  /**
   * Open command palette via keyboard shortcut
   */
  async open(): Promise<void> {
    const isMac = process.platform === 'darwin';
    const modifier = isMac ? 'Meta' : 'Control';
    await this.page.keyboard.press(`${modifier}+k`);
    await expect(this.dialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
  }

  /**
   * Close command palette via Escape
   */
  async closeViaEscape(): Promise<void> {
    await this.page.keyboard.press('Escape');
    await expect(this.dialog).not.toBeVisible({ timeout: Timeouts.DIALOG_CLOSE });
  }

  /**
   * Close command palette by clicking backdrop
   */
  async closeViaBackdrop(): Promise<void> {
    await this.backdrop.click({ position: { x: 10, y: 10 } });
    await expect(this.dialog).not.toBeVisible({ timeout: Timeouts.DIALOG_CLOSE });
  }

  /**
   * Search in the command palette
   */
  async search(query: string): Promise<void> {
    await this.searchInput.fill(query);
    // Wait for cmdk filtering
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }

  /**
   * Clear search input
   */
  async clearSearch(): Promise<void> {
    await this.searchInput.clear();
    await this.page.waitForTimeout(Timeouts.STABILITY_WAIT);
  }

  /**
   * Get all visible navigation items
   */
  getNavigationItems(): Locator {
    return this.navigationGroup.locator('[cmdk-item]');
  }

  /**
   * Get all visible quick action items
   */
  getActionItems(): Locator {
    return this.actionsGroup.locator('[cmdk-item]');
  }

  /**
   * Get a specific navigation item by text
   */
  getNavigationItem(text: string): Locator {
    return this.navigationGroup.locator(`[cmdk-item]:has-text("${text}")`);
  }

  /**
   * Get a specific action item by text
   */
  getActionItem(text: string): Locator {
    return this.actionsGroup.locator(`[cmdk-item]:has-text("${text}")`);
  }

  /**
   * Select a navigation item by text (click)
   */
  async selectNavigationItem(text: string): Promise<void> {
    const item = this.getNavigationItem(text);
    await expect(item).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await item.click();
  }

  /**
   * Select an action item by text (click)
   */
  async selectActionItem(text: string): Promise<void> {
    const item = this.getActionItem(text);
    await expect(item).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    await item.click();
  }

  /**
   * Navigate using keyboard (arrow down to item, then Enter)
   */
  async selectWithKeyboard(arrowDownCount: number): Promise<void> {
    for (let i = 0; i < arrowDownCount; i++) {
      await this.page.keyboard.press('ArrowDown');
    }
    await this.page.keyboard.press('Enter');
  }

  /**
   * Check if "Current" badge is shown for a navigation item
   */
  async expectCurrentBadge(itemText: string): Promise<void> {
    const item = this.getNavigationItem(itemText);
    await expect(item.locator('text=Current')).toBeVisible({ timeout: Timeouts.QUICK_CHECK });
  }

  /**
   * Verify the command palette is open with expected structure
   */
  async expectPaletteOpen(): Promise<void> {
    await expect(this.dialog).toBeVisible({ timeout: Timeouts.DIALOG_OPEN });
    await expect(this.searchInput).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }

  /**
   * Verify the command palette is closed
   */
  async expectPaletteClosed(): Promise<void> {
    await expect(this.dialog).not.toBeVisible({ timeout: Timeouts.DIALOG_CLOSE });
  }

  /**
   * Verify no results message is shown
   */
  async expectNoResults(): Promise<void> {
    await expect(this.noResults).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
  }
}

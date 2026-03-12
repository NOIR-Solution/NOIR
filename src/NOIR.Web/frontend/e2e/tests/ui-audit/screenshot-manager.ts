import * as fs from 'fs/promises';
import * as path from 'path';
import type { Page } from '@playwright/test';

// .ui-audit/ lives at project root (next to src/)
// __dirname = e2e/tests/ui-audit → 6 levels up to project root (NOIR/)
const PROJECT_ROOT = path.resolve(__dirname, '..', '..', '..', '..', '..', '..');
const OUTPUT_DIR = path.join(PROJECT_ROOT, '.ui-audit', 'screenshots');

export class ScreenshotManager {
  private async ensureDir(subdir: string): Promise<string> {
    const dir = path.join(OUTPUT_DIR, subdir);
    await fs.mkdir(dir, { recursive: true });
    return dir;
  }

  private async waitForToastDismiss(page: Page): Promise<void> {
    try {
      await page.locator('[data-sonner-toast]').waitFor({ state: 'hidden', timeout: 5_000 });
    } catch {
      // No toast or already dismissed
    }
  }

  async takePage(page: Page, pageId: string): Promise<string> {
    await this.waitForToastDismiss(page);
    const dir = await this.ensureDir('pages');
    const filePath = path.join(dir, `${pageId}.png`);
    await page.screenshot({ path: filePath, fullPage: true });
    return `screenshots/pages/${pageId}.png`;
  }

  async takeTab(page: Page, pageId: string, tabId: string): Promise<string> {
    await this.waitForToastDismiss(page);
    const dir = await this.ensureDir('tabs');
    const filename = `${pageId}--${tabId}.png`;
    const filePath = path.join(dir, filename);
    await page.screenshot({ path: filePath, fullPage: true });
    return `screenshots/tabs/${filename}`;
  }

  async takeDialog(page: Page, pageId: string, dialogId: string): Promise<string> {
    const dir = await this.ensureDir('dialogs');
    const filename = `${pageId}--${dialogId}.png`;
    const filePath = path.join(dir, filename);
    await page.screenshot({ path: filePath });
    return `screenshots/dialogs/${filename}`;
  }
}

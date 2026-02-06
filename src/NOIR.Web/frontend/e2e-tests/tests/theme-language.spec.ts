import { test, expect } from '@playwright/test';
import { DashboardPage, Timeouts } from '../pages';

/**
 * Theme and Language Switching E2E Tests
 *
 * Theme and Language are inside the UserProfileDropdown submenu in the sidebar.
 * Flow: Click user profile button → submenu appears → select option
 */
test.describe('Theme and Language @theme-language', () => {
  let dashboard: DashboardPage;

  test.beforeEach(async ({ page }) => {
    dashboard = new DashboardPage(page);
    await dashboard.navigate();
    await dashboard.expectDashboardLoaded();
  });

  test.describe('Theme Toggle @P1', () => {
    test.afterEach(async ({ page }) => {
      // Reset to light theme after each test
      try {
        await dashboard.openUserProfileMenu();
        const themeSubmenu = page.locator('[role="menuitem"]:has-text("Theme"), [role="menuitem"]:has-text("Chủ đề")');
        await themeSubmenu.first().hover();
        await page.waitForTimeout(Timeouts.STABILITY_WAIT);
        const lightOption = page.locator('[role="menuitem"]:has-text("Light"), [role="menuitem"]:has-text("Sáng")');
        await lightOption.first().click();
        await page.waitForTimeout(Timeouts.SEARCH_WAIT);
      } catch {
        // Best-effort cleanup - don't fail test on cleanup errors
      }
    });

    test('TL-001: Theme submenu is accessible from user profile', async ({ page }) => {
      await dashboard.openUserProfileMenu();

      // Theme submenu trigger should be visible
      const themeSubmenu = page.locator('[role="menuitem"]:has-text("Theme"), [role="menuitem"]:has-text("Chủ đề")');
      await expect(themeSubmenu.first()).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    });

    test('TL-002: Switch to dark mode', async ({ page }) => {
      await dashboard.openUserProfileMenu();

      // Open theme submenu
      const themeSubmenu = page.locator('[role="menuitem"]:has-text("Theme"), [role="menuitem"]:has-text("Chủ đề")');
      await themeSubmenu.first().hover();
      await page.waitForTimeout(Timeouts.STABILITY_WAIT);

      // Select dark mode from submenu
      const darkOption = page.locator('[role="menuitem"]:has-text("Dark"), [role="menuitem"]:has-text("Tối")');
      await expect(darkOption.first()).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
      await darkOption.first().click();

      // Wait for theme to apply
      await page.waitForTimeout(Timeouts.SEARCH_WAIT);

      // Verify dark class is on html element
      const htmlClass = await page.locator('html').getAttribute('class');
      expect(htmlClass).toContain('dark');
    });

    test('TL-003: Switch to light mode', async ({ page }) => {
      // First switch to dark
      await dashboard.openUserProfileMenu();
      const themeSubmenu = page.locator('[role="menuitem"]:has-text("Theme"), [role="menuitem"]:has-text("Chủ đề")');
      await themeSubmenu.first().hover();
      await page.waitForTimeout(Timeouts.STABILITY_WAIT);
      const darkOption = page.locator('[role="menuitem"]:has-text("Dark"), [role="menuitem"]:has-text("Tối")');
      await darkOption.first().click();
      await page.waitForTimeout(Timeouts.SEARCH_WAIT);

      // Then switch to light
      await dashboard.openUserProfileMenu();
      await themeSubmenu.first().hover();
      await page.waitForTimeout(Timeouts.STABILITY_WAIT);
      const lightOption = page.locator('[role="menuitem"]:has-text("Light"), [role="menuitem"]:has-text("Sáng")');
      await expect(lightOption.first()).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
      await lightOption.first().click();
      await page.waitForTimeout(Timeouts.SEARCH_WAIT);

      // Verify dark class is removed
      const htmlClass = await page.locator('html').getAttribute('class');
      expect(htmlClass).not.toContain('dark');
    });

    test('TL-004: Theme persists after page reload', async ({ page }) => {
      // Switch to dark mode
      await dashboard.openUserProfileMenu();
      const themeSubmenu = page.locator('[role="menuitem"]:has-text("Theme"), [role="menuitem"]:has-text("Chủ đề")');
      await themeSubmenu.first().hover();
      await page.waitForTimeout(Timeouts.STABILITY_WAIT);
      const darkOption = page.locator('[role="menuitem"]:has-text("Dark"), [role="menuitem"]:has-text("Tối")');
      await darkOption.first().click();
      await page.waitForTimeout(Timeouts.SEARCH_WAIT);

      // Reload page
      await page.reload();
      await dashboard.expectDashboardLoaded();

      // Verify dark mode persists
      const htmlClass = await page.locator('html').getAttribute('class');
      expect(htmlClass).toContain('dark');
    });
  });

  test.describe('Language Switching @P1', () => {
    test.afterEach(async ({ page }) => {
      // Reset to English after each test
      try {
        await dashboard.openUserProfileMenu();
        const langSubmenu = page.locator('[role="menuitem"]:has-text("Language"), [role="menuitem"]:has-text("Ngôn ngữ")');
        await langSubmenu.first().hover();
        await page.waitForTimeout(Timeouts.STABILITY_WAIT);
        const enOption = page.locator('[role="menuitem"]:has-text("English")');
        if (await enOption.isVisible({ timeout: Timeouts.QUICK_CHECK })) {
          await enOption.click();
          await page.waitForTimeout(Timeouts.SEARCH_WAIT);
        }
      } catch {
        // Best-effort cleanup - don't fail test on cleanup errors
      }
    });

    test('TL-010: Language submenu is accessible from user profile', async ({ page }) => {
      await dashboard.openUserProfileMenu();

      // Language submenu trigger should be visible
      const langSubmenu = page.locator('[role="menuitem"]:has-text("Language"), [role="menuitem"]:has-text("Ngôn ngữ")');
      await expect(langSubmenu.first()).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    });

    test('TL-011: Switch to Vietnamese', async ({ page }) => {
      await dashboard.openUserProfileMenu();

      // Open language submenu
      const langSubmenu = page.locator('[role="menuitem"]:has-text("Language"), [role="menuitem"]:has-text("Ngôn ngữ")');
      await langSubmenu.first().hover();
      await page.waitForTimeout(Timeouts.STABILITY_WAIT);

      // Select Vietnamese
      const viOption = page.locator('[role="menuitem"]:has-text("Tiếng Việt")');
      await expect(viOption).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
      await viOption.click();
      await page.waitForTimeout(Timeouts.SEARCH_WAIT);

      // Verify language changed in localStorage
      const storedLang = await page.evaluate(() => localStorage.getItem('noir-language'));
      expect(storedLang).toBe('vi');
    });

    test('TL-012: Switch back to English from Vietnamese', async ({ page }) => {
      // Switch to Vietnamese first
      await dashboard.openUserProfileMenu();
      const langSubmenu = page.locator('[role="menuitem"]:has-text("Language"), [role="menuitem"]:has-text("Ngôn ngữ")');
      await langSubmenu.first().hover();
      await page.waitForTimeout(Timeouts.STABILITY_WAIT);
      const viOption = page.locator('[role="menuitem"]:has-text("Tiếng Việt")');
      await viOption.click();
      await page.waitForTimeout(Timeouts.SEARCH_WAIT);

      // Now switch back to English
      await dashboard.openUserProfileMenu();
      const langSubmenuVi = page.locator('[role="menuitem"]:has-text("Ngôn ngữ"), [role="menuitem"]:has-text("Language")');
      await langSubmenuVi.first().hover();
      await page.waitForTimeout(Timeouts.STABILITY_WAIT);
      const enOption = page.locator('[role="menuitem"]:has-text("English")');
      await expect(enOption).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
      await enOption.click();
      await page.waitForTimeout(Timeouts.SEARCH_WAIT);

      const storedLang = await page.evaluate(() => localStorage.getItem('noir-language'));
      expect(storedLang).toBe('en');
    });

    test('TL-013: Language persists after page reload', async ({ page }) => {
      // Switch to Vietnamese
      await dashboard.openUserProfileMenu();
      const langSubmenu = page.locator('[role="menuitem"]:has-text("Language"), [role="menuitem"]:has-text("Ngôn ngữ")');
      await langSubmenu.first().hover();
      await page.waitForTimeout(Timeouts.STABILITY_WAIT);
      const viOption = page.locator('[role="menuitem"]:has-text("Tiếng Việt")');
      await viOption.click();
      await page.waitForTimeout(Timeouts.SEARCH_WAIT);

      // Reload page
      await page.reload();
      await dashboard.expectDashboardLoaded();

      // Verify Vietnamese persists
      const storedLang = await page.evaluate(() => localStorage.getItem('noir-language'));
      expect(storedLang).toBe('vi');
    });

    test('TL-014: Selected language shows check mark', async ({ page }) => {
      await dashboard.openUserProfileMenu();

      // Open language submenu
      const langSubmenu = page.locator('[role="menuitem"]:has-text("Language"), [role="menuitem"]:has-text("Ngôn ngữ")');
      await langSubmenu.first().hover();
      await page.waitForTimeout(Timeouts.STABILITY_WAIT);

      // English should have a check mark (svg) since it's the default
      const englishItem = page.locator('[role="menuitem"]:has-text("English")');
      await expect(englishItem).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
      const checkIcon = englishItem.locator('svg');
      await expect(checkIcon).toBeVisible({ timeout: Timeouts.QUICK_CHECK });

      // Close menu
      await page.keyboard.press('Escape');
    });
  });
});

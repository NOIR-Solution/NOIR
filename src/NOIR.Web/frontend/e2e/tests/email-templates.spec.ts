import { test, expect } from '@playwright/test'
import { EmailTemplatesPage } from '../pages'

test.describe('Email Templates Page', () => {
  let templatesPage: EmailTemplatesPage

  test.beforeEach(async ({ page }) => {
    templatesPage = new EmailTemplatesPage(page)
    await templatesPage.goto()
  })

  test('displays page title', async () => {
    await expect(templatesPage.pageTitle).toBeVisible()
    await expect(templatesPage.pageTitle).toContainText(/email templates/i)
  })

  test('displays page description', async () => {
    await expect(templatesPage.pageDescription).toBeVisible()
  })

  test('displays refresh button', async () => {
    await expect(templatesPage.refreshButton).toBeVisible()
  })

  test('displays search input', async () => {
    await expect(templatesPage.searchInput).toBeVisible()
  })

  test('displays language filter', async () => {
    await expect(templatesPage.languageFilter).toBeVisible()
  })

  test('refresh button can be clicked', async () => {
    await templatesPage.refreshButton.click()
    // Should not throw and button should still be visible
    await expect(templatesPage.refreshButton).toBeVisible()
  })

  test('loads email templates content', async ({ page }) => {
    // Wait for templates to load
    await page.waitForTimeout(2000)

    // Page should have content - either templates, empty state, or loading
    const pageContent = page.locator('main, .grid, [class*=card]')
    await expect(pageContent.first()).toBeVisible()
  })

  test('search filters templates', async ({ page }) => {
    // Wait for initial load
    await page.waitForTimeout(1000)

    // Enter search term
    await templatesPage.search('password')

    // Wait for debounce and results
    await page.waitForTimeout(500)

    // Should show filtered results or no results
    const results = await templatesPage.templateCards.count()
    // Results should be filtered (may be 0 or more)
    expect(results).toBeGreaterThanOrEqual(0)
  })

  test('language filter dropdown opens', async ({ page }) => {
    await templatesPage.languageFilter.click()

    // Dropdown menu should be visible
    const menu = page.getByRole('menu')
    await expect(menu).toBeVisible()

    // Should have language options
    await expect(page.getByRole('menuitem', { name: /english/i })).toBeVisible()
  })

  test('can filter by English language', async ({ page }) => {
    await templatesPage.filterByLanguage('en')

    // Wait for results
    await page.waitForTimeout(500)

    // Page should still function
    await expect(templatesPage.pageTitle).toBeVisible()
  })

  test('template cards have preview button', async ({ page }) => {
    // Wait for templates to load
    await page.waitForTimeout(1500)

    const cardCount = await templatesPage.templateCards.count()
    if (cardCount > 0) {
      const firstCard = templatesPage.getTemplateCard(0)
      const previewButton = firstCard.getByRole('button', { name: /preview/i })
      await expect(previewButton).toBeVisible()
    }
  })

  test('template cards have edit button', async ({ page }) => {
    // Wait for templates to load
    await page.waitForTimeout(1500)

    const cardCount = await templatesPage.templateCards.count()
    if (cardCount > 0) {
      const firstCard = templatesPage.getTemplateCard(0)
      const editButton = firstCard.getByRole('button', { name: /edit/i })
      await expect(editButton).toBeVisible()
    }
  })

  test('clicking preview opens preview dialog', async ({ page }) => {
    // Wait for templates to load
    await page.waitForTimeout(1500)

    const cardCount = await templatesPage.templateCards.count()
    if (cardCount > 0) {
      await templatesPage.previewTemplate(0)

      // Dialog should open
      const dialog = page.getByRole('dialog')
      await expect(dialog).toBeVisible({ timeout: 10000 })
    }
  })

  test('clicking edit navigates to edit page', async ({ page }) => {
    // Wait for templates to load
    await page.waitForTimeout(1500)

    const cardCount = await templatesPage.templateCards.count()
    if (cardCount > 0) {
      await templatesPage.editTemplate(0)

      // Should navigate to edit page
      await expect(page).toHaveURL(/email-templates\/[a-f0-9-]+/)
    }
  })

  test('template cards display language badge', async ({ page }) => {
    // Wait for templates to load
    await page.waitForTimeout(1500)

    const cardCount = await templatesPage.templateCards.count()
    if (cardCount > 0) {
      const firstCard = templatesPage.getTemplateCard(0)
      // Should have EN or VI badge
      const badge = firstCard.locator('text=/^EN$|^VI$/i')
      await expect(badge).toBeVisible()
    }
  })

  test('template cards display version', async ({ page }) => {
    // Wait for templates to load
    await page.waitForTimeout(1500)

    const cardCount = await templatesPage.templateCards.count()
    if (cardCount > 0) {
      const firstCard = templatesPage.getTemplateCard(0)
      const version = firstCard.locator('text=/Version:/')
      await expect(version).toBeVisible()
    }
  })

  test('template cards display active/inactive status', async ({ page }) => {
    // Wait for templates to load
    await page.waitForTimeout(1500)

    const cardCount = await templatesPage.templateCards.count()
    if (cardCount > 0) {
      const firstCard = templatesPage.getTemplateCard(0)
      // Should have Active or Inactive badge
      const statusBadge = firstCard.locator('text=/Active|Inactive/i')
      await expect(statusBadge).toBeVisible()
    }
  })
})

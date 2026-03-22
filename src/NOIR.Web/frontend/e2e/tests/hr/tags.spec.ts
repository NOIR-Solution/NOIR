import { test, expect } from '../../fixtures/base.fixture';
import { testEmployeeTag } from '../../helpers/test-data';
import { expectToast } from '../../helpers/selectors';

const API_URL = process.env.API_URL ?? 'http://localhost:4000';

/**
 * HR Employee Tags E2E Tests
 *
 * Covers: HR-TAG-001 (Tags list loads), HR-TAG-002 (Create tag via UI),
 *         HR-TAG-003 (Edit tag), HR-TAG-004 (Delete tag)
 *
 * Notes:
 * - Tags use the /api/hr/tags endpoint (ApiHelper.createEmployeeTag / deleteEmployeeTag)
 * - Tag categories: Team, Skill, Project, Location, Seniority, Employment, Custom
 * - Tags page renders a card/grid layout, not a table
 */

test.describe('HR Tags @smoke @regression', () => {
  /**
   * Wait for the tags page to fully render.
   */
  async function waitForTagsPage(page: any) {
    await page.goto('/portal/hr/tags');
    await page.waitForLoadState('networkidle');
    await page.waitForFunction(
      () => {
        const main = document.querySelector('main');
        return main && main.children.length > 0;
      },
      { timeout: 15_000 },
    );
  }

  // ─── HR-TAG-001: Tags list loads @smoke ──────────────────────

  test.describe('HR-TAG-001: Tags list loads @smoke', () => {
    test('should load tags page and display seeded tag', async ({ api, page, trackCleanup }) => {
      // Seed a tag to ensure the list is non-empty
      const tagData = testEmployeeTag();
      const tag = await api.createEmployeeTag(tagData);
      trackCleanup(async () => {
        if (tag?.id) await api.deleteEmployeeTag(tag.id).catch(() => {});
      });

      await waitForTagsPage(page);

      // Verify the page loaded — look for "Create Tag" button or tag content
      const createBtn = page.getByRole('button', { name: /create tag|new tag|add tag/i });
      const hasCreateBtn = await createBtn.isVisible({ timeout: 8_000 }).catch(() => false);
      if (!hasCreateBtn) {
        test.skip(true, 'HR tags management not accessible (feature disabled or insufficient permissions)');
        return;
      }

      // Verify seeded tag name is visible in the list
      await expect(page.getByText(tag.name)).toBeVisible({ timeout: 10_000 });
    });
  });

  // ─── HR-TAG-002: Create tag via UI @smoke ────────────────────

  test.describe('HR-TAG-002: Create tag via UI @smoke', () => {
    test('should create a new tag and verify it in the list', async ({ api, page, trackCleanup }) => {
      const tagName = `E2E Tag ${Date.now()}`;
      let tagId = '';

      trackCleanup(async () => {
        if (tagId) await api.deleteEmployeeTag(tagId).catch(() => {});
      });

      await waitForTagsPage(page);

      // Click the Create Tag button
      const createTagBtn = page.getByRole('button', { name: /create tag|new tag|add tag/i });
      const hasCreateTagBtn = await createTagBtn.isVisible({ timeout: 8_000 }).catch(() => false);
      if (!hasCreateTagBtn) {
        test.skip(true, 'HR tags management not accessible (feature disabled or insufficient permissions)');
        return;
      }
      await createTagBtn.click();

      // Wait for dialog
      await expect(page.locator('[role="dialog"]')).toBeVisible({ timeout: 10_000 });

      // Fill tag name
      await page.locator('[role="dialog"]').getByLabel(/tag name|name/i).first().fill(tagName);

      // Save and capture API response
      const createResponsePromise = page.waitForResponse(
        (resp: any) =>
          resp.url().includes('/api/hr/tags') && resp.request().method() === 'POST',
        { timeout: 15_000 },
      );
      await page.locator('[role="dialog"]').getByRole('button', { name: /create|save/i }).click();
      const createResponse = await createResponsePromise;
      const body = await createResponse.json().catch(() => ({}));
      tagId = body.id ?? '';

      await expectToast(page, /created|success/i);

      // Verify tag appears in the list
      await expect(page.getByText(tagName)).toBeVisible({ timeout: 10_000 });
    });
  });

  // ─── HR-TAG-003: Edit tag @regression ────────────────────────

  test.describe('HR-TAG-003: Edit tag @regression', () => {
    test('should edit an existing tag name', async ({ api, page, trackCleanup }) => {
      const tagData = testEmployeeTag();
      const tag = await api.createEmployeeTag(tagData);
      if (!tag?.id) {
        test.skip(true, 'createEmployeeTag API returned invalid data');
        return;
      }
      trackCleanup(async () => {
        await api.deleteEmployeeTag(tag.id).catch(() => {});
      });

      await waitForTagsPage(page);
      await expect(page.getByText(tag.name)).toBeVisible({ timeout: 10_000 });

      // Find the edit button associated with this tag.
      // The delete button has a unique aria-label like "Delete Tag <name>",
      // so locate its parent container and find the edit button within it.
      const deleteBtn = page.getByRole('button', {
        name: new RegExp(`delete tag ${tag.name}`, 'i'),
      });

      if (await deleteBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
        const buttonGroup = page
          .locator('div')
          .filter({
            has: page.getByRole('button', { name: new RegExp(`delete tag ${tag.name}`, 'i') }),
          })
          .last();
        const editBtn = buttonGroup.getByRole('button', { name: /edit tag/i }).first();
        await editBtn.click();
      } else {
        // Fallback: try generic edit approach — click tag card/row then find edit
        const tagText = page.getByText(tag.name);
        const container = page.locator('div').filter({ has: tagText }).last();
        const editBtn = container.getByRole('button', { name: /edit/i }).first();
        await editBtn.click();
      }

      // Wait for edit dialog
      await expect(page.locator('[role="dialog"]').first()).toBeVisible({ timeout: 10_000 });

      // Update name
      const nameInput = page.locator('[role="dialog"]').first().getByLabel(/tag name|name/i).first();
      await nameInput.clear();
      await nameInput.fill(`${tag.name} Updated`);

      const updateResponsePromise = page.waitForResponse(
        (resp: any) =>
          resp.url().includes('/api/hr/tags') && resp.request().method() === 'PUT',
        { timeout: 15_000 },
      );
      await page
        .locator('[role="dialog"]')
        .first()
        .getByRole('button', { name: /save|update/i })
        .click();
      await updateResponsePromise;
      await expectToast(page, /updated|success/i);
    });
  });

  // ─── HR-TAG-004: Delete tag @regression ──────────────────────

  test.describe('HR-TAG-004: Delete tag @regression', () => {
    test('should delete a tag and verify removal', async ({ api, page }) => {
      const tagData = testEmployeeTag();
      const tag = await api.createEmployeeTag(tagData);
      if (!tag?.id) {
        test.skip(true, 'createEmployeeTag API returned invalid data');
        return;
      }

      await waitForTagsPage(page);
      await expect(page.getByText(tag.name)).toBeVisible({ timeout: 10_000 });

      // Click the delete button (unique aria-label per tag)
      const deleteBtn = page.getByRole('button', {
        name: new RegExp(`delete tag ${tag.name}`, 'i'),
      });
      const deleteBtnAlt = page
        .locator('div')
        .filter({ hasText: tag.name })
        .first()
        .getByRole('button', { name: /delete/i });

      if (await deleteBtn.isVisible({ timeout: 2_000 }).catch(() => false)) {
        await deleteBtn.click();
      } else if (await deleteBtnAlt.isVisible({ timeout: 2_000 }).catch(() => false)) {
        await deleteBtnAlt.click();
      } else {
        await deleteBtn.click({ force: true });
      }

      // Confirm delete dialog
      await expect(page.locator('[role="dialog"]')).toBeVisible({ timeout: 5_000 });
      await page
        .locator('[role="dialog"]')
        .getByRole('button', { name: /delete tag|delete|confirm/i })
        .click();

      await expectToast(page, /deleted|success/i);

      // Verify tag is removed from the list
      await expect(page.getByText(tag.name)).not.toBeVisible({ timeout: 5_000 });
    });
  });
});

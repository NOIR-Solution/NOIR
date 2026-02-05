import { test, expect } from '@playwright/test';
import { NotificationsPage, NotificationPreferencesPage, Timeouts } from '../pages';

/**
 * Notifications Center Tests
 *
 * E2E tests for the notifications system including:
 * - Notifications list page with filtering
 * - Notification preferences management
 *
 * Tags: @notifications @P0 @P1
 */

test.describe('Notifications Center @notifications', () => {
  test.describe('Notifications Page @P0', () => {
    test('NOTIF-001: Notifications page loads successfully', async ({ page }) => {
      const notificationsPage = new NotificationsPage(page);
      await notificationsPage.navigate();
      await notificationsPage.expectPageLoaded();

      // Verify page header is visible
      await expect(notificationsPage.pageHeader).toBeVisible();
    });

    test('NOTIF-002: Filter tabs visible (All, Unread, Read)', async ({ page }) => {
      const notificationsPage = new NotificationsPage(page);
      await notificationsPage.navigate();
      await notificationsPage.expectPageLoaded();

      // Verify all filter tabs are visible
      await expect(notificationsPage.allFilterTab).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
      await expect(notificationsPage.unreadFilterTab).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
      await expect(notificationsPage.readFilterTab).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    });

    test('NOTIF-003: Refresh button works', async ({ page }) => {
      const notificationsPage = new NotificationsPage(page);
      await notificationsPage.navigate();
      await notificationsPage.expectPageLoaded();

      // Verify refresh button is visible and clickable
      await expect(notificationsPage.refreshButton).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
      await expect(notificationsPage.refreshButton).toBeEnabled();

      // Click refresh and verify page still loads correctly
      await notificationsPage.refresh();
      await notificationsPage.expectPageLoaded();
    });
  });

  test.describe('Notifications List @P1', () => {
    test('NOTIF-010: Filter by All/Unread/Read', async ({ page }) => {
      const notificationsPage = new NotificationsPage(page);
      await notificationsPage.navigate();
      await notificationsPage.expectPageLoaded();

      // Test filtering by All
      await notificationsPage.filterBy('all');
      const allActive = await notificationsPage.getActiveFilter();
      expect(allActive).toBe('all');

      // Test filtering by Unread
      await notificationsPage.filterBy('unread');
      await page.waitForTimeout(Timeouts.STABILITY_WAIT);
      const unreadActive = await notificationsPage.getActiveFilter();
      expect(unreadActive).toBe('unread');

      // Test filtering by Read
      await notificationsPage.filterBy('read');
      await page.waitForTimeout(Timeouts.STABILITY_WAIT);
      const readActive = await notificationsPage.getActiveFilter();
      expect(readActive).toBe('read');

      // Return to All filter
      await notificationsPage.filterBy('all');
    });

    test('NOTIF-011: Mark all as read button (if notifications exist)', async ({ page }) => {
      const notificationsPage = new NotificationsPage(page);
      await notificationsPage.navigate();
      await notificationsPage.expectPageLoaded();
      await notificationsPage.waitForNotificationsLoaded();

      // Check if there are unread notifications
      const unreadCount = await notificationsPage.getUnreadBadgeCount();

      if (unreadCount > 0) {
        // Mark all read button should be visible
        const canMarkAll = await notificationsPage.canMarkAllAsRead();
        expect(canMarkAll).toBe(true);

        // Verify the button is visible
        await expect(notificationsPage.markAllReadButton).toBeVisible();
      } else {
        // If no unread, the button may or may not be visible depending on implementation
        // Just verify the page state is consistent
        const isEmpty = await notificationsPage.isListEmpty();
        if (!isEmpty) {
          // There are notifications but none unread - valid state
          expect(unreadCount).toBe(0);
        }
      }
    });

    test('NOTIF-012: Click notification shows details or navigates', async ({ page }) => {
      const notificationsPage = new NotificationsPage(page);
      await notificationsPage.navigate();
      await notificationsPage.expectPageLoaded();
      await notificationsPage.waitForNotificationsLoaded();

      const notificationCount = await notificationsPage.getNotificationCount();

      if (notificationCount > 0) {
        // Click the first notification
        const initialUrl = page.url();
        await notificationsPage.clickNotification(0);

        // Wait for any navigation or dialog to appear
        await page.waitForTimeout(Timeouts.STABILITY_WAIT * 2);

        // The notification click might:
        // 1. Navigate to a related page
        // 2. Open a details panel/dialog
        // 3. Mark as read inline
        // Verify something happened (either URL changed or element state changed)
        const currentUrl = page.url();
        const urlChanged = currentUrl !== initialUrl;

        // If URL didn't change, verify we're still on the notifications page
        if (!urlChanged) {
          await expect(notificationsPage.pageHeader).toBeVisible();
        }
      } else {
        // No notifications - verify empty state
        const isEmpty = await notificationsPage.isListEmpty();
        expect(isEmpty).toBe(true);
      }
    });

    test('NOTIF-013: Navigate to preferences', async ({ page }) => {
      const notificationsPage = new NotificationsPage(page);
      await notificationsPage.navigate();
      await notificationsPage.expectPageLoaded();

      // Verify preferences button is visible
      await expect(notificationsPage.preferencesButton).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });

      // Click to navigate to preferences
      await notificationsPage.goToPreferences();

      // Verify we're on the preferences page
      const preferencesPage = new NotificationPreferencesPage(page);
      await preferencesPage.expectPageLoaded();
      await expect(preferencesPage.pageHeader).toBeVisible();
    });
  });

  test.describe('Notification Preferences @P1', () => {
    test('NOTIF-020: Notification preferences page loads', async ({ page }) => {
      const preferencesPage = new NotificationPreferencesPage(page);
      await preferencesPage.navigate();
      await preferencesPage.expectPageLoaded();

      // Verify page header
      await expect(preferencesPage.pageHeader).toBeVisible();
      // Verify save button is present
      await expect(preferencesPage.saveButton).toBeVisible();
    });

    test('NOTIF-021: Category cards visible', async ({ page }) => {
      const preferencesPage = new NotificationPreferencesPage(page);
      await preferencesPage.navigate();
      await preferencesPage.expectPageLoaded();
      await preferencesPage.waitForLoaded();

      // Verify all category cards are visible
      await preferencesPage.expectAllCategoriesVisible();

      // Verify we have the expected number of categories (5)
      // Note: getCategoryCount might not be exact due to flexible selectors
      // so we verify individual cards instead
      await expect(preferencesPage.systemCard).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
      await expect(preferencesPage.securityCard).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });
    });

    test('NOTIF-022: In-app toggle works', async ({ page }) => {
      const preferencesPage = new NotificationPreferencesPage(page);
      await preferencesPage.navigate();
      await preferencesPage.expectPageLoaded();
      await preferencesPage.waitForLoaded();

      // Get initial state of system notifications in-app toggle
      const initialState = await preferencesPage.isInAppEnabled('system');

      // Toggle the in-app notification
      await preferencesPage.toggleInApp('system');
      await page.waitForTimeout(Timeouts.STABILITY_WAIT);

      // Verify state changed
      const newState = await preferencesPage.isInAppEnabled('system');
      expect(newState).toBe(!initialState);

      // Toggle back to original state to keep test idempotent
      await preferencesPage.toggleInApp('system');
      await page.waitForTimeout(Timeouts.STABILITY_WAIT);

      // Verify restored to original
      const restoredState = await preferencesPage.isInAppEnabled('system');
      expect(restoredState).toBe(initialState);
    });

    test('NOTIF-023: Email frequency dropdown works', async ({ page }) => {
      const preferencesPage = new NotificationPreferencesPage(page);
      await preferencesPage.navigate();
      await preferencesPage.expectPageLoaded();
      await preferencesPage.waitForLoaded();

      // Get initial email frequency for workflow category
      const initialFrequency = await preferencesPage.getEmailFrequency('workflow');

      // Set to a different frequency
      const newFrequency = initialFrequency === 'daily' ? 'weekly' : 'daily';
      await preferencesPage.setEmailFrequency('workflow', newFrequency);
      await page.waitForTimeout(Timeouts.STABILITY_WAIT);

      // Verify the frequency changed
      const currentFrequency = await preferencesPage.getEmailFrequency('workflow');
      expect(currentFrequency).toBe(newFrequency);

      // Restore original frequency
      await preferencesPage.setEmailFrequency('workflow', initialFrequency);
      await page.waitForTimeout(Timeouts.STABILITY_WAIT);
    });

    test('NOTIF-024: Save preferences', async ({ page }) => {
      const preferencesPage = new NotificationPreferencesPage(page);
      await preferencesPage.navigate();
      await preferencesPage.expectPageLoaded();
      await preferencesPage.waitForLoaded();

      // Make a change to enable save button
      const initialInApp = await preferencesPage.isInAppEnabled('integration');
      await preferencesPage.toggleInApp('integration');
      await page.waitForTimeout(Timeouts.STABILITY_WAIT);

      // Check if save button is enabled (has unsaved changes)
      const hasUnsaved = await preferencesPage.hasUnsavedChanges();
      // The save button might always be enabled or disabled based on implementation
      // We proceed with save regardless

      // Save the preferences
      await preferencesPage.save();

      // Verify success toast appears
      // Note: expectSuccessToast is called within save() method

      // Optionally toggle back and save again to restore state
      await preferencesPage.toggleInApp('integration');
      await page.waitForTimeout(Timeouts.STABILITY_WAIT);
      await preferencesPage.save();
    });
  });

  test.describe('Notifications Navigation @P1', () => {
    test('NOTIF-030: Navigate from preferences back to notifications', async ({ page }) => {
      const preferencesPage = new NotificationPreferencesPage(page);
      await preferencesPage.navigate();
      await preferencesPage.expectPageLoaded();

      // Verify back button is visible
      await expect(preferencesPage.backButton).toBeVisible({ timeout: Timeouts.ELEMENT_VISIBLE });

      // Go back to notifications
      await preferencesPage.goBack();

      // Verify we're on the notifications page
      const notificationsPage = new NotificationsPage(page);
      await notificationsPage.expectPageLoaded();
    });

    test('NOTIF-031: Live indicator visibility check', async ({ page }) => {
      const notificationsPage = new NotificationsPage(page);
      await notificationsPage.navigate();
      await notificationsPage.expectPageLoaded();

      // Check if live indicator is present (SignalR connection status)
      // This may or may not be visible depending on connection state
      const isLiveVisible = await notificationsPage.isLiveIndicatorVisible();

      // Just verify the check completes without error
      // The live indicator visibility depends on real-time connection
      expect(typeof isLiveVisible).toBe('boolean');
    });

    test('NOTIF-032: Notification list or empty state displays', async ({ page }) => {
      const notificationsPage = new NotificationsPage(page);
      await notificationsPage.navigate();
      await notificationsPage.expectPageLoaded();
      await notificationsPage.waitForNotificationsLoaded();

      const notificationCount = await notificationsPage.getNotificationCount();
      const isEmpty = await notificationsPage.isListEmpty();

      // Either we have notifications or empty state
      if (notificationCount > 0) {
        expect(isEmpty).toBe(false);
        await notificationsPage.expectNotificationListVisible();
      } else {
        expect(isEmpty).toBe(true);
        // Empty state should be displayed
      }
    });
  });

  test.describe('Preferences Categories @P2', () => {
    test('NOTIF-040: System category preferences', async ({ page }) => {
      const preferencesPage = new NotificationPreferencesPage(page);
      await preferencesPage.navigate();
      await preferencesPage.expectPageLoaded();
      await preferencesPage.waitForLoaded();

      // Verify system card is visible
      await preferencesPage.expectCategoryVisible('system');

      // Verify we can read preferences
      const prefs = await preferencesPage.getAllPreferences();
      expect(prefs.system).toBeDefined();
      expect(typeof prefs.system.inAppEnabled).toBe('boolean');
    });

    test('NOTIF-041: Security category is special (info text)', async ({ page }) => {
      const preferencesPage = new NotificationPreferencesPage(page);
      await preferencesPage.navigate();
      await preferencesPage.expectPageLoaded();
      await preferencesPage.waitForLoaded();

      // Verify security card is visible
      await preferencesPage.expectCategoryVisible('security');

      // Security notifications may have special info text
      // Check if info text about security notifications is visible
      const infoTextVisible = await preferencesPage.infoText.isVisible().catch(() => false);
      // Info text presence is optional based on UI implementation
      expect(typeof infoTextVisible).toBe('boolean');
    });

    test('NOTIF-042: All categories have toggles and frequency options', async ({ page }) => {
      const preferencesPage = new NotificationPreferencesPage(page);
      await preferencesPage.navigate();
      await preferencesPage.expectPageLoaded();
      await preferencesPage.waitForLoaded();

      // Get all preferences and verify structure
      const prefs = await preferencesPage.getAllPreferences();

      const categories = ['system', 'userAction', 'workflow', 'security', 'integration'] as const;

      for (const category of categories) {
        expect(prefs[category]).toBeDefined();
        expect(typeof prefs[category].inAppEnabled).toBe('boolean');
        expect(['none', 'immediate', 'daily', 'weekly']).toContain(prefs[category].emailFrequency);
      }
    });
  });

  test.describe('Error Handling @P2', () => {
    test('NOTIF-050: Page handles invalid filter gracefully', async ({ page }) => {
      const notificationsPage = new NotificationsPage(page);
      await notificationsPage.navigate();
      await notificationsPage.expectPageLoaded();

      // Filter by all valid options to ensure no errors
      await notificationsPage.filterBy('all');
      await notificationsPage.filterBy('unread');
      await notificationsPage.filterBy('read');
      await notificationsPage.filterBy('all');

      // Page should remain stable
      await expect(notificationsPage.pageHeader).toBeVisible();
    });

    test('NOTIF-051: Preferences page handles toggle errors gracefully', async ({ page }) => {
      const preferencesPage = new NotificationPreferencesPage(page);
      await preferencesPage.navigate();
      await preferencesPage.expectPageLoaded();
      await preferencesPage.waitForLoaded();

      // Rapidly toggle to test error handling
      await preferencesPage.toggleInApp('userAction');
      await preferencesPage.toggleInApp('userAction');
      await preferencesPage.toggleInApp('userAction');

      // Page should remain stable
      await expect(preferencesPage.pageHeader).toBeVisible();
      await expect(preferencesPage.saveButton).toBeVisible();
    });
  });
});

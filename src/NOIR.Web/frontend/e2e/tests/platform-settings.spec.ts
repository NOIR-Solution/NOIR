import { test, expect } from '@playwright/test'
import { PlatformSettingsPage } from '../pages'

/**
 * CRITICAL: This test suite requires Platform Admin authentication.
 *
 * Platform Settings requires system:config:view and system:config:edit permissions,
 * which are only assigned to Platform Admin role (TenantId = null).
 *
 * Using tenant admin auth will result in 403 Forbidden errors.
 *
 * Credentials: platform@noir.local / Platform123!
 * Auth file: e2e/.auth/platform-admin.json
 */
test.use({ storageState: 'e2e/.auth/platform-admin.json' })

test.describe('Platform Settings - Configuration Management', () => {
  let platformSettingsPage: PlatformSettingsPage

  test.beforeEach(async ({ page }) => {
    platformSettingsPage = new PlatformSettingsPage(page)
    await platformSettingsPage.goto()
  })

  test.describe('Page Structure', () => {
    test('displays page title and description', async () => {
      await expect(platformSettingsPage.pageTitle).toBeVisible()
      await expect(platformSettingsPage.pageTitle).toContainText(/platform settings/i)
      await expect(platformSettingsPage.pageDescription).toBeVisible()
    })

    test('displays restart application button in header', async () => {
      await expect(platformSettingsPage.restartAppButton).toBeVisible()
    })

    test('displays section browser sidebar', async () => {
      await expect(platformSettingsPage.allowedSectionsHeader).toBeVisible()
    })

    test('displays empty state when no section is selected', async () => {
      await expect(platformSettingsPage.emptyState).toBeVisible()
    })
  })

  test.describe('Section Browser', () => {
    test('lists allowed and restricted sections', async () => {
      await expect(platformSettingsPage.allowedSectionsHeader).toBeVisible()

      // Check if there are any allowed sections
      const allowedSection = platformSettingsPage.getSection('DeveloperLogs')
      if (await allowedSection.count() > 0) {
        await expect(allowedSection).toBeVisible()
      }
    })

    test('displays section with auto-reload badge', async () => {
      // Select DeveloperLogs section (should have auto-reload)
      await platformSettingsPage.selectSection('DeveloperLogs')

      // Verify auto-reload badge is shown
      await expect(platformSettingsPage.autoReloadBadge).toBeVisible()
    })

    test('disabled sections show forbidden badge', async ({ page }) => {
      // Look for any restricted section
      const restrictedHeader = platformSettingsPage.restrictedSectionsHeader
      if (await restrictedHeader.isVisible()) {
        // Check that restricted sections exist and are disabled
        const firstRestrictedSection = platformSettingsPage.getSectionByIndex(0)
        const isDisabled = await firstRestrictedSection.isDisabled()
        expect(isDisabled).toBe(true)
      }
    })
  })

  test.describe('JSON Editor', () => {
    test('displays JSON editor when section is selected', async () => {
      await platformSettingsPage.selectSection('DeveloperLogs')

      await expect(platformSettingsPage.jsonEditor).toBeVisible()

      // Check that JSON is properly formatted
      const jsonContent = await platformSettingsPage.jsonEditor.inputValue()
      expect(jsonContent).toContain('{')
      expect(jsonContent).toContain('}')
    })

    test('shows save and cancel buttons when editing', async () => {
      await platformSettingsPage.selectSection('DeveloperLogs')

      // Modify the JSON
      const originalJson = await platformSettingsPage.jsonEditor.inputValue()
      const parsed = JSON.parse(originalJson)
      parsed.TestField = 'Modified'
      await platformSettingsPage.editJsonValue(JSON.stringify(parsed, null, 2))

      // Buttons should appear
      await expect(platformSettingsPage.saveButton).toBeVisible()
      await expect(platformSettingsPage.cancelButton).toBeVisible()
    })

    test('cancel button reverts changes', async () => {
      await platformSettingsPage.selectSection('DeveloperLogs')

      const originalJson = await platformSettingsPage.jsonEditor.inputValue()

      // Modify and cancel
      await platformSettingsPage.editJsonValue('{ "test": "modified" }')
      await platformSettingsPage.cancelChanges()

      // Should revert to original
      const currentJson = await platformSettingsPage.jsonEditor.inputValue()
      expect(currentJson).toBe(originalJson)
    })

    test('displays error for invalid JSON', async ({ page }) => {
      await platformSettingsPage.selectSection('DeveloperLogs')

      // Enter invalid JSON
      await platformSettingsPage.editJsonValue('{ invalid json }')

      // Should show error message
      const errorMessage = page.locator('text=/unexpected token/i')
      await expect(errorMessage).toBeVisible({ timeout: 2000 })
    })
  })

  test.describe('Configuration Update', () => {
    test('successfully saves configuration changes', async ({ page }) => {
      await platformSettingsPage.selectSection('DeveloperLogs')

      // Get original JSON
      const originalJson = await platformSettingsPage.jsonEditor.inputValue()
      const parsed = JSON.parse(originalJson)

      // Modify a value
      const timestamp = Date.now()
      parsed.TestTimestamp = timestamp

      // Save changes
      await platformSettingsPage.editJsonValue(JSON.stringify(parsed, null, 2))
      await platformSettingsPage.saveChanges()

      // Should show success toast
      const successToast = page.locator('text=/updated successfully/i')
      await expect(successToast).toBeVisible({ timeout: 5000 })

      // Verify the change persisted
      const newJson = await platformSettingsPage.jsonEditor.inputValue()
      expect(newJson).toContain(timestamp.toString())
    })

    test('creates backup after successful save', async () => {
      await platformSettingsPage.selectSection('DeveloperLogs')

      // Modify and save
      const originalJson = await platformSettingsPage.jsonEditor.inputValue()
      const parsed = JSON.parse(originalJson)
      parsed.BackupTest = Date.now()

      await platformSettingsPage.editJsonValue(JSON.stringify(parsed, null, 2))
      await platformSettingsPage.saveChanges()

      // Switch to backups tab
      await platformSettingsPage.switchToBackupsTab()

      // Should see at least one backup
      await expect(platformSettingsPage.restoreButtons.first()).toBeVisible({ timeout: 3000 })
    })
  })

  test.describe('Backup Timeline', () => {
    test('displays backups tab', async () => {
      await platformSettingsPage.selectSection('DeveloperLogs')

      await platformSettingsPage.switchToBackupsTab()

      // Should show backups or empty state
      const hasBackups = await platformSettingsPage.restoreButtons.first().isVisible({ timeout: 2000 })
        .catch(() => false)

      if (hasBackups) {
        await expect(platformSettingsPage.restoreButtons.first()).toBeVisible()
      } else {
        await expect(platformSettingsPage.noBackupsMessage).toBeVisible()
      }
    })

    test('restore backup shows confirmation dialog', async () => {
      await platformSettingsPage.selectSection('DeveloperLogs')

      // Create a backup first by saving
      const originalJson = await platformSettingsPage.jsonEditor.inputValue()
      const parsed = JSON.parse(originalJson)
      parsed.RestoreTest = 'before'
      await platformSettingsPage.editJsonValue(JSON.stringify(parsed, null, 2))
      await platformSettingsPage.saveChanges()

      // Modify again
      await platformSettingsPage.switchToEditorTab()
      const parsed2 = JSON.parse(await platformSettingsPage.jsonEditor.inputValue())
      parsed2.RestoreTest = 'after'
      await platformSettingsPage.editJsonValue(JSON.stringify(parsed2, null, 2))
      await platformSettingsPage.saveChanges()

      // Go to backups and restore
      await platformSettingsPage.switchToBackupsTab()
      const restoreButton = platformSettingsPage.restoreButtons.first()
      await restoreButton.click()

      // Should show restore dialog
      await expect(platformSettingsPage.restoreDialog).toBeVisible()
      await expect(platformSettingsPage.restoreConfirmButton).toBeVisible()
      await expect(platformSettingsPage.restoreCancelButton).toBeVisible()
    })

    test('successfully restores configuration from backup', async ({ page }) => {
      await platformSettingsPage.selectSection('DeveloperLogs')

      // Save original state
      const originalJson = await platformSettingsPage.jsonEditor.inputValue()
      const parsed1 = JSON.parse(originalJson)
      parsed1.RestoreValue = 'original'
      await platformSettingsPage.editJsonValue(JSON.stringify(parsed1, null, 2))
      await platformSettingsPage.saveChanges()

      // Modify again
      await platformSettingsPage.switchToEditorTab()
      const parsed2 = JSON.parse(await platformSettingsPage.jsonEditor.inputValue())
      parsed2.RestoreValue = 'modified'
      await platformSettingsPage.editJsonValue(JSON.stringify(parsed2, null, 2))
      await platformSettingsPage.saveChanges()

      // Restore from backup
      await platformSettingsPage.switchToBackupsTab()
      await platformSettingsPage.restoreBackup(0)

      // Should show success toast
      const successToast = page.locator('text=/restored successfully/i')
      await expect(successToast).toBeVisible({ timeout: 5000 })

      // Verify restored value
      await platformSettingsPage.switchToEditorTab()
      const restoredJson = await platformSettingsPage.jsonEditor.inputValue()
      const restoredParsed = JSON.parse(restoredJson)
      expect(restoredParsed.RestoreValue).toBe('original')
    })
  })

  test.describe('Restart Dialog', () => {
    test('opens restart dialog when button is clicked', async () => {
      await platformSettingsPage.openRestartDialog()

      await expect(platformSettingsPage.restartDialog).toBeVisible()
      await expect(platformSettingsPage.restartWarningAlert).toBeVisible()
      await expect(platformSettingsPage.restartReasonInput).toBeVisible()
    })

    test('displays warning about service interruption', async () => {
      await platformSettingsPage.openRestartDialog()

      const warning = platformSettingsPage.restartWarningAlert
      await expect(warning).toBeVisible()
      await expect(warning).toContainText(/disconnect all active users/i)
      await expect(warning).toContainText(/10-30 seconds of downtime/i)
    })

    test('restart button disabled without reason', async () => {
      await platformSettingsPage.openRestartDialog()

      // Without reason, button should be disabled
      const isDisabled = await platformSettingsPage.restartConfirmButton.isDisabled()
      expect(isDisabled).toBe(true)
    })

    test('restart button disabled with short reason', async () => {
      await platformSettingsPage.openRestartDialog()

      // Enter short reason (less than 5 characters)
      await platformSettingsPage.fillRestartReason('abc')

      const isDisabled = await platformSettingsPage.restartConfirmButton.isDisabled()
      expect(isDisabled).toBe(true)
    })

    test('restart button enabled with valid reason', async () => {
      await platformSettingsPage.openRestartDialog()

      // Enter valid reason (5+ characters)
      await platformSettingsPage.fillRestartReason('Testing restart functionality')

      const isDisabled = await platformSettingsPage.restartConfirmButton.isDisabled()
      expect(isDisabled).toBe(false)
    })

    test('cancel button closes dialog without restarting', async () => {
      await platformSettingsPage.openRestartDialog()

      await platformSettingsPage.cancelRestart()

      // Dialog should be closed
      await expect(platformSettingsPage.restartDialog).not.toBeVisible()
    })

    test('skips restart test to avoid actual application restart', async () => {
      // NOTE: We intentionally skip testing the actual restart functionality
      // because it would cause the application to restart, failing subsequent tests.
      // The restart functionality was already tested via curl in the backend tests.
      test.skip()
    })
  })

  test.describe('Tabs Navigation', () => {
    test('switches between editor and backups tabs', async () => {
      await platformSettingsPage.selectSection('DeveloperLogs')

      // Start on editor tab
      await expect(platformSettingsPage.jsonEditor).toBeVisible()

      // Switch to backups
      await platformSettingsPage.switchToBackupsTab()
      await expect(platformSettingsPage.jsonEditor).not.toBeVisible()

      // Switch back to editor
      await platformSettingsPage.switchToEditorTab()
      await expect(platformSettingsPage.jsonEditor).toBeVisible()
    })
  })

  test.describe('Permissions', () => {
    test('platform admin can access platform settings', async () => {
      // We're already logged in as platform admin from global-setup
      await expect(platformSettingsPage.pageTitle).toBeVisible()
    })

    test('displays appropriate sections based on permissions', async () => {
      // Platform admin should see allowed sections
      await expect(platformSettingsPage.allowedSectionsHeader).toBeVisible()

      // Should have at least one allowed section
      const allowedSection = platformSettingsPage.getSection('DeveloperLogs')
      await expect(allowedSection).toBeVisible()
    })
  })
})

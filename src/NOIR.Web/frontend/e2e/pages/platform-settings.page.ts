import { Page, Locator } from '@playwright/test'

export class PlatformSettingsPage {
  readonly page: Page
  readonly pageTitle: Locator
  readonly pageDescription: Locator
  readonly restartAppButton: Locator

  // Section Browser
  readonly sectionsList: Locator
  readonly allowedSectionsHeader: Locator
  readonly restrictedSectionsHeader: Locator
  readonly emptyState: Locator

  // Editor area
  readonly jsonEditor: Locator
  readonly saveButton: Locator
  readonly cancelButton: Locator
  readonly autoReloadBadge: Locator
  readonly requiresRestartBadge: Locator
  readonly forbiddenBadge: Locator

  // Tabs
  readonly editorTab: Locator
  readonly backupsTab: Locator

  // Backups timeline
  readonly backupsList: Locator
  readonly restoreButtons: Locator
  readonly noBackupsMessage: Locator

  // Restart Dialog
  readonly restartDialog: Locator
  readonly restartReasonInput: Locator
  readonly restartConfirmButton: Locator
  readonly restartCancelButton: Locator
  readonly restartWarningAlert: Locator

  // Restore Dialog
  readonly restoreDialog: Locator
  readonly restoreConfirmButton: Locator
  readonly restoreCancelButton: Locator

  constructor(page: Page) {
    this.page = page
    this.pageTitle = page.getByRole('heading', { level: 1, name: /platform settings/i })
    this.pageDescription = page.locator('p.text-muted-foreground').first()
    this.restartAppButton = page.getByRole('button', { name: /restart application/i })

    // Section Browser
    this.sectionsList = page.locator('.space-y-1')
    this.allowedSectionsHeader = page.locator('text=/allowed sections/i')
    this.restrictedSectionsHeader = page.locator('text=/restricted sections/i')
    this.emptyState = page.locator('text=/select a section/i')

    // Editor area
    this.jsonEditor = page.locator('textarea.font-mono')
    this.saveButton = page.getByRole('button', { name: /save & apply/i })
    this.cancelButton = page.getByRole('button', { name: /^cancel$/i })
    this.autoReloadBadge = page.locator('text=/auto-reload/i')
    this.requiresRestartBadge = page.locator('text=/requires restart/i')
    this.forbiddenBadge = page.locator('text=/forbidden/i')

    // Tabs
    this.editorTab = page.getByRole('tab', { name: /current value/i })
    this.backupsTab = page.getByRole('tab', { name: /backups/i })

    // Backups timeline
    this.backupsList = page.locator('[class*="space-y-4"]').filter({ hasText: /created by/i })
    this.restoreButtons = page.getByRole('button', { name: /^restore$/i })
    this.noBackupsMessage = page.locator('text=/no backups/i')

    // Restart Dialog
    this.restartDialog = page.getByRole('dialog').filter({ hasText: /restart application/i })
    this.restartReasonInput = page.getByLabel(/reason/i)
    this.restartConfirmButton = this.restartDialog.getByRole('button', { name: /restart/i })
    this.restartCancelButton = this.restartDialog.getByRole('button', { name: /cancel/i })
    this.restartWarningAlert = page.getByTestId('restart-warning-alert')

    // Restore Dialog
    this.restoreDialog = page.getByRole('dialog').filter({ hasText: /restore backup/i })
    this.restoreConfirmButton = this.restoreDialog.getByRole('button', { name: /restore/i })
    this.restoreCancelButton = this.restoreDialog.getByRole('button', { name: /cancel/i })
  }

  async goto() {
    await this.page.goto('/portal/admin/platform-settings')
    await this.page.waitForLoadState('networkidle')
  }

  async selectSection(sectionName: string) {
    const sectionButton = this.page.getByRole('button', { name: new RegExp(sectionName, 'i') })
    await sectionButton.click()
    await this.page.waitForLoadState('networkidle')
  }

  async editJsonValue(newValue: string) {
    await this.jsonEditor.clear()
    await this.jsonEditor.fill(newValue)
  }

  async saveChanges() {
    await this.saveButton.click()
    await this.page.waitForLoadState('networkidle')
  }

  async cancelChanges() {
    await this.cancelButton.click()
  }

  async switchToBackupsTab() {
    await this.backupsTab.click()
    await this.page.waitForTimeout(500) // Wait for tab content to render
  }

  async switchToEditorTab() {
    await this.editorTab.click()
    await this.page.waitForTimeout(500)
  }

  async restoreBackup(index: number = 0) {
    await this.restoreButtons.nth(index).click()
    await this.restoreDialog.waitFor({ state: 'visible' })
    await this.restoreConfirmButton.click()
    await this.page.waitForLoadState('networkidle')
  }

  async openRestartDialog() {
    await this.restartAppButton.click()
    await this.restartDialog.waitFor({ state: 'visible' })
  }

  async fillRestartReason(reason: string) {
    await this.restartReasonInput.fill(reason)
  }

  async confirmRestart() {
    await this.restartConfirmButton.click()
  }

  async cancelRestart() {
    await this.restartCancelButton.click()
  }

  getSection(sectionName: string) {
    return this.page.getByRole('button', { name: new RegExp(sectionName, 'i') })
  }

  getSectionByIndex(index: number) {
    return this.page.locator('button[class*="justify-start"]').nth(index)
  }
}

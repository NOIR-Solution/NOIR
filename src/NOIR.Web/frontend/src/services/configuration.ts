import { apiClient } from './apiClient'

export interface ConfigurationSection {
  name: string
  displayName: string
  isAllowed: boolean
  requiresRestart: boolean
  currentValue: Record<string, unknown>
}

export interface ConfigurationBackup {
  id: string
  createdAt: string
  createdBy: string
  filePath: string
  sizeBytes: number
}

export interface RestartStatus {
  environment: string
  canRestart: boolean
  isAllowed: boolean | null
  lastRestartTime: string | null
  environmentSupportsAutoRestart: boolean
}

export interface RestartResult {
  message: string
  environment: string
  initiatedAt: string
}

export const configurationApi = {
  /**
   * Get all available configuration sections
   */
  getSections: async (): Promise<ConfigurationSection[]> => {
    return apiClient<ConfigurationSection[]>('/admin/config/sections')
  },

  /**
   * Get a specific configuration section by name
   */
  getSection: async (sectionName: string): Promise<ConfigurationSection> => {
    return apiClient<ConfigurationSection>(`/admin/config/sections/${sectionName}`)
  },

  /**
   * Update a configuration section
   * @param sectionName - Name of the section to update
   * @param newValue - JSON object with new configuration values
   */
  updateSection: async (
    sectionName: string,
    newValue: Record<string, unknown>
  ): Promise<ConfigurationBackup> => {
    // Backend expects newValueJson as a JSON string
    const newValueJson = JSON.stringify(newValue)
    return apiClient<ConfigurationBackup>(`/admin/config/sections/${sectionName}`, {
      method: 'PUT',
      body: JSON.stringify({ newValueJson }),
    })
  },

  /**
   * Get all configuration backups
   */
  getBackups: async (): Promise<ConfigurationBackup[]> => {
    return apiClient<ConfigurationBackup[]>('/admin/config/backups')
  },

  /**
   * Rollback configuration to a specific backup
   */
  rollbackBackup: async (backupId: string): Promise<void> => {
    await apiClient<void>(`/admin/config/backups/${backupId}/rollback`, {
      method: 'POST',
    })
  },

  /**
   * Get restart status and capability
   */
  getRestartStatus: async (): Promise<RestartStatus> => {
    return apiClient<RestartStatus>('/admin/config/restart/status')
  },

  /**
   * Restart the application
   * @param reason - Reason for restart (required, min 5 characters)
   */
  restartApplication: async (reason: string): Promise<RestartResult> => {
    return apiClient<RestartResult>('/admin/config/restart', {
      method: 'POST',
      body: JSON.stringify({ reason }),
    })
  },
}

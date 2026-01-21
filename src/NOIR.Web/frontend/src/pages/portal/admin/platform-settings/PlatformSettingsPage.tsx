import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Settings, AlertTriangle, CheckCircle2, XCircle, RotateCw } from 'lucide-react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { toast } from 'sonner'
import { usePageContext } from '@/hooks/usePageContext'
import { configurationApi, type ConfigurationSection } from '@/services/configuration'
import { SectionBrowser } from './components/SectionBrowser'
import { JsonEditor } from './components/JsonEditor'
import { BackupTimeline } from './components/BackupTimeline'
import { RestartDialog } from './components/RestartDialog'

export default function PlatformSettingsPage() {
  usePageContext('PlatformSettings')
  const { t } = useTranslation()

  const [sections, setSections] = useState<ConfigurationSection[]>([])
  const [selectedSection, setSelectedSection] = useState<ConfigurationSection | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [editedValue, setEditedValue] = useState<Record<string, unknown> | null>(null)
  const [hasChanges, setHasChanges] = useState(false)
  const [showRestartDialog, setShowRestartDialog] = useState(false)
  const [activeTab, setActiveTab] = useState<'editor' | 'backups'>('editor')

  // Load sections on mount
  useEffect(() => {
    loadSections()
  }, [])

  const loadSections = async () => {
    try {
      setIsLoading(true)
      const data = await configurationApi.getSections()
      setSections(data)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load configuration sections'
      toast.error(message)
    } finally {
      setIsLoading(false)
    }
  }

  const handleSectionSelect = async (section: ConfigurationSection) => {
    try {
      // Load full section data with current values
      const fullSection = await configurationApi.getSection(section.name)
      setSelectedSection(fullSection)
      setEditedValue(fullSection.currentValue)
      setHasChanges(false)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load section details'
      toast.error(message)
    }
  }

  const handleValueChange = (newValue: Record<string, unknown>) => {
    setEditedValue(newValue)
    setHasChanges(JSON.stringify(newValue) !== JSON.stringify(selectedSection?.currentValue))
  }

  const handleSave = async () => {
    if (!selectedSection || !editedValue) return

    try {
      setIsSaving(true)
      await configurationApi.updateSection(selectedSection.name, editedValue)

      const message = selectedSection.requiresRestart
        ? 'Configuration updated successfully. Please restart the application.'
        : 'Configuration updated and reloaded automatically.'
      toast.success(message)

      // Reload section to get updated values
      const updated = await configurationApi.getSection(selectedSection.name)
      setSelectedSection(updated)
      setEditedValue(updated.currentValue)
      setHasChanges(false)

      // Switch to backups tab to show the new backup
      setActiveTab('backups')
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to update configuration'
      toast.error(message)
    } finally {
      setIsSaving(false)
    }
  }

  const handleCancel = () => {
    if (selectedSection) {
      setEditedValue(selectedSection.currentValue)
      setHasChanges(false)
    }
  }

  const handleBackupRestore = async () => {
    if (!selectedSection) return

    // Reload section after restore
    try {
      const updated = await configurationApi.getSection(selectedSection.name)
      setSelectedSection(updated)
      setEditedValue(updated.currentValue)
      setHasChanges(false)

      toast.success('Configuration restored successfully and reloaded automatically.')
    } catch (err) {
      // Section might not exist or be accessible after restore
      setSelectedSection(null)
      setEditedValue(null)
    }
  }

  return (
    <div className="h-full flex flex-col">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <div className="flex items-center gap-2 mb-2">
            <Settings className="h-6 w-6" />
            <h1 className="text-3xl font-bold tracking-tight">{t('platformSettings.title')}</h1>
          </div>
          <p className="text-muted-foreground">{t('platformSettings.description')}</p>
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={() => setShowRestartDialog(true)}
          className="gap-2"
        >
          <RotateCw className="h-4 w-4" />
          {t('platformSettings.restartApp')}
        </Button>
      </div>

      {/* Error messages are shown via toast */}

      {/* Main Content - Split View */}
      <div className="flex-1 grid grid-cols-12 gap-6 overflow-hidden">
        {/* Left Sidebar - Section Browser */}
        <div className="col-span-3 overflow-auto">
          <SectionBrowser
            sections={sections}
            selectedSection={selectedSection}
            onSectionSelect={handleSectionSelect}
            isLoading={isLoading}
          />
        </div>

        {/* Right Content - Editor and Backups */}
        <div className="col-span-9 flex flex-col overflow-hidden">
          {!selectedSection ? (
            // Empty State
            <Card className="flex-1 flex items-center justify-center">
              <CardContent className="text-center py-12">
                <Settings className="h-12 w-12 mx-auto mb-4 text-muted-foreground" />
                <p className="text-muted-foreground">{t('platformSettings.selectSection')}</p>
              </CardContent>
            </Card>
          ) : (
            // Selected Section View
            <div className="flex flex-col h-full gap-4">
              {/* Section Header */}
              <Card>
                <CardHeader>
                  <div className="flex items-start justify-between">
                    <div>
                      <CardTitle className="flex items-center gap-2">
                        {selectedSection.displayName}
                        {!selectedSection.isAllowed && (
                          <Badge variant="destructive">
                            <XCircle className="h-3 w-3 mr-1" />
                            {t('platformSettings.forbidden')}
                          </Badge>
                        )}
                        {selectedSection.requiresRestart ? (
                          <Badge variant="secondary">
                            <AlertTriangle className="h-3 w-3 mr-1" />
                            {t('platformSettings.requiresRestart')}
                          </Badge>
                        ) : (
                          <Badge variant="default" className="bg-green-100 text-green-800 hover:bg-green-200">
                            <CheckCircle2 className="h-3 w-3 mr-1" />
                            {t('platformSettings.autoReload')}
                          </Badge>
                        )}
                      </CardTitle>
                      <CardDescription>{selectedSection.name}</CardDescription>
                    </div>
                    {selectedSection.isAllowed && hasChanges && (
                      <div className="flex gap-2">
                        <Button variant="outline" size="sm" onClick={handleCancel}>
                          {t('platformSettings.cancel')}
                        </Button>
                        <Button size="sm" onClick={handleSave} disabled={isSaving}>
                          {isSaving ? t('common.saving') : t('platformSettings.save')}
                        </Button>
                      </div>
                    )}
                  </div>
                </CardHeader>
              </Card>

              {/* Tabs - Editor and Backups */}
              <Tabs value={activeTab} onValueChange={(v) => setActiveTab(v as 'editor' | 'backups')} className="flex-1 flex flex-col overflow-hidden">
                <TabsList className="w-full justify-start">
                  <TabsTrigger value="editor">{t('platformSettings.currentValue')}</TabsTrigger>
                  <TabsTrigger value="backups">{t('platformSettings.backups')}</TabsTrigger>
                </TabsList>

                <TabsContent value="editor" className="flex-1 overflow-auto mt-4">
                  <JsonEditor
                    value={editedValue || {}}
                    onChange={handleValueChange}
                    readOnly={!selectedSection.isAllowed}
                  />
                </TabsContent>

                <TabsContent value="backups" className="flex-1 overflow-auto mt-4">
                  <BackupTimeline onRestore={handleBackupRestore} />
                </TabsContent>
              </Tabs>
            </div>
          )}
        </div>
      </div>

      {/* Restart Dialog */}
      <RestartDialog open={showRestartDialog} onOpenChange={setShowRestartDialog} />
    </div>
  )
}

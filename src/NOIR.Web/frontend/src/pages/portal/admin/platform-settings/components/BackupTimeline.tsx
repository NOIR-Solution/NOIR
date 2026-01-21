import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { History, RotateCcw, Clock, User, HardDrive } from 'lucide-react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Skeleton } from '@/components/ui/skeleton'
import { toast } from 'sonner'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { configurationApi, type ConfigurationBackup } from '@/services/configuration'

interface BackupTimelineProps {
  onRestore?: () => void
}

export function BackupTimeline({ onRestore }: BackupTimelineProps) {
  const { t } = useTranslation()

  const [backups, setBackups] = useState<ConfigurationBackup[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isRestoring, setIsRestoring] = useState(false)
  const [selectedBackup, setSelectedBackup] = useState<ConfigurationBackup | null>(null)
  const [showRestoreDialog, setShowRestoreDialog] = useState(false)

  useEffect(() => {
    loadBackups()
  }, [])

  const loadBackups = async () => {
    try {
      setIsLoading(true)
      const data = await configurationApi.getBackups()
      // Sort by created date descending (newest first)
      const sorted = data.sort((a, b) =>
        new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
      )
      setBackups(sorted)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load backups'
      toast.error(message)
    } finally {
      setIsLoading(false)
    }
  }

  const handleRestoreClick = (backup: ConfigurationBackup) => {
    setSelectedBackup(backup)
    setShowRestoreDialog(true)
  }

  const handleRestoreConfirm = async () => {
    if (!selectedBackup) return

    try {
      setIsRestoring(true)
      await configurationApi.rollbackBackup(selectedBackup.id)

      toast.success(`Restored backup from ${formatDate(selectedBackup.createdAt)}`)

      // Reload backups and notify parent
      await loadBackups()
      onRestore?.()

      setShowRestoreDialog(false)
      setSelectedBackup(null)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to restore backup'
      toast.error(message)
    } finally {
      setIsRestoring(false)
    }
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString()
  }

  const formatBytes = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
  }

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <History className="h-5 w-5" />
            {t('platformSettings.backupTimeline')}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-24 w-full" />
          ))}
        </CardContent>
      </Card>
    )
  }

  return (
    <>
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <History className="h-5 w-5" />
            {t('platformSettings.backupTimeline')}
          </CardTitle>
          <CardDescription>
            Automatic backups created before each configuration change
          </CardDescription>
        </CardHeader>
        <CardContent>
          {backups.length === 0 ? (
            <div className="flex items-center gap-2 p-4 text-muted-foreground">
              <History className="h-4 w-4" />
              <p>{t('platformSettings.noBackups')}</p>
            </div>
          ) : (
            <ScrollArea className="h-[400px]">
              <div className="space-y-4">
                {backups.map((backup, index) => (
                  <Card key={backup.id} className="overflow-hidden">
                    <CardContent className="p-4">
                      <div className="flex items-start justify-between gap-4">
                        <div className="flex-1 space-y-2">
                          {/* Timestamp */}
                          <div className="flex items-center gap-2">
                            <Clock className="h-4 w-4 text-muted-foreground" />
                            <span className="font-medium">{formatDate(backup.createdAt)}</span>
                            {index === 0 && (
                              <span className="text-xs bg-primary/10 text-primary px-2 py-0.5 rounded">
                                Latest
                              </span>
                            )}
                          </div>

                          {/* Created By */}
                          <div className="flex items-center gap-2 text-sm text-muted-foreground">
                            <User className="h-4 w-4" />
                            <span>{backup.createdBy}</span>
                          </div>

                          {/* File Size */}
                          <div className="flex items-center gap-2 text-sm text-muted-foreground">
                            <HardDrive className="h-4 w-4" />
                            <span>{formatBytes(backup.sizeBytes)}</span>
                          </div>
                        </div>

                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handleRestoreClick(backup)}
                          className="gap-2"
                        >
                          <RotateCcw className="h-4 w-4" />
                          {t('platformSettings.restore')}
                        </Button>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </ScrollArea>
          )}
        </CardContent>
      </Card>

      {/* Restore Confirmation Dialog */}
      <Dialog open={showRestoreDialog} onOpenChange={setShowRestoreDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('platformSettings.restoreBackup')}</DialogTitle>
            <DialogDescription>
              Are you sure you want to restore configuration from this backup?
            </DialogDescription>
          </DialogHeader>

          {selectedBackup && (
            <div className="space-y-2 py-4">
              <div className="flex items-center gap-2 text-sm">
                <Clock className="h-4 w-4 text-muted-foreground" />
                <span className="font-medium">{formatDate(selectedBackup.createdAt)}</span>
              </div>
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <User className="h-4 w-4" />
                <span>{selectedBackup.createdBy}</span>
              </div>
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <HardDrive className="h-4 w-4" />
                <span>{formatBytes(selectedBackup.sizeBytes)}</span>
              </div>
            </div>
          )}

          <div className="p-3 bg-muted rounded-md text-sm text-muted-foreground">
            A backup of the current configuration will be created before restoring.
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setShowRestoreDialog(false)}>
              {t('platformSettings.cancel')}
            </Button>
            <Button onClick={handleRestoreConfirm} disabled={isRestoring}>
              {isRestoring ? 'Restoring...' : t('platformSettings.restore')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}

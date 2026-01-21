import { useState, useEffect, createElement } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertTriangle, RotateCw, Server, CheckCircle2, XCircle } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Badge } from '@/components/ui/badge'
import { toast } from 'sonner'
import { configurationApi, type RestartStatus } from '@/services/configuration'

interface RestartDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function RestartDialog({ open, onOpenChange }: RestartDialogProps) {
  const { t } = useTranslation()

  const [reason, setReason] = useState('')
  const [isRestarting, setIsRestarting] = useState(false)
  const [restartStatus, setRestartStatus] = useState<RestartStatus | null>(null)

  useEffect(() => {
    if (open) {
      loadRestartStatus()
    }
  }, [open])

  const loadRestartStatus = async () => {
    try {
      const status = await configurationApi.getRestartStatus()
      setRestartStatus(status)
    } catch (err) {
      // Ignore errors silently - restart status is optional
      console.error('Failed to load restart status:', err)
    }
  }

  const handleRestart = async () => {
    if (!reason.trim() || reason.trim().length < 5) {
      toast.error('Reason must be at least 5 characters')
      return
    }

    try {
      setIsRestarting(true)
      const result = await configurationApi.restartApplication(reason.trim())

      toast.success(`Restart Initiated: ${result.message}`)

      // Close dialog and reset form
      onOpenChange(false)
      setReason('')

      // Show a warning that the app will reload
      setTimeout(() => {
        toast.info('Application Restarting - The page will reload in a few seconds...')
      }, 2000)

      // Attempt to reload the page after a delay
      setTimeout(() => {
        window.location.reload()
      }, 5000)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to restart application'
      toast.error(message)
    } finally {
      setIsRestarting(false)
    }
  }

  const getEnvironmentBadge = (environment: string) => {
    const envLower = environment.toLowerCase()
    if (envLower.includes('docker')) return { variant: 'default' as const, icon: CheckCircle2 }
    if (envLower.includes('kubernetes')) return { variant: 'default' as const, icon: CheckCircle2 }
    if (envLower.includes('iis')) return { variant: 'default' as const, icon: CheckCircle2 }
    if (envLower.includes('kestrel')) return { variant: 'secondary' as const, icon: XCircle }
    return { variant: 'outline' as const, icon: Server }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[600px]">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <RotateCw className="h-5 w-5" />
            {t('platformSettings.restartApp')}
          </DialogTitle>
          <DialogDescription>
            Restart the application to apply configuration changes that require it
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          {/* Warning Alert */}
          <div className="p-4 bg-destructive/10 border border-destructive/20 rounded-md" data-testid="restart-warning-alert">
            <div className="flex items-start gap-3">
              <AlertTriangle className="h-5 w-5 text-destructive flex-shrink-0 mt-0.5" />
              <div className="space-y-2">
                <h4 className="font-semibold text-destructive">WARNING: Service Interruption</h4>
                <div className="text-sm text-destructive/90 space-y-2">
                  <p>Restarting the application will:</p>
                  <ul className="list-disc list-inside space-y-1 ml-2">
                    <li>Disconnect all active users</li>
                    <li>Interrupt in-flight requests</li>
                    <li>Cause 10-30 seconds of downtime</li>
                  </ul>
                </div>
              </div>
            </div>
          </div>

          {/* Environment Status */}
          {restartStatus && (
            <div className="space-y-2 p-4 border rounded-lg">
              <div className="flex items-center justify-between">
                <span className="text-sm font-medium">Environment</span>
                <Badge variant={getEnvironmentBadge(restartStatus.environment).variant} className="gap-1">
                  {createElement(getEnvironmentBadge(restartStatus.environment).icon, { className: 'h-3 w-3' })}
                  {restartStatus.environment}
                </Badge>
              </div>

              {!restartStatus.environmentSupportsAutoRestart && (
                <div className="mt-2 p-3 bg-destructive/10 border border-destructive/20 rounded-md">
                  <div className="flex items-start gap-2">
                    <AlertTriangle className="h-4 w-4 text-destructive flex-shrink-0" />
                    <p className="text-xs text-destructive">
                      {restartStatus.environment === 'Kestrel'
                        ? 'Kestrel detected - ensure process manager (systemd) is configured for automatic restart'
                        : 'This environment may require manual restart'}
                    </p>
                  </div>
                </div>
              )}
            </div>
          )}

          {/* Reason Input */}
          <div className="space-y-2">
            <Label htmlFor="restart-reason">{t('platformSettings.restartReason')}</Label>
            <Textarea
              id="restart-reason"
              placeholder={t('platformSettings.restartReasonPlaceholder')}
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              className="min-h-[80px]"
            />
            <p className="text-xs text-muted-foreground">
              Minimum 5 characters. This will be logged in the audit trail.
            </p>
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t('platformSettings.cancel')}
          </Button>
          <Button
            variant="destructive"
            onClick={handleRestart}
            disabled={isRestarting || !reason.trim() || reason.trim().length < 5}
          >
            {isRestarting ? 'Restarting...' : t('platformSettings.restartConfirm')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

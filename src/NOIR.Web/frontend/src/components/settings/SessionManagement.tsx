/**
 * SessionManagement Component
 *
 * Displays and manages active sessions for the current user.
 * Users can view all their active sessions and revoke any except current.
 */
import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Monitor, Smartphone, Globe, Trash2, Loader2, RefreshCw, CheckCircle2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { toast } from 'sonner'
import { getActiveSessions, revokeSession } from '@/services/auth'
import type { ActiveSession } from '@/types'

function getDeviceIcon(userAgent: string | null) {
  if (!userAgent) return Globe
  const ua = userAgent.toLowerCase()
  if (ua.includes('mobile') || ua.includes('android') || ua.includes('iphone')) {
    return Smartphone
  }
  return Monitor
}

function getDeviceInfo(session: ActiveSession): string {
  if (session.deviceName) return session.deviceName

  if (!session.userAgent) return 'Unknown Device'

  const ua = session.userAgent
  // Parse browser and OS from user agent
  let browser = 'Unknown Browser'
  let os = 'Unknown OS'

  if (ua.includes('Chrome')) browser = 'Chrome'
  else if (ua.includes('Firefox')) browser = 'Firefox'
  else if (ua.includes('Safari')) browser = 'Safari'
  else if (ua.includes('Edge')) browser = 'Edge'

  if (ua.includes('Windows')) os = 'Windows'
  else if (ua.includes('Mac')) os = 'macOS'
  else if (ua.includes('Linux')) os = 'Linux'
  else if (ua.includes('Android')) os = 'Android'
  else if (ua.includes('iPhone') || ua.includes('iPad')) os = 'iOS'

  return `${browser} on ${os}`
}

function formatDate(dateString: string): string {
  const date = new Date(dateString)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))

  if (diffDays === 0) {
    return 'Today at ' + date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
  } else if (diffDays === 1) {
    return 'Yesterday at ' + date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
  } else if (diffDays < 7) {
    return `${diffDays} days ago`
  } else {
    return date.toLocaleDateString()
  }
}

export function SessionManagement() {
  const { t } = useTranslation('auth')
  const [sessions, setSessions] = useState<ActiveSession[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isRevoking, setIsRevoking] = useState<string | null>(null)
  const [sessionToRevoke, setSessionToRevoke] = useState<ActiveSession | null>(null)

  const loadSessions = async () => {
    setIsLoading(true)
    try {
      const data = await getActiveSessions()
      setSessions(data)
    } catch {
      toast.error(t('sessions.loadError'))
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    loadSessions()
  }, [])

  const handleRevoke = async () => {
    if (!sessionToRevoke) return

    setIsRevoking(sessionToRevoke.id)
    try {
      await revokeSession(sessionToRevoke.id)
      setSessions((prev) => prev.filter((s) => s.id !== sessionToRevoke.id))
      toast.success(t('sessions.revokeSuccess'))
    } catch {
      toast.error(t('sessions.revokeError'))
    } finally {
      setIsRevoking(null)
      setSessionToRevoke(null)
    }
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <CardTitle className="flex items-center gap-2">
              <Monitor className="h-5 w-5" />
              {t('sessions.title')}
            </CardTitle>
            <CardDescription>{t('sessions.description')}</CardDescription>
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={loadSessions}
            disabled={isLoading}
          >
            <RefreshCw className={`h-4 w-4 mr-2 ${isLoading ? 'animate-spin' : ''}`} />
            {t('common.refresh')}
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        ) : sessions.length === 0 ? (
          <p className="text-center text-muted-foreground py-8">
            {t('sessions.noSessions')}
          </p>
        ) : (
          <div className="space-y-4">
            {sessions.map((session) => {
              const Icon = getDeviceIcon(session.userAgent)
              return (
                <div
                  key={session.id}
                  className="flex items-center justify-between p-4 rounded-lg border bg-card"
                >
                  <div className="flex items-center gap-4">
                    <div className="flex items-center justify-center w-10 h-10 rounded-full bg-muted">
                      <Icon className="h-5 w-5 text-muted-foreground" />
                    </div>
                    <div>
                      <div className="flex items-center gap-2">
                        <p className="font-medium">{getDeviceInfo(session)}</p>
                        {session.isCurrent && (
                          <Badge variant="default" className="text-xs">
                            <CheckCircle2 className="h-3 w-3 mr-1" />
                            {t('sessions.current')}
                          </Badge>
                        )}
                      </div>
                      <div className="flex items-center gap-2 text-sm text-muted-foreground">
                        {session.ipAddress && <span>{session.ipAddress}</span>}
                        <span>-</span>
                        <span>{formatDate(session.createdAt)}</span>
                      </div>
                    </div>
                  </div>
                  {!session.isCurrent && (
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setSessionToRevoke(session)}
                      disabled={isRevoking === session.id}
                      className="text-destructive hover:text-destructive hover:bg-destructive/10"
                    >
                      {isRevoking === session.id ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                      ) : (
                        <Trash2 className="h-4 w-4" />
                      )}
                    </Button>
                  )}
                </div>
              )
            })}
          </div>
        )}
      </CardContent>

      {/* Revoke Confirmation Dialog */}
      <AlertDialog open={!!sessionToRevoke} onOpenChange={() => setSessionToRevoke(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t('sessions.revokeTitle')}</AlertDialogTitle>
            <AlertDialogDescription>
              {t('sessions.revokeDescription', {
                device: sessionToRevoke ? getDeviceInfo(sessionToRevoke) : '',
              })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleRevoke}
              className="border border-destructive bg-transparent text-destructive hover:bg-destructive hover:text-white"
            >
              {t('sessions.revoke')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </Card>
  )
}

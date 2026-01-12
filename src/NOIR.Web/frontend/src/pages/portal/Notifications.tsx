/**
 * Notifications Page
 *
 * Full notification history page with filtering and pagination.
 */
import { Link } from 'react-router-dom'
import { Settings } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { NotificationList } from '@/components/notifications'
import { useNotificationContext } from '@/contexts/NotificationContext'

export default function Notifications() {
  const { connectionState } = useNotificationContext()

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Notifications</h1>
          <p className="text-muted-foreground">
            Manage your notifications and stay up to date.
            {connectionState === 'connected' && (
              <span className="ml-2 inline-flex items-center text-green-600">
                <span className="size-2 rounded-full bg-green-600 mr-1.5 animate-pulse" />
                Live
              </span>
            )}
          </p>
        </div>
        <Button variant="outline" asChild>
          <Link to="/portal/settings/notifications">
            <Settings className="size-4 mr-2" />
            Preferences
          </Link>
        </Button>
      </div>

      {/* Notification List */}
      <NotificationList />
    </div>
  )
}

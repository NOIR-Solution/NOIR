/**
 * NotificationPreferences Page
 *
 * Manage notification preferences per category.
 */
import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft, Save, Bell, Mail, Shield, Workflow, Users, Settings2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Label } from '@/components/ui/label'
import { toast } from 'sonner'
import { getPreferences, updatePreferences } from '@/services/notifications'
import type { NotificationPreference, NotificationCategory, EmailFrequency } from '@/types'
import { cn } from '@/lib/utils'

const categoryConfig: Record<NotificationCategory, { icon: typeof Bell; label: string; description: string }> = {
  system: {
    icon: Settings2,
    label: 'System',
    description: 'System updates, maintenance notices, and announcements',
  },
  userAction: {
    icon: Users,
    label: 'User Actions',
    description: 'Notifications about user activity and interactions',
  },
  workflow: {
    icon: Workflow,
    label: 'Workflow',
    description: 'Approvals, task assignments, and workflow updates',
  },
  security: {
    icon: Shield,
    label: 'Security',
    description: 'Login alerts, password changes, and security events',
  },
  integration: {
    icon: Bell,
    label: 'Integration',
    description: 'External service notifications and API events',
  },
}

const emailFrequencyOptions: { value: EmailFrequency; label: string }[] = [
  { value: 'none', label: 'Never' },
  { value: 'immediate', label: 'Immediate' },
  { value: 'daily', label: 'Daily digest' },
  { value: 'weekly', label: 'Weekly digest' },
]

export default function NotificationPreferences() {
  const [preferences, setPreferences] = useState<NotificationPreference[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [hasChanges, setHasChanges] = useState(false)

  // Fetch preferences on mount
  useEffect(() => {
    const fetchPreferences = async () => {
      try {
        const data = await getPreferences()
        setPreferences(data)
      } catch (error) {
        console.error('Failed to fetch preferences:', error)
        toast.error('Failed to load preferences')
      } finally {
        setIsLoading(false)
      }
    }
    fetchPreferences()
  }, [])

  const handleInAppToggle = (category: NotificationCategory) => {
    setPreferences((prev) =>
      prev.map((p) =>
        p.category === category ? { ...p, inAppEnabled: !p.inAppEnabled } : p
      )
    )
    setHasChanges(true)
  }

  const handleEmailFrequencyChange = (category: NotificationCategory, frequency: EmailFrequency) => {
    setPreferences((prev) =>
      prev.map((p) =>
        p.category === category ? { ...p, emailFrequency: frequency } : p
      )
    )
    setHasChanges(true)
  }

  const handleSave = async () => {
    setIsSaving(true)
    try {
      await updatePreferences({
        preferences: preferences.map((p) => ({
          category: p.category,
          inAppEnabled: p.inAppEnabled,
          emailFrequency: p.emailFrequency,
        })),
      })
      toast.success('Preferences saved successfully')
      setHasChanges(false)
    } catch (error) {
      console.error('Failed to save preferences:', error)
      toast.error('Failed to save preferences')
    } finally {
      setIsSaving(false)
    }
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary" />
      </div>
    )
  }

  return (
    <div className="space-y-6 max-w-3xl">
      {/* Page Header */}
      <div className="flex items-center justify-between">
        <div className="space-y-1">
          <div className="flex items-center gap-2">
            <Button variant="ghost" size="icon" asChild className="h-8 w-8">
              <Link to="/portal/notifications">
                <ArrowLeft className="size-4" />
              </Link>
            </Button>
            <h1 className="text-2xl font-bold text-foreground">Notification Preferences</h1>
          </div>
          <p className="text-muted-foreground pl-10">
            Choose how you want to receive notifications for each category.
          </p>
        </div>
        <Button onClick={handleSave} disabled={!hasChanges || isSaving}>
          <Save className="size-4 mr-2" />
          {isSaving ? 'Saving...' : 'Save Changes'}
        </Button>
      </div>

      {/* Preferences Grid */}
      <div className="space-y-4">
        {preferences.map((pref) => {
          const config = categoryConfig[pref.category] || categoryConfig.system
          const Icon = config.icon

          return (
            <Card key={pref.category} className="overflow-hidden">
              <CardHeader className="pb-3">
                <div className="flex items-center gap-3">
                  <div className="flex items-center justify-center w-10 h-10 rounded-lg bg-primary/10">
                    <Icon className="size-5 text-primary" />
                  </div>
                  <div>
                    <CardTitle className="text-base">{config.label}</CardTitle>
                    <CardDescription className="text-sm">{config.description}</CardDescription>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                {/* In-app notifications toggle */}
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <Bell className="size-4 text-muted-foreground" />
                    <Label htmlFor={`inapp-${pref.category}`} className="cursor-pointer">
                      In-app notifications
                    </Label>
                  </div>
                  <button
                    id={`inapp-${pref.category}`}
                    type="button"
                    role="switch"
                    aria-checked={pref.inAppEnabled}
                    onClick={() => handleInAppToggle(pref.category)}
                    className={cn(
                      'relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2',
                      pref.inAppEnabled ? 'bg-primary' : 'bg-input'
                    )}
                  >
                    <span
                      className={cn(
                        'pointer-events-none inline-block h-5 w-5 transform rounded-full bg-background shadow ring-0 transition duration-200 ease-in-out',
                        pref.inAppEnabled ? 'translate-x-5' : 'translate-x-0'
                      )}
                    />
                  </button>
                </div>

                {/* Email frequency */}
                <div className="space-y-2">
                  <div className="flex items-center gap-2">
                    <Mail className="size-4 text-muted-foreground" />
                    <Label>Email notifications</Label>
                  </div>
                  <div className="flex flex-wrap gap-2 pl-6">
                    {emailFrequencyOptions.map((option) => (
                      <button
                        key={option.value}
                        onClick={() => handleEmailFrequencyChange(pref.category, option.value)}
                        className={cn(
                          'px-3 py-1.5 text-sm rounded-md border transition-colors',
                          pref.emailFrequency === option.value
                            ? 'bg-primary text-primary-foreground border-primary'
                            : 'bg-background hover:bg-muted border-input'
                        )}
                      >
                        {option.label}
                      </button>
                    ))}
                  </div>
                </div>
              </CardContent>
            </Card>
          )
        })}
      </div>

      {/* Info */}
      <p className="text-sm text-muted-foreground">
        Note: Security notifications are always sent immediately by email for your protection.
      </p>
    </div>
  )
}

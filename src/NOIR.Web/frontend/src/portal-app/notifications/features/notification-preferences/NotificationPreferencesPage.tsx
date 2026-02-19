/**
 * NotificationPreferences Page
 *
 * Manage notification preferences per category.
 */
import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { ViewTransitionLink } from '@/components/navigation/ViewTransitionLink'
import { ArrowLeft, Save, Bell, Mail, Shield, Workflow, Users, Settings2, Loader2 } from 'lucide-react'
import {
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Label,
  Skeleton,
} from '@uikit'

import { toast } from 'sonner'
import { getPreferences, updatePreferences } from '@/services/notifications'
import type { NotificationPreference, NotificationCategory, EmailFrequency } from '@/types'
import { cn } from '@/lib/utils'

const categoryConfig: Record<NotificationCategory, { icon: typeof Bell }> = {
  system: { icon: Settings2 },
  userAction: { icon: Users },
  workflow: { icon: Workflow },
  security: { icon: Shield },
  integration: { icon: Bell },
}

const emailFrequencyOptions: { value: EmailFrequency }[] = [
  { value: 'none' },
  { value: 'immediate' },
  { value: 'daily' },
  { value: 'weekly' },
]

export const NotificationPreferencesPage = () => {
  const { t } = useTranslation('common')
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
      } catch {
        toast.error(t('notifications.failedToLoad'))
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
      toast.success(t('notifications.savedSuccessfully'))
      setHasChanges(false)
    } catch {
      toast.error(t('notifications.failedToSave'))
    } finally {
      setIsSaving(false)
    }
  }

  if (isLoading) {
    return (
      <div className="container max-w-4xl py-6">
        {/* Header skeleton */}
        <div className="flex items-center justify-between mb-8">
          <div className="space-y-1">
            <div className="flex items-center gap-3">
              <Skeleton className="h-8 w-8 rounded" />
              <Skeleton className="h-7 w-[220px]" />
            </div>
            <Skeleton className="h-4 w-[320px] ml-11" />
          </div>
          <Skeleton className="h-10 w-[130px]" />
        </div>
        {/* Cards skeleton - matches actual content structure */}
        <div className="space-y-4">
          {Array.from({ length: 5 }).map((_, i) => (
            <Card key={i} className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardHeader className="pb-4">
                <div className="flex items-center gap-3">
                  <Skeleton className="h-10 w-10 rounded-lg" />
                  <div className="space-y-2">
                    <Skeleton className="h-4 w-[100px]" />
                    <Skeleton className="h-3 w-[250px]" />
                  </div>
                </div>
              </CardHeader>
              <CardContent className="space-y-5 pt-0">
                <div className="flex items-center justify-between py-2">
                  <div className="flex items-center gap-3">
                    <Skeleton className="h-4 w-4 rounded" />
                    <Skeleton className="h-4 w-[140px]" />
                  </div>
                  <Skeleton className="h-6 w-11 rounded-full" />
                </div>
                <div className="space-y-3">
                  <div className="flex items-center gap-3">
                    <Skeleton className="h-4 w-4 rounded" />
                    <Skeleton className="h-4 w-[130px]" />
                  </div>
                  <div className="flex gap-2 ml-7">
                    {Array.from({ length: 4 }).map((_, j) => (
                      <Skeleton key={j} className="h-8 w-[80px] rounded-md" />
                    ))}
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    )
  }

  return (
    <div className="container max-w-4xl py-6">
      {/* Page Header */}
      <div className="flex items-center justify-between mb-8">
        <div className="space-y-1">
          <div className="flex items-center gap-3">
            <Button variant="ghost" size="icon" asChild className="h-8 w-8">
              <ViewTransitionLink to="/portal/notifications">
                <ArrowLeft className="size-4" />
              </ViewTransitionLink>
            </Button>
            <h1 className="text-2xl font-bold text-foreground">{t('notifications.preferencesTitle')}</h1>
          </div>
          <p className="text-muted-foreground ml-11">
            {t('notifications.preferencesDescription')}
          </p>
        </div>
        <Button onClick={handleSave} disabled={!hasChanges || isSaving}>
          {isSaving ? (
            <Loader2 className="size-4 mr-2 animate-spin" />
          ) : (
            <Save className="size-4 mr-2" />
          )}
          {isSaving ? t('buttons.saving') : t('notifications.saveChanges')}
        </Button>
      </div>

      {/* Preferences Grid */}
      <div className="space-y-4">
        {preferences.map((pref) => {
          const config = categoryConfig[pref.category] || categoryConfig.system
          const Icon = config.icon

          return (
            <Card key={pref.category} className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardHeader className="pb-4">
                <div className="flex items-center gap-3">
                  <div className="flex items-center justify-center w-10 h-10 rounded-lg bg-primary/10">
                    <Icon className="size-5 text-primary" />
                  </div>
                  <div>
                    <CardTitle className="text-base">{t(`notifications.categories.${pref.category}`)}</CardTitle>
                    <CardDescription className="text-sm">{t(`notifications.categories.${pref.category}Description`)}</CardDescription>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="space-y-5 pt-0">
                {/* In-app notifications toggle */}
                <div className="flex items-center justify-between py-2">
                  <div className="flex items-center gap-3">
                    <Bell className="size-4 text-muted-foreground" />
                    <Label htmlFor={`inapp-${pref.category}`} className="cursor-pointer font-normal">
                      {t('notifications.inAppNotifications')}
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
                <div className="space-y-3">
                  <div className="flex items-center gap-3">
                    <Mail className="size-4 text-muted-foreground" />
                    <Label className="font-normal">{t('notifications.emailNotifications')}</Label>
                  </div>
                  <div className="flex flex-wrap gap-2 ml-7">
                    {emailFrequencyOptions.map((option) => (
                      <button
                        key={option.value}
                        onClick={() => handleEmailFrequencyChange(pref.category, option.value)}
                        className={cn(
                          'px-3 py-1.5 text-sm rounded-md border transition-colors cursor-pointer',
                          pref.emailFrequency === option.value
                            ? 'bg-primary text-primary-foreground border-primary'
                            : 'bg-background hover:bg-muted border-input'
                        )}
                      >
                        {t(`notifications.emailFrequency.${option.value}`)}
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
      <p className="text-sm text-muted-foreground mt-6">
        {t('notifications.securityNote')}
      </p>
    </div>
  )
}

export default NotificationPreferencesPage

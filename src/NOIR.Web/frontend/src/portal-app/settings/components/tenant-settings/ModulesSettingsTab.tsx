import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Box } from 'lucide-react'
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Skeleton,
  Switch,
} from '@uikit'

import { useFeatures, useModuleCatalog, useToggleModule } from '@/hooks/useFeatures'
import { iconMap, SIDEBAR_GROUPS, buildSidebarGroups, getItemModules, type ResolvedItem } from '../shared/moduleConstants'

export interface ModulesSettingsTabProps {
  canEdit: boolean
}

/**
 * ModulesSettingsTab - Shows tenant-configurable modules grouped by sidebar sections.
 * Module names use the same localization keys as sidebar nav items for 100% consistency.
 * Core modules and platform-only modules (Tenants, Developer Logs) are hidden.
 */
export const ModulesSettingsTab = ({ canEdit }: ModulesSettingsTabProps) => {
  const { t } = useTranslation('common')
  const { data: catalog, isLoading: catalogLoading } = useModuleCatalog()
  const { data: features, isLoading: featuresLoading } = useFeatures()
  const toggleMutation = useToggleModule()

  const isLoading = catalogLoading || featuresLoading

  const groups = useMemo(
    () => buildSidebarGroups(catalog?.modules ?? []),
    [catalog],
  )

  const handleToggle = async (item: ResolvedItem, isEnabled: boolean) => {
    try {
      const names = getItemModules(item).map(m => m.name)
      await Promise.all(names.map(name => toggleMutation.mutateAsync({ featureName: name, isEnabled })))
      toast.success(t('featureManagement.toggleSuccess'))
    } catch {
      toast.error(t('featureManagement.toggleError'))
    }
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="space-y-2">
          <Skeleton className="h-6 w-48" />
          <Skeleton className="h-4 w-96" />
        </div>
        {SIDEBAR_GROUPS.map((group) => (
          <Card key={group.key}>
            <CardHeader>
              <Skeleton className="h-5 w-32" />
            </CardHeader>
            <CardContent className="space-y-4">
              {group.modules.map((mod) => (
                <div key={mod.name} className="flex items-center justify-between py-3">
                  <div className="flex items-center gap-3">
                    <Skeleton className="h-9 w-9 rounded-lg" />
                    <div className="space-y-1.5">
                      <Skeleton className="h-4 w-40" />
                      <Skeleton className="h-3 w-64" />
                    </div>
                  </div>
                  <Skeleton className="h-5 w-9 rounded-full" />
                </div>
              ))}
            </CardContent>
          </Card>
        ))}
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h3 className="text-lg font-medium">{t('featureManagement.title')}</h3>
        <p className="text-sm text-muted-foreground">{t('featureManagement.description')}</p>
      </div>

      {groups.map(group => {
        // Hide modules the platform admin explicitly disabled (isAvailable === false).
        // If features haven't loaded yet or module has no entry, show it (same as Sidebar behavior).
        const availableItems = group.items.filter((item) => {
          const allModules = getItemModules(item)
          return allModules.every(m => features?.[m.name]?.isAvailable !== false)
        })

        // Hide entire group if all modules are unavailable
        if (availableItems.length === 0) return null

        return (
          <Card key={group.key}>
            <CardHeader className="pb-3">
              <CardTitle className="text-base">
                {t(group.labelKey)}
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-1 pt-0">
              {availableItems.map((item) => {
                const { module, titleKey, descKey, iconOverride } = item
                const allModules = getItemModules(item)
                const isEnabled = allModules.every(m => features?.[m.name]?.isEnabled ?? m.defaultEnabled)
                const Icon = iconMap[iconOverride ?? module.icon] ?? Box
                const displayName = t(titleKey)

                return (
                  <div
                    key={module.name}
                    className="flex items-center justify-between py-3 px-2 -mx-2 rounded-lg transition-colors hover:bg-muted/50"
                  >
                    <div className="flex items-center gap-3 min-w-0">
                      <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-muted/80 text-muted-foreground">
                        <Icon className="h-[18px] w-[18px]" />
                      </div>
                      <div className="min-w-0 space-y-0.5">
                        <span className="font-medium text-sm leading-none">
                          {displayName}
                        </span>
                        <p className="text-xs text-muted-foreground leading-snug">
                          {t(descKey ?? module.descriptionKey)}
                        </p>
                      </div>
                    </div>
                    <Switch
                      checked={isEnabled}
                      disabled={!canEdit || toggleMutation.isPending}
                      onCheckedChange={(checked) => handleToggle(item, checked)}
                      className="cursor-pointer shrink-0 ml-4"
                      aria-label={t('featureManagement.toggleModule', { module: displayName })}
                    />
                  </div>
                )
              })}
            </CardContent>
          </Card>
        )
      })}
    </div>
  )
}

import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Box } from 'lucide-react'
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Checkbox,
  Skeleton,
  Switch,
} from '@uikit'

import { useFeatures, useModuleCatalog, useToggleModule } from '@/hooks/useFeatures'
import { iconMap, SIDEBAR_GROUPS, buildSidebarGroups, getItemModules, computeCheckState, type ResolvedItem } from '../shared/moduleConstants'

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

  /** Toggle one or more modules sequentially to avoid optimistic update race conditions. */
  const handleToggleNames = async (names: string[], isEnabled: boolean) => {
    let failCount = 0
    for (const name of names) {
      try {
        await toggleMutation.mutateAsync({ featureName: name, isEnabled })
      } catch {
        failCount++
      }
    }
    if (failCount === 0) {
      toast.success(t('featureManagement.toggleSuccess'))
    } else if (failCount === names.length) {
      toast.error(t('featureManagement.toggleError'))
    } else {
      toast.warning(t('featureManagement.bulkPartialError', { count: failCount }))
    }
  }

  const handleToggle = (item: ResolvedItem, isEnabled: boolean) =>
    handleToggleNames(getItemModules(item).map(m => m.name), isEnabled)

  const handleGroupToggle = (items: ResolvedItem[], isEnabled: boolean) =>
    handleToggleNames(items.flatMap(item => getItemModules(item).map(m => m.name)), isEnabled)

  if (isLoading) {
    return (
      <div className="space-y-4">
        <div className="space-y-1">
          <Skeleton className="h-6 w-48" />
          <Skeleton className="h-4 w-96" />
        </div>
        {SIDEBAR_GROUPS.map((group) => (
          <Card key={group.key} className="gap-2 py-3">
            <CardHeader className="px-4">
              <div className="flex items-center justify-between">
                <Skeleton className="h-5 w-32" />
                <Skeleton className="h-5 w-5 rounded" />
              </div>
            </CardHeader>
            <CardContent className="space-y-0 px-4">
              {group.modules.map((mod) => (
                <div key={mod.name} className="flex items-center justify-between py-2">
                  <div className="flex items-center gap-3">
                    <Skeleton className="h-8 w-8 rounded-md" />
                    <div className="space-y-1">
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
    <div className="space-y-4">
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

        // Group-level tri-state: all on → checked, all off → unchecked, mixed → indeterminate
        const itemStates = availableItems.map((item) =>
          getItemModules(item).every(m => features?.[m.name]?.isEnabled ?? m.defaultEnabled),
        )
        const groupCheckState = computeCheckState(itemStates)
        const groupLabel = t(group.labelKey)

        return (
          <Card key={group.key} className="gap-2 py-3">
            <CardHeader className="px-4">
              <div className="flex items-center justify-between">
                <CardTitle className="text-base">{groupLabel}</CardTitle>
                <Checkbox
                  checked={groupCheckState === 'indeterminate' ? 'indeterminate' : groupCheckState}
                  onCheckedChange={(checked) => handleGroupToggle(availableItems, !!checked)}
                  disabled={!canEdit || toggleMutation.isPending}
                  className="cursor-pointer shrink-0"
                  aria-label={t('featureManagement.toggleGroup', { group: groupLabel })}
                />
              </div>
            </CardHeader>
            <CardContent className="space-y-0 px-4">
              {availableItems.map((item) => {
                const { module, titleKey, descKey, iconOverride } = item
                const allModules = getItemModules(item)
                const isEnabled = allModules.every(m => features?.[m.name]?.isEnabled ?? m.defaultEnabled)
                const Icon = iconMap[iconOverride ?? module.icon] ?? Box
                const displayName = t(titleKey)

                return (
                  <div
                    key={module.name}
                    className="flex items-center justify-between py-2 px-2 -mx-2 rounded-lg transition-colors hover:bg-muted/50"
                  >
                    <div className="flex items-center gap-3 min-w-0">
                      <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md bg-muted/80 text-muted-foreground">
                        <Icon className="h-4 w-4" />
                      </div>
                      <div className="min-w-0">
                        <span className="font-medium text-sm leading-none">
                          {displayName}
                        </span>
                        <p className="text-xs text-muted-foreground leading-snug mt-0.5">
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

import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { AlertCircle, Box } from 'lucide-react'
import {
  Badge,
  Skeleton,
  Switch,
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@uikit'
import { getStatusBadgeClasses } from '@/utils/statusBadge'

import { useTenantFeatureStates, useSetModuleAvailability } from '@/hooks/useFeatures'
import {
  iconMap,
  SIDEBAR_GROUPS,
  buildSidebarGroups,
  getItemModules,
  type ResolvedItem,
} from '@/portal-app/settings/components/shared'

export interface TenantModulesTabProps {
  tenantId: string
  canEdit: boolean
  /** Compact mode for dialog context — no cards, smaller spacing */
  compact?: boolean
}

/**
 * Platform admin tab for managing module availability per tenant.
 * Controls `isAvailable` (platform-level), distinct from tenant's own `isEnabled`.
 */
export const TenantModulesTab = ({ tenantId, canEdit, compact }: TenantModulesTabProps) => {
  const { t } = useTranslation('common')
  const { data: catalog, isLoading, isError } = useTenantFeatureStates(tenantId)
  const availabilityMutation = useSetModuleAvailability()

  const groups = useMemo(
    () => buildSidebarGroups(catalog?.modules ?? []),
    [catalog],
  )

  const handleToggleAvailability = async (item: ResolvedItem, isAvailable: boolean) => {
    try {
      const names = getItemModules(item).map(m => m.name)
      await Promise.all(names.map(name => availabilityMutation.mutateAsync({ tenantId, featureName: name, isAvailable })))
      toast.success(t('featureManagement.availabilitySuccess'))
    } catch {
      toast.error(t('featureManagement.availabilityError'))
    }
  }

  if (isLoading) {
    return (
      <div className="space-y-4">
        {compact ? (
          Array.from({ length: 6 }).map((_, i) => (
            <div key={`skeleton-${i}`} className="flex items-center justify-between py-2">
              <div className="flex items-center gap-3">
                <Skeleton className="h-8 w-8 rounded-lg" />
                <div className="space-y-1">
                  <Skeleton className="h-4 w-32" />
                  <Skeleton className="h-3 w-48" />
                </div>
              </div>
              <Skeleton className="h-5 w-9 rounded-full" />
            </div>
          ))
        ) : (
          <>
            <div className="space-y-2">
              <Skeleton className="h-6 w-48" />
              <Skeleton className="h-4 w-96" />
            </div>
            {SIDEBAR_GROUPS.map((group) => (
              <div key={group.key} className="space-y-3">
                <Skeleton className="h-5 w-32" />
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
              </div>
            ))}
          </>
        )}
      </div>
    )
  }

  if (isError) {
    return (
      <div className="flex items-center justify-center gap-2 py-8 text-destructive text-sm">
        <AlertCircle className="h-4 w-4" />
        {t('messages.operationFailed')}
      </div>
    )
  }

  if (compact) {
    return (
      <TooltipProvider delayDuration={300}>
        <div className="max-h-[60vh] overflow-y-auto overflow-x-hidden p-0.5 -m-0.5">
          {/* Column headers */}
          <div className="flex items-center gap-2 pb-2 mb-1 border-b border-border/50 text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
            <span className="flex-1 min-w-0">{t('featureManagement.matrixModuleColumn')}</span>
            <span className="w-[4.5rem] text-center shrink-0">{t('labels.status')}</span>
            <span className="w-[4.5rem] text-center shrink-0">{t('featureManagement.available')}</span>
          </div>

          {groups.map(group => (
            <div key={group.key}>
              <div className="sticky top-0 z-10 bg-background py-1.5">
                <span className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                  {t(group.labelKey)}
                </span>
              </div>
              {group.items.map((item) => {
                const { module, titleKey, descKey, iconOverride } = item
                const allModules = getItemModules(item)
                const isAvailable = allModules.every(m => m.isAvailable ?? true)
                const isEnabled = allModules.every(m => m.isEnabled ?? m.defaultEnabled)
                const Icon = iconMap[iconOverride ?? module.icon] ?? Box
                const displayName = t(titleKey)

                return (
                  <div
                    key={module.name}
                    className="flex items-center gap-2 py-1.5 px-1 -mx-1 rounded-lg transition-colors hover:bg-muted/50"
                  >
                    {/* Icon + Name */}
                    <div className="flex items-center gap-2 flex-1 min-w-0">
                      <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-md bg-muted/80 text-muted-foreground">
                        <Icon className="h-3.5 w-3.5" />
                      </div>
                      <Tooltip>
                        <TooltipTrigger asChild>
                          <span className="font-medium text-sm leading-none truncate cursor-default">
                            {displayName}
                          </span>
                        </TooltipTrigger>
                        <TooltipContent side="bottom" align="start">
                          <p className="text-xs">{t(descKey ?? module.descriptionKey)}</p>
                        </TooltipContent>
                      </Tooltip>
                    </div>

                    {/* Tenant status dot */}
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <div className="w-[4.5rem] flex justify-center shrink-0">
                          <span
                            className={`inline-block h-2 w-2 rounded-full ${
                              isEnabled
                                ? 'bg-emerald-500'
                                : 'bg-muted-foreground/30'
                            }${!isAvailable ? ' opacity-40' : ''}`}
                          />
                        </div>
                      </TooltipTrigger>
                      <TooltipContent>
                        {isEnabled ? t('featureManagement.tenantEnabled') : t('featureManagement.tenantDisabled')}
                      </TooltipContent>
                    </Tooltip>

                    {/* Availability switch */}
                    <div className="w-[4.5rem] flex justify-center shrink-0">
                      <Switch
                        checked={isAvailable}
                        disabled={!canEdit || availabilityMutation.isPending}
                        onCheckedChange={(checked) => handleToggleAvailability(item, checked)}
                        className="cursor-pointer"
                        aria-label={t('featureManagement.availabilityToggle', { module: displayName })}
                      />
                    </div>
                  </div>
                )
              })}
            </div>
          ))}
        </div>
      </TooltipProvider>
    )
  }

  // Full page mode
  return (
    <div className="space-y-6">
      <div>
        <h3 className="text-lg font-medium">{t('featureManagement.platformTitle')}</h3>
        <p className="text-sm text-muted-foreground">{t('featureManagement.platformDescription')}</p>
      </div>

      {groups.map(group => (
        <div key={group.key} className="rounded-xl border border-border/50 overflow-hidden">
          <div className="bg-muted/30 px-4 py-2.5 border-b border-border/50">
            <span className="text-sm font-semibold">{t(group.labelKey)}</span>
          </div>
          <div className="divide-y divide-border/30">
            {group.items.map((item) => {
              const { module, titleKey, descKey, linkedModules, iconOverride } = item
              const allModules = [module, ...linkedModules]
              const isAvailable = allModules.every(m => m.isAvailable ?? true)
              const isEnabled = allModules.every(m => m.isEnabled ?? m.defaultEnabled)
              const Icon = iconMap[iconOverride ?? module.icon] ?? Box
              const displayName = t(titleKey)

              return (
                <div
                  key={module.name}
                  className="flex items-center justify-between py-3 px-4 transition-colors hover:bg-muted/30"
                >
                  <div className="flex items-center gap-3 min-w-0">
                    <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-muted/80 text-muted-foreground">
                      <Icon className="h-[18px] w-[18px]" />
                    </div>
                    <div className="min-w-0 space-y-0.5">
                      <span className="font-medium text-sm leading-none">
                        {displayName}
                      </span>
                      <div className="flex items-center gap-2">
                        <p className="text-xs text-muted-foreground leading-snug">
                          {t(descKey ?? module.descriptionKey)}
                        </p>
                        <Badge variant="outline" className={`${getStatusBadgeClasses(isAvailable && isEnabled ? 'green' : 'gray')} text-[10px] px-1.5 py-0${!isAvailable ? ' opacity-50' : ''}`}>
                          {isEnabled ? t('featureManagement.tenantEnabled') : t('featureManagement.tenantDisabled')}
                        </Badge>
                      </div>
                    </div>
                  </div>
                  <Switch
                    checked={isAvailable}
                    disabled={!canEdit || availabilityMutation.isPending}
                    onCheckedChange={(checked) => handleToggleAvailability(item, checked)}
                    className="cursor-pointer shrink-0 ml-4"
                    aria-label={t('featureManagement.availabilityToggle', { module: displayName })}
                  />
                </div>
              )
            })}
          </div>
        </div>
      ))}
    </div>
  )
}

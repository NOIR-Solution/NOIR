import { Fragment, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { AlertCircle, AlertTriangle, Box, Building, Filter } from 'lucide-react'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Checkbox,
  EmptyState,
  Label,
  Skeleton,
  Switch,
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@uikit'

import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { useAllTenantFeatureStates, useSetModuleAvailability } from '@/hooks/useFeatures'
import { useTenantsQuery } from '@/portal-app/user-access/queries'
import { EditTenantDialog } from '@/portal-app/user-access/components/tenants/EditTenantDialog'
import { buildSidebarGroups, iconMap, getItemModuleNames } from '@/portal-app/settings/components/shared'
import type { ModuleDto, TenantListItem } from '@/types'

/** Compute checkbox tri-state from a list of booleans */
const computeCheckState = (states: boolean[]): boolean | 'indeterminate' => {
  if (states.length === 0) return true
  if (states.every(Boolean)) return true
  if (states.every((s) => !s)) return false
  return 'indeterminate'
}

/**
 * Platform-level module availability matrix.
 * Rows = modules (grouped by category), Columns = tenants.
 *
 * Uses raw <table> elements instead of the Table UIKit component
 * to avoid nested overflow-x-auto containers that break position:sticky.
 */
export const PlatformModulesOverviewTab = () => {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  const canEdit = hasPermission(Permissions.FeaturesUpdate)

  const [diffOnly, setDiffOnly] = useState(false)
  const [editTenant, setEditTenant] = useState<TenantListItem | null>(null)

  // Fetch tenants
  const { data: tenantsData, isLoading: tenantsLoading, isError: tenantsError } = useTenantsQuery({
    page: 1,
    pageSize: 100,
  })
  const tenants = tenantsData?.items ?? []
  const hasMoreTenants = (tenantsData?.totalCount ?? 0) > tenants.length

  // Fetch feature states for all tenants in parallel
  const tenantQueries = useAllTenantFeatureStates(tenants.map((t) => t.id))
  const allQueriesLoaded = tenantQueries.length > 0 && tenantQueries.every((q) => q.data !== undefined || q.isError)

  // Matrix mutation
  const mutation = useSetModuleAvailability()

  // Derive module list from the first loaded tenant query (avoids separate catalog endpoint)
  const firstLoadedModules = tenantQueries.find((q) => q.data)?.data?.modules
  const groups = useMemo(
    () => buildSidebarGroups(firstLoadedModules ?? []),
    [firstLoadedModules],
  )

  // Build tenant state map: tenantId → moduleName → ModuleDto
  const tenantStates = useMemo(() => {
    const map = new Map<string, Map<string, ModuleDto>>()
    tenants.forEach((tenant, i) => {
      const query = tenantQueries[i]
      if (query?.data) {
        const moduleMap = new Map(query.data.modules.map((m) => [m.name, m]))
        map.set(tenant.id, moduleMap)
      }
    })
    return map
  }, [tenants, tenantQueries])

  // All toggleable module names (flat list, including linked modules)
  const allModuleNames = useMemo(
    () => groups.flatMap((g) => g.items.flatMap(getItemModuleNames)),
    [groups],
  )

  // Pre-compute diff set: module items with different availability across tenants
  // Uses primary module name as key; checks all linked modules as a unit
  const diffSet = useMemo(() => {
    const set = new Set<string>()
    if (tenants.length > 1) {
      for (const group of groups) {
        for (const item of group.items) {
          const names = getItemModuleNames(item)
          const states = tenants.map((tenant) =>
            names.every(name => tenantStates.get(tenant.id)?.get(name)?.isAvailable ?? true),
          )
          if (!states.every((s) => s === states[0])) set.add(item.module.name)
        }
      }
    }
    return set
  }, [groups, tenants, tenantStates])

  const handleToggle = async (tenantId: string, moduleNames: string[], isAvailable: boolean) => {
    try {
      await Promise.all(moduleNames.map(name => mutation.mutateAsync({ tenantId, featureName: name, isAvailable })))
      toast.success(t('featureManagement.availabilitySuccess'))
    } catch {
      toast.error(t('featureManagement.availabilityError'))
    }
  }

  // Bulk toggle: enable/disable ALL modules for a single tenant
  const handleBulkToggleTenant = async (tenantId: string, isAvailable: boolean) => {
    const results = await Promise.allSettled(
      allModuleNames.map((name) => mutation.mutateAsync({ tenantId, featureName: name, isAvailable })),
    )
    const failCount = results.filter((r) => r.status === 'rejected').length
    if (failCount === 0) {
      toast.success(t('featureManagement.bulkSuccess'))
    } else if (failCount === allModuleNames.length) {
      toast.error(t('featureManagement.availabilityError'))
    } else {
      toast.warning(t('featureManagement.bulkPartialError', { count: failCount }))
    }
  }

  // Bulk toggle: enable/disable a module (+ linked) across ALL tenants
  const handleBulkToggleModule = async (moduleNames: string[], isAvailable: boolean) => {
    const results = await Promise.allSettled(
      tenants.flatMap((tenant) =>
        moduleNames.map(name => mutation.mutateAsync({ tenantId: tenant.id, featureName: name, isAvailable })),
      ),
    )
    const failCount = results.filter((r) => r.status === 'rejected').length
    if (failCount === 0) {
      toast.success(t('featureManagement.bulkSuccess'))
    } else if (failCount === results.length) {
      toast.error(t('featureManagement.availabilityError'))
    } else {
      toast.warning(t('featureManagement.bulkPartialError', { count: failCount }))
    }
  }

  const allModulesAvailableForTenant = (tenantId: string) => {
    const stateMap = tenantStates.get(tenantId)
    if (!stateMap) return true as boolean | 'indeterminate'
    return computeCheckState(allModuleNames.map((name) => stateMap.get(name)?.isAvailable ?? true))
  }

  const moduleAvailableAcrossAllTenants = (moduleNames: string[]) =>
    computeCheckState(tenants.map((tenant) =>
      moduleNames.every(name => tenantStates.get(tenant.id)?.get(name)?.isAvailable ?? true),
    ))

  const isLoading = tenantsLoading || (tenants.length > 0 && !allQueriesLoaded)

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <Skeleton className="h-6 w-48" />
          <Skeleton className="h-4 w-96" />
        </CardHeader>
        <CardContent className="space-y-3">
          {Array.from({ length: 8 }).map((_, i) => (
            <Skeleton key={i} className="h-10 w-full" />
          ))}
        </CardContent>
      </Card>
    )
  }

  if (tenantsError) {
    return (
      <Card>
        <CardContent className="flex items-center justify-center gap-2 py-8 text-destructive text-sm">
          <AlertCircle className="h-4 w-4" />
          {t('messages.operationFailed')}
        </CardContent>
      </Card>
    )
  }

  if (tenants.length === 0) {
    return (
      <Card>
        <CardContent className="py-8">
          <EmptyState
            icon={Building}
            title={t('tenants.noTenantsFound')}
            description={t('tenants.noTenantsFoundDescription')}
          />
        </CardContent>
      </Card>
    )
  }

  const diffCount = diffSet.size

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('featureManagement.modulesOverview')}</CardTitle>
        <CardDescription>{t('featureManagement.matrixDescription')}</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Truncation warning */}
        {hasMoreTenants && (
          <div className="flex items-center gap-2 rounded-md border border-amber-200 bg-amber-50/60 px-3 py-2 text-sm text-amber-700 dark:border-amber-800 dark:bg-amber-950/30 dark:text-amber-400">
            <AlertTriangle className="h-4 w-4 shrink-0" />
            {t('featureManagement.tenantTruncationWarning', { shown: tenants.length, total: tenantsData?.totalCount })}
          </div>
        )}

        {/* Diff filter */}
        {diffCount > 0 && (
          <div className="flex items-center gap-2">
            <Switch
              id="diff-filter"
              checked={diffOnly}
              onCheckedChange={setDiffOnly}
              className="cursor-pointer"
            />
            <Label htmlFor="diff-filter" className="text-sm cursor-pointer flex items-center gap-1.5">
              <Filter className="h-3.5 w-3.5" />
              {t('featureManagement.showDifferencesOnly', { count: diffCount })}
            </Label>
          </div>
        )}

        {/* Matrix table — uses raw <table> to avoid nested overflow from Table component */}
        <TooltipProvider delayDuration={300}>
          <div className="overflow-x-auto rounded-lg border border-border/50">
            <table className="w-full caption-bottom text-sm border-separate border-spacing-0">
              <thead>
                <tr>
                  {/* Module column header */}
                  <th className="sticky left-0 z-20 bg-background text-left align-middle font-medium text-muted-foreground px-3 py-2 min-w-[220px] border-b border-border">
                    {t('featureManagement.matrixModuleColumn')}
                  </th>
                  {/* "All" column header for per-module bulk toggle */}
                  <th className="text-center align-middle font-medium text-muted-foreground px-2 py-2 min-w-[52px] bg-muted/30 border-b border-border">
                    <span className="text-[10px] uppercase tracking-wider">
                      {t('labels.all')}
                    </span>
                  </th>
                  {/* Tenant column headers — entire cell is clickable */}
                  {tenants.map((tenant) => (
                    <th
                      key={tenant.id}
                      className="text-center align-middle px-2 py-2 min-w-[90px] max-w-[130px] cursor-pointer hover:bg-muted/50 transition-colors border-b border-border"
                      onClick={() => setEditTenant(tenant)}
                      title={`${tenant.name || tenant.identifier} — ${t('featureManagement.clickToConfigureTenant')}`}
                    >
                      <span className="text-xs font-medium truncate block">
                        {tenant.name || tenant.identifier}
                      </span>
                    </th>
                  ))}
                </tr>
                {/* Bulk toggle row: per-tenant column toggle */}
                <tr>
                  <td className="sticky left-0 z-20 bg-muted/30 px-3 py-1.5 text-left align-middle border-b-2 border-border/50">
                    <span className="text-[10px] font-medium uppercase tracking-wider text-muted-foreground">
                      {t('featureManagement.bulkToggle')}
                    </span>
                  </td>
                  <td className="bg-muted/30 align-middle border-b-2 border-border/50" />
                  {tenants.map((tenant) => {
                    const bulkState = allModulesAvailableForTenant(tenant.id)
                    return (
                      <td key={tenant.id} className="text-center py-1.5 align-middle border-b-2 border-border/50">
                        <Checkbox
                          checked={bulkState === 'indeterminate' ? 'indeterminate' : bulkState}
                          // Indeterminate → enable all (deterministic: mixed state moves to fully enabled)
                          onCheckedChange={(checked) =>
                            handleBulkToggleTenant(tenant.id, !!checked)
                          }
                          disabled={!canEdit || mutation.isPending}
                          className="cursor-pointer mx-auto"
                          aria-label={t('featureManagement.bulkToggleTenantLabel', {
                            tenant: tenant.name || tenant.identifier,
                          })}
                        />
                      </td>
                    )
                  })}
                </tr>
              </thead>
              <tbody>
                {groups.map((group) => {
                  const visibleItems = diffOnly
                    ? group.items.filter((item) => diffSet.has(item.module.name))
                    : group.items

                  if (visibleItems.length === 0) return null

                  return (
                    <Fragment key={group.key}>
                      {/* Group header row — sticky left cell keeps label visible when scrolled */}
                      <tr>
                        <td className="sticky left-0 z-10 bg-muted/50 px-3 py-2 border-t border-border/30">
                          <span className="text-xs font-semibold uppercase tracking-wide text-muted-foreground whitespace-nowrap">
                            {t(group.labelKey)}
                          </span>
                        </td>
                        <td colSpan={tenants.length + 1} className="bg-muted/50 border-t border-border/30" />
                      </tr>

                      {/* Module rows */}
                      {visibleItems.map((item) => {
                        const { module, titleKey, descKey, iconOverride } = item
                        const itemNames = getItemModuleNames(item)
                        const isDiff = diffSet.has(module.name)
                        const Icon = iconMap[iconOverride ?? module.icon] ?? Box
                        const bulkModuleState = moduleAvailableAcrossAllTenants(itemNames)

                        return (
                          <tr
                            key={module.name}
                            className={`transition-colors hover:bg-muted/50 ${isDiff ? 'bg-amber-50/50 dark:bg-amber-950/20' : ''}`}
                          >
                            {/* Module name + icon — sticky column */}
                            <td className="sticky left-0 z-10 bg-background px-3 py-2 align-middle border-b border-border/30">
                              <div className="flex items-center gap-2">
                                <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-md bg-muted/80 text-muted-foreground">
                                  <Icon className="h-3.5 w-3.5" />
                                </div>
                                {isDiff && (
                                  <Tooltip>
                                    <TooltipTrigger>
                                      <AlertTriangle className="h-3.5 w-3.5 text-amber-500 shrink-0" />
                                    </TooltipTrigger>
                                    <TooltipContent>
                                      {t('featureManagement.diffTooltip')}
                                    </TooltipContent>
                                  </Tooltip>
                                )}
                                <Tooltip>
                                  <TooltipTrigger asChild>
                                    <span className="text-sm font-medium cursor-default">{t(titleKey)}</span>
                                  </TooltipTrigger>
                                  <TooltipContent side="right">
                                    <p className="text-xs">{t(descKey ?? module.descriptionKey)}</p>
                                  </TooltipContent>
                                </Tooltip>
                              </div>
                            </td>

                            {/* Bulk toggle for this module across all tenants */}
                            <td className="text-center py-2 bg-muted/10 align-middle border-b border-border/30">
                              <Checkbox
                                checked={bulkModuleState === 'indeterminate' ? 'indeterminate' : bulkModuleState}
                                onCheckedChange={(checked) =>
                                  handleBulkToggleModule(itemNames, !!checked)
                                }
                                disabled={!canEdit || mutation.isPending}
                                className="cursor-pointer mx-auto"
                                aria-label={t('featureManagement.bulkToggleModuleLabel', {
                                  module: t(titleKey),
                                })}
                              />
                            </td>

                            {/* Per-tenant availability checkboxes */}
                            {tenants.map((tenant) => {
                              const isAvailable = itemNames.every(
                                name => tenantStates.get(tenant.id)?.get(name)?.isAvailable ?? true,
                              )

                              return (
                                <td key={tenant.id} className="text-center py-2 align-middle border-b border-border/30">
                                  <Checkbox
                                    checked={isAvailable}
                                    onCheckedChange={(checked) =>
                                      handleToggle(tenant.id, itemNames, !!checked)
                                    }
                                    disabled={!canEdit || mutation.isPending}
                                    className="cursor-pointer mx-auto"
                                    aria-label={t('featureManagement.matrixCellLabel', {
                                      module: t(titleKey),
                                      tenant: tenant.name || tenant.identifier,
                                    })}
                                  />
                                </td>
                              )
                            })}
                          </tr>
                        )
                      })}
                    </Fragment>
                  )
                })}
              </tbody>
            </table>
          </div>
        </TooltipProvider>
      </CardContent>

      <EditTenantDialog
        tenant={editTenant}
        open={!!editTenant}
        onOpenChange={(open) => !open && setEditTenant(null)}
        activeTab="modules"
      />
    </Card>
  )
}

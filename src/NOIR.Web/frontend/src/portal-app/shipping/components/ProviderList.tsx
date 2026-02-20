import { useState, useDeferredValue } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  Check,
  Pencil,
  Plus,
  Search,
  Truck,
  X,
  XCircle,
} from 'lucide-react'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  EmptyState,
  Input,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@uikit'
import { useShippingProvidersQuery } from '@/portal-app/shipping/queries'
import {
  useActivateProviderMutation,
  useDeactivateProviderMutation,
} from '@/portal-app/shipping/queries'
import type { ShippingProviderDto } from '@/types/shipping'
import { ProviderFormDialog } from './ProviderFormDialog'

const getHealthBadgeVariant = (status: string) => {
  switch (status) {
    case 'Healthy':
      return 'default'
    case 'Degraded':
      return 'secondary'
    case 'Unhealthy':
      return 'destructive'
    default:
      return 'outline'
  }
}

const getHealthBadgeClass = (status: string) => {
  switch (status) {
    case 'Healthy':
      return 'bg-emerald-100 text-emerald-700 border-emerald-200 dark:bg-emerald-900/30 dark:text-emerald-400 dark:border-emerald-800'
    case 'Degraded':
      return 'bg-amber-100 text-amber-700 border-amber-200 dark:bg-amber-900/30 dark:text-amber-400 dark:border-amber-800'
    case 'Unhealthy':
      return 'bg-red-100 text-red-700 border-red-200 dark:bg-red-900/30 dark:text-red-400 dark:border-red-800'
    default:
      return ''
  }
}

export const ProviderList = () => {
  const { t } = useTranslation('common')
  const { data: providers, isLoading } = useShippingProvidersQuery()
  const activateMutation = useActivateProviderMutation()
  const deactivateMutation = useDeactivateProviderMutation()

  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch

  const [formOpen, setFormOpen] = useState(false)
  const [editProvider, setEditProvider] = useState<ShippingProviderDto | null>(null)
  const [toggleProvider, setToggleProvider] = useState<ShippingProviderDto | null>(null)

  const filteredProviders = (providers ?? []).filter((p) => {
    if (!deferredSearch) return true
    const search = deferredSearch.toLowerCase()
    return (
      p.displayName.toLowerCase().includes(search) ||
      p.providerName.toLowerCase().includes(search) ||
      p.providerCode.toLowerCase().includes(search)
    )
  })

  const handleToggleActive = async () => {
    if (!toggleProvider) return
    try {
      if (toggleProvider.isActive) {
        await deactivateMutation.mutateAsync(toggleProvider.id)
        toast.success(t('shipping.providerDeactivated', { name: toggleProvider.displayName }))
      } else {
        await activateMutation.mutateAsync(toggleProvider.id)
        toast.success(t('shipping.providerActivated', { name: toggleProvider.displayName }))
      }
    } catch {
      toast.error(t('shipping.toggleFailed'))
    } finally {
      setToggleProvider(null)
    }
  }

  const handleEdit = (provider: ShippingProviderDto) => {
    setEditProvider(provider)
    setFormOpen(true)
  }

  const handleCreate = () => {
    setEditProvider(null)
    setFormOpen(true)
  }

  return (
    <>
      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader>
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="space-y-1">
              <CardTitle>{t('shipping.allProviders', 'All Providers')}</CardTitle>
              <CardDescription>
                {t('shipping.providerCount', {
                  count: providers?.length ?? 0,
                  defaultValue: `${providers?.length ?? 0} providers configured`,
                })}
              </CardDescription>
            </div>
            <div className="flex items-center gap-3">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder={t('shipping.searchProviders', 'Search providers...')}
                  value={searchInput}
                  onChange={(e) => setSearchInput(e.target.value)}
                  className="pl-10 w-full sm:w-48"
                  aria-label={t('shipping.searchProviders', 'Search providers')}
                />
                {searchInput && (
                  <Button
                    variant="ghost"
                    size="icon"
                    className="absolute right-1 top-1/2 -translate-y-1/2 h-6 w-6 cursor-pointer"
                    onClick={() => setSearchInput('')}
                    aria-label={t('labels.clearSearch', 'Clear search')}
                  >
                    <X className="h-3.5 w-3.5" />
                  </Button>
                )}
              </div>
              <Button onClick={handleCreate} className="group shadow-lg hover:shadow-xl transition-all duration-300 cursor-pointer">
                <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
                {t('shipping.addProvider', 'Add Provider')}
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent className={isSearchStale ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
          <div className="rounded-xl border border-border/50 overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('shipping.providerName', 'Provider')}</TableHead>
                  <TableHead>{t('shipping.code', 'Code')}</TableHead>
                  <TableHead>{t('shipping.environment', 'Environment')}</TableHead>
                  <TableHead>{t('labels.status', 'Status')}</TableHead>
                  <TableHead>{t('shipping.health', 'Health')}</TableHead>
                  <TableHead>{t('shipping.features', 'Features')}</TableHead>
                  <TableHead className="text-right">{t('labels.actions', 'Actions')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {isLoading ? (
                  [...Array(4)].map((_, i) => (
                    <TableRow key={i} className="animate-pulse">
                      <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-16" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-20 rounded-full" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-16 rounded-full" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-16 rounded-full" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                      <TableCell className="text-right"><Skeleton className="h-8 w-20 ml-auto" /></TableCell>
                    </TableRow>
                  ))
                ) : filteredProviders.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} className="p-0">
                      <EmptyState
                        icon={Truck}
                        title={t('shipping.noProvidersFound', 'No providers found')}
                        description={t('shipping.noProvidersDescription', 'Configure a shipping provider to start creating shipments.')}
                        className="border-0 rounded-none px-4 py-12"
                      />
                    </TableCell>
                  </TableRow>
                ) : (
                  filteredProviders.map((provider) => (
                    <TableRow key={provider.id} className="group transition-colors hover:bg-muted/50">
                      <TableCell>
                        <div className="flex flex-col">
                          <span className="font-medium text-sm">{provider.displayName}</span>
                          <span className="text-xs text-muted-foreground">{provider.providerName}</span>
                        </div>
                      </TableCell>
                      <TableCell>
                        <span className="font-mono text-sm">{provider.providerCode}</span>
                      </TableCell>
                      <TableCell>
                        <Badge variant={provider.environment === 'Production' ? 'default' : 'secondary'}>
                          {t(`shipping.env.${provider.environment.toLowerCase()}`, provider.environment)}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <Badge variant={provider.isActive ? 'default' : 'outline'} className={provider.isActive ? 'bg-emerald-100 text-emerald-700 border-emerald-200 dark:bg-emerald-900/30 dark:text-emerald-400 dark:border-emerald-800' : ''}>
                          {provider.isActive ? t('labels.active', 'Active') : t('labels.inactive', 'Inactive')}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <Badge variant={getHealthBadgeVariant(provider.healthStatus)} className={getHealthBadgeClass(provider.healthStatus)}>
                          {t(`shipping.healthStatus.${provider.healthStatus.toLowerCase()}`, provider.healthStatus)}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <div className="flex gap-1">
                          {provider.supportsCod && (
                            <Badge variant="outline" className="text-xs">COD</Badge>
                          )}
                          {provider.supportsInsurance && (
                            <Badge variant="outline" className="text-xs">{t('shipping.insurance', 'Insurance')}</Badge>
                          )}
                        </div>
                      </TableCell>
                      <TableCell className="text-right">
                        <div className="flex items-center justify-end gap-1">
                          <Button
                            variant="ghost"
                            size="sm"
                            className="cursor-pointer h-9 w-9 p-0"
                            onClick={() => handleEdit(provider)}
                            aria-label={t('shipping.editProvider', { name: provider.displayName, defaultValue: `Edit ${provider.displayName}` })}
                          >
                            <Pencil className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            className="cursor-pointer h-9 w-9 p-0"
                            onClick={() => setToggleProvider(provider)}
                            aria-label={provider.isActive
                              ? t('shipping.deactivate', { name: provider.displayName, defaultValue: `Deactivate ${provider.displayName}` })
                              : t('shipping.activate', { name: provider.displayName, defaultValue: `Activate ${provider.displayName}` })
                            }
                          >
                            {provider.isActive ? (
                              <XCircle className="h-4 w-4 text-muted-foreground" />
                            ) : (
                              <Check className="h-4 w-4 text-emerald-600" />
                            )}
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>
        </CardContent>
      </Card>

      <ProviderFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        provider={editProvider}
      />

      {/* Toggle active/inactive confirmation */}
      <AlertDialog open={!!toggleProvider} onOpenChange={(open) => !open && setToggleProvider(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <div className="flex items-center gap-3 mb-2">
              <div className="p-2 rounded-xl bg-primary/10 border border-primary/20">
                <Truck className="h-5 w-5 text-primary" />
              </div>
            </div>
            <AlertDialogTitle>
              {toggleProvider?.isActive
                ? t('shipping.confirmDeactivate', 'Deactivate Provider?')
                : t('shipping.confirmActivate', 'Activate Provider?')}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {toggleProvider?.isActive
                ? t('shipping.deactivateDescription', {
                    name: toggleProvider?.displayName,
                    defaultValue: `This will disable "${toggleProvider?.displayName}" from being used in checkout.`,
                  })
                : t('shipping.activateDescription', {
                    name: toggleProvider?.displayName,
                    defaultValue: `This will enable "${toggleProvider?.displayName}" for use in checkout.`,
                  })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel className="cursor-pointer">
              {t('buttons.cancel', 'Cancel')}
            </AlertDialogCancel>
            <AlertDialogAction
              onClick={handleToggleActive}
              className="cursor-pointer"
            >
              {toggleProvider?.isActive
                ? t('shipping.deactivateBtn', 'Deactivate')
                : t('shipping.activateBtn', 'Activate')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  )
}

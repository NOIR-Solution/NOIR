import { useState, useDeferredValue } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  Check,
  Loader2,
  Pencil,
  Plus,
  Search,
  Truck,
  X,
  XCircle,
} from 'lucide-react'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
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
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import { useShippingProvidersQuery } from '@/portal-app/shipping/queries'
import {
  useActivateProviderMutation,
  useDeactivateProviderMutation,
} from '@/portal-app/shipping/queries'
import type { ShippingProviderDto } from '@/types/shipping'
import { ProviderFormDialog } from './ProviderFormDialog'

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
              <Button onClick={handleCreate} className="group transition-all duration-300 cursor-pointer">
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
                  <TableHead className="w-10 sticky left-0 z-10 bg-background"></TableHead>
                  <TableHead>{t('shipping.providerName', 'Provider')}</TableHead>
                  <TableHead>{t('shipping.code', 'Code')}</TableHead>
                  <TableHead>{t('labels.environment', 'Environment')}</TableHead>
                  <TableHead>{t('labels.status', 'Status')}</TableHead>
                  <TableHead>{t('shipping.health', 'Health')}</TableHead>
                  <TableHead>{t('shipping.features', 'Features')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {isLoading ? (
                  [...Array(4)].map((_, i) => (
                    <TableRow key={i} className="animate-pulse">
                      <TableCell className="sticky left-0 z-10 bg-background"><Skeleton className="h-8 w-20" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-16" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-20 rounded-full" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-16 rounded-full" /></TableCell>
                      <TableCell><Skeleton className="h-5 w-16 rounded-full" /></TableCell>
                      <TableCell><Skeleton className="h-4 w-24" /></TableCell>
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
                      <TableCell className="sticky left-0 z-10 bg-background">
                        <div className="flex items-center gap-1">
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
                        <Badge variant="outline" className={getStatusBadgeClasses(provider.environment === 'Production' ? 'green' : 'yellow')}>
                          {t(`shipping.env.${provider.environment.toLowerCase()}`, provider.environment)}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <Badge variant="outline" className={getStatusBadgeClasses(provider.isActive ? 'green' : 'gray')}>
                          {provider.isActive ? t('labels.active', 'Active') : t('labels.inactive', 'Inactive')}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <Badge variant="outline" className={getStatusBadgeClasses(
                          provider.healthStatus === 'Healthy' ? 'green' :
                          provider.healthStatus === 'Degraded' ? 'yellow' :
                          provider.healthStatus === 'Unhealthy' ? 'red' : 'gray'
                        )}>
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
      <Credenza open={!!toggleProvider} onOpenChange={(open) => !open && setToggleProvider(null)}>
        <CredenzaContent>
          <CredenzaHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-primary/10 border border-primary/20">
                <Truck className="h-5 w-5 text-primary" />
              </div>
              <div>
                <CredenzaTitle>
                  {toggleProvider?.isActive
                    ? t('shipping.confirmDeactivate', 'Deactivate Provider?')
                    : t('shipping.confirmActivate', 'Activate Provider?')}
                </CredenzaTitle>
                <CredenzaDescription>
                  {toggleProvider?.isActive
                    ? t('shipping.deactivateDescription', {
                        name: toggleProvider?.displayName,
                        defaultValue: `This will disable "${toggleProvider?.displayName}" from being used in checkout.`,
                      })
                    : t('shipping.activateDescription', {
                        name: toggleProvider?.displayName,
                        defaultValue: `This will enable "${toggleProvider?.displayName}" for use in checkout.`,
                      })}
                </CredenzaDescription>
              </div>
            </div>
          </CredenzaHeader>
          <CredenzaBody />
          <CredenzaFooter>
            <Button variant="outline" onClick={() => setToggleProvider(null)} disabled={activateMutation.isPending || deactivateMutation.isPending} className="cursor-pointer">
              {t('buttons.cancel', 'Cancel')}
            </Button>
            <Button
              onClick={handleToggleActive}
              disabled={activateMutation.isPending || deactivateMutation.isPending}
              className="cursor-pointer"
            >
              {(activateMutation.isPending || deactivateMutation.isPending) && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {toggleProvider?.isActive
                ? t('shipping.deactivateBtn', 'Deactivate')
                : t('shipping.activateBtn', 'Activate')}
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>
    </>
  )
}

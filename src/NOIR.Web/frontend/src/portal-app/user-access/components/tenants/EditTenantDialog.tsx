import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaHeader,
  CredenzaTitle,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@uikit'
import { Settings, Blocks } from 'lucide-react'
import { TenantFormValidated, type UpdateTenantFormData } from './TenantFormValidated'
import { TenantModulesTab } from './TenantModulesTab'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import type { ProvisionTenantRequest } from '@/types'
import { getTenant, updateTenant } from '@/services/tenants'
import { ApiError } from '@/services/apiClient'
import type { TenantListItem, Tenant } from '@/types'

interface EditTenantDialogProps {
  tenant: TenantListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  /** Called after a successful details save. Not needed for modules-only usage. */
  onSuccess?: () => void
  /** Controlled active tab (synced to URL by parent) */
  activeTab?: 'details' | 'modules'
  /** Called when user switches tab */
  onTabChange?: (tab: 'details' | 'modules') => void
}

export const EditTenantDialog = ({ tenant, open, onOpenChange, onSuccess, activeTab = 'details', onTabChange }: EditTenantDialogProps) => {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  const canEditFeatures = hasPermission(Permissions.FeaturesUpdate)
  const [fullTenant, setFullTenant] = useState<Tenant | null>(null)
  const [loading, setLoading] = useState(false)

  // Controlled/uncontrolled tab: use parent's onTabChange if provided, else local state
  const [internalTab, setInternalTab] = useState(activeTab)
  const effectiveTab = onTabChange ? activeTab : internalTab
  const handleTabChange = (tab: 'details' | 'modules') => {
    onTabChange ? onTabChange(tab) : setInternalTab(tab)
  }

  // Sync internal tab when activeTab prop changes (e.g., dialog re-opens with different tab)
  useEffect(() => {
    if (!onTabChange) setInternalTab(activeTab)
  }, [activeTab, onTabChange])

  // Fetch full tenant data when dialog opens
  useEffect(() => {
    if (open && tenant) {
      const fetchTenant = async () => {
        setLoading(true)
        try {
          const data = await getTenant(tenant.id)
          setFullTenant(data)
        } catch (err) {
          const message = err instanceof ApiError ? err.message : 'Failed to load tenant'
          toast.error(message)
          // Only close if on details tab; modules tab works without fullTenant
          if (effectiveTab === 'details') {
            onOpenChange(false)
          }
        } finally {
          setLoading(false)
        }
      }
      fetchTenant()
    } else {
      setFullTenant(null)
    }
  }, [open, tenant, onOpenChange])

  const handleSubmit = async (data: ProvisionTenantRequest | UpdateTenantFormData) => {
    if (!tenant) return
    const updateData = data as UpdateTenantFormData
    await updateTenant(tenant.id, {
      identifier: updateData.identifier,
      name: updateData.name,
      description: updateData.description || undefined,
      note: updateData.note || undefined,
      isActive: updateData.isActive ?? true,
    })
    toast.success(t('messages.updateSuccess'))
    onOpenChange(false)
    onSuccess?.()
  }

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[650px]">
        <CredenzaHeader>
          <CredenzaTitle>{tenant?.name || tenant?.identifier || t('tenants.editTitle')}</CredenzaTitle>
          <CredenzaDescription>{t('tenants.editDescription')}</CredenzaDescription>
        </CredenzaHeader>
        <CredenzaBody>
          <Tabs value={effectiveTab} onValueChange={(v) => handleTabChange(v as 'details' | 'modules')}>
            <TabsList className="w-full">
              <TabsTrigger value="details" className="cursor-pointer flex-1 gap-1.5">
                <Settings className="h-3.5 w-3.5" />
                {t('tenants.tabs.details')}
              </TabsTrigger>
              <TabsTrigger value="modules" className="cursor-pointer flex-1 gap-1.5">
                <Blocks className="h-3.5 w-3.5" />
                {t('tenants.tabs.modules')}
              </TabsTrigger>
            </TabsList>

            <TabsContent value="details" className="mt-4">
              {loading ? (
                <div className="space-y-4">
                  <div className="space-y-2">
                    <div className="h-4 w-20 bg-muted animate-pulse rounded" />
                    <div className="h-10 w-full bg-muted animate-pulse rounded-md" />
                    <div className="h-3 w-48 bg-muted animate-pulse rounded" />
                  </div>
                  <div className="space-y-2">
                    <div className="h-4 w-16 bg-muted animate-pulse rounded" />
                    <div className="h-10 w-full bg-muted animate-pulse rounded-md" />
                  </div>
                  <div className="flex items-center space-x-2">
                    <div className="h-4 w-4 bg-muted animate-pulse rounded" />
                    <div className="h-4 w-12 bg-muted animate-pulse rounded" />
                  </div>
                  <div className="flex flex-col-reverse gap-2 pt-4 sm:flex-row sm:justify-end">
                    <div className="h-10 w-full sm:w-20 bg-muted animate-pulse rounded-md" />
                    <div className="h-10 w-full sm:w-20 bg-muted animate-pulse rounded-md" />
                  </div>
                </div>
              ) : fullTenant ? (
                <TenantFormValidated
                  tenant={fullTenant}
                  onSubmit={handleSubmit}
                  onCancel={() => onOpenChange(false)}
                />
              ) : null}
            </TabsContent>

            <TabsContent value="modules" className="mt-4">
              {tenant && (
                <TenantModulesTab
                  tenantId={tenant.id}
                  canEdit={canEditFeatures}
                  compact
                />
              )}
            </TabsContent>
          </Tabs>
        </CredenzaBody>
      </CredenzaContent>
    </Credenza>
  )
}

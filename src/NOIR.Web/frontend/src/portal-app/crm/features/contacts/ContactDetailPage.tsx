import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams, useNavigate } from 'react-router-dom'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { EntityConflictDialog } from '@/components/EntityConflictDialog'
import { EntityDeletedDialog } from '@/components/EntityDeletedDialog'
import {
  ArrowLeft,
  Building2,
  Contact,
  ExternalLink,
  Mail,
  Pencil,
  Phone,
  Briefcase,
  User,
} from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import { useUrlTab } from '@/hooks/useUrlTab'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  EmptyState,
  Separator,
  Skeleton,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@uikit'
import { useContactQuery, useLeadsQuery } from '@/portal-app/crm/queries'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import { ContactDialog } from './components/ContactDialog'
import { ActivityTimeline } from '../../components/ActivityTimeline'

export const ContactDetailPage = () => {
  const { t } = useTranslation('common')
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  const { formatDateTime } = useRegionalSettings()
  usePageContext('CRM Contacts')

  const canUpdate = hasPermission(Permissions.CrmContactsUpdate)

  const { data: contact, isLoading, error: queryError, refetch } = useContactQuery(id)
  const { data: leadsResponse } = useLeadsQuery({ pageSize: 50 })
  const contactLeads = (leadsResponse?.items ?? []).filter(l => l.contactId === id)
  const { activeTab, handleTabChange, isPending: isTabPending } = useUrlTab({ defaultTab: 'overview' })

  const { conflictSignal, deletedSignal, dismissConflict, reloadAndRestart, isReconnecting } = useEntityUpdateSignal({
    entityType: 'CrmContact',
    entityId: id,
    onAutoReload: refetch,
    onNavigateAway: () => navigate('/portal/crm/contacts'),
  })

  const [showEditDialog, setShowEditDialog] = useState(false)

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-64 w-full" />
      </div>
    )
  }

  if (queryError || !contact) {
    return (
      <EmptyState
        icon={Contact}
        title={t('errors.notFound')}
        description={t('errors.resourceNotFound')}
      />
    )
  }

  const getLeadStatusColor = (status: string) => {
    const colorMap: Record<string, 'blue' | 'green' | 'red' | 'gray'> = { Active: 'blue', Won: 'green', Lost: 'red' }
    return getStatusBadgeClasses(colorMap[status] || 'gray')
  }

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <EntityConflictDialog signal={conflictSignal} onContinueEditing={dismissConflict} onReloadAndRestart={reloadAndRestart} />
      <EntityDeletedDialog signal={deletedSignal} onGoBack={() => navigate('/portal/crm/contacts')} />

      <div className="flex items-center gap-4">
        <Button
          variant="ghost"
          size="icon"
          onClick={() => navigate('/portal/crm/contacts')}
          aria-label={t('labels.back')}
          className="cursor-pointer"
        >
          <ArrowLeft className="h-5 w-5" />
        </Button>
        <div className="flex-1">
          <h1 className="text-2xl font-bold tracking-tight">{contact.firstName} {contact.lastName}</h1>
          <p className="text-muted-foreground">{contact.email}</p>
        </div>
        {canUpdate && (
          <Button variant="outline" onClick={() => setShowEditDialog(true)} className="cursor-pointer">
            <Pencil className="h-4 w-4 mr-2" />
            {t('labels.edit')}
          </Button>
        )}
      </div>

      <Tabs value={activeTab} onValueChange={handleTabChange}>
        <TabsList>
          <TabsTrigger value="overview" className="cursor-pointer">{t('labels.overview')}</TabsTrigger>
          <TabsTrigger value="deals" className="cursor-pointer">{t('crm.leads.title')}</TabsTrigger>
          <TabsTrigger value="activities" className="cursor-pointer">{t('crm.activities.title')}</TabsTrigger>
        </TabsList>

        <div className={isTabPending ? 'opacity-70 transition-opacity' : 'transition-opacity'}>
          <TabsContent value="overview" className="space-y-6 mt-6">
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <Card className="shadow-sm">
                <CardHeader>
                  <CardTitle className="text-base">{t('labels.details')}</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="flex items-center gap-3">
                    <Mail className="h-4 w-4 text-muted-foreground" />
                    <span className="text-sm">{contact.email}</span>
                  </div>
                  {contact.phone && (
                    <div className="flex items-center gap-3">
                      <Phone className="h-4 w-4 text-muted-foreground" />
                      <span className="text-sm">{contact.phone}</span>
                    </div>
                  )}
                  {contact.jobTitle && (
                    <div className="flex items-center gap-3">
                      <Briefcase className="h-4 w-4 text-muted-foreground" />
                      <span className="text-sm">{contact.jobTitle}</span>
                    </div>
                  )}
                  {contact.companyName && (
                    <div className="flex items-center gap-3">
                      <Building2 className="h-4 w-4 text-muted-foreground" />
                      <Button
                        variant="link"
                        className="p-0 h-auto text-sm cursor-pointer"
                        onClick={() => navigate(`/portal/crm/companies/${contact.companyId}`)}
                      >
                        {contact.companyName}
                        <ExternalLink className="h-3 w-3 ml-1" />
                      </Button>
                    </div>
                  )}
                  {contact.ownerName && (
                    <div className="flex items-center gap-3">
                      <User className="h-4 w-4 text-muted-foreground" />
                      <span className="text-sm">{contact.ownerName}</span>
                    </div>
                  )}
                  <Separator />
                  <div className="flex items-center gap-3">
                    <span className="text-sm text-muted-foreground">{t('crm.contacts.source')}:</span>
                    <Badge variant="outline">{t(`crm.sources.${contact.source}`)}</Badge>
                  </div>
                  {contact.customerId && (
                    <div className="flex items-center gap-3">
                      <span className="text-sm text-muted-foreground">{t('crm.contacts.linkedCustomer')}:</span>
                      <Button
                        variant="link"
                        className="p-0 h-auto text-sm cursor-pointer"
                        onClick={() => navigate(`/portal/ecommerce/customers/${contact.customerId}`)}
                      >
                        {t('crm.contacts.viewCustomer')}
                        <ExternalLink className="h-3 w-3 ml-1" />
                      </Button>
                    </div>
                  )}
                  <div className="text-xs text-muted-foreground">
                    {t('labels.created')}: {formatDateTime(contact.createdAt)}
                  </div>
                </CardContent>
              </Card>

              {contact.notes && (
                <Card className="shadow-sm">
                  <CardHeader>
                    <CardTitle className="text-base">{t('crm.contacts.notes')}</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <p className="text-sm whitespace-pre-wrap">{contact.notes}</p>
                  </CardContent>
                </Card>
              )}
            </div>
          </TabsContent>

          <TabsContent value="deals" className="mt-6">
            <Card className="shadow-sm">
              <CardHeader>
                <CardTitle className="text-base">{t('crm.leads.title')} ({contactLeads.length})</CardTitle>
              </CardHeader>
              <CardContent>
                {contactLeads.length === 0 ? (
                  <EmptyState
                    icon={Contact}
                    title={t('crm.leads.noLeadsFound')}
                    description={t('crm.leads.noLeadsDescription')}
                    className="border-0 py-8"
                  />
                ) : (
                  <div className="space-y-3">
                    {contactLeads.map((lead) => (
                      <div
                        key={lead.id}
                        className="flex items-center justify-between p-3 rounded-lg border hover:bg-muted/50 cursor-pointer transition-colors"
                        onClick={() => navigate(`/portal/crm/pipeline/deals/${lead.id}`)}
                      >
                        <div>
                          <p className="font-medium text-sm">{lead.title}</p>
                          <p className="text-xs text-muted-foreground">{lead.stageName}</p>
                        </div>
                        <div className="flex items-center gap-2">
                          <span className="text-sm font-medium">
                            {new Intl.NumberFormat(undefined, { style: 'currency', currency: lead.currency }).format(lead.value)}
                          </span>
                          <Badge variant="outline" className={getLeadStatusColor(lead.status)}>
                            {t(`crm.statuses.${lead.status}`)}
                          </Badge>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="activities" className="mt-6">
            <ActivityTimeline contactId={id} />
          </TabsContent>
        </div>
      </Tabs>

      <ContactDialog
        open={showEditDialog}
        onOpenChange={setShowEditDialog}
        contact={contact}
      />
    </div>
  )
}

export default ContactDetailPage

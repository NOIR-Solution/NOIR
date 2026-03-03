import { useTranslation } from 'react-i18next'
import { useParams, useNavigate } from 'react-router-dom'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { EntityConflictDialog } from '@/components/EntityConflictDialog'
import { EntityDeletedDialog } from '@/components/EntityDeletedDialog'
import {
  ArrowLeft,
  Building2,
  Calendar,
  Contact,
  DollarSign,
  ExternalLink,
  Kanban,
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
import { useLeadQuery } from '@/portal-app/crm/queries'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { getStatusBadgeClasses } from '@/utils/statusBadge'
import { LeadStatusActions } from './components/LeadStatusActions'
import { ActivityTimeline } from '../../components/ActivityTimeline'

export const DealDetailPage = () => {
  const { t } = useTranslation('common')
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  const { formatDateTime, formatDate } = useRegionalSettings()
  usePageContext('CRM Pipeline')

  const canManage = hasPermission(Permissions.CrmLeadsManage)

  const { data: lead, isLoading, error: queryError, refetch } = useLeadQuery(id)
  const { activeTab, handleTabChange, isPending: isTabPending } = useUrlTab({ defaultTab: 'overview' })

  const { conflictSignal, deletedSignal, dismissConflict, reloadAndRestart, isReconnecting } = useEntityUpdateSignal({
    entityType: 'Lead',
    entityId: id,
    onAutoReload: refetch,
    onNavigateAway: () => navigate('/portal/crm/pipeline'),
  })

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-64 w-full" />
      </div>
    )
  }

  if (queryError || !lead) {
    return (
      <EmptyState
        icon={Kanban}
        title={t('errors.notFound')}
        description={t('errors.resourceNotFound')}
      />
    )
  }

  const getStatusColor = (status: string) => {
    const colorMap: Record<string, 'blue' | 'green' | 'red' | 'gray'> = { Active: 'blue', Won: 'green', Lost: 'red' }
    return getStatusBadgeClasses(colorMap[status] || 'gray')
  }

  const formattedValue = new Intl.NumberFormat(undefined, {
    style: 'currency',
    currency: lead.currency || 'USD',
  }).format(lead.value)

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <EntityConflictDialog signal={conflictSignal} onContinueEditing={dismissConflict} onReloadAndRestart={reloadAndRestart} />
      <EntityDeletedDialog signal={deletedSignal} onGoBack={() => navigate('/portal/crm/pipeline')} />

      <div className="flex items-center gap-4">
        <Button
          variant="ghost"
          size="icon"
          onClick={() => navigate('/portal/crm/pipeline')}
          aria-label={t('labels.back')}
          className="cursor-pointer"
        >
          <ArrowLeft className="h-5 w-5" />
        </Button>
        <div className="flex-1">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold tracking-tight">{lead.title}</h1>
            <Badge variant="outline" className={getStatusColor(lead.status)}>
              {t(`crm.statuses.${lead.status}`)}
            </Badge>
          </div>
          <p className="text-muted-foreground">
            {lead.pipelineName} &middot; {lead.stageName}
          </p>
        </div>
        {canManage && <LeadStatusActions leadId={lead.id} status={lead.status} />}
      </div>

      <Tabs value={activeTab} onValueChange={handleTabChange}>
        <TabsList>
          <TabsTrigger value="overview" className="cursor-pointer">{t('labels.overview')}</TabsTrigger>
          <TabsTrigger value="activities" className="cursor-pointer">{t('crm.activities.title')}</TabsTrigger>
        </TabsList>

        <div className={isTabPending ? 'opacity-70 transition-opacity' : 'transition-opacity'}>
          <TabsContent value="overview" className="space-y-6 mt-6">
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {/* Deal Info */}
              <Card className="shadow-sm">
                <CardHeader>
                  <CardTitle className="text-base">{t('crm.leads.title')}</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="flex items-center gap-3">
                    <DollarSign className="h-4 w-4 text-muted-foreground" />
                    <span className="text-lg font-bold">{formattedValue}</span>
                  </div>

                  <div className="flex items-center gap-3">
                    <div className="h-3 w-3 rounded-full" style={{ backgroundColor: lead.stageColor }} />
                    <span className="text-sm">{lead.stageName}</span>
                  </div>

                  {lead.expectedCloseDate && (
                    <div className="flex items-center gap-3">
                      <Calendar className="h-4 w-4 text-muted-foreground" />
                      <span className="text-sm">{formatDate(lead.expectedCloseDate)}</span>
                    </div>
                  )}

                  {lead.ownerName && (
                    <div className="flex items-center gap-3">
                      <User className="h-4 w-4 text-muted-foreground" />
                      <span className="text-sm">{lead.ownerName}</span>
                    </div>
                  )}

                  <Separator />

                  {/* Contact link */}
                  <div className="flex items-center gap-3">
                    <Contact className="h-4 w-4 text-muted-foreground" />
                    <Button
                      variant="link"
                      className="p-0 h-auto text-sm cursor-pointer"
                      onClick={() => navigate(`/portal/crm/contacts/${lead.contactId}`)}
                    >
                      {lead.contactName}
                      <ExternalLink className="h-3 w-3 ml-1" />
                    </Button>
                  </div>

                  {/* Company link */}
                  {lead.companyName && (
                    <div className="flex items-center gap-3">
                      <Building2 className="h-4 w-4 text-muted-foreground" />
                      <Button
                        variant="link"
                        className="p-0 h-auto text-sm cursor-pointer"
                        onClick={() => navigate(`/portal/crm/companies/${lead.companyId}`)}
                      >
                        {lead.companyName}
                        <ExternalLink className="h-3 w-3 ml-1" />
                      </Button>
                    </div>
                  )}

                  {lead.wonAt && (
                    <div className="text-sm text-green-600 dark:text-green-400">
                      {t('crm.leads.wonAt')}: {formatDateTime(lead.wonAt)}
                    </div>
                  )}
                  {lead.lostAt && (
                    <div className="text-sm text-red-600 dark:text-red-400">
                      {t('crm.leads.lostAt')}: {formatDateTime(lead.lostAt)}
                      {lead.lostReason && (
                        <p className="text-xs mt-1">{t('crm.leads.lostReason')}: {lead.lostReason}</p>
                      )}
                    </div>
                  )}

                  <div className="text-xs text-muted-foreground">
                    {t('labels.created')}: {formatDateTime(lead.createdAt)}
                  </div>
                </CardContent>
              </Card>

              {/* Notes */}
              {lead.notes && (
                <Card className="shadow-sm">
                  <CardHeader>
                    <CardTitle className="text-base">{t('crm.leads.notes')}</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <p className="text-sm whitespace-pre-wrap">{lead.notes}</p>
                  </CardContent>
                </Card>
              )}
            </div>
          </TabsContent>

          <TabsContent value="activities" className="mt-6">
            <ActivityTimeline leadId={id} />
          </TabsContent>
        </div>
      </Tabs>
    </div>
  )
}

export default DealDetailPage

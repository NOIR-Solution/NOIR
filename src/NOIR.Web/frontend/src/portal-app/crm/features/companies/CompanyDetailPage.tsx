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
  Globe,
  Mail,
  MapPin,
  Pencil,
  Phone,
  User,
} from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
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
} from '@uikit'
import { useCompanyQuery, useContactsQuery } from '@/portal-app/crm/queries'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { CompanyDialog } from './components/CompanyDialog'

export const CompanyDetailPage = () => {
  const { t } = useTranslation('common')
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  const { formatDateTime } = useRegionalSettings()
  usePageContext('CRM Companies')

  const canUpdate = hasPermission(Permissions.CrmCompaniesUpdate)

  const { data: company, isLoading, error: queryError, refetch } = useCompanyQuery(id)
  const { data: contactsResponse } = useContactsQuery({ companyId: id, pageSize: 50 })
  const contacts = contactsResponse?.items ?? []

  const { conflictSignal, deletedSignal, dismissConflict, reloadAndRestart, isReconnecting } = useEntityUpdateSignal({
    entityType: 'CrmCompany',
    entityId: id,
    onAutoReload: refetch,
    onNavigateAway: () => navigate('/portal/crm/companies'),
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

  if (queryError || !company) {
    return (
      <EmptyState
        icon={Building2}
        title={t('errors.notFound')}
        description={t('errors.resourceNotFound')}
      />
    )
  }

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <EntityConflictDialog signal={conflictSignal} onContinueEditing={dismissConflict} onReloadAndRestart={reloadAndRestart} />
      <EntityDeletedDialog signal={deletedSignal} onGoBack={() => navigate('/portal/crm/companies')} />

      <div className="flex items-center gap-4">
        <Button
          variant="ghost"
          size="icon"
          onClick={() => navigate('/portal/crm/companies')}
          aria-label={t('labels.back')}
          className="cursor-pointer"
        >
          <ArrowLeft className="h-5 w-5" />
        </Button>
        <div className="flex-1">
          <h1 className="text-2xl font-bold tracking-tight">{company.name}</h1>
          {company.industry && <p className="text-muted-foreground">{company.industry}</p>}
        </div>
        {canUpdate && (
          <Button variant="outline" onClick={() => setShowEditDialog(true)} className="cursor-pointer">
            <Pencil className="h-4 w-4 mr-2" />
            {t('labels.edit')}
          </Button>
        )}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card className="shadow-sm">
          <CardHeader>
            <CardTitle className="text-base">{t('labels.details')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {company.domain && (
              <div className="flex items-center gap-3">
                <Globe className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm">{company.domain}</span>
              </div>
            )}
            {company.phone && (
              <div className="flex items-center gap-3">
                <Phone className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm">{company.phone}</span>
              </div>
            )}
            {company.website && (
              <div className="flex items-center gap-3">
                <ExternalLink className="h-4 w-4 text-muted-foreground" />
                <a href={company.website} target="_blank" rel="noopener noreferrer" className="text-sm text-primary hover:underline">
                  {company.website}
                </a>
              </div>
            )}
            {company.address && (
              <div className="flex items-center gap-3">
                <MapPin className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm">{company.address}</span>
              </div>
            )}
            {company.ownerName && (
              <div className="flex items-center gap-3">
                <User className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm">{company.ownerName}</span>
              </div>
            )}
            <Separator />
            {company.taxId && (
              <div className="flex items-center gap-3">
                <span className="text-sm text-muted-foreground">{t('crm.companies.taxId')}:</span>
                <span className="text-sm">{company.taxId}</span>
              </div>
            )}
            {company.employeeCount != null && (
              <div className="flex items-center gap-3">
                <span className="text-sm text-muted-foreground">{t('crm.companies.employeeCount')}:</span>
                <span className="text-sm">{company.employeeCount}</span>
              </div>
            )}
            <div className="text-xs text-muted-foreground">
              {t('labels.created')}: {formatDateTime(company.createdAt)}
            </div>
          </CardContent>
        </Card>

        {company.notes && (
          <Card className="shadow-sm">
            <CardHeader>
              <CardTitle className="text-base">{t('crm.companies.notes')}</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm whitespace-pre-wrap">{company.notes}</p>
            </CardContent>
          </Card>
        )}
      </div>

      {/* Contacts list */}
      <Card className="shadow-sm">
        <CardHeader>
          <CardTitle className="text-base">{t('crm.contacts.title')} ({contacts.length})</CardTitle>
        </CardHeader>
        <CardContent>
          {contacts.length === 0 ? (
            <EmptyState
              icon={Contact}
              title={t('crm.contacts.noContactsFound')}
              description={t('crm.contacts.noContactsDescription')}
              className="border-0 py-8"
            />
          ) : (
            <div className="space-y-3">
              {contacts.map((contact) => (
                <div
                  key={contact.id}
                  className="flex items-center justify-between p-3 rounded-lg border hover:bg-muted/50 cursor-pointer transition-colors"
                  onClick={() => navigate(`/portal/crm/contacts/${contact.id}`)}
                >
                  <div className="flex items-center gap-3">
                    <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center">
                      <Mail className="h-4 w-4 text-primary" />
                    </div>
                    <div>
                      <p className="font-medium text-sm">{contact.firstName} {contact.lastName}</p>
                      <p className="text-xs text-muted-foreground">{contact.email}</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    {contact.jobTitle && <span className="text-xs text-muted-foreground">{contact.jobTitle}</span>}
                    <Badge variant="secondary">{contact.leadCount} {t('crm.contacts.leadsCount').toLowerCase()}</Badge>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <CompanyDialog
        open={showEditDialog}
        onOpenChange={setShowEditDialog}
        company={company}
      />
    </div>
  )
}

export default CompanyDetailPage

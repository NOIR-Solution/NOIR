import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { useUrlTab } from '@/hooks/useUrlTab'
import { Settings, Mail, FileText, Scale, Blocks } from 'lucide-react'
import { PageHeader, Tabs, TabsContent, TabsList, TabsTrigger } from '@uikit'

import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { usePageContext } from '@/hooks/usePageContext'
import {
  PlatformSmtpSettingsTab,
  PlatformEmailTemplatesTab,
  PlatformLegalPagesTab,
  PlatformModulesOverviewTab,
} from '../../components/platform-settings'

/**
 * Platform Settings Page
 * Tabbed layout for managing platform-wide SMTP, email templates, and legal pages.
 */
export const PlatformSettingsPage = () => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  const canEdit = hasPermission(Permissions.PlatformSettingsManage)
  usePageContext('PlatformSettings')

  const { activeTab, handleTabChange, isPending: isTabPending } = useUrlTab({ defaultTab: 'smtp' })

  return (
    <div className={`container py-6 space-y-6 ${activeTab === 'modules' ? 'max-w-full' : 'max-w-4xl'}`}>
      <PageHeader
        icon={Settings}
        title={t('platformSettings.title')}
        description={t('platformSettings.description')}
      />

      {/* Tabs */}
      <Tabs value={activeTab} onValueChange={handleTabChange} className={isTabPending ? 'space-y-4 opacity-70 transition-opacity duration-200' : 'space-y-4 transition-opacity duration-200'}>
        <TabsList>
          <TabsTrigger value="smtp" className="cursor-pointer">
            <Mail className="h-4 w-4 mr-2" />
            {t('platformSettings.tabs.smtp')}
          </TabsTrigger>
          <TabsTrigger value="emailTemplates" className="cursor-pointer">
            <FileText className="h-4 w-4 mr-2" />
            {t('platformSettings.tabs.emailTemplates')}
          </TabsTrigger>
          <TabsTrigger value="legalPages" className="cursor-pointer">
            <Scale className="h-4 w-4 mr-2" />
            {t('platformSettings.tabs.legalPages')}
          </TabsTrigger>
          <TabsTrigger value="modules" className="cursor-pointer">
            <Blocks className="h-4 w-4 mr-2" />
            {t('platformSettings.tabs.modules')}
          </TabsTrigger>
        </TabsList>

        <TabsContent value="smtp">
          <PlatformSmtpSettingsTab canEdit={canEdit} />
        </TabsContent>

        <TabsContent value="emailTemplates">
          <PlatformEmailTemplatesTab onEdit={(id) => navigate(`/portal/email-templates/${id}?from=platform`)} />
        </TabsContent>

        <TabsContent value="legalPages">
          <PlatformLegalPagesTab onEdit={(id) => navigate(`/portal/legal-pages/${id}?from=platform`)} />
        </TabsContent>

        <TabsContent value="modules">
          <PlatformModulesOverviewTab />
        </TabsContent>
      </Tabs>
    </div>
  )
}

export default PlatformSettingsPage

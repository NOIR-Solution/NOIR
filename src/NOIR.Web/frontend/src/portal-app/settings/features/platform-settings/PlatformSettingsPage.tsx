import { useState, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { Settings, Mail, FileText, Scale } from 'lucide-react'
import { PageHeader, Tabs, TabsContent, TabsList, TabsTrigger } from '@uikit'

import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { usePageContext } from '@/hooks/usePageContext'
import {
  PlatformSmtpSettingsTab,
  PlatformEmailTemplatesTab,
  PlatformLegalPagesTab,
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

  // Active tab state
  const [activeTab, setActiveTab] = useState('smtp')
  const [isTabPending, startTabTransition] = useTransition()

  const handleTabChange = (tab: string) => {
    startTabTransition(() => {
      setActiveTab(tab)
    })
  }

  return (
    <div className="container max-w-4xl py-6 space-y-6">
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
      </Tabs>
    </div>
  )
}

export default PlatformSettingsPage

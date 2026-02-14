import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { Settings, Mail, FileText, Scale } from 'lucide-react'
import { PageHeader, Tabs, TabsContent, TabsList, TabsTrigger } from '@uikit'

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
const PlatformSettingsPage = () => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  usePageContext('PlatformSettings')

  // Active tab state
  const [activeTab, setActiveTab] = useState('smtp')

  return (
    <div className="container max-w-4xl py-6 space-y-6">
      <PageHeader
        icon={Settings}
        title={t('platformSettings.title')}
        description={t('platformSettings.description')}
      />

      {/* Tabs */}
      <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-4">
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
          <PlatformSmtpSettingsTab />
        </TabsContent>

        <TabsContent value="emailTemplates">
          <PlatformEmailTemplatesTab onEdit={(id) => navigate(`/portal/email-templates/${id}`)} />
        </TabsContent>

        <TabsContent value="legalPages">
          <PlatformLegalPagesTab onEdit={(id) => navigate(`/portal/legal-pages/${id}`)} />
        </TabsContent>
      </Tabs>
    </div>
  )
}

export default PlatformSettingsPage

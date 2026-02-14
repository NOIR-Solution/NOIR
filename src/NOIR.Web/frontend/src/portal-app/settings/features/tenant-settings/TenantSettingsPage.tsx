import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useSearchParams } from 'react-router-dom'
import {
  Settings,
  Palette,
  Phone,
  Globe,
  Mail,
  FileText,
  Scale,
  CreditCard,
} from 'lucide-react'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { usePageContext } from '@/hooks/usePageContext'
import { PageHeader, Tabs, TabsContent, TabsList, TabsTrigger } from '@uikit'

import {
  BrandingSettingsTab,
  ContactSettingsTab,
  RegionalSettingsTab,
  SmtpSettingsTab,
  EmailTemplatesTab,
  LegalPagesTab,
  PaymentGatewaysTab,
} from '../../components/tenant-settings'

/**
 * Tenant Settings Page
 * Tabbed layout for managing branding, contact, regional, SMTP, email templates, and legal pages.
 */
export const TenantSettingsPage = () => {
  usePageContext('Tenant Settings')
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  const canEdit = hasPermission(Permissions.TenantSettingsUpdate)
  const [searchParams, setSearchParams] = useSearchParams()

  const [activeTab, setActiveTab] = useState(() => searchParams.get('tab') || 'branding')

  const handleTabChange = (tab: string) => {
    setActiveTab(tab)
    setSearchParams({ tab }, { replace: true })
  }

  return (
    <div className="container max-w-4xl py-6 space-y-6">
      <PageHeader
        icon={Settings}
        title={t('tenantSettings.title')}
        description={t('tenantSettings.description')}
      />

      {/* Tabbed Content */}
      <Tabs value={activeTab} onValueChange={handleTabChange} className="space-y-4">
        <TabsList className="flex-wrap h-auto">
          {/* Core Identity & Business Info */}
          <TabsTrigger value="branding" className="cursor-pointer">
            <Palette className="h-4 w-4 mr-2" />
            {t('tenantSettings.tabs.branding')}
          </TabsTrigger>
          <TabsTrigger value="contact" className="cursor-pointer">
            <Phone className="h-4 w-4 mr-2" />
            {t('tenantSettings.tabs.contact')}
          </TabsTrigger>
          <TabsTrigger value="regional" className="cursor-pointer">
            <Globe className="h-4 w-4 mr-2" />
            {t('tenantSettings.tabs.regional')}
          </TabsTrigger>
          {/* Business Operations */}
          <TabsTrigger value="paymentGateways" className="cursor-pointer">
            <CreditCard className="h-4 w-4 mr-2" />
            {t('tenantSettings.tabs.paymentGateways')}
          </TabsTrigger>
          {/* Communication Stack (SMTP before Templates - dependency order) */}
          <TabsTrigger value="smtp" className="cursor-pointer">
            <Mail className="h-4 w-4 mr-2" />
            {t('tenantSettings.tabs.smtp')}
          </TabsTrigger>
          <TabsTrigger value="emailTemplates" className="cursor-pointer">
            <FileText className="h-4 w-4 mr-2" />
            {t('tenantSettings.tabs.emailTemplates')}
          </TabsTrigger>
          {/* Compliance */}
          <TabsTrigger value="legalPages" className="cursor-pointer">
            <Scale className="h-4 w-4 mr-2" />
            {t('tenantSettings.tabs.legalPages')}
          </TabsTrigger>
        </TabsList>

        {/* Core Identity & Business Info */}
        <TabsContent value="branding">
          <BrandingSettingsTab canEdit={canEdit} />
        </TabsContent>
        <TabsContent value="contact">
          <ContactSettingsTab canEdit={canEdit} />
        </TabsContent>
        <TabsContent value="regional">
          <RegionalSettingsTab canEdit={canEdit} />
        </TabsContent>
        {/* Business Operations */}
        <TabsContent value="paymentGateways">
          <PaymentGatewaysTab />
        </TabsContent>
        {/* Communication Stack */}
        <TabsContent value="smtp">
          <SmtpSettingsTab canEdit={canEdit} />
        </TabsContent>
        <TabsContent value="emailTemplates">
          <EmailTemplatesTab onEdit={(id) => navigate(`/portal/email-templates/${id}`)} />
        </TabsContent>
        {/* Compliance */}
        <TabsContent value="legalPages">
          <LegalPagesTab onEdit={(id) => navigate(`/portal/legal-pages/${id}`)} />
        </TabsContent>
      </Tabs>
    </div>
  )
}

export default TenantSettingsPage

import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { useUrlTab } from '@/hooks/useUrlTab'
import {
  Settings,
  Palette,
  Phone,
  Globe,
  Mail,
  FileText,
  Scale,
  CreditCard,
  Truck,
  Blocks,
  Webhook,
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
  ShippingProvidersTab,
  ModulesSettingsTab,
  WebhooksSettingsTab,
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
  const { activeTab, handleTabChange, isPending: isTabPending } = useUrlTab({ defaultTab: 'branding' })

  return (
    <div className="container max-w-7xl py-6 space-y-6">
      <PageHeader
        icon={Settings}
        title={t('tenantSettings.title')}
        description={t('tenantSettings.description')}
      />

      {/* Tabbed Content */}
      <Tabs value={activeTab} onValueChange={handleTabChange} className={isTabPending ? 'space-y-4 opacity-70 transition-opacity duration-200' : 'space-y-4 transition-opacity duration-200'}>
        <TabsList className="w-full h-auto justify-start overflow-x-auto flex-nowrap">
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
          <TabsTrigger value="shippingProviders" className="cursor-pointer">
            <Truck className="h-4 w-4 mr-2" />
            {t('tenantSettings.tabs.shippingProviders')}
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
          {/* Modules */}
          <TabsTrigger value="modules" className="cursor-pointer">
            <Blocks className="h-4 w-4 mr-2" />
            {t('tenantSettings.tabs.modules')}
          </TabsTrigger>
          {/* Integrations */}
          <TabsTrigger value="webhooks" className="cursor-pointer">
            <Webhook className="h-4 w-4 mr-2" />
            {t('tenantSettings.tabs.webhooks')}
          </TabsTrigger>
        </TabsList>

        {/* Core Identity & Business Info — form-based, narrow */}
        <TabsContent value="branding" className="max-w-3xl">
          <BrandingSettingsTab canEdit={canEdit} />
        </TabsContent>
        <TabsContent value="contact" className="max-w-3xl">
          <ContactSettingsTab canEdit={canEdit} />
        </TabsContent>
        <TabsContent value="regional" className="max-w-3xl">
          <RegionalSettingsTab canEdit={canEdit} />
        </TabsContent>
        {/* Business Operations — card grids, wider */}
        <TabsContent value="paymentGateways">
          <PaymentGatewaysTab />
        </TabsContent>
        <TabsContent value="shippingProviders">
          <ShippingProvidersTab />
        </TabsContent>
        {/* Communication Stack — SMTP is form (narrow), templates are card grid (medium) */}
        <TabsContent value="smtp" className="max-w-3xl">
          <SmtpSettingsTab canEdit={canEdit} />
        </TabsContent>
        <TabsContent value="emailTemplates" className="max-w-5xl">
          <EmailTemplatesTab onEdit={(id) => navigate(`/portal/email-templates/${id}?from=tenant`)} />
        </TabsContent>
        {/* Compliance — card grid, medium */}
        <TabsContent value="legalPages" className="max-w-5xl">
          <LegalPagesTab onEdit={(id) => navigate(`/portal/legal-pages/${id}?from=tenant`)} />
        </TabsContent>
        {/* Modules — toggle list, medium-narrow */}
        <TabsContent value="modules" className="max-w-4xl">
          <ModulesSettingsTab canEdit={canEdit} />
        </TabsContent>
        {/* Integrations — table, full width */}
        <TabsContent value="webhooks">
          <WebhooksSettingsTab canEdit={canEdit} />
        </TabsContent>
      </Tabs>
    </div>
  )
}

export default TenantSettingsPage

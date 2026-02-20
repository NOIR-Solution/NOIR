import { useState, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { Truck } from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import {
  PageHeader,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@uikit'
import { ProviderList } from '@/portal-app/shipping/components/ProviderList'
import { ShipmentLookup } from '@/portal-app/shipping/components/ShipmentLookup'

export const ShippingPage = () => {
  const { t } = useTranslation('common')
  usePageContext('Shipping')

  const [activeTab, setActiveTab] = useState('providers')
  const [isTabPending, startTabTransition] = useTransition()

  const handleTabChange = (value: string) => {
    startTabTransition(() => {
      setActiveTab(value)
    })
  }

  return (
    <div className="container max-w-4xl py-6 space-y-6">
      <PageHeader
        icon={Truck}
        title={t('shipping.title', 'Shipping Management')}
        description={t('shipping.description', 'Manage shipping providers and track shipments')}
        responsive
      />

      <Tabs value={activeTab} onValueChange={handleTabChange}>
        <TabsList>
          <TabsTrigger value="providers" className="cursor-pointer">
            {t('shipping.providers', 'Providers')}
          </TabsTrigger>
          <TabsTrigger value="shipments" className="cursor-pointer">
            {t('shipping.shipments', 'Shipments')}
          </TabsTrigger>
        </TabsList>

        <TabsContent value="providers" className={isTabPending ? 'mt-6 opacity-70 transition-opacity duration-200' : 'mt-6 transition-opacity duration-200'}>
          <ProviderList />
        </TabsContent>

        <TabsContent value="shipments" className={isTabPending ? 'mt-6 opacity-70 transition-opacity duration-200' : 'mt-6 transition-opacity duration-200'}>
          <ShipmentLookup />
        </TabsContent>
      </Tabs>
    </div>
  )
}

export default ShippingPage

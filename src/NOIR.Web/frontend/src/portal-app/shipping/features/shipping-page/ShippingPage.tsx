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
  const [, startTabTransition] = useTransition()

  const handleTabChange = (value: string) => {
    startTabTransition(() => {
      setActiveTab(value)
    })
  }

  return (
    <div className="space-y-6 animate-in fade-in-0 slide-in-from-bottom-4 duration-500">
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

        <TabsContent value="providers" className="mt-6">
          <ProviderList />
        </TabsContent>

        <TabsContent value="shipments" className="mt-6">
          <ShipmentLookup />
        </TabsContent>
      </Tabs>
    </div>
  )
}

export default ShippingPage

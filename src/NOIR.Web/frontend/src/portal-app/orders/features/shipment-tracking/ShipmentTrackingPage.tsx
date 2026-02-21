import { useTranslation } from 'react-i18next'
import { Truck } from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import { PageHeader } from '@uikit'
import { ShipmentLookup } from '@/portal-app/shipping/components/ShipmentLookup'

export const ShipmentTrackingPage = () => {
  const { t } = useTranslation('common')
  usePageContext('ShipmentTracking')

  return (
    <div className="space-y-6">
      <PageHeader
        icon={Truck}
        title={t('ecommerce.shipmentTracking', 'Shipment Tracking')}
        description={t('shipping.trackingDescription', 'Track and manage shipments for your orders')}
        responsive
      />

      <ShipmentLookup />
    </div>
  )
}

export default ShipmentTrackingPage

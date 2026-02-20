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
        title={t('shipping.lookupShipment', 'Look Up Shipment')}
        description={t('shipping.lookupDescription', 'Search by tracking number or order ID to view shipment details.')}
        responsive
      />

      <ShipmentLookup />
    </div>
  )
}

export default ShipmentTrackingPage

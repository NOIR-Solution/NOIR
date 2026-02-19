import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  Package,
  Search,
  Truck,
} from 'lucide-react'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  EmptyState,
  Input,
  Skeleton,
} from '@uikit'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { formatCurrency } from '@/lib/utils/currency'
import { getShippingOrderByTracking, getShippingOrderByOrderId } from '@/services/shipping'
import type { ShippingOrderDto } from '@/types/shipping'
import { getShippingStatusColor } from '../utils/shippingStatus'
import { ShipmentDetail } from './ShipmentDetail'

export const ShipmentLookup = () => {
  const { t } = useTranslation('common')
  const { formatDateTime } = useRegionalSettings()

  const [searchValue, setSearchValue] = useState('')
  const [isSearching, setIsSearching] = useState(false)
  const [shipment, setShipment] = useState<ShippingOrderDto | null>(null)
  const [hasSearched, setHasSearched] = useState(false)
  const [selectedShipment, setSelectedShipment] = useState<ShippingOrderDto | null>(null)

  const handleSearch = async () => {
    const value = searchValue.trim()
    if (!value) return

    setIsSearching(true)
    setHasSearched(true)
    setShipment(null)

    try {
      // Try as tracking number first, then as order ID (UUID format)
      const isUuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value)

      let result: ShippingOrderDto
      if (isUuid) {
        result = await getShippingOrderByOrderId(value)
      } else {
        result = await getShippingOrderByTracking(value)
      }
      setShipment(result)
    } catch {
      toast.error(t('shipping.shipmentNotFound', 'Shipment not found'))
    } finally {
      setIsSearching(false)
    }
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleSearch()
    }
  }

  if (selectedShipment) {
    return (
      <ShipmentDetail
        shipment={selectedShipment}
        onBack={() => setSelectedShipment(null)}
      />
    )
  }

  return (
    <div className="space-y-6">
      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader>
          <CardTitle>{t('shipping.lookupShipment', 'Look Up Shipment')}</CardTitle>
          <CardDescription>
            {t('shipping.lookupDescription', 'Search by tracking number or order ID to view shipment details.')}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex gap-3">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder={t('shipping.lookupPlaceholder', 'Enter tracking number or order ID...')}
                value={searchValue}
                onChange={(e) => setSearchValue(e.target.value)}
                onKeyDown={handleKeyDown}
                className="pl-10"
                aria-label={t('shipping.lookupShipment', 'Look up shipment')}
              />
            </div>
            <Button
              onClick={handleSearch}
              disabled={!searchValue.trim() || isSearching}
              className="cursor-pointer"
            >
              <Search className="h-4 w-4 mr-2" />
              {t('buttons.search', 'Search')}
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Results */}
      {isSearching && (
        <Card className="shadow-sm">
          <CardContent className="pt-6">
            <div className="space-y-4">
              <div className="flex items-center gap-4">
                <Skeleton className="h-10 w-10 rounded-lg" />
                <div className="space-y-2 flex-1">
                  <Skeleton className="h-4 w-1/3" />
                  <Skeleton className="h-3 w-1/4" />
                </div>
                <Skeleton className="h-6 w-20 rounded-full" />
              </div>
              <div className="grid grid-cols-4 gap-4">
                {[...Array(4)].map((_, i) => (
                  <div key={i} className="space-y-1">
                    <Skeleton className="h-3 w-16" />
                    <Skeleton className="h-4 w-24" />
                  </div>
                ))}
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {!isSearching && hasSearched && !shipment && (
        <Card className="shadow-sm">
          <CardContent className="p-0">
            <EmptyState
              icon={Package}
              title={t('shipping.noShipmentFound', 'No shipment found')}
              description={t('shipping.noShipmentFoundDescription', 'No shipment matches the given tracking number or order ID.')}
              className="border-0 rounded-none px-4 py-12"
            />
          </CardContent>
        </Card>
      )}

      {!isSearching && shipment && (
        <Card
          className="shadow-sm hover:shadow-lg transition-all duration-300 cursor-pointer"
          onClick={() => setSelectedShipment(shipment)}
        >
          <CardContent className="pt-6">
            <div className="flex items-start justify-between">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-xl bg-primary/10 border border-primary/20">
                  <Truck className="h-5 w-5 text-primary" />
                </div>
                <div>
                  <p className="font-medium">{shipment.trackingNumber}</p>
                  <p className="text-sm text-muted-foreground">
                    {shipment.providerName} &middot; {shipment.serviceTypeName}
                  </p>
                </div>
              </div>
              <Badge variant="outline" className={getShippingStatusColor(shipment.status)}>
                {t(`shipping.status.${shipment.status.toLowerCase()}`, shipment.status)}
              </Badge>
            </div>

            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mt-4 pt-4 border-t">
              <div>
                <p className="text-xs text-muted-foreground">{t('shipping.totalFee', 'Total Fee')}</p>
                <p className="text-sm font-medium">{formatCurrency(shipment.totalShippingFee, 'VND')}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">{t('shipping.createdAt', 'Created')}</p>
                <p className="text-sm">{formatDateTime(shipment.createdAt)}</p>
              </div>
              {shipment.estimatedDeliveryDate && (
                <div>
                  <p className="text-xs text-muted-foreground">{t('shipping.estimatedDelivery', 'Est. Delivery')}</p>
                  <p className="text-sm">{formatDateTime(shipment.estimatedDeliveryDate)}</p>
                </div>
              )}
              {shipment.actualDeliveryDate && (
                <div>
                  <p className="text-xs text-muted-foreground">{t('shipping.actualDelivery', 'Delivered')}</p>
                  <p className="text-sm">{formatDateTime(shipment.actualDeliveryDate)}</p>
                </div>
              )}
            </div>

            <p className="text-xs text-muted-foreground mt-3">
              {t('shipping.clickForDetails', 'Click to view full tracking details')}
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  )
}

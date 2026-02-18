export const shippingKeys = {
  all: ['shipping'] as const,
  providers: () => [...shippingKeys.all, 'providers'] as const,
  provider: (id: string) => [...shippingKeys.providers(), id] as const,
  orders: () => [...shippingKeys.all, 'orders'] as const,
  orderByTracking: (trackingNumber: string) => [...shippingKeys.orders(), 'tracking', trackingNumber] as const,
  orderByOrderId: (orderId: string) => [...shippingKeys.orders(), 'order', orderId] as const,
  tracking: (trackingNumber: string) => [...shippingKeys.all, 'tracking', trackingNumber] as const,
}

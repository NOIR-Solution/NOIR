export const paymentGatewayKeys = {
  all: ['paymentGateways'] as const,
  gateways: () => [...paymentGatewayKeys.all, 'gateways'] as const,
  schemas: () => [...paymentGatewayKeys.all, 'schemas'] as const,
}

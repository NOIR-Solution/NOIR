import { useQuery } from '@tanstack/react-query'
import { getPaymentGateways, getGatewaySchemas } from '@/services/paymentGateways'
import { paymentGatewayKeys } from './queryKeys'

export const usePaymentGatewaysListQuery = () =>
  useQuery({
    queryKey: paymentGatewayKeys.gateways(),
    queryFn: () => getPaymentGateways(),
  })

export const useGatewaySchemasQuery = () =>
  useQuery({
    queryKey: paymentGatewayKeys.schemas(),
    queryFn: () => getGatewaySchemas(),
  })

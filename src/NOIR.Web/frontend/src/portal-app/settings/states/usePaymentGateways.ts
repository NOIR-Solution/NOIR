import { useState, useEffect, useCallback } from 'react'
import type {
  PaymentGateway,
  GatewaySchemas,
  GatewaySchema,
  ConfigureGatewayRequest,
  UpdateGatewayRequest,
  TestConnectionResult,
} from '@/types'
import {
  getPaymentGateways,
  getGatewaySchemas,
  configureGateway,
  updateGateway,
  toggleGatewayActive,
  testGatewayConnection,
} from '@/services/paymentGateways'
import { ApiError } from '@/services/apiClient'

// ============================================================================
// Payment Gateways Hook
// ============================================================================

interface UsePaymentGatewaysState {
  gateways: PaymentGateway[]
  schemas: GatewaySchemas | null
  loading: boolean
  error: string | null
}

interface UsePaymentGatewaysReturn extends UsePaymentGatewaysState {
  refresh: () => Promise<void>
  configure: (request: ConfigureGatewayRequest) => Promise<{ success: boolean; gateway?: PaymentGateway; error?: string }>
  update: (id: string, request: UpdateGatewayRequest) => Promise<{ success: boolean; gateway?: PaymentGateway; error?: string }>
  toggleActive: (id: string, isActive: boolean) => Promise<{ success: boolean; error?: string }>
  testConnection: (id: string) => Promise<TestConnectionResult>
  getGatewayByProvider: (provider: string) => PaymentGateway | undefined
  getSchemaByProvider: (provider: string) => GatewaySchema | undefined
  availableProviders: string[]
}

export const usePaymentGateways = (): UsePaymentGatewaysReturn => {
  const [state, setState] = useState<UsePaymentGatewaysState>({
    gateways: [],
    schemas: null,
    loading: true,
    error: null,
  })

  const fetchData = useCallback(async () => {
    setState(prev => ({ ...prev, loading: true, error: null }))

    try {
      // Fetch both gateways and schemas in parallel
      const [gateways, schemas] = await Promise.all([
        getPaymentGateways(),
        getGatewaySchemas(),
      ])

      setState({
        gateways,
        schemas,
        loading: false,
        error: null,
      })
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : 'Failed to load payment gateways'
      setState(prev => ({ ...prev, loading: false, error: message }))
    }
  }, [])

  useEffect(() => {
    fetchData()
  }, [fetchData])

  const configure = useCallback(async (
    request: ConfigureGatewayRequest
  ): Promise<{ success: boolean; gateway?: PaymentGateway; error?: string }> => {
    try {
      const gateway = await configureGateway(request)
      await fetchData() // Refresh the list
      return { success: true, gateway }
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : 'Failed to configure gateway'
      return { success: false, error: message }
    }
  }, [fetchData])

  const update = useCallback(async (
    id: string,
    request: UpdateGatewayRequest
  ): Promise<{ success: boolean; gateway?: PaymentGateway; error?: string }> => {
    try {
      const gateway = await updateGateway(id, request)
      await fetchData() // Refresh the list
      return { success: true, gateway }
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : 'Failed to update gateway'
      return { success: false, error: message }
    }
  }, [fetchData])

  const toggleActive = useCallback(async (
    id: string,
    isActive: boolean
  ): Promise<{ success: boolean; error?: string }> => {
    // Optimistic update
    setState(prev => ({
      ...prev,
      gateways: prev.gateways.map(g =>
        g.id === id ? { ...g, isActive } : g
      ),
    }))

    try {
      await toggleGatewayActive(id, isActive)
      return { success: true }
    } catch (err) {
      // Revert on error
      await fetchData()
      const message = err instanceof ApiError
        ? err.message
        : 'Failed to toggle gateway status'
      return { success: false, error: message }
    }
  }, [fetchData])

  const testConnectionHandler = useCallback(async (
    id: string
  ): Promise<TestConnectionResult> => {
    try {
      return await testGatewayConnection(id)
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : 'Connection test failed'
      return {
        success: false,
        message,
        errorCode: 'API_ERROR',
      }
    }
  }, [])

  const getGatewayByProvider = useCallback((provider: string): PaymentGateway | undefined => {
    return state.gateways.find(g => g.provider === provider)
  }, [state.gateways])

  const getSchemaByProvider = useCallback((provider: string): GatewaySchema | undefined => {
    return state.schemas?.schemas[provider]
  }, [state.schemas])

  // Get list of all available providers from schemas
  const availableProviders = state.schemas
    ? Object.keys(state.schemas.schemas)
    : []

  return {
    ...state,
    refresh: fetchData,
    configure,
    update,
    toggleActive,
    testConnection: testConnectionHandler,
    getGatewayByProvider,
    getSchemaByProvider,
    availableProviders,
  }
}

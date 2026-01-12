/**
 * useAuditStream - SignalR hook for real-time audit event streaming
 *
 * This hook manages a SignalR connection to receive real-time audit events,
 * statistics updates, and notifications from the server.
 *
 * @example
 * const {
 *   connectionState,
 *   stats,
 *   recentEvents,
 *   subscribeToDashboard,
 *   subscribeToEntityType,
 * } = useAuditStream()
 */
import { useState, useEffect, useCallback, useRef } from 'react'
import * as signalR from '@microsoft/signalr'
import { getAccessToken } from '@/services/tokenStorage'
import type {
  UnifiedAuditEvent,
  AuditStatsUpdate,
  AuditConnectionInfo,
  HttpRequestAuditEvent,
  HandlerAuditEvent,
  EntityAuditEvent,
} from '@/types'

export type ConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting' | 'error'

const MAX_RECENT_EVENTS = 100

// Convert backend event types to unified frontend format
function httpRequestToUnified(evt: HttpRequestAuditEvent): UnifiedAuditEvent {
  return {
    eventType: 'HttpRequest',
    entityType: 'HttpRequest',
    entityId: evt.id,
    operation: evt.httpMethod,
    userId: evt.userId,
    userName: evt.userEmail,
    tenantId: evt.tenantId,
    timestamp: evt.timestamp,
    correlationId: evt.correlationId,
    details: {
      url: evt.url,
      statusCode: evt.statusCode,
      ipAddress: evt.ipAddress,
      durationMs: evt.durationMs,
      handlerCount: evt.handlerCount,
      entityChangeCount: evt.entityChangeCount,
    },
  }
}

function handlerToUnified(evt: HandlerAuditEvent): UnifiedAuditEvent {
  return {
    eventType: 'Handler',
    entityType: evt.targetDtoType || 'Handler',
    entityId: evt.targetDtoId,
    operation: evt.operationType,
    userId: null,
    userName: null,
    tenantId: null,
    timestamp: evt.timestamp,
    correlationId: evt.correlationId,
    details: {
      handlerName: evt.handlerName,
      isSuccess: evt.isSuccess,
      errorMessage: evt.errorMessage,
      durationMs: evt.durationMs,
      entityChangeCount: evt.entityChangeCount,
    },
  }
}

function entityToUnified(evt: EntityAuditEvent): UnifiedAuditEvent {
  return {
    eventType: 'EntityChange',
    entityType: evt.entityType,
    entityId: evt.entityId,
    operation: evt.operation,
    userId: null,
    userName: null,
    tenantId: null,
    timestamp: evt.timestamp,
    correlationId: evt.correlationId,
    details: {
      version: evt.version,
      changeSummary: evt.changeSummary,
    },
  }
}

interface UseAuditStreamOptions {
  /** Auto-connect on mount (default: true) */
  autoConnect?: boolean
  /** Auto-subscribe to dashboard updates (default: true) */
  autoDashboard?: boolean
  /** Maximum recent events to keep in memory (default: 100) */
  maxRecentEvents?: number
}

interface UseAuditStreamReturn {
  /** Current connection state */
  connectionState: ConnectionState
  /** Current audit statistics */
  stats: AuditStatsUpdate | null
  /** Recent audit events (most recent first) */
  recentEvents: UnifiedAuditEvent[]
  /** Connection info from server */
  connectionInfo: AuditConnectionInfo | null
  /** Error message if connection failed */
  error: string | null
  /** Connect to the audit hub */
  connect: () => Promise<void>
  /** Disconnect from the audit hub */
  disconnect: () => Promise<void>
  /** Subscribe to all events */
  subscribeToAllEvents: () => Promise<void>
  /** Unsubscribe from all events */
  unsubscribeFromAllEvents: () => Promise<void>
  /** Subscribe to a specific entity type */
  subscribeToEntityType: (entityType: string) => Promise<void>
  /** Unsubscribe from a specific entity type */
  unsubscribeFromEntityType: (entityType: string) => Promise<void>
  /** Subscribe to dashboard updates */
  subscribeToDashboard: () => Promise<void>
  /** Unsubscribe from dashboard updates */
  unsubscribeFromDashboard: () => Promise<void>
  /** Request a manual stats refresh */
  requestStatsRefresh: () => Promise<void>
  /** Clear recent events */
  clearRecentEvents: () => void
}

export function useAuditStream(options: UseAuditStreamOptions = {}): UseAuditStreamReturn {
  const {
    autoConnect = true,
    autoDashboard = true,
    maxRecentEvents = MAX_RECENT_EVENTS,
  } = options

  const [connectionState, setConnectionState] = useState<ConnectionState>('disconnected')
  const [stats, setStats] = useState<AuditStatsUpdate | null>(null)
  const [recentEvents, setRecentEvents] = useState<UnifiedAuditEvent[]>([])
  const [connectionInfo, setConnectionInfo] = useState<AuditConnectionInfo | null>(null)
  const [error, setError] = useState<string | null>(null)

  const connectionRef = useRef<signalR.HubConnection | null>(null)
  const isConnectingRef = useRef(false)
  const isMountedRef = useRef(true)

  // Reset mounted ref on mount
  useEffect(() => {
    isMountedRef.current = true
    return () => {
      isMountedRef.current = false
    }
  }, [])

  // Add event to recent events (prepend, limit to max, deduplicate)
  const addEvent = useCallback((event: UnifiedAuditEvent) => {
    if (!isMountedRef.current) return
    setRecentEvents(prev => {
      // Deduplicate by entityId (unique event ID)
      // This prevents duplicates when subscribed to multiple groups
      const isDuplicate = prev.some(e =>
        e.entityId === event.entityId &&
        e.eventType === event.eventType &&
        e.timestamp === event.timestamp
      )
      if (isDuplicate) return prev

      const newEvents = [event, ...prev]
      if (newEvents.length > maxRecentEvents) {
        return newEvents.slice(0, maxRecentEvents)
      }
      return newEvents
    })
  }, [maxRecentEvents])

  // Clear recent events
  const clearRecentEvents = useCallback(() => {
    setRecentEvents([])
  }, [])

  // Create connection with proper configuration
  const createConnection = useCallback(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/audit', {
        accessTokenFactory: () => getAccessToken() || '',
        withCredentials: true,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff: 0, 2, 4, 8, 16, 32 seconds, then null to stop
          if (retryContext.previousRetryCount < 6) {
            return Math.pow(2, retryContext.previousRetryCount) * 1000
          }
          return null
        },
      })
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    // Set up event handlers with mounted checks
    // Listen for all three audit event types from backend
    connection.on('ReceiveHttpRequestAudit', (event: HttpRequestAuditEvent) => {
      addEvent(httpRequestToUnified(event))
    })

    connection.on('ReceiveHandlerAudit', (event: HandlerAuditEvent) => {
      addEvent(handlerToUnified(event))
    })

    connection.on('ReceiveEntityAudit', (event: EntityAuditEvent) => {
      addEvent(entityToUnified(event))
    })

    connection.on('ReceiveStatsUpdate', (update: AuditStatsUpdate) => {
      if (isMountedRef.current) setStats(update)
    })

    connection.on('ReceiveConnectionConfirmed', (info: AuditConnectionInfo) => {
      if (!isMountedRef.current) return
      setConnectionInfo(info)
      setStats(info.initialStats)
    })

    connection.on('ReceiveError', (errorMessage: string) => {
      console.error('Audit hub error:', errorMessage)
      if (isMountedRef.current) setError(errorMessage)
    })

    // Connection state handlers with mounted checks
    connection.onreconnecting(() => {
      if (isMountedRef.current) setConnectionState('reconnecting')
    })

    connection.onreconnected(() => {
      if (!isMountedRef.current) return
      setConnectionState('connected')
      setError(null)
    })

    connection.onclose((err) => {
      if (!isMountedRef.current) return
      setConnectionState('disconnected')
      if (err) {
        setError(err.message)
      }
    })

    return connection
  }, [addEvent])

  // Connect to the hub
  const connect = useCallback(async () => {
    if (!isMountedRef.current) return
    if (isConnectingRef.current || connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      return
    }

    isConnectingRef.current = true
    setConnectionState('connecting')
    setError(null)

    try {
      const connection = createConnection()
      connectionRef.current = connection

      await connection.start()

      // Check if still mounted after async operation
      if (!isMountedRef.current) {
        await connection.stop()
        return
      }

      setConnectionState('connected')

      // Auto-subscribe to dashboard if enabled
      if (autoDashboard) {
        await connection.invoke('SubscribeToDashboard')
      }
    } catch (err) {
      if (!isMountedRef.current) return
      // Ignore AbortError - this happens in React strict mode when the component
      // unmounts during the connection negotiation phase
      if (err instanceof Error && err.name === 'AbortError') {
        return
      }
      setConnectionState('error')
      setError(err instanceof Error ? err.message : 'Failed to connect')
      console.error('Failed to connect to audit hub:', err)
    } finally {
      isConnectingRef.current = false
    }
  }, [createConnection, autoDashboard])

  // Disconnect from the hub
  const disconnect = useCallback(async () => {
    const connection = connectionRef.current
    connectionRef.current = null
    if (connection) {
      try {
        await connection.stop()
      } catch (err) {
        console.error('Error disconnecting:', err)
      }
      if (isMountedRef.current) {
        setConnectionState('disconnected')
      }
    }
  }, [])

  // Hub method wrappers
  const subscribeToAllEvents = useCallback(async () => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke('SubscribeToAllEvents')
    }
  }, [])

  const unsubscribeFromAllEvents = useCallback(async () => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke('UnsubscribeFromAllEvents')
    }
  }, [])

  const subscribeToEntityType = useCallback(async (entityType: string) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke('SubscribeToEntityType', entityType)
    }
  }, [])

  const unsubscribeFromEntityType = useCallback(async (entityType: string) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke('UnsubscribeFromEntityType', entityType)
    }
  }, [])

  const subscribeToDashboard = useCallback(async () => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke('SubscribeToDashboard')
    }
  }, [])

  const unsubscribeFromDashboard = useCallback(async () => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke('UnsubscribeFromDashboard')
    }
  }, [])

  const requestStatsRefresh = useCallback(async () => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke('RequestStatsRefresh')
    }
  }, [])

  // Auto-connect on mount with delay to handle React strict mode double-mount
  useEffect(() => {
    if (!autoConnect) return

    // Small delay to skip React strict mode's first mount/unmount cycle
    // In strict mode: mount -> unmount -> mount (we want to connect on the final mount)
    const timeoutId = setTimeout(() => {
      if (isMountedRef.current) {
        connect()
      }
    }, 100)

    return () => {
      clearTimeout(timeoutId)
      disconnect()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [autoConnect]) // Only depend on autoConnect, not callbacks which change on every render

  return {
    connectionState,
    stats,
    recentEvents,
    connectionInfo,
    error,
    connect,
    disconnect,
    subscribeToAllEvents,
    unsubscribeFromAllEvents,
    subscribeToEntityType,
    unsubscribeFromEntityType,
    subscribeToDashboard,
    unsubscribeFromDashboard,
    requestStatsRefresh,
    clearRecentEvents,
  }
}

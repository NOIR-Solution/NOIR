/**
 * SignalR Hook for Real-time Notifications
 *
 * Provides a reusable hook for establishing and managing SignalR connections.
 * Features:
 * - Automatic connection with JWT authentication
 * - Auto-reconnect on disconnect
 * - Connection state management
 * - Typed event handlers
 */
import { useEffect, useState, useCallback, useRef } from 'react'
import * as signalR from '@microsoft/signalr'
import { getAccessToken } from '@/services/tokenStorage'
import type { Notification } from '@/types'

export type ConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting'

interface UseSignalROptions {
  /** Whether to automatically connect on mount */
  autoConnect?: boolean
  /** Called when a new notification is received */
  onNotification?: (notification: Notification) => void
  /** Called when unread count is updated */
  onUnreadCountUpdate?: (count: number) => void
  /** Called when connection state changes */
  onConnectionChange?: (state: ConnectionState) => void
}

interface UseSignalRReturn {
  /** Current connection state */
  connectionState: ConnectionState
  /** Manually start the connection */
  connect: () => Promise<void>
  /** Manually stop the connection */
  disconnect: () => Promise<void>
  /** Whether currently connected */
  isConnected: boolean
}

/**
 * Hook for managing SignalR notification connection
 *
 * @example
 * ```tsx
 * const { connectionState, isConnected } = useSignalR({
 *   autoConnect: true,
 *   onNotification: (notification) => {
 *     console.log('New notification:', notification)
 *   },
 *   onUnreadCountUpdate: (count) => {
 *     setUnreadCount(count)
 *   },
 * })
 * ```
 */
export function useSignalR(options: UseSignalROptions = {}): UseSignalRReturn {
  const {
    autoConnect = true,
    onNotification,
    onUnreadCountUpdate,
    onConnectionChange,
  } = options

  const [connectionState, setConnectionState] = useState<ConnectionState>('disconnected')
  const connectionRef = useRef<signalR.HubConnection | null>(null)
  const mountedRef = useRef(true)

  // Update connection state and notify
  const updateState = useCallback((state: ConnectionState) => {
    if (mountedRef.current) {
      setConnectionState(state)
      onConnectionChange?.(state)
    }
  }, [onConnectionChange])

  // Build the SignalR connection
  const buildConnection = useCallback(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/notifications', {
        accessTokenFactory: () => getAccessToken() || '',
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff: 0, 2s, 4s, 8s, 16s, then 30s max
          if (retryContext.previousRetryCount >= 5) {
            return 30000
          }
          return Math.min(Math.pow(2, retryContext.previousRetryCount) * 1000, 30000)
        },
      })
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    // Set up event handlers
    connection.on('ReceiveNotification', (notification: Notification) => {
      if (mountedRef.current && onNotification) {
        onNotification(notification)
      }
    })

    connection.on('UpdateUnreadCount', (count: number) => {
      if (mountedRef.current && onUnreadCountUpdate) {
        onUnreadCountUpdate(count)
      }
    })

    // Handle connection state changes
    connection.onreconnecting(() => {
      updateState('reconnecting')
    })

    connection.onreconnected(() => {
      updateState('connected')
    })

    connection.onclose(() => {
      updateState('disconnected')
    })

    return connection
  }, [onNotification, onUnreadCountUpdate, updateState])

  // Connect to SignalR hub
  const connect = useCallback(async () => {
    // Don't connect if already connecting/connected
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected ||
        connectionRef.current?.state === signalR.HubConnectionState.Connecting) {
      return
    }

    // Build new connection if needed
    if (!connectionRef.current) {
      connectionRef.current = buildConnection()
    }

    try {
      updateState('connecting')
      await connectionRef.current.start()
      updateState('connected')
    } catch {
      // Connection will be retried automatically
      updateState('disconnected')
      // Retry after 5 seconds
      if (mountedRef.current) {
        setTimeout(() => {
          if (mountedRef.current) {
            connect()
          }
        }, 5000)
      }
    }
  }, [buildConnection, updateState])

  // Disconnect from SignalR hub
  const disconnect = useCallback(async () => {
    if (connectionRef.current) {
      try {
        await connectionRef.current.stop()
      } catch {
        // Disconnect error is non-critical
      }
      connectionRef.current = null
      updateState('disconnected')
    }
  }, [updateState])

  // Auto-connect on mount
  useEffect(() => {
    mountedRef.current = true

    if (autoConnect) {
      // Small delay to ensure auth token is ready
      const timer = setTimeout(() => {
        if (mountedRef.current && getAccessToken()) {
          connect()
        }
      }, 100)
      return () => clearTimeout(timer)
    }
  }, [autoConnect, connect])

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      mountedRef.current = false
      if (connectionRef.current) {
        connectionRef.current.stop()
        connectionRef.current = null
      }
    }
  }, [])

  return {
    connectionState,
    connect,
    disconnect,
    isConnected: connectionState === 'connected',
  }
}

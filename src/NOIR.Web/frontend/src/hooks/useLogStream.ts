/**
 * Log Stream SignalR Hook
 *
 * Provides real-time log streaming via SignalR connection to /hubs/logstream.
 * Features:
 * - Automatic connection with JWT authentication
 * - Auto-reconnect on disconnect
 * - Client-side filtering support
 * - Log entry buffering with max size
 */
import { useEffect, useState, useCallback, useRef } from 'react'
import * as signalR from '@microsoft/signalr'
import { getAccessToken } from '@/services/tokenStorage'
import type { LogEntryDto, LogBufferStatsDto, ErrorClusterDto, DevLogLevel } from '@/services/developerLogs'

export type LogConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting'

export interface LogStreamFilter {
  minLevel: DevLogLevel
  sources?: string[]
  searchPattern?: string
  exceptionsOnly?: boolean
}

interface UseLogStreamOptions {
  /** Whether to automatically connect on mount */
  autoConnect?: boolean
  /** Maximum number of entries to keep in memory */
  maxEntries?: number
  /** Initial filter settings */
  initialFilter?: LogStreamFilter
  /** Called when connection state changes */
  onConnectionChange?: (state: LogConnectionState) => void
}

interface UseLogStreamReturn {
  /** Current connection state */
  connectionState: LogConnectionState
  /** All log entries (most recent first) */
  entries: LogEntryDto[]
  /** Current log level from server */
  currentLevel: string
  /** Buffer statistics */
  bufferStats: LogBufferStatsDto | null
  /** Error clusters */
  errorClusters: ErrorClusterDto[]
  /** Whether streaming is paused */
  isPaused: boolean
  /** Manually start the connection */
  connect: () => Promise<void>
  /** Manually stop the connection */
  disconnect: () => Promise<void>
  /** Whether currently connected */
  isConnected: boolean
  /** Pause/resume streaming */
  setPaused: (paused: boolean) => void
  /** Clear local entries */
  clearEntries: () => void
  /** Update filter (sends to server) */
  setFilter: (filter: LogStreamFilter) => void
  /** Request historical entries */
  requestHistory: (count: number, beforeId?: number) => void
  /** Request error summary */
  requestErrorSummary: (maxClusters?: number) => void
  /** Request buffer stats */
  requestBufferStats: () => void
  /** Current filter */
  filter: LogStreamFilter
}

const DEFAULT_FILTER: LogStreamFilter = {
  minLevel: 'Information',
}

/**
 * Hook for managing SignalR log streaming connection
 *
 * @example
 * ```tsx
 * const {
 *   connectionState,
 *   entries,
 *   isPaused,
 *   setPaused,
 *   setFilter,
 * } = useLogStream({
 *   autoConnect: true,
 *   maxEntries: 1000,
 * })
 * ```
 */
export function useLogStream(options: UseLogStreamOptions = {}): UseLogStreamReturn {
  const {
    autoConnect = true,
    maxEntries = 1000,
    initialFilter = DEFAULT_FILTER,
    onConnectionChange,
  } = options

  const [connectionState, setConnectionState] = useState<LogConnectionState>('disconnected')
  const [entries, setEntries] = useState<LogEntryDto[]>([])
  const [currentLevel, setCurrentLevel] = useState<string>('Information')
  const [bufferStats, setBufferStats] = useState<LogBufferStatsDto | null>(null)
  const [errorClusters, setErrorClusters] = useState<ErrorClusterDto[]>([])
  const [isPaused, setIsPaused] = useState(false)
  const [filter, setFilterState] = useState<LogStreamFilter>(initialFilter)

  const connectionRef = useRef<signalR.HubConnection | null>(null)
  const mountedRef = useRef(true)
  const pausedRef = useRef(isPaused)

  // Keep pausedRef in sync
  useEffect(() => {
    pausedRef.current = isPaused
  }, [isPaused])

  // Update connection state and notify
  const updateState = useCallback((state: LogConnectionState) => {
    if (mountedRef.current) {
      setConnectionState(state)
      onConnectionChange?.(state)
    }
  }, [onConnectionChange])

  // Add entry to the list (with max size limit)
  const addEntry = useCallback((entry: LogEntryDto) => {
    if (pausedRef.current) return

    setEntries(prev => {
      const newEntries = [entry, ...prev]
      if (newEntries.length > maxEntries) {
        return newEntries.slice(0, maxEntries)
      }
      return newEntries
    })
  }, [maxEntries])

  // Add batch of entries
  const addEntries = useCallback((newEntries: LogEntryDto[]) => {
    if (pausedRef.current) return

    setEntries(prev => {
      // Merge and sort by ID (newest first)
      const merged = [...newEntries, ...prev]
        .sort((a, b) => b.id - a.id)
      // Remove duplicates by ID
      const uniqueIds = new Set<number>()
      const unique = merged.filter(e => {
        if (uniqueIds.has(e.id)) return false
        uniqueIds.add(e.id)
        return true
      })
      // Limit size
      if (unique.length > maxEntries) {
        return unique.slice(0, maxEntries)
      }
      return unique
    })
  }, [maxEntries])

  // Build the SignalR connection
  const buildConnection = useCallback(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/logstream', {
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
    connection.on('ReceiveLogEntry', (entry: LogEntryDto) => {
      if (mountedRef.current) {
        addEntry(entry)
      }
    })

    connection.on('ReceiveLogBatch', (batch: LogEntryDto[]) => {
      if (mountedRef.current) {
        addEntries(batch)
      }
    })

    connection.on('ReceiveLevelChanged', (level: string) => {
      if (mountedRef.current) {
        setCurrentLevel(level)
      }
    })

    connection.on('ReceiveBufferStats', (stats: LogBufferStatsDto) => {
      if (mountedRef.current) {
        setBufferStats(stats)
      }
    })

    connection.on('ReceiveErrorSummary', (clusters: ErrorClusterDto[]) => {
      if (mountedRef.current) {
        setErrorClusters(clusters)
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
  }, [addEntry, addEntries, updateState])

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

  // Set filter (client-side only - no server-side filtering for scalability)
  const setFilter = useCallback((newFilter: LogStreamFilter) => {
    setFilterState(newFilter)
  }, [])

  // Request history
  const requestHistory = useCallback((count: number, beforeId?: number) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      connectionRef.current.invoke('RequestHistory', count, beforeId).catch(console.error)
    }
  }, [])

  // Request error summary
  const requestErrorSummary = useCallback((maxClusters = 10) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      connectionRef.current.invoke('RequestErrorSummary', maxClusters).catch(console.error)
    }
  }, [])

  // Request buffer stats
  const requestBufferStats = useCallback(() => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      connectionRef.current.invoke('RequestBufferStats').catch(console.error)
    }
  }, [])

  // Clear local entries
  const clearEntries = useCallback(() => {
    setEntries([])
  }, [])

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
    entries,
    currentLevel,
    bufferStats,
    errorClusters,
    isPaused,
    connect,
    disconnect,
    isConnected: connectionState === 'connected',
    setPaused: setIsPaused,
    clearEntries,
    setFilter,
    requestHistory,
    requestErrorSummary,
    requestBufferStats,
    filter,
  }
}

import { useEffect, useRef, useState, useCallback } from 'react'
import * as signalR from '@microsoft/signalr'
import { useTranslation } from 'react-i18next'
import { toast } from '@/lib/toast'
import { getAccessToken } from '@/services/tokenStorage'
import { useAuthContext } from '@/contexts/AuthContext'
import type { EntityUpdateSignal } from '@/types/signals'

/** Delay before clearing the reconnecting banner */
const RECONNECT_BANNER_MS = 2_000

interface UseEntityUpdateSignalOptions {
  entityType: string
  entityId?: string
  isDirty?: boolean
  onCollectionUpdate?: () => void
  onAutoReload?: () => void
  onNavigateAway?: () => void
}

interface UseEntityUpdateSignalResult {
  conflictSignal: EntityUpdateSignal | null
  deletedSignal: EntityUpdateSignal | null
  dismissConflict: () => void
  reloadAndRestart: () => void
  isReconnecting: boolean
}

export const useEntityUpdateSignal = (options: UseEntityUpdateSignalOptions): UseEntityUpdateSignalResult => {
  const {
    entityType,
    entityId,
    isDirty = false,
    onCollectionUpdate,
    onAutoReload,
    onNavigateAway,
  } = options

  const { t } = useTranslation('common')
  const { user } = useAuthContext()
  const tenantId = user?.tenantId ?? 'default'

  const [conflictSignal, setConflictSignal] = useState<EntityUpdateSignal | null>(null)
  const [deletedSignal, setDeletedSignal] = useState<EntityUpdateSignal | null>(null)
  const [isReconnecting, setIsReconnecting] = useState(false)

  const connectionRef = useRef<signalR.HubConnection | null>(null)
  const mountedRef = useRef(true)
  const isDirtyRef = useRef(isDirty)
  const entityIdRef = useRef(entityId)
  const prevEntityIdRef = useRef<string | undefined>(undefined)

  // Keep refs in sync with latest prop values
  isDirtyRef.current = isDirty
  entityIdRef.current = entityId

  // Stable callback refs to avoid rebuilding the connection on every render
  const onCollectionUpdateRef = useRef(onCollectionUpdate)
  const onAutoReloadRef = useRef(onAutoReload)
  const onNavigateAwayRef = useRef(onNavigateAway)
  onCollectionUpdateRef.current = onCollectionUpdate
  onAutoReloadRef.current = onAutoReload
  onNavigateAwayRef.current = onNavigateAway

  const dismissConflict = useCallback(() => {
    setConflictSignal(null)
  }, [])

  const reloadAndRestart = useCallback(() => {
    onAutoReloadRef.current?.()
    setConflictSignal(null)
  }, [])

  // Build connection and set up event listeners
  useEffect(() => {
    mountedRef.current = true

    const token = getAccessToken()
    if (!token) return

    // Custom logger: suppress transient "stopped during negotiation" noise from
    // React StrictMode's double-effect invocation. Real errors still surface.
    const hubLogger: signalR.ILogger = {
      log: (level: signalR.LogLevel, message: string) => {
        if (message.includes('stopped during negotiation')) return
        if (level === signalR.LogLevel.Error) console.error(`[SignalR] ${message}`)
        else if (level === signalR.LogLevel.Warning) console.warn(`[SignalR] ${message}`)
      },
    }

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/notifications', {
        accessTokenFactory: () => getAccessToken() || '',
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.previousRetryCount >= 5) return 30000
          return Math.min(Math.pow(2, retryContext.previousRetryCount) * 1000, 30000)
        },
      })
      .configureLogging(hubLogger)
      .build()

    connectionRef.current = connection

    // --- Event handlers ---

    connection.on('EntityUpdated', (signal: EntityUpdateSignal) => {
      if (!mountedRef.current) return

      // Only process signals for our entity type + id
      if (signal.entityType !== entityType) return
      if (entityIdRef.current && signal.entityId !== entityIdRef.current) return

      if (signal.operation === 'Deleted') {
        setDeletedSignal(signal)
        return
      }

      if (!isDirtyRef.current) {
        onAutoReloadRef.current?.()
        toast.info(t('entityUpdate.autoReloaded'))
      } else {
        setConflictSignal(signal)
      }
    })

    connection.on('EntityCollectionUpdated', (signal: EntityUpdateSignal) => {
      if (!mountedRef.current) return
      if (signal.entityType !== entityType) return
      onCollectionUpdateRef.current?.()
    })

    // --- Connection state handlers ---

    connection.onreconnecting(() => {
      if (mountedRef.current) setIsReconnecting(true)
    })

    connection.onreconnected(() => {
      if (!mountedRef.current) return

      // Re-join groups after reconnect
      connection.invoke('JoinEntityList', entityType, tenantId).catch(() => {})
      if (entityIdRef.current) {
        connection.invoke('JoinEntity', entityType, entityIdRef.current, tenantId).catch(() => {})
      }

      // Trigger data refresh on reconnect
      onCollectionUpdateRef.current?.()
      if (entityIdRef.current) {
        onAutoReloadRef.current?.()
      }

      // Clear reconnecting banner after delay
      setTimeout(() => {
        if (mountedRef.current) setIsReconnecting(false)
      }, RECONNECT_BANNER_MS)
    })

    connection.onclose(() => {
      if (mountedRef.current) setIsReconnecting(false)
    })

    // --- Start connection and join groups ---

    const start = async () => {
      try {
        await connection.start()

        if (!mountedRef.current) {
          connection.stop()
          return
        }

        await connection.invoke('JoinEntityList', entityType, tenantId)

        if (entityIdRef.current) {
          await connection.invoke('JoinEntity', entityType, entityIdRef.current, tenantId)
        }

        prevEntityIdRef.current = entityIdRef.current
      } catch {
        // Connection will retry automatically via withAutomaticReconnect
      }
    }

    start()

    return () => {
      mountedRef.current = false

      const cleanup = async () => {
        if (connection.state === signalR.HubConnectionState.Connected) {
          try {
            await connection.invoke('LeaveEntityList', entityType, tenantId)
            if (entityIdRef.current) {
              await connection.invoke('LeaveEntity', entityType, entityIdRef.current, tenantId)
            }
          } catch {
            // Best-effort leave
          }
        }
        connection.stop()
      }

      cleanup()
      connectionRef.current = null
    }
  }, [entityType, tenantId, t])

  // Handle entityId changes — leave old group, join new group
  useEffect(() => {
    const connection = connectionRef.current
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) return

    const prev = prevEntityIdRef.current
    const next = entityId

    if (prev === next) return

    const swap = async () => {
      try {
        if (prev) {
          await connection.invoke('LeaveEntity', entityType, prev, tenantId)
        }
        if (next) {
          await connection.invoke('JoinEntity', entityType, next, tenantId)
        }
        prevEntityIdRef.current = next
      } catch {
        // Best-effort group swap
      }
    }

    swap()
  }, [entityId, entityType, tenantId])

  return {
    conflictSignal,
    deletedSignal,
    dismissConflict,
    reloadAndRestart,
    isReconnecting,
  }
}

import { useState, useEffect } from 'react'

interface NetworkStatus {
  /** Whether the browser reports being online */
  isOnline: boolean
  /** Whether we were previously offline (for showing "back online" message) */
  wasOffline: boolean
  /** Timestamp when we reconnected (null if never disconnected) */
  reconnectedAt: Date | null
}

/**
 * Hook to track browser online/offline status
 *
 * Uses the Navigator.onLine API and online/offline events.
 * Note: This only detects if the browser has network access,
 * not if the API server is reachable.
 */
export const useNetworkStatus = (): NetworkStatus => {
  const [status, setStatus] = useState<NetworkStatus>(() => ({
    isOnline: typeof navigator !== 'undefined' ? navigator.onLine : true,
    wasOffline: false,
    reconnectedAt: null,
  }))

  useEffect(() => {
    if (typeof window === 'undefined') return

    const handleOnline = () => {
      setStatus((prev) => ({
        isOnline: true,
        wasOffline: !prev.isOnline ? true : prev.wasOffline,
        reconnectedAt: !prev.isOnline ? new Date() : prev.reconnectedAt,
      }))
    }

    const handleOffline = () => {
      setStatus((prev) => ({
        ...prev,
        isOnline: false,
      }))
    }

    window.addEventListener('online', handleOnline)
    window.addEventListener('offline', handleOffline)

    return () => {
      window.removeEventListener('online', handleOnline)
      window.removeEventListener('offline', handleOffline)
    }
  }, [])

  return status
}

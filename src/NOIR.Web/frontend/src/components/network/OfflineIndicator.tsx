import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { WifiOff, Wifi } from 'lucide-react'
import { motion, AnimatePresence } from 'framer-motion'
import { useNetworkStatus } from '@/hooks/useNetworkStatus'

/**
 * OfflineIndicator - Shows a banner when the user is offline
 *
 * Displays a fixed banner at the bottom-left when offline.
 * Shows a brief "Back online" message when connection is restored.
 */
export const OfflineIndicator = () => {
  const { t } = useTranslation('common')
  const { isOnline, wasOffline, reconnectedAt } = useNetworkStatus()
  const [showReconnected, setShowReconnected] = useState(false)

  // Show "back online" message briefly when reconnecting
  useEffect(() => {
    if (isOnline && wasOffline && reconnectedAt) {
      setShowReconnected(true)
      const timer = setTimeout(() => {
        setShowReconnected(false)
      }, 3000) // Hide after 3 seconds
      return () => clearTimeout(timer)
    }
  }, [isOnline, wasOffline, reconnectedAt])

  return (
    <AnimatePresence>
      {!isOnline && (
        <motion.div
          initial={{ opacity: 0, y: 20, scale: 0.95 }}
          animate={{ opacity: 1, y: 0, scale: 1 }}
          exit={{ opacity: 0, y: 20, scale: 0.95 }}
          transition={{ duration: 0.2 }}
          className="fixed bottom-4 left-4 z-50 flex items-center gap-2 px-4 py-2.5
                     bg-amber-100 dark:bg-amber-900/90 border border-amber-300 dark:border-amber-700
                     rounded-lg shadow-lg"
          role="alert"
          aria-live="assertive"
        >
          <WifiOff className="h-4 w-4 text-amber-600 dark:text-amber-400 flex-shrink-0" />
          <span className="text-sm font-medium text-amber-800 dark:text-amber-200">
            {t('network.offline', "You're offline")}
          </span>
        </motion.div>
      )}

      {showReconnected && (
        <motion.div
          initial={{ opacity: 0, y: 20, scale: 0.95 }}
          animate={{ opacity: 1, y: 0, scale: 1 }}
          exit={{ opacity: 0, y: 20, scale: 0.95 }}
          transition={{ duration: 0.2 }}
          className="fixed bottom-4 left-4 z-50 flex items-center gap-2 px-4 py-2.5
                     bg-green-100 dark:bg-green-900/90 border border-green-300 dark:border-green-700
                     rounded-lg shadow-lg"
          role="status"
          aria-live="polite"
        >
          <Wifi className="h-4 w-4 text-green-600 dark:text-green-400 flex-shrink-0" />
          <span className="text-sm font-medium text-green-800 dark:text-green-200">
            {t('network.backOnline', 'Back online')}
          </span>
        </motion.div>
      )}
    </AnimatePresence>
  )
}

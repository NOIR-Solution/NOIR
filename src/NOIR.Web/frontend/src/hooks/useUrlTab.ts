import { useTransition, useCallback } from 'react'
import { useSearchParams } from 'react-router-dom'

interface UseUrlTabOptions {
  defaultTab: string
  paramName?: string
}

/**
 * URL-synced tab state hook.
 * - Derives active tab from URL search params (single source of truth)
 * - Updates URL on tab change (replace, no history pollution)
 * - Preserves existing search params (functional updater)
 * - Omits default tab from URL for cleaner URLs
 * - Built-in useTransition for non-blocking updates
 */
export const useUrlTab = ({ defaultTab, paramName = 'tab' }: UseUrlTabOptions) => {
  const [searchParams, setSearchParams] = useSearchParams()
  const [isPending, startTransition] = useTransition()

  const activeTab = searchParams.get(paramName) || defaultTab

  const handleTabChange = useCallback((tab: string) => {
    startTransition(() => {
      setSearchParams(prev => {
        const next = new URLSearchParams(prev)
        if (tab === defaultTab) next.delete(paramName)
        else next.set(paramName, tab)
        return next
      }, { replace: true })
    })
  }, [setSearchParams, paramName, defaultTab, startTransition])

  return { activeTab, handleTabChange, isPending }
}

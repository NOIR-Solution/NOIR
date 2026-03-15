import { useState, useEffect, useRef } from 'react'

/** Shared threshold before showing stale/loading indicators (ms). */
export const STALE_DELAY_MS = 500

/**
 * Delays a boolean transitioning to `true` by `delay` ms.
 * Transitions to `false` are immediate.
 *
 * Use case: avoid flashing a loading/stale indicator when the API
 * responds faster than the delay threshold (e.g. 500 ms).
 */
export const useDelayedLoading = (value: boolean, delay = STALE_DELAY_MS): boolean => {
  const [delayed, setDelayed] = useState(false)
  const valueRef = useRef(value)

  useEffect(() => {
    valueRef.current = value

    if (!value) {
      setDelayed(false)
      return
    }

    const timer = setTimeout(() => {
      if (valueRef.current) {
        setDelayed(true)
      }
    }, delay)

    return () => clearTimeout(timer)
  }, [value, delay])

  return delayed
}

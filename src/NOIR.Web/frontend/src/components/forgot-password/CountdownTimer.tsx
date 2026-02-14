import { useState, useEffect, useCallback } from 'react'
import { useTranslation } from 'react-i18next'

interface CountdownTimerProps {
  targetTime: Date | string
  onComplete?: () => void
}

/**
 * Countdown timer component for resend cooldown
 */
export const CountdownTimer = ({ targetTime, onComplete }: CountdownTimerProps) => {
  const { t } = useTranslation('auth')
  const [remainingSeconds, setRemainingSeconds] = useState(0)

  const calculateRemaining = useCallback(() => {
    const target = typeof targetTime === 'string' ? new Date(targetTime) : targetTime
    const now = new Date()
    const diff = Math.max(0, Math.floor((target.getTime() - now.getTime()) / 1000))
    return diff
  }, [targetTime])

  useEffect(() => {
    setRemainingSeconds(calculateRemaining())

    const interval = setInterval(() => {
      const remaining = calculateRemaining()
      setRemainingSeconds(remaining)

      if (remaining <= 0) {
        clearInterval(interval)
        onComplete?.()
      }
    }, 1000)

    return () => clearInterval(interval)
  }, [calculateRemaining, onComplete])

  if (remainingSeconds <= 0) {
    return null
  }

  const minutes = Math.floor(remainingSeconds / 60)
  const seconds = remainingSeconds % 60

  return (
    <span className="text-muted-foreground text-sm">
      {t('forgotPassword.verify.resendIn', {
        time: minutes > 0
          ? `${minutes}:${seconds.toString().padStart(2, '0')}`
          : `${seconds}s`
      })}
    </span>
  )
}

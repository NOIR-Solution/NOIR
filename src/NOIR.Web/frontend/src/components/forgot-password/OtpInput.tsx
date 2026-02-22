import * as React from 'react'
import { useRef, useEffect, useState, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'

interface OtpInputProps {
  length?: number
  value: string
  onChange: (value: string) => void
  onComplete?: (value: string) => void
  disabled?: boolean
  error?: boolean
  autoFocus?: boolean
}

/**
 * 6-digit OTP input component with individual boxes
 * Features: auto-focus next box, backspace navigation, paste support, auto-submit
 */
export const OtpInput = ({
  length = 6,
  value,
  onChange,
  onComplete,
  disabled = false,
  error = false,
  autoFocus = true,
}: OtpInputProps) => {
  const { t } = useTranslation('auth')
  const inputRefs = useRef<(HTMLInputElement | null)[]>([])
  const [focusedIndex, setFocusedIndex] = useState<number | null>(null)
  // Track the last value for which onComplete was called to prevent duplicate calls
  const lastCompletedValueRef = useRef<string | null>(null)

  // Split value into individual digits
  const digits = value.split('').slice(0, length)
  while (digits.length < length) {
    digits.push('')
  }

  // Auto-focus first input on mount
  useEffect(() => {
    if (autoFocus && !disabled && inputRefs.current[0]) {
      inputRefs.current[0].focus()
    }
  }, [autoFocus, disabled])

  // Track completion state and prevent duplicate onComplete calls
  // The ref ensures we only call onComplete once per unique OTP value,
  // preventing infinite loops when parent components re-render on error.
  // The ref resets when value is incomplete, allowing re-entry of the same OTP.
  // Also respects disabled state to prevent calls during submission.
  useEffect(() => {
    if (value.length < length) {
      lastCompletedValueRef.current = null
      return
    }

    // Don't trigger completion if disabled (e.g., during form submission)
    if (disabled) {
      return
    }

    if (onComplete && value !== lastCompletedValueRef.current) {
      lastCompletedValueRef.current = value
      onComplete(value)
    }
  }, [value, length, onComplete, disabled])

  const focusInput = useCallback((index: number) => {
    const input = inputRefs.current[index]
    if (input) {
      input.focus()
      input.select()
    }
  }, [])

  const handleChange = useCallback((index: number, e: React.ChangeEvent<HTMLInputElement>) => {
    const inputValue = e.target.value

    // Handle single digit input
    if (/^\d$/.test(inputValue)) {
      const newDigits = [...digits]
      newDigits[index] = inputValue
      const newValue = newDigits.join('')
      onChange(newValue)

      // Move to next input
      if (index < length - 1) {
        focusInput(index + 1)
      }
    }
  }, [digits, length, onChange, focusInput])

  const handleKeyDown = useCallback((index: number, e: React.KeyboardEvent<HTMLInputElement>) => {
    switch (e.key) {
      case 'Backspace':
        e.preventDefault()
        const newDigits = [...digits]

        if (digits[index]) {
          // Clear current digit
          newDigits[index] = ''
          onChange(newDigits.join(''))
        } else if (index > 0) {
          // Move to previous and clear it
          newDigits[index - 1] = ''
          onChange(newDigits.join(''))
          focusInput(index - 1)
        }
        break

      case 'ArrowLeft':
        e.preventDefault()
        if (index > 0) {
          focusInput(index - 1)
        }
        break

      case 'ArrowRight':
        e.preventDefault()
        if (index < length - 1) {
          focusInput(index + 1)
        }
        break

      case 'Delete':
        e.preventDefault()
        const clearedDigits = [...digits]
        clearedDigits[index] = ''
        onChange(clearedDigits.join(''))
        break
    }
  }, [digits, length, onChange, focusInput])

  const handlePaste = useCallback((e: React.ClipboardEvent) => {
    e.preventDefault()
    const pastedData = e.clipboardData.getData('text')
    const pastedDigits = pastedData.replace(/\D/g, '').slice(0, length)

    if (pastedDigits.length > 0) {
      onChange(pastedDigits)

      // Focus last filled input or the next empty one
      const focusIndex = Math.min(pastedDigits.length, length - 1)
      focusInput(focusIndex)
    }
  }, [length, onChange, focusInput])

  const handleFocus = useCallback((index: number) => {
    setFocusedIndex(index)
    inputRefs.current[index]?.select()
  }, [])

  const handleBlur = useCallback(() => {
    setFocusedIndex(null)
  }, [])

  return (
    <div className="flex justify-center gap-2 sm:gap-3" onPaste={handlePaste}>
      {digits.map((digit, index) => (
        <input
          key={index}
          ref={(el) => {
            inputRefs.current[index] = el
          }}
          type="text"
          inputMode="numeric"
          pattern="[0-9]*"
          maxLength={1}
          value={digit}
          onChange={(e) => handleChange(index, e)}
          onKeyDown={(e) => handleKeyDown(index, e)}
          onFocus={() => handleFocus(index)}
          onBlur={handleBlur}
          disabled={disabled}
          className={cn(
            'w-12 h-14 sm:w-14 sm:h-16 text-center text-2xl font-semibold rounded-lg border-2',
            'bg-background transition-all duration-200',
            'focus:outline-none focus:ring-2 focus:ring-offset-2',
            disabled && 'opacity-50 cursor-not-allowed bg-muted',
            error
              ? 'border-destructive focus:border-destructive focus:ring-destructive/20'
              : focusedIndex === index
                ? 'border-blue-600 ring-2 ring-blue-600/20'
                : 'border-border hover:border-blue-400'
          )}
          aria-label={t('otp.digitAriaLabel', { current: index + 1, total: length, defaultValue: `Digit ${index + 1} of ${length}` })}
        />
      ))}
    </div>
  )
}

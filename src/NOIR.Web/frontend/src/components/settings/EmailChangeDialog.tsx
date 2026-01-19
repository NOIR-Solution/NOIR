/**
 * Email Change Dialog Component
 *
 * Multi-step dialog for changing email address with OTP verification:
 * 1. Enter new email address
 * 2. Enter OTP sent to new email
 * 3. Success message
 *
 * OTP step uses direct state management (matching Password Reset flow)
 * to avoid infinite loops from react-hook-form integration with OtpInput.
 */
import { useState, useCallback, useMemo, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { Mail, Loader2, CheckCircle2, ArrowLeft } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { OtpInput } from '@/components/forgot-password/OtpInput'
import { CountdownTimer } from '@/components/forgot-password/CountdownTimer'
import {
  requestEmailChange,
  verifyEmailChange,
  resendEmailChangeOtp,
  ApiError,
} from '@/services/profile'
import { useValidatedForm } from '@/hooks/useValidatedForm'
import { requestEmailChangeSchema } from '@/validation/schemas.generated'
import { createValidationTranslator } from '@/lib/validation-i18n'
import { z } from 'zod'

type Step = 'email' | 'otp' | 'success'

// Email step schema - extend to check new email differs from current
const createEmailStepSchema = (currentEmail: string) =>
  requestEmailChangeSchema.refine((data) => data.newEmail !== currentEmail, {
    message: 'New email must be different from current email',
    path: ['newEmail'],
  })

type EmailStepFormData = z.infer<typeof requestEmailChangeSchema>

interface EmailChangeDialogProps {
  currentEmail: string
  onSuccess: () => void
  trigger?: React.ReactNode
  /** Controlled mode: external open state */
  open?: boolean
  /** Controlled mode: external open state setter */
  onOpenChange?: (open: boolean) => void
}

export function EmailChangeDialog({
  currentEmail,
  onSuccess,
  trigger,
  open: controlledOpen,
  onOpenChange: controlledOnOpenChange,
}: EmailChangeDialogProps) {
  const { t } = useTranslation('auth')
  const { t: tCommon } = useTranslation('common')
  // Support both controlled and uncontrolled modes
  const [internalOpen, setInternalOpen] = useState(false)
  const isControlled = controlledOpen !== undefined
  const open = isControlled ? controlledOpen : internalOpen
  const setOpen = isControlled ? (controlledOnOpenChange ?? (() => {})) : setInternalOpen
  const [step, setStep] = useState<Step>('email')

  // Memoized translation function for validation errors
  const translateError = useMemo(() => createValidationTranslator(tCommon), [tCommon])

  // OTP session state
  const [sessionToken, setSessionToken] = useState('')
  const sessionTokenRef = useRef(sessionToken) // Ref to avoid stale closure
  const [maskedEmail, setMaskedEmail] = useState('')
  const [expiresAt, setExpiresAt] = useState<Date | null>(null)
  const [canResend, setCanResend] = useState(false)
  const [remainingResends, setRemainingResends] = useState(3)
  const [isResending, setIsResending] = useState(false)

  // OTP step - direct state management (matching Password Reset flow)
  // This avoids infinite loops from react-hook-form integration with OtpInput
  const [otp, setOtp] = useState('')
  const [otpError, setOtpError] = useState('')
  const [isVerifying, setIsVerifying] = useState(false)

  // Email step form
  const emailForm = useValidatedForm<EmailStepFormData>({
    schema: createEmailStepSchema(currentEmail),
    defaultValues: { newEmail: '' },
    onSubmit: async (data) => {
      const result = await requestEmailChange(data.newEmail)
      setSessionToken(result.sessionToken)
      sessionTokenRef.current = result.sessionToken // Keep ref in sync
      setMaskedEmail(result.maskedEmail)
      setExpiresAt(new Date(result.expiresAt))
      setStep('otp')
    },
  })

  const resetDialog = useCallback(() => {
    setStep('email')
    emailForm.reset()
    // Reset OTP step state
    setOtp('')
    setOtpError('')
    setIsVerifying(false)
    setSessionToken('')
    sessionTokenRef.current = '' // Keep ref in sync
    setMaskedEmail('')
    setExpiresAt(null)
    setCanResend(false)
    setRemainingResends(3)
  }, [emailForm])

  const handleOpenChange = (isOpen: boolean) => {
    setOpen(isOpen)
    if (!isOpen) {
      // Reset after close animation
      setTimeout(resetDialog, 300)
    }
  }

  const handleResendOtp = async () => {
    if (!canResend || remainingResends <= 0) return

    setIsResending(true)
    setOtpError('')

    try {
      const result = await resendEmailChangeOtp(sessionToken)
      setRemainingResends(result.remainingResends)
      setCanResend(false)
      setOtp('')

      if (result.nextResendAt) {
        setExpiresAt(new Date(result.nextResendAt))
      }
    } catch (err) {
      if (err instanceof ApiError) {
        setOtpError(err.message)
      } else {
        setOtpError(t('profile.email.resendFailed'))
      }
    } finally {
      setIsResending(false)
    }
  }

  const handleTimerComplete = () => {
    setCanResend(true)
  }

  const handleOtpChange = (value: string) => {
    setOtp(value)
    setOtpError('')
  }

  // OTP verification handler with guard (matching Password Reset flow)
  const handleOtpComplete = useCallback(async (value: string) => {
    // Guard against duplicate calls while verifying
    if (isVerifying) return

    setOtpError('')
    setIsVerifying(true)

    try {
      await verifyEmailChange(sessionTokenRef.current, value)
      setStep('success')
      // Refresh user data immediately so UI updates while success message shows
      await onSuccess()
      // Close dialog after short delay so user can see success message
      setTimeout(() => {
        handleOpenChange(false)
      }, 2000)
    } catch (err) {
      setOtp('') // Clear OTP on error (matching Password Reset flow)
      if (err instanceof ApiError) {
        setOtpError(err.message)
      } else {
        setOtpError(t('profile.email.verifyFailed'))
      }
    } finally {
      setIsVerifying(false)
    }
  }, [isVerifying, onSuccess, t])

  const handleBack = () => {
    setStep('email')
    setOtp('')
    setOtpError('')
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      {/* Only render trigger when not in controlled mode */}
      {!isControlled && (
        <DialogTrigger asChild>
          {trigger || (
            <Button type="button" variant="outline" size="sm">
              {t('profile.email.change')}
            </Button>
          )}
        </DialogTrigger>
      )}
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <div className="flex items-center gap-3 mb-1">
            <div className="flex items-center justify-center w-10 h-10 rounded-full bg-blue-100 dark:bg-blue-900/30">
              <Mail className="h-5 w-5 text-blue-600 dark:text-blue-400" />
            </div>
            <DialogTitle className="text-lg">{t('profile.email.changeTitle')}</DialogTitle>
          </div>
          <DialogDescription className="pl-[52px]">
            {step === 'email' && t('profile.email.changeDescription')}
            {step === 'otp' && t('profile.email.otpDescription', { email: maskedEmail })}
            {step === 'success' && t('profile.email.successDescription')}
          </DialogDescription>
        </DialogHeader>

        <div className="mt-4">
          {/* Step 1: Enter new email */}
          {step === 'email' && (
            <form
              onSubmit={(e) => {
                e.stopPropagation() // Prevent any bubbling to parent forms
                emailForm.handleSubmit(e)
              }}
              className="space-y-5"
            >
              <div className="space-y-2">
                <Label htmlFor="currentEmail" className="text-sm font-medium">
                  {t('profile.email.current')}
                </Label>
                <div className="relative">
                  <Mail className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                  <Input
                    id="currentEmail"
                    type="email"
                    value={currentEmail}
                    disabled
                    className="pl-10 bg-muted/50 text-muted-foreground"
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="newEmail" className="text-sm font-medium">
                  {t('profile.email.new')}
                </Label>
                <div className="relative group">
                  <Mail className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground group-focus-within:text-primary transition-colors" />
                  <Input
                    id="newEmail"
                    type="email"
                    {...emailForm.form.register('newEmail')}
                    placeholder={t('profile.email.newPlaceholder')}
                    className="pl-10"
                    disabled={emailForm.isSubmitting}
                    autoFocus
                    aria-invalid={!!emailForm.form.formState.errors.newEmail}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter') {
                        e.stopPropagation() // Prevent Enter from escaping to parent forms
                      }
                    }}
                  />
                </div>
                {emailForm.form.formState.errors.newEmail && (
                  <p className="text-sm text-destructive">{translateError(emailForm.form.formState.errors.newEmail.message)}</p>
                )}
              </div>

              {emailForm.serverError && (
                <div className="p-3 rounded-lg bg-destructive/10 border border-destructive/20">
                  <p className="text-sm text-destructive">{emailForm.serverError}</p>
                </div>
              )}

              <Button
                type="submit"
                disabled={emailForm.isSubmitting}
                className="w-full"
                onClick={(e) => e.stopPropagation()}
              >
                {emailForm.isSubmitting ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    {t('profile.email.sending')}
                  </>
                ) : (
                  t('profile.email.sendCode')
                )}
              </Button>
            </form>
          )}

          {/* Step 2: Enter OTP */}
          {step === 'otp' && (
            <div className="space-y-5">
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={handleBack}
                className="-ml-2 -mt-2"
              >
                <ArrowLeft className="mr-2 h-4 w-4" />
                {t('common.back')}
              </Button>

              <div className="text-center space-y-2">
                <div className="w-14 h-14 mx-auto rounded-full bg-blue-100 dark:bg-blue-900/30 flex items-center justify-center mb-4">
                  <Mail className="h-7 w-7 text-blue-600 dark:text-blue-400" />
                </div>
                <p className="text-sm text-muted-foreground">
                  {t('profile.email.enterCode')}
                </p>
                <p className="text-sm font-medium text-foreground">{maskedEmail}</p>
              </div>

              <OtpInput
                value={otp}
                onChange={handleOtpChange}
                onComplete={handleOtpComplete}
                disabled={isVerifying}
                error={!!otpError}
              />

              {otpError && (
                <div className="p-3 rounded-lg bg-destructive/10 border border-destructive/20">
                  <p className="text-sm text-destructive text-center">
                    {otpError}
                  </p>
                </div>
              )}

              {/* Loading State */}
              {isVerifying && (
                <div className="flex justify-center">
                  <div className="flex items-center gap-2 text-muted-foreground">
                    <Loader2 className="h-5 w-5 animate-spin" />
                    <span>{t('profile.email.verifying')}</span>
                  </div>
                </div>
              )}

              <div className="text-center space-y-2">
                {expiresAt && !canResend && (
                  <CountdownTimer
                    targetTime={expiresAt}
                    onComplete={handleTimerComplete}
                  />
                )}

                {canResend && remainingResends > 0 && (
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={handleResendOtp}
                    disabled={isResending}
                  >
                    {isResending ? (
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    ) : null}
                    {t('profile.email.resend')} ({remainingResends})
                  </Button>
                )}

                {remainingResends <= 0 && (
                  <p className="text-sm text-muted-foreground">
                    {t('profile.email.noMoreResends')}
                  </p>
                )}
              </div>
            </div>
          )}

          {/* Step 3: Success */}
          {step === 'success' && (
            <div className="text-center py-6">
              <div className="w-16 h-16 mx-auto mb-4 rounded-full bg-green-100 dark:bg-green-900/20 flex items-center justify-center">
                <CheckCircle2 className="h-8 w-8 text-green-600" />
              </div>
              <h3 className="text-lg font-semibold mb-2">
                {t('profile.email.successTitle')}
              </h3>
              <p className="text-sm text-muted-foreground">
                {t('profile.email.successMessage')}
              </p>
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}

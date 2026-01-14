/**
 * Email Change Dialog Component
 *
 * Multi-step dialog for changing email address with OTP verification:
 * 1. Enter new email address
 * 2. Enter OTP sent to new email
 * 3. Success message
 */
import { useState, useCallback, useMemo } from 'react'
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
import { requestEmailChangeSchema, verifyEmailChangeSchema } from '@/validation/schemas.generated'
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

// OTP step schema - sessionToken is managed in state, not form
const otpStepSchema = verifyEmailChangeSchema.pick({ otp: true })
type OtpStepFormData = z.infer<typeof otpStepSchema>

interface EmailChangeDialogProps {
  currentEmail: string
  onSuccess: () => void
  trigger?: React.ReactNode
}

export function EmailChangeDialog({
  currentEmail,
  onSuccess,
  trigger,
}: EmailChangeDialogProps) {
  const { t } = useTranslation('auth')
  const { t: tCommon } = useTranslation('common')
  const [open, setOpen] = useState(false)
  const [step, setStep] = useState<Step>('email')

  // Memoized translation function for validation errors
  const translateError = useMemo(() => createValidationTranslator(tCommon), [tCommon])

  // OTP session state
  const [sessionToken, setSessionToken] = useState('')
  const [maskedEmail, setMaskedEmail] = useState('')
  const [expiresAt, setExpiresAt] = useState<Date | null>(null)
  const [canResend, setCanResend] = useState(false)
  const [remainingResends, setRemainingResends] = useState(3)
  const [isResending, setIsResending] = useState(false)

  // Email step form
  const emailForm = useValidatedForm<EmailStepFormData>({
    schema: createEmailStepSchema(currentEmail),
    defaultValues: { newEmail: '' },
    onSubmit: async (data) => {
      const result = await requestEmailChange(data.newEmail)
      setSessionToken(result.sessionToken)
      setMaskedEmail(result.maskedEmail)
      setExpiresAt(new Date(result.expiresAt))
      setStep('otp')
    },
  })

  // OTP step form
  const otpForm = useValidatedForm<OtpStepFormData>({
    schema: otpStepSchema,
    defaultValues: { otp: '' },
    onSubmit: async (data) => {
      await verifyEmailChange(sessionToken, data.otp)
      setStep('success')
      // Refresh user data after short delay
      setTimeout(() => {
        onSuccess()
        handleOpenChange(false)
      }, 2000)
    },
  })

  const resetDialog = useCallback(() => {
    setStep('email')
    emailForm.reset()
    otpForm.reset()
    setSessionToken('')
    setMaskedEmail('')
    setExpiresAt(null)
    setCanResend(false)
    setRemainingResends(3)
  }, [emailForm, otpForm])

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

    try {
      const result = await resendEmailChangeOtp(sessionToken)
      setRemainingResends(result.remainingResends)
      setCanResend(false)

      if (result.nextResendAt) {
        setExpiresAt(new Date(result.nextResendAt))
      }
    } catch (err) {
      if (err instanceof ApiError) {
        otpForm.setServerError(err.message)
      } else {
        otpForm.setServerError(t('profile.email.resendFailed'))
      }
    } finally {
      setIsResending(false)
    }
  }

  const handleTimerComplete = () => {
    setCanResend(true)
  }

  const handleOtpChange = (value: string) => {
    otpForm.form.setValue('otp', value)
    otpForm.clearServerError()
  }

  const handleOtpComplete = (value: string) => {
    otpForm.form.setValue('otp', value)
    otpForm.handleSubmit()
  }

  const handleBack = () => {
    setStep('email')
    otpForm.reset()
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogTrigger asChild>
        {trigger || (
          <Button type="button" variant="outline" size="sm">
            {t('profile.email.change')}
          </Button>
        )}
      </DialogTrigger>
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
            <form onSubmit={emailForm.handleSubmit} className="space-y-5">
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
                value={otpForm.form.watch('otp')}
                onChange={handleOtpChange}
                onComplete={handleOtpComplete}
                disabled={otpForm.isSubmitting}
                error={!!otpForm.form.formState.errors.otp || !!otpForm.serverError}
              />

              {(otpForm.form.formState.errors.otp || otpForm.serverError) && (
                <div className="p-3 rounded-lg bg-destructive/10 border border-destructive/20">
                  <p className="text-sm text-destructive text-center">
                    {otpForm.form.formState.errors.otp?.message ? translateError(otpForm.form.formState.errors.otp.message) : otpForm.serverError}
                  </p>
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

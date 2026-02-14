import { useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { ViewTransitionLink } from '@/components/navigation/ViewTransitionLink'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { ArrowLeft, ShieldCheck, Mail, Loader2 } from 'lucide-react'
import { Button, Card, CardContent } from '@uikit'

import { LanguageSwitcher } from '@/i18n/LanguageSwitcher'
import { OtpInput } from '@/components/forgot-password/OtpInput'
import { CountdownTimer } from '@/components/forgot-password/CountdownTimer'
import { verifyOtp, resendOtp, ApiError } from '@/services/forgotPassword'

interface SessionData {
  sessionToken: string
  maskedEmail: string
  expiresAt: string
  otpLength: number
}

/**
 * OTP Verification Page
 * User enters the 6-digit code sent to their email
 */
export const VerifyOtpPage = () => {
  const navigate = useNavigate()
  const { t } = useTranslation('auth')
  const [otp, setOtp] = useState('')
  const [error, setError] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [isResending, setIsResending] = useState(false)
  const [session, setSession] = useState<SessionData | null>(null)
  const [canResend, setCanResend] = useState(false)
  const [nextResendAt, setNextResendAt] = useState<Date | null>(null)
  const [remainingResends, setRemainingResends] = useState(3)

  // Load session data on mount
  useEffect(() => {
    const data = sessionStorage.getItem('passwordReset')
    if (!data) {
      navigate('/forgot-password')
      return
    }

    try {
      const parsed = JSON.parse(data) as SessionData
      setSession(parsed)
      // Set initial cooldown to 60 seconds from now
      setNextResendAt(new Date(Date.now() + 60 * 1000))
    } catch {
      navigate('/forgot-password')
    }
  }, [navigate])

  const handleComplete = useCallback(async (value: string) => {
    if (!session || isLoading) return

    setError('')
    setIsLoading(true)

    try {
      const result = await verifyOtp(session.sessionToken, value)

      // Store reset token for next step
      sessionStorage.setItem('passwordReset', JSON.stringify({
        ...session,
        resetToken: result.resetToken,
        resetTokenExpiresAt: result.expiresAt,
      }))

      toast.success(t('forgotPassword.verify.success'))
      navigate('/forgot-password/reset')
    } catch (err) {
      setOtp('') // Clear OTP on error
      if (err instanceof ApiError) {
        setError(err.message)
      } else {
        setError(t('forgotPassword.verify.failed'))
      }
    } finally {
      setIsLoading(false)
    }
  }, [session, isLoading, navigate, t])

  const handleResend = async () => {
    if (!session || !canResend || isResending) return

    setIsResending(true)
    setError('')

    try {
      const result = await resendOtp(session.sessionToken)
      setNextResendAt(new Date(result.nextResendAt))
      setRemainingResends(result.remainingResends)
      setCanResend(false)
      setOtp('')
      toast.success(t('forgotPassword.verify.otpResent'))
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 429) {
          setError(t('forgotPassword.verify.cooldownActive'))
        } else {
          setError(err.message)
        }
      } else {
        setError(t('forgotPassword.verify.resendFailed'))
      }
    } finally {
      setIsResending(false)
    }
  }

  const handleCooldownComplete = useCallback(() => {
    setCanResend(true)
  }, [])

  if (!session) {
    return null
  }

  return (
    <div className="min-h-screen flex flex-col lg:flex-row w-full bg-background">
      {/* Left Side - Form */}
      <div className="flex-1 flex items-center justify-center p-4 sm:p-6 lg:p-8 relative">
        {/* Language Switcher */}
        <div className="absolute top-4 right-4 sm:top-6 sm:right-6 z-10">
          <LanguageSwitcher variant="dropdown" />
        </div>

        <div className="w-full max-w-md space-y-8 animate-fade-in">
          {/* Back Link */}
          <ViewTransitionLink
            to="/forgot-password"
            className="inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors"
          >
            <ArrowLeft className="w-4 h-4" />
            {t('forgotPassword.verify.back')}
          </ViewTransitionLink>

          {/* Logo & Title */}
          <div className="text-center space-y-2">
            <div className="flex items-center justify-center mb-6">
              <div className="flex items-center justify-center w-16 h-16 rounded-2xl bg-gradient-to-br from-blue-700 to-cyan-700 shadow-xl">
                <Mail className="w-8 h-8 text-white" />
              </div>
            </div>
            <h1 className="text-3xl font-bold tracking-tight text-foreground">
              {t('forgotPassword.verify.title')}
            </h1>
            <p className="text-muted-foreground">
              {t('forgotPassword.verify.subtitle', { email: session.maskedEmail })}
            </p>
          </div>

          {/* OTP Card */}
          <Card className="backdrop-blur-xl bg-background/80 border-border/50 shadow-2xl">
            <CardContent className="p-6 sm:p-8 space-y-6">
              {/* OTP Input */}
              <OtpInput
                length={session.otpLength}
                value={otp}
                onChange={setOtp}
                onComplete={handleComplete}
                disabled={isLoading}
                error={!!error}
              />

              {/* Error Message */}
              {error && (
                <div className="p-4 rounded-xl bg-destructive/10 border border-destructive/20 animate-fade-in">
                  <p className="text-sm text-destructive font-medium text-center">{error}</p>
                </div>
              )}

              {/* Loading State */}
              {isLoading && (
                <div className="flex justify-center">
                  <div className="flex items-center gap-2 text-muted-foreground">
                    <Loader2 className="h-5 w-5 animate-spin" />
                    <span>{t('forgotPassword.verify.verifying')}</span>
                  </div>
                </div>
              )}

              {/* Resend Section */}
              <div className="text-center space-y-2 pt-2">
                <p className="text-sm text-muted-foreground">
                  {t('forgotPassword.verify.didntReceive')}
                </p>
                <div className="flex items-center justify-center gap-2">
                  {canResend ? (
                    <Button
                      variant="link"
                      onClick={handleResend}
                      disabled={isResending || remainingResends <= 0}
                      className="text-blue-600 hover:text-blue-700 p-0 h-auto font-medium"
                    >
                      {isResending ? t('forgotPassword.verify.resending') : t('forgotPassword.verify.resendCode')}
                    </Button>
                  ) : nextResendAt ? (
                    <CountdownTimer
                      targetTime={nextResendAt}
                      onComplete={handleCooldownComplete}
                    />
                  ) : null}
                </div>
                {remainingResends < 3 && remainingResends > 0 && (
                  <p className="text-xs text-muted-foreground">
                    {t('forgotPassword.verify.resendsRemaining', { count: remainingResends })}
                  </p>
                )}
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Right Side - Decorative Panel */}
      <div className="hidden lg:flex flex-1 relative overflow-hidden">
        <div className="absolute inset-0 bg-gradient-to-br from-blue-700 via-cyan-700 to-teal-700" />
        <div className="absolute inset-0">
          <div className="absolute top-0 -left-4 w-72 h-72 bg-white/20 rounded-full blur-3xl animate-blob" />
          <div className="absolute top-0 -right-4 w-72 h-72 bg-cyan-400/30 rounded-full blur-3xl animate-blob animation-delay-2000" />
          <div className="absolute -bottom-8 left-20 w-72 h-72 bg-teal-400/30 rounded-full blur-3xl animate-blob animation-delay-4000" />
        </div>
        <div className="absolute inset-0 opacity-10">
          <div className="absolute inset-0" style={{
            backgroundImage: 'radial-gradient(circle at 1px 1px, white 1px, transparent 0)',
            backgroundSize: '40px 40px'
          }} />
        </div>
        <div className="relative z-10 flex items-center justify-center p-8 lg:p-12 w-full">
          <div className="text-center space-y-8 max-w-md animate-fade-in-up">
            <div className="inline-flex rounded-2xl p-5 bg-white/10 backdrop-blur-sm shadow-lg">
              <ShieldCheck className="w-14 h-14 text-white" />
            </div>
            <h2 className="text-4xl lg:text-5xl font-bold text-white leading-tight">
              {t('forgotPassword.verify.secureTitle')}
            </h2>
            <p className="text-xl text-white/80 leading-relaxed">
              {t('forgotPassword.verify.secureDescription')}
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}

export default VerifyOtpPage

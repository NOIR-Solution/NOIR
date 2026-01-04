import * as React from 'react'
import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Mail, ArrowLeft, ShieldCheck, KeyRound } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent } from '@/components/ui/card'
import { LanguageSwitcher } from '@/i18n/LanguageSwitcher'
import { isValidEmail } from '@/lib/validation'
import { requestPasswordReset, ApiError } from '@/services/forgotPassword'

/**
 * Forgot Password Page - Email entry step
 * Requests OTP to be sent to user's email
 */
export default function ForgotPasswordPage() {
  const navigate = useNavigate()
  const { t } = useTranslation('auth')
  const [email, setEmail] = useState('')
  const [error, setError] = useState('')
  const [isLoading, setIsLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')

    if (!email) {
      setError(t('forgotPassword.emailRequired'))
      return
    }

    if (!isValidEmail(email)) {
      setError(t('forgotPassword.invalidEmail'))
      return
    }

    setIsLoading(true)

    try {
      const result = await requestPasswordReset(email)

      // Store session info for next step
      sessionStorage.setItem('passwordReset', JSON.stringify({
        sessionToken: result.sessionToken,
        maskedEmail: result.maskedEmail,
        expiresAt: result.expiresAt,
        otpLength: result.otpLength,
      }))

      toast.success(t('forgotPassword.otpSent'))
      navigate('/forgot-password/verify')
    } catch (err) {
      if (err instanceof ApiError) {
        // Rate limiting
        if (err.status === 429) {
          setError(t('forgotPassword.tooManyRequests'))
        } else {
          setError(err.message)
        }
      } else {
        setError(t('forgotPassword.requestFailed'))
      }
    } finally {
      setIsLoading(false)
    }
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
          <Link
            to="/login"
            className="inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors"
          >
            <ArrowLeft className="w-4 h-4" />
            {t('forgotPassword.backToLogin')}
          </Link>

          {/* Logo & Title */}
          <div className="text-center space-y-2">
            <div className="flex items-center justify-center mb-6">
              <div className="flex items-center justify-center w-16 h-16 rounded-2xl bg-gradient-to-br from-blue-700 to-cyan-700 shadow-xl">
                <KeyRound className="w-8 h-8 text-white" />
              </div>
            </div>
            <h1 className="text-3xl font-bold tracking-tight text-foreground">
              {t('forgotPassword.title')}
            </h1>
            <p className="text-muted-foreground">
              {t('forgotPassword.subtitle')}
            </p>
          </div>

          {/* Form Card */}
          <Card className="backdrop-blur-xl bg-background/80 border-border/50 shadow-2xl">
            <CardContent className="p-6 sm:p-8">
              <form onSubmit={handleSubmit} className="space-y-6">
                {/* Email Field */}
                <div className="space-y-2">
                  <Label htmlFor="email" className="text-foreground font-medium">
                    {t('forgotPassword.emailLabel')}
                  </Label>
                  <div className="relative group">
                    <Mail className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground group-focus-within:text-blue-600 transition-colors" />
                    <Input
                      id="email"
                      type="email"
                      placeholder={t('forgotPassword.emailPlaceholder')}
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      className="pl-10 h-12 bg-background border-border focus:border-blue-600 focus:ring-blue-600/20 transition-all"
                      required
                      autoFocus
                    />
                  </div>
                  <p className="text-xs text-muted-foreground">
                    {t('forgotPassword.emailHint')}
                  </p>
                </div>

                {/* Error Message */}
                {error && (
                  <div className="p-4 rounded-xl bg-destructive/10 border border-destructive/20 animate-fade-in">
                    <p className="text-sm text-destructive font-medium">{error}</p>
                  </div>
                )}

                {/* Submit Button */}
                <Button
                  type="submit"
                  disabled={isLoading}
                  className="w-full h-12 text-base font-semibold rounded-xl bg-gradient-to-r from-blue-700 to-cyan-700 hover:from-blue-800 hover:to-cyan-800 text-white shadow-lg hover:shadow-xl transition-all duration-200 hover:scale-[1.01]"
                >
                  {isLoading ? (
                    <span className="flex items-center gap-2">
                      <svg className="animate-spin h-5 w-5" viewBox="0 0 24 24">
                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                      </svg>
                      {t('forgotPassword.sending')}
                    </span>
                  ) : (
                    t('forgotPassword.sendCode')
                  )}
                </Button>
              </form>
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
              {t('forgotPassword.secureTitle')}
            </h2>
            <p className="text-xl text-white/80 leading-relaxed">
              {t('forgotPassword.secureDescription')}
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}

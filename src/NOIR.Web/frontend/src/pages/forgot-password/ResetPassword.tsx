import * as React from 'react'
import { useState, useEffect, useMemo } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { ArrowLeft, Lock, Eye, EyeOff, ShieldCheck, KeyRound, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent } from '@/components/ui/card'
import { LanguageSwitcher } from '@/i18n/LanguageSwitcher'
import { PasswordStrengthIndicator } from '@/components/forgot-password/PasswordStrengthIndicator'
import { getPasswordStrength } from '@/lib/passwordValidation'
import { resetPasswordSchema } from '@/validation/schemas.generated'
import { translateValidationError } from '@/lib/validation-i18n'
import { resetPassword, ApiError } from '@/services/forgotPassword'

interface SessionData {
  sessionToken: string
  maskedEmail: string
  resetToken: string
  resetTokenExpiresAt: string
}

/**
 * Reset Password Page
 * User sets their new password after OTP verification
 */
export default function ResetPasswordPage() {
  const navigate = useNavigate()
  const { t } = useTranslation('auth')
  const { t: tCommon } = useTranslation('common')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)
  const [error, setError] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [session, setSession] = useState<SessionData | null>(null)

  // Memoized translation function for validation errors
  const translateError = useMemo(() => (msg: string | undefined) => translateValidationError(msg, tCommon), [tCommon])

  // Load session data on mount
  useEffect(() => {
    const data = sessionStorage.getItem('passwordReset')
    if (!data) {
      navigate('/forgot-password')
      return
    }

    try {
      const parsed = JSON.parse(data) as SessionData
      if (!parsed.resetToken) {
        navigate('/forgot-password/verify')
        return
      }
      setSession(parsed)
    } catch {
      navigate('/forgot-password')
    }
  }, [navigate])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')

    if (!session) return

    // Validate using generated Zod schema
    const validation = resetPasswordSchema.safeParse({
      resetToken: session.resetToken,
      newPassword: password,
    })
    if (!validation.success) {
      const firstError = validation.error.issues[0]
      setError(translateError(firstError?.message) || t('forgotPassword.reset.invalidInput'))
      return
    }

    // Additional UX validation: password strength
    const strength = getPasswordStrength(password)
    if (!strength.isValid) {
      setError(t('forgotPassword.reset.passwordTooWeak'))
      return
    }

    // Additional UX validation: confirm password match
    if (password !== confirmPassword) {
      setError(t('forgotPassword.reset.passwordsDoNotMatch'))
      return
    }

    setIsLoading(true)

    try {
      await resetPassword(session.resetToken, password)

      // Clear session data
      sessionStorage.removeItem('passwordReset')

      toast.success(t('forgotPassword.reset.success'))
      navigate('/forgot-password/success')
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message)
      } else {
        setError(t('forgotPassword.reset.failed'))
      }
    } finally {
      setIsLoading(false)
    }
  }

  if (!session) {
    return null
  }

  const passwordStrength = getPasswordStrength(password)

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
            to="/forgot-password/verify"
            className="inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors"
          >
            <ArrowLeft className="w-4 h-4" />
            {t('forgotPassword.reset.back')}
          </Link>

          {/* Logo & Title */}
          <div className="text-center space-y-2">
            <div className="flex items-center justify-center mb-6">
              <div className="flex items-center justify-center w-16 h-16 rounded-2xl bg-gradient-to-br from-blue-700 to-cyan-700 shadow-xl">
                <KeyRound className="w-8 h-8 text-white" />
              </div>
            </div>
            <h1 className="text-3xl font-bold tracking-tight text-foreground">
              {t('forgotPassword.reset.title')}
            </h1>
            <p className="text-muted-foreground">
              {t('forgotPassword.reset.subtitle')}
            </p>
          </div>

          {/* Form Card */}
          <Card className="backdrop-blur-xl bg-background/80 border-border/50 shadow-2xl">
            <CardContent className="p-6 sm:p-8">
              <form onSubmit={handleSubmit} className="space-y-6">
                {/* New Password Field */}
                <div className="space-y-2">
                  <Label htmlFor="password" className="text-foreground font-medium">
                    {t('forgotPassword.reset.newPassword')}
                  </Label>
                  <div className="relative group">
                    <Lock className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground group-focus-within:text-blue-600 transition-colors" />
                    <Input
                      id="password"
                      type={showPassword ? 'text' : 'password'}
                      placeholder={t('forgotPassword.reset.newPasswordPlaceholder')}
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                      className="pl-10 pr-10 h-12 bg-background border-border focus:border-blue-600 focus:ring-blue-600/20 transition-all"
                      required
                      autoFocus
                    />
                    <button
                      type="button"
                      onClick={() => setShowPassword(!showPassword)}
                      className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors p-1 rounded-md hover:bg-accent"
                      aria-label={showPassword ? t('login.hidePassword') : t('login.showPassword')}
                    >
                      {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                    </button>
                  </div>

                  {/* Password Strength Indicator */}
                  <PasswordStrengthIndicator password={password} />
                </div>

                {/* Confirm Password Field */}
                <div className="space-y-2">
                  <Label htmlFor="confirmPassword" className="text-foreground font-medium">
                    {t('forgotPassword.reset.confirmPassword')}
                  </Label>
                  <div className="relative group">
                    <Lock className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground group-focus-within:text-blue-600 transition-colors" />
                    <Input
                      id="confirmPassword"
                      type={showConfirmPassword ? 'text' : 'password'}
                      placeholder={t('forgotPassword.reset.confirmPasswordPlaceholder')}
                      value={confirmPassword}
                      onChange={(e) => setConfirmPassword(e.target.value)}
                      className="pl-10 pr-10 h-12 bg-background border-border focus:border-blue-600 focus:ring-blue-600/20 transition-all"
                      required
                    />
                    <button
                      type="button"
                      onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                      className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors p-1 rounded-md hover:bg-accent"
                      aria-label={showConfirmPassword ? t('login.hidePassword') : t('login.showPassword')}
                    >
                      {showConfirmPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                    </button>
                  </div>
                  {/* Match indicator */}
                  {confirmPassword && (
                    <p className={`text-xs ${password === confirmPassword ? 'text-green-600' : 'text-destructive'}`}>
                      {password === confirmPassword
                        ? t('forgotPassword.reset.passwordsMatch')
                        : t('forgotPassword.reset.passwordsDoNotMatch')}
                    </p>
                  )}
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
                  disabled={isLoading || !passwordStrength.isValid || password !== confirmPassword}
                  className="w-full h-12 text-base font-semibold rounded-xl bg-gradient-to-r from-blue-700 to-cyan-700 hover:from-blue-800 hover:to-cyan-800 text-white shadow-lg hover:shadow-xl transition-all duration-200 hover:scale-[1.01] disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:scale-100"
                >
                  {isLoading ? (
                    <span className="flex items-center gap-2">
                      <Loader2 className="h-5 w-5 animate-spin" />
                      {t('forgotPassword.reset.resetting')}
                    </span>
                  ) : (
                    t('forgotPassword.reset.submit')
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
              {t('forgotPassword.reset.secureTitle')}
            </h2>
            <p className="text-xl text-white/80 leading-relaxed">
              {t('forgotPassword.reset.secureDescription')}
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}

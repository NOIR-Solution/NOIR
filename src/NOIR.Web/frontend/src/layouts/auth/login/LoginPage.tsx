import { useState, useEffect } from "react"
import { useNavigate, useSearchParams } from "react-router-dom"
import { ViewTransitionLink } from '@/components/navigation/ViewTransitionLink'
import { useTranslation } from "react-i18next"
import { Mail, Lock, Eye, EyeOff, ShieldCheck, Sparkles, Loader2 } from "lucide-react"
import { Button, Input, Label, Card, CardContent, ThemeToggleCompact } from '@uikit'
import { useLogin } from "@/hooks/useLogin"
import { useAuthContext } from "@/contexts/AuthContext"
import { LanguageSwitcher } from "@/i18n/LanguageSwitcher"
import { OrganizationSelection } from "@/components/auth/OrganizationSelection"
import type { TenantOption } from "@/types"
import { motion, AnimatePresence } from "framer-motion"

/**
 * Validates the return URL to prevent open redirect attacks (CWE-601).
 * Only allows relative URLs that start with a single forward slash.
 */
const validateReturnUrl = (url: string): string => {
  // Only allow relative URLs that start with / and don't start with //
  // This prevents open redirect to external domains
  if (url && url.startsWith('/') && !url.startsWith('//')) {
    return url
  }
  // Default redirect to portal after login
  return '/portal'
}

/**
 * Login Page - Multi-step login with organization selection
 * Step 1: Enter email + password → Submit
 * Step 2: If multiple tenants → Show organization selection step
 * Step 3: Complete login and redirect
 */
export const LoginPage = () => {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const returnUrl = searchParams.get('returnUrl') || '/portal'
  const { login } = useLogin()
  const { isAuthenticated, isLoading: isAuthLoading } = useAuthContext()
  const { t } = useTranslation('auth')

  // Step management
  type LoginStep = 'credentials' | 'organization'
  const [currentStep, setCurrentStep] = useState<LoginStep>('credentials')

  // Form state
  const [email, setEmail] = useState(import.meta.env.DEV ? "admin@noir.local" : "")
  const [password, setPassword] = useState(import.meta.env.DEV ? "123qwe" : "")
  const [showPassword, setShowPassword] = useState(false)

  // Organization selection state
  const [availableTenants, setAvailableTenants] = useState<TenantOption[]>([])

  // Loading/error state
  const [isLoggingIn, setIsLoggingIn] = useState(false)
  const [serverError, setServerError] = useState<string | null>(null)
  const [emailError, setEmailError] = useState<string | null>(null)
  const [passwordError, setPasswordError] = useState<string | null>(null)

  // Redirect to portal if already logged in
  useEffect(() => {
    if (isAuthenticated) {
      const safeReturnUrl = validateReturnUrl(returnUrl)
      navigate(safeReturnUrl, { replace: true })
    }
  }, [isAuthenticated, navigate, returnUrl])

  // Handle login form submission
  const handleLoginSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setServerError(null)
    setEmailError(null)
    setPasswordError(null)

    // Basic validation
    let hasErrors = false
    if (!email.trim()) {
      setEmailError(t('login.emailRequired', 'Email is required'))
      hasErrors = true
    } else {
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
      if (!emailRegex.test(email)) {
        setEmailError(t('login.emailInvalid', 'Please enter a valid email address'))
        hasErrors = true
      }
    }

    if (!password.trim()) {
      setPasswordError(t('login.passwordRequired', 'Password is required'))
      hasErrors = true
    }

    if (hasErrors) return

    setIsLoggingIn(true)

    try {
      const result = await login({ email, password })

      if (result.success) {
        // Login successful - redirect
        const safeReturnUrl = validateReturnUrl(returnUrl)
        navigate(safeReturnUrl)
      } else if (result.requiresTenantSelection && result.availableTenants) {
        // Multiple tenants matched - show organization selection step
        setAvailableTenants(result.availableTenants)
        setCurrentStep('organization')
      }
    } catch (error) {
      if (error instanceof Error) {
        setServerError(error.message)
      } else {
        setServerError(t('login.genericError', 'An error occurred. Please try again.'))
      }
    } finally {
      setIsLoggingIn(false)
    }
  }

  // Handle organization selection
  const handleOrganizationSelect = async (tenantId: string | null) => {
    setIsLoggingIn(true)
    setServerError(null)

    try {
      const result = await login({
        email,
        password,
        tenantId
      })

      if (result.success) {
        const safeReturnUrl = validateReturnUrl(returnUrl)
        navigate(safeReturnUrl)
      }
    } catch (error) {
      if (error instanceof Error) {
        setServerError(error.message)
      } else {
        setServerError(t('login.genericError', 'An error occurred. Please try again.'))
      }
    } finally {
      setIsLoggingIn(false)
    }
  }

  // Handle back to login
  const handleBackToLogin = () => {
    setCurrentStep('credentials')
    setServerError(null)
  }

  // Show loading indicator while checking auth status (prevents flash of login form)
  if (isAuthLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    )
  }

  return (
    <div className="min-h-screen flex flex-col lg:flex-row w-full bg-background">
      {/* Left Side - Login Form or Organization Selection */}
      <div className="flex-1 flex items-center justify-center p-4 sm:p-6 lg:p-8 relative">
        {/* Language & Theme Switcher - Top Right */}
        <div className="absolute top-4 right-4 sm:top-6 sm:right-6 z-10 flex items-center gap-2">
          <LanguageSwitcher variant="dropdown" />
          <ThemeToggleCompact />
        </div>

        <AnimatePresence mode="wait">
          {currentStep === 'organization' ? (
            <motion.div
              key="organization"
              initial={{ opacity: 0, x: 20 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: -20 }}
              transition={{ duration: 0.3, ease: 'easeInOut' }}
              className="w-full flex items-center justify-center"
            >
              <OrganizationSelection
                organizations={availableTenants}
                onSelect={handleOrganizationSelect}
                onBack={handleBackToLogin}
                userEmail={email}
                isLoading={isLoggingIn}
                error={serverError}
              />
            </motion.div>
          ) : (
            <motion.div
              key="credentials"
              initial={{ opacity: 0, x: -20 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: 20 }}
              transition={{ duration: 0.3, ease: 'easeInOut' }}
              className="w-full flex items-center justify-center"
            >
              <div className="w-full max-w-md space-y-8 animate-fade-in">
                {/* Logo & Title */}
                <div className="text-center space-y-2">
                  <div className="flex items-center justify-center mb-6">
                    <div className="flex items-center justify-center w-16 h-16 rounded-2xl bg-gradient-to-br from-blue-700 to-cyan-700 shadow-xl">
                      <ShieldCheck className="w-8 h-8 text-white" />
                    </div>
                  </div>
                  <h1 className="text-3xl font-bold tracking-tight text-foreground">
                    {t('login.pageTitle')}
                  </h1>
                  <p className="text-muted-foreground">
                    {t('login.secureAccess')}
                  </p>
                </div>

                {/* Login Card */}
                <Card className="backdrop-blur-xl bg-background/80 border-border/50 shadow-2xl">
                  <CardContent className="p-6 sm:p-8">
                    <form onSubmit={handleLoginSubmit} className="space-y-6">
                      {/* Email Field */}
                      <div className="space-y-2">
                        <Label htmlFor="email" className="text-foreground font-medium">
                          {t('login.emailLabel')}
                        </Label>
                        <div className="relative group">
                          <Mail className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground group-focus-within:text-blue-600 transition-colors" />
                          <Input
                            id="email"
                            type="email"
                            placeholder={t('login.emailPlaceholder')}
                            value={email}
                            onChange={(e) => {
                              setEmail(e.target.value)
                              setEmailError(null)
                            }}
                            className="pl-10 h-12 bg-background border-border focus:border-blue-600 focus:ring-blue-600/20 transition-all"
                            aria-label={t('login.emailLabel')}
                            aria-invalid={!!emailError}
                            autoFocus
                          />
                        </div>
                        {emailError && (
                          <p role="alert" aria-live="assertive" className="text-sm font-medium text-destructive">{emailError}</p>
                        )}
                      </div>

                      {/* Password Field */}
                      <div className="space-y-2">
                        <Label htmlFor="password" className="text-foreground font-medium">
                          {t('login.password')}
                        </Label>
                        <div className="relative group">
                          <Lock className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground group-focus-within:text-blue-600 transition-colors" />
                          <Input
                            id="password"
                            type={showPassword ? "text" : "password"}
                            placeholder={t('login.passwordPlaceholder')}
                            value={password}
                            onChange={(e) => {
                              setPassword(e.target.value)
                              setPasswordError(null)
                            }}
                            className="pl-10 pr-10 h-12 bg-background border-border focus:border-blue-600 focus:ring-blue-600/20 transition-all"
                            aria-label={t('login.password')}
                            aria-invalid={!!passwordError}
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
                        {passwordError && (
                          <p role="alert" aria-live="assertive" className="text-sm font-medium text-destructive">{passwordError}</p>
                        )}
                        {/* Forgot Password Link */}
                        <div className="flex justify-end mt-1">
                          <ViewTransitionLink
                            to="/forgot-password"
                            className="text-sm text-blue-600 hover:text-blue-700 hover:underline transition-colors"
                          >
                            {t('login.forgotPassword')}
                          </ViewTransitionLink>
                        </div>
                      </div>

                      {/* Error Message */}
                      {serverError && (
                        <div role="alert" aria-live="assertive" className="p-4 rounded-xl bg-destructive/10 border border-destructive/20 animate-fade-in">
                          <p className="text-sm text-destructive font-medium">{serverError}</p>
                        </div>
                      )}

                      {/* Submit Button */}
                      <Button
                        type="submit"
                        disabled={isLoggingIn}
                        className="w-full h-12 text-base font-semibold rounded-xl bg-gradient-to-r from-blue-700 to-cyan-700 hover:from-blue-800 hover:to-cyan-800 text-white shadow-lg hover:shadow-xl transition-all duration-200 hover:scale-[1.01]"
                      >
                        {isLoggingIn ? (
                          <span className="flex items-center gap-2">
                            <Loader2 className="h-5 w-5 animate-spin" />
                            {t('login.submitting')}
                          </span>
                        ) : (
                          t('login.submit')
                        )}
                      </Button>
                    </form>
                  </CardContent>
                </Card>

                {/* Footer Text */}
                <div className="text-center">
                  <p className="text-xs text-muted-foreground">
                    {t('login.protectedBy')}
                  </p>
                </div>
              </div>
            </motion.div>
          )}
        </AnimatePresence>
      </div>

      {/* Right Side - Decorative Panel (Static - Never Reloads) */}
      <div className="hidden lg:flex flex-1 relative overflow-hidden">
        {/* Gradient Background - Deeper blue-teal */}
        <div className="absolute inset-0 bg-gradient-to-br from-blue-700 via-cyan-700 to-teal-700" />

        {/* Animated Blobs */}
        <div className="absolute inset-0">
          <div className="absolute top-0 -left-4 w-72 h-72 bg-white/20 rounded-full blur-3xl animate-blob" />
          <div className="absolute top-0 -right-4 w-72 h-72 bg-cyan-400/30 rounded-full blur-3xl animate-blob animation-delay-2000" />
          <div className="absolute -bottom-8 left-20 w-72 h-72 bg-teal-400/30 rounded-full blur-3xl animate-blob animation-delay-4000" />
        </div>

        {/* Grid Pattern Overlay */}
        <div className="absolute inset-0 opacity-10">
          <div className="absolute inset-0" style={{
            backgroundImage: 'radial-gradient(circle at 1px 1px, white 1px, transparent 0)',
            backgroundSize: '40px 40px'
          }} />
        </div>

        {/* Content */}
        <div className="relative z-10 flex items-center justify-center p-8 lg:p-12 w-full">
          <div className="text-center space-y-8 max-w-md animate-fade-in-up">
            {/* Icon */}
            <div className="inline-flex rounded-2xl p-5 bg-white/10 backdrop-blur-sm shadow-lg">
              <ShieldCheck className="w-14 h-14 text-white" />
            </div>

            {/* Title */}
            <h2 className="text-4xl lg:text-5xl font-bold text-white leading-tight">
              {t('login.secureAuthTitle')}
            </h2>

            {/* Description */}
            <p className="text-xl text-white/80 leading-relaxed">
              {t('login.secureAuthDescription')}
            </p>

            {/* Feature Pills */}
            <div className="flex flex-wrap justify-center gap-3 pt-4">
              <div className="flex items-center gap-2 px-4 py-2 rounded-full bg-white/10 backdrop-blur-sm text-white/90 text-sm">
                <Sparkles className="w-4 h-4" />
                <span>Enterprise Grade</span>
              </div>
              <div className="flex items-center gap-2 px-4 py-2 rounded-full bg-white/10 backdrop-blur-sm text-white/90 text-sm">
                <ShieldCheck className="w-4 h-4" />
                <span>256-bit Encryption</span>
              </div>
            </div>

          </div>
        </div>
      </div>
    </div>
  )
}

export default LoginPage

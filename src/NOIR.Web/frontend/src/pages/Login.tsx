import * as React from "react"
import { useState } from "react"
import { useNavigate, useSearchParams } from "react-router-dom"
import { useTranslation } from "react-i18next"
import { Mail, Lock, Eye, EyeOff, ShieldCheck } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Card, CardContent } from "@/components/ui/card"
import { useLogin } from "@/hooks/useLogin"
import { themeClasses } from "@/config/theme"
import { isValidEmail } from "@/lib/validation"

interface AnimatedBlobProps {
  color: string;
  position: string;
  delay?: string;
}

const AnimatedBlob = ({ color, position, delay = "" }: AnimatedBlobProps) => (
  <div className={`absolute ${position} w-72 h-72 ${color} rounded-full mix-blend-screen filter blur-xl opacity-70 animate-blob ${delay}`} />
)

const GradientWave = () => (
  <div className="absolute inset-0 opacity-20">
    <svg className="absolute inset-0 w-full h-full" preserveAspectRatio="none" viewBox="0 0 1440 560">
      <defs>
        <linearGradient id="gradient1" x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stopColor={themeClasses.svgGradientStart} stopOpacity="0.3" />
          <stop offset="100%" stopColor={themeClasses.svgGradientEnd} stopOpacity="0.1" />
        </linearGradient>
      </defs>
      <path fill="url(#gradient1)" d="M0,224L48,213.3C96,203,192,181,288,181.3C384,181,480,203,576,218.7C672,235,768,245,864,234.7C960,224,1056,192,1152,186.7C1248,181,1344,203,1392,213.3L1440,224L1440,560L1392,560C1344,560,1248,560,1152,560C1056,560,960,560,864,560C768,560,672,560,576,560C480,560,384,560,288,560C192,560,96,560,48,560L0,560Z" />
    </svg>
  </div>
)

/**
 * Validates the return URL to prevent open redirect attacks (CWE-601).
 * Only allows relative URLs that start with a single forward slash.
 */
function validateReturnUrl(url: string): string {
  // Only allow relative URLs that start with / and don't start with //
  // This prevents open redirect to external domains
  if (url && url.startsWith('/') && !url.startsWith('//')) {
    return url
  }
  return '/'
}

export default function LoginPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const returnUrl = searchParams.get('returnUrl') || '/'
  const { login } = useLogin()
  const { t } = useTranslation('auth')

  const [email, setEmail] = useState("")
  const [password, setPassword] = useState("")
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState("")
  const [isLoading, setIsLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError("")

    if (!email || !password) {
      setError(t('login.enterBothFields'))
      return
    }

    if (!isValidEmail(email)) {
      setError(t('login.invalidEmail'))
      return
    }

    setIsLoading(true)

    try {
      await login({ email, password })
      // Redirect to validated return URL (prevents open redirect)
      const safeReturnUrl = validateReturnUrl(returnUrl)
      navigate(safeReturnUrl)
    } catch (err) {
      setError(err instanceof Error ? err.message : t('login.authFailed'))
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex flex-col lg:flex-row w-full bg-background">
      <div className="flex-1 flex items-center justify-center p-4 sm:p-6 lg:p-8 bg-white">
        <div className="w-full max-w-md space-y-8">
          <div className="text-center space-y-2">
            <div className="flex items-center justify-center mb-6">
              <div className={`flex items-center justify-center w-16 h-16 rounded-2xl ${themeClasses.iconContainer} ${themeClasses.iconContainerShadow}`}>
                <ShieldCheck className="w-8 h-8 text-white" />
              </div>
            </div>
            <h1 className="text-3xl font-bold tracking-tight text-gray-900">
              {t('login.pageTitle')}
            </h1>
            <p className="text-gray-600">
              {t('login.secureAccess')}
            </p>
          </div>

          <Card className="bg-white border-gray-200 shadow-xl">
            <CardContent className="p-6 sm:p-8">
              <form onSubmit={handleSubmit} className="space-y-6">
                <div className="space-y-2">
                  <Label htmlFor="email" className="text-gray-700">
                    {t('login.emailLabel')}
                  </Label>
                  <div className="relative">
                    <Mail className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
                    <Input
                      id="email"
                      type="email"
                      placeholder={t('login.emailPlaceholder')}
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      className="pl-10 bg-white border-gray-300 text-gray-900 placeholder:text-gray-400"
                      required
                    />
                  </div>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="password" className="text-gray-700">
                    {t('login.password')}
                  </Label>
                  <div className="relative">
                    <Lock className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
                    <Input
                      id="password"
                      type={showPassword ? "text" : "password"}
                      placeholder={t('login.passwordPlaceholder')}
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                      className="pl-10 pr-10 bg-white border-gray-300 text-gray-900 placeholder:text-gray-400"
                      required
                    />
                    <button
                      type="button"
                      onClick={() => setShowPassword(!showPassword)}
                      className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 transition-colors"
                      aria-label={showPassword ? t('login.hidePassword') : t('login.showPassword')}
                    >
                      {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                    </button>
                  </div>
                </div>

                {error && (
                  <div className="p-3 rounded-lg bg-red-50 border border-red-200">
                    <p className="text-sm text-red-600">{error}</p>
                  </div>
                )}

                <Button
                  type="submit"
                  disabled={isLoading}
                  className={`w-full h-11 ${themeClasses.buttonPrimary} font-medium transition-all duration-300`}
                >
                  {isLoading ? (
                    <span className="flex items-center gap-2">
                      <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24">
                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                      </svg>
                      {t('login.submitting')}
                    </span>
                  ) : (
                    t('login.submit')
                  )}
                </Button>
              </form>

              {/* Only show default credentials in development */}
              {import.meta.env.DEV && (
                <div className="mt-6 text-center">
                  <p className="text-xs text-gray-500">
                    {t('login.defaultCredentials')}: <code className="bg-gray-100 px-1 py-0.5 rounded">admin@noir.local</code> / <code className="bg-gray-100 px-1 py-0.5 rounded">123qwe</code>
                  </p>
                </div>
              )}
            </CardContent>
          </Card>

          <div className="text-center">
            <p className="text-xs text-gray-500">
              {t('login.protectedBy')}
            </p>
          </div>
        </div>
      </div>

      <div className="hidden lg:flex flex-1 relative overflow-hidden">
        <div className={`absolute inset-0 ${themeClasses.gradient}`} />

        <div className="absolute inset-0">
          <AnimatedBlob color={themeClasses.blobPrimary} position="top-0 -left-4" />
          <AnimatedBlob color={themeClasses.blobSecondary} position="top-0 -right-4" delay="animation-delay-2000" />
          <AnimatedBlob color={themeClasses.blobAccent} position="-bottom-8 left-20" delay="animation-delay-4000" />
        </div>

        <GradientWave />

        <div className="relative z-10 flex items-center justify-center p-8 lg:p-12 w-full">
          <div className="text-center space-y-6 max-w-md">
            <div className="inline-flex rounded-full p-4 bg-white/10 backdrop-blur-sm mb-4">
              <ShieldCheck className="w-12 h-12 text-white" />
            </div>
            <h2 className="text-3xl lg:text-4xl font-bold text-white">
              {t('login.secureAuthTitle')}
            </h2>
            <p className="text-lg text-white/80">
              {t('login.secureAuthDescription')}
            </p>
            <div className="flex justify-center gap-2 pt-4">
              <div className="w-2 h-2 rounded-full bg-white/100" />
              <div className="w-2 h-2 rounded-full bg-white/80" />
              <div className="w-2 h-2 rounded-full bg-white/60" />
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

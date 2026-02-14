import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { CheckCircle, ShieldCheck, ArrowRight } from 'lucide-react'
import { Button, Card, CardContent } from '@uikit'

import { LanguageSwitcher } from '@/i18n/LanguageSwitcher'

/**
 * Success Page - Password reset complete
 * Confirmation message with link to login
 */
export default function AuthSuccessPage() {
  const navigate = useNavigate()
  const { t } = useTranslation('auth')

  const handleGoToLogin = () => {
    navigate('/login')
  }

  return (
    <div className="min-h-screen flex flex-col lg:flex-row w-full bg-background">
      {/* Left Side - Content */}
      <div className="flex-1 flex items-center justify-center p-4 sm:p-6 lg:p-8 relative">
        {/* Language Switcher */}
        <div className="absolute top-4 right-4 sm:top-6 sm:right-6 z-10">
          <LanguageSwitcher variant="dropdown" />
        </div>

        <div className="w-full max-w-md space-y-8 animate-fade-in">
          {/* Success Card */}
          <Card className="backdrop-blur-xl bg-background/80 border-border/50 shadow-2xl">
            <CardContent className="p-8 sm:p-10 text-center space-y-6">
              {/* Success Icon */}
              <div className="flex justify-center">
                <div className="flex items-center justify-center w-20 h-20 rounded-full bg-green-100 dark:bg-green-900/30">
                  <CheckCircle className="w-12 h-12 text-green-600 dark:text-green-500" />
                </div>
              </div>

              {/* Title & Description */}
              <div className="space-y-2">
                <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-foreground">
                  {t('forgotPassword.success.title')}
                </h1>
                <p className="text-muted-foreground">
                  {t('forgotPassword.success.description')}
                </p>
              </div>

              {/* Security Notice */}
              <div className="p-4 rounded-xl bg-blue-50 dark:bg-blue-900/20 border border-blue-100 dark:border-blue-800">
                <p className="text-sm text-blue-700 dark:text-blue-300">
                  {t('forgotPassword.success.securityNotice')}
                </p>
              </div>

              {/* Go to Login Button */}
              <Button
                onClick={handleGoToLogin}
                className="w-full h-12 text-base font-semibold rounded-xl bg-gradient-to-r from-blue-700 to-cyan-700 hover:from-blue-800 hover:to-cyan-800 text-white shadow-lg hover:shadow-xl transition-all duration-200 hover:scale-[1.01]"
              >
                <span className="flex items-center gap-2">
                  {t('forgotPassword.success.goToLogin')}
                  <ArrowRight className="w-5 h-5" />
                </span>
              </Button>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Right Side - Decorative Panel */}
      <div className="hidden lg:flex flex-1 relative overflow-hidden">
        <div className="absolute inset-0 bg-gradient-to-br from-green-600 via-teal-600 to-cyan-700" />
        <div className="absolute inset-0">
          <div className="absolute top-0 -left-4 w-72 h-72 bg-white/20 rounded-full blur-3xl animate-blob" />
          <div className="absolute top-0 -right-4 w-72 h-72 bg-teal-400/30 rounded-full blur-3xl animate-blob animation-delay-2000" />
          <div className="absolute -bottom-8 left-20 w-72 h-72 bg-green-400/30 rounded-full blur-3xl animate-blob animation-delay-4000" />
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
              {t('forgotPassword.success.secureTitle')}
            </h2>
            <p className="text-xl text-white/80 leading-relaxed">
              {t('forgotPassword.success.secureDescription')}
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}

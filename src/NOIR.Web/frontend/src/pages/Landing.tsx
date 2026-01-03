import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ShieldCheck, ArrowRight, Sparkles } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { themeClasses } from '@/config/theme'
import { LanguageSwitcher } from '@/i18n/LanguageSwitcher'
import { AnimatedBlob, GradientWave } from '@/components/decorative/BackgroundAnimations'

/**
 * Landing Page - Professional hero with animated gradient matching Login page design
 * Inspired by modern SaaS landing pages and shadcn/ui patterns
 */
export default function LandingPage() {
  const { t } = useTranslation('common')

  return (
    <div className="min-h-screen bg-white relative overflow-hidden">
      {/* Animated Background */}
      <div className="absolute inset-0 overflow-hidden">
        <div className={`absolute inset-0 ${themeClasses.gradient} opacity-[0.03]`} />
        <AnimatedBlob color={themeClasses.blobPrimary} position="top-0 -left-20" size="w-96 h-96" />
        <AnimatedBlob color={themeClasses.blobSecondary} position="top-20 -right-20" size="w-80 h-80" delay="animation-delay-2000" />
        <AnimatedBlob color={themeClasses.blobAccent} position="bottom-0 left-1/3" size="w-72 h-72" delay="animation-delay-4000" />
        <GradientWave id="landingGradient" />
      </div>

      {/* Header */}
      <header className="fixed top-0 left-0 right-0 z-50 bg-white/80 backdrop-blur-md border-b border-gray-100/50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between h-16">
            {/* Logo */}
            <Link to="/" className="flex items-center gap-3 group">
              <div className={`flex h-10 w-10 items-center justify-center rounded-xl ${themeClasses.iconContainer} ${themeClasses.iconContainerShadow} transition-transform group-hover:scale-105`}>
                <ShieldCheck className="h-5 w-5 text-white" />
              </div>
              <span className="font-bold text-xl text-gray-900">NOIR</span>
            </Link>

            {/* Navigation */}
            <div className="flex items-center gap-4">
              <LanguageSwitcher variant="dropdown" />
              <Link to="/login">
                <Button variant="ghost" size="sm" className="text-gray-600 hover:text-gray-900">
                  {t('auth.signIn')}
                </Button>
              </Link>
              <Link to="/portal">
                <Button size="sm" className={`${themeClasses.buttonPrimary} transition-all duration-300 hover:scale-[1.02]`}>
                  {t('nav.portal')}
                  <ArrowRight className="ml-1.5 h-4 w-4" />
                </Button>
              </Link>
            </div>
          </div>
        </div>
      </header>

      {/* Hero Section */}
      <section className="relative min-h-screen flex items-center justify-center pt-16">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20 lg:py-32">
          <div className="text-center max-w-4xl mx-auto">
            {/* Badge */}
            <div className="inline-flex items-center gap-2 rounded-full border border-gray-200/80 bg-white/80 backdrop-blur-sm px-4 py-2 text-sm text-gray-600 mb-8 shadow-sm">
              <Sparkles className={`h-4 w-4 ${themeClasses.textPrimary}`} />
              <span>{t('landing.badge')}</span>
            </div>

            {/* Headline */}
            <h1 className="text-4xl sm:text-5xl lg:text-6xl xl:text-7xl font-bold tracking-tight text-gray-900 leading-tight">
              {t('landing.welcomeTo')}{' '}
              <span className={`${themeClasses.textPrimary} relative`}>
                NOIR
                <svg
                  className="absolute -bottom-2 left-0 w-full h-3 opacity-30"
                  viewBox="0 0 200 12"
                  preserveAspectRatio="none"
                >
                  <path
                    d="M0,8 Q50,0 100,8 T200,8"
                    fill="none"
                    stroke={themeClasses.svgGradientStart}
                    strokeWidth="4"
                    strokeLinecap="round"
                  />
                </svg>
              </span>
            </h1>

            {/* Subheadline */}
            <p className="mt-8 text-lg sm:text-xl text-gray-600 max-w-2xl mx-auto leading-relaxed">
              {t('landing.description')}
            </p>

            {/* CTA Buttons */}
            <div className="mt-12 flex flex-col sm:flex-row items-center justify-center gap-4">
              <Link to="/portal">
                <Button
                  size="lg"
                  className={`${themeClasses.buttonPrimary} h-12 px-8 text-base transition-all duration-300 hover:scale-[1.02] hover:shadow-xl`}
                >
                  {t('landing.accessPortal')}
                  <ArrowRight className="ml-2 h-5 w-5" />
                </Button>
              </Link>
              <Link to="/login">
                <Button
                  variant="outline"
                  size="lg"
                  className="h-12 px-8 text-base border-gray-300 hover:bg-gray-50 transition-all duration-300"
                >
                  {t('auth.signIn')}
                </Button>
              </Link>
            </div>

            {/* Trust Indicators */}
            <div className="mt-16 flex flex-wrap items-center justify-center gap-8 text-sm text-gray-500">
              <div className="flex items-center gap-2">
                <div className={`w-2 h-2 rounded-full ${themeClasses.bgPrimary}`} />
                <span>Enterprise Security</span>
              </div>
              <div className="flex items-center gap-2">
                <div className={`w-2 h-2 rounded-full ${themeClasses.bgPrimary}`} />
                <span>Real-time Sync</span>
              </div>
              <div className="flex items-center gap-2">
                <div className={`w-2 h-2 rounded-full ${themeClasses.bgPrimary}`} />
                <span>Multi-tenant</span>
              </div>
            </div>
          </div>
        </div>

        {/* Decorative Elements */}
        <div className="absolute bottom-0 left-0 right-0 h-px bg-gradient-to-r from-transparent via-gray-200 to-transparent" />
      </section>

      {/* Footer */}
      <footer className="relative z-10 border-t border-gray-100 bg-white/80 backdrop-blur-sm py-8">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
            <div className="flex items-center gap-2">
              <div className={`flex h-6 w-6 items-center justify-center rounded-lg ${themeClasses.iconContainer}`}>
                <ShieldCheck className="h-3 w-3 text-white" />
              </div>
              <span className="text-sm font-medium text-gray-900">NOIR</span>
            </div>
            <p className="text-sm text-gray-500">
              © {new Date().getFullYear()} NOIR. {t('landing.allRightsReserved')}
            </p>
          </div>
        </div>
      </footer>
    </div>
  )
}

import { ViewTransitionLink } from '@/components/navigation/ViewTransitionLink'
import { useTranslation } from 'react-i18next'
import { ShieldCheck, ArrowRight, Sparkles, Zap, Users } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { LanguageSwitcher } from '@/i18n/LanguageSwitcher'
import { ThemeToggleCompact } from '@/components/ui/theme-toggle'

/**
 * Landing Page - Professional hero section with blue-teal color scheme
 * Uses universally accessible colors (avoids red-green, colorblind-friendly)
 */
export default function LandingPage() {
  const { t } = useTranslation('common')

  return (
    <div className="relative min-h-screen w-full overflow-hidden bg-background">
      {/* Animated Gradient Mesh Background - Deeper blue-teal theme */}
      <div className="absolute inset-0 -z-10">
        <div className="absolute inset-0 bg-gradient-to-br from-blue-600/20 via-cyan-600/20 to-teal-600/20 animate-gradient-shift" />
        <div className="absolute top-0 left-1/4 w-96 h-96 bg-blue-600/30 rounded-full blur-3xl animate-blob" />
        <div className="absolute top-1/4 right-1/4 w-96 h-96 bg-cyan-600/30 rounded-full blur-3xl animate-blob animation-delay-2000" />
        <div className="absolute bottom-1/4 left-1/3 w-96 h-96 bg-teal-600/30 rounded-full blur-3xl animate-blob animation-delay-4000" />
      </div>

      {/* Glassmorphism Navigation Header */}
      <nav className="relative z-50 backdrop-blur-xl bg-background/80 supports-[backdrop-filter]:bg-background/80 shadow-sm border-b border-border/50">
        <div className="container mx-auto px-6 lg:px-8">
          <div className="flex items-center justify-between h-20">
            {/* Logo */}
            <ViewTransitionLink to="/" className="flex items-center gap-3 group">
              <div className="w-10 h-10 rounded-2xl bg-gradient-to-br from-blue-700 to-cyan-700 flex items-center justify-center shadow-lg transition-transform group-hover:scale-105">
                <ShieldCheck className="w-5 h-5 text-white" />
              </div>
              <span className="text-xl font-semibold text-foreground">NOIR</span>
            </ViewTransitionLink>

            {/* Right Section */}
            <div className="flex items-center gap-3">
              <LanguageSwitcher variant="dropdown" />
              <ThemeToggleCompact />
              <ViewTransitionLink to="/portal">
                <Button className="px-6 h-10 text-[15px] font-medium rounded-full bg-foreground text-background hover:bg-foreground/90 shadow-lg hover:shadow-xl transition-all duration-200">
                  {t('nav.portal')}
                  <ArrowRight className="ml-2 w-4 h-4" />
                </Button>
              </ViewTransitionLink>
            </div>
          </div>
        </div>
      </nav>

      {/* Hero Content */}
      <div className="relative z-10 container mx-auto px-4 sm:px-6 lg:px-8 pt-20 sm:pt-32 pb-16 sm:pb-24">
        <div className="max-w-4xl mx-auto text-center">
          {/* Animated Badge */}
          <div className="inline-flex items-center justify-center mb-8 animate-fade-in">
            <Badge
              variant="secondary"
              className="px-5 py-2.5 text-[15px] font-normal backdrop-blur-xl bg-background/60 border border-border/50 shadow-sm hover:shadow-md transition-all duration-200 hover:scale-[1.01] rounded-full"
            >
              <Sparkles className="w-4 h-4 mr-2 text-blue-600" />
              <span className="text-foreground">
                {t('landing.badge')}
              </span>
            </Badge>
          </div>

          {/* Headline with Gradient Text - Blue-teal */}
          <h1 className="text-5xl sm:text-6xl md:text-7xl lg:text-8xl font-semibold text-foreground mb-6 leading-[1.05] tracking-tight animate-fade-in-up">
            {t('landing.welcomeTo')}{' '}
            <span className="bg-gradient-to-r from-blue-600 via-cyan-600 to-teal-600 bg-clip-text text-transparent">
              NOIR
            </span>
          </h1>

          {/* Subheadline */}
          <p className="text-xl sm:text-2xl md:text-[28px] text-muted-foreground mb-12 max-w-3xl mx-auto leading-snug font-normal animate-fade-in-up animation-delay-200">
            {t('landing.description')}
          </p>

          {/* CTA Button */}
          <div className="flex items-center justify-center mb-16 animate-fade-in-up animation-delay-400">
            <ViewTransitionLink to="/portal">
              <Button
                size="lg"
                className="px-8 py-6 text-lg font-semibold rounded-full bg-foreground text-background hover:bg-foreground/90 shadow-2xl hover:shadow-3xl transition-all duration-200 hover:scale-[1.02]"
              >
                {t('landing.accessPortal')}
                <ArrowRight className="ml-2 w-5 h-5" />
              </Button>
            </ViewTransitionLink>
          </div>

          {/* Trust Indicators - Blue-teal gradients */}
          <div className="animate-fade-in-up animation-delay-600">
            <p className="text-sm text-muted-foreground mb-8 tracking-wide font-normal">
              {t('landing.trustedBy', { defaultValue: 'Trusted by innovative teams worldwide' })}
            </p>
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 max-w-4xl mx-auto">
              <div className="flex items-center justify-center gap-3 p-5 rounded-2xl backdrop-blur-xl bg-background/60 border border-border/50 shadow-sm hover:shadow-md transition-all duration-200 hover:scale-[1.01]">
                <div className="w-11 h-11 rounded-full bg-gradient-to-br from-blue-600 to-cyan-600 flex items-center justify-center flex-shrink-0 shadow-md">
                  <ShieldCheck className="w-5 h-5 text-white" />
                </div>
                <div className="text-left">
                  <p className="text-[15px] font-semibold text-foreground">Enterprise Security</p>
                  <p className="text-sm text-muted-foreground font-normal">Bank-level encryption</p>
                </div>
              </div>
              <div className="flex items-center justify-center gap-3 p-5 rounded-2xl backdrop-blur-xl bg-background/60 border border-border/50 shadow-sm hover:shadow-md transition-all duration-200 hover:scale-[1.01]">
                <div className="w-11 h-11 rounded-full bg-gradient-to-br from-cyan-600 to-teal-600 flex items-center justify-center flex-shrink-0 shadow-md">
                  <Zap className="w-5 h-5 text-white" />
                </div>
                <div className="text-left">
                  <p className="text-[15px] font-semibold text-foreground">Real-time Sync</p>
                  <p className="text-sm text-muted-foreground font-normal">99.9% uptime SLA</p>
                </div>
              </div>
              <div className="flex items-center justify-center gap-3 p-5 rounded-2xl backdrop-blur-xl bg-background/60 border border-border/50 shadow-sm hover:shadow-md transition-all duration-200 hover:scale-[1.01]">
                <div className="w-11 h-11 rounded-full bg-gradient-to-br from-teal-600 to-blue-600 flex items-center justify-center flex-shrink-0 shadow-md">
                  <Users className="w-5 h-5 text-white" />
                </div>
                <div className="text-left">
                  <p className="text-[15px] font-semibold text-foreground">Multi-tenant</p>
                  <p className="text-sm text-muted-foreground font-normal">Scalable architecture</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Footer */}
      <footer className="relative z-10 border-t border-border/50 backdrop-blur-xl bg-background/60 py-8">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
            <div className="flex items-center gap-2">
              <div className="flex h-6 w-6 items-center justify-center rounded-lg bg-gradient-to-br from-blue-700 to-cyan-700">
                <ShieldCheck className="h-3 w-3 text-white" />
              </div>
              <span className="text-sm font-medium text-foreground">NOIR</span>
            </div>
            <div className="flex items-center gap-6">
              <ViewTransitionLink to="/terms" className="text-sm text-muted-foreground hover:text-foreground transition-colors">
                Terms
              </ViewTransitionLink>
              <ViewTransitionLink to="/privacy" className="text-sm text-muted-foreground hover:text-foreground transition-colors">
                Privacy
              </ViewTransitionLink>
              <p className="text-sm text-muted-foreground">
                © {new Date().getFullYear()} NOIR. {t('landing.allRightsReserved')}
              </p>
            </div>
          </div>
        </div>
      </footer>
    </div>
  )
}

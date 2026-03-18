import { useTranslation } from 'react-i18next'
import { ViewTransitionLink } from '@/components/navigation/ViewTransitionLink'
import { ShieldCheck, ArrowLeft } from 'lucide-react'
import { Button, Skeleton } from '@uikit'
import { usePublicLegalPageQuery } from '@/hooks/queries/usePublicQueries'
import { useRegionalSettingsOptional } from '@/contexts/RegionalSettingsContext'

/**
 * Public Terms of Service page.
 * Fetches the legal page content from the API and renders it.
 */
export const TermsPage = () => {
  const { t } = useTranslation('common')
  const regional = useRegionalSettingsOptional()
  const { data: page, isLoading: loading, isError } = usePublicLegalPageQuery('terms-of-service')
  const error = isError ? t('welcome.terms.loadError') : null

  if (loading) {
    return (
      <div className="min-h-screen bg-background">
        <div className="max-w-4xl mx-auto px-4 py-16">
          <div className="space-y-4">
            <Skeleton className="h-8 w-64" />
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-3/4" />
            <Skeleton className="h-4 w-5/6" />
          </div>
        </div>
      </div>
    )
  }

  if (error || !page) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="text-center">
          <p className="text-muted-foreground mb-4">{error || t('welcome.pageNotFound')}</p>
          <Button variant="outline" asChild>
            <ViewTransitionLink to="/">
              <ArrowLeft className="h-4 w-4 mr-2" />
              {t('welcome.backToHome')}
            </ViewTransitionLink>
          </Button>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      {/* Navigation Header */}
      <nav className="border-b border-border/50 bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60 sticky top-0 z-50">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between h-16">
            <ViewTransitionLink to="/" className="flex items-center gap-2 group">
              <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-blue-700 to-cyan-700 flex items-center justify-center">
                <ShieldCheck className="w-4 h-4 text-white" />
              </div>
              <span className="text-lg font-semibold text-foreground">NOIR</span>
            </ViewTransitionLink>
            <Button variant="ghost" size="sm" asChild>
              <ViewTransitionLink to="/">
                <ArrowLeft className="h-4 w-4 mr-2" />
                Back
              </ViewTransitionLink>
            </Button>
          </div>
        </div>
      </nav>

      {/* Content */}
      <main className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        <h1 className="text-3xl font-bold text-foreground mb-2">{page.title}</h1>
        <p className="text-sm text-muted-foreground mb-8">
          Last updated: {regional?.formatDate(page.lastModified) ?? new Date(page.lastModified).toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric' })}
        </p>
        <div
          className="prose prose-neutral dark:prose-invert max-w-none"
          dangerouslySetInnerHTML={{ __html: page.htmlContent }}
        />
      </main>

      {/* Footer */}
      <footer className="border-t border-border/50 py-6">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <div className="h-5 w-5 rounded bg-gradient-to-br from-blue-700 to-cyan-700 flex items-center justify-center">
                <ShieldCheck className="h-3 w-3 text-white" />
              </div>
              <span className="text-sm font-medium text-foreground">NOIR</span>
            </div>
            <div className="flex items-center gap-4 text-sm text-muted-foreground">
              <ViewTransitionLink to="/privacy" className="hover:text-foreground transition-colors">
                Privacy Policy
              </ViewTransitionLink>
            </div>
          </div>
        </div>
      </footer>
    </div>
  )
}

export default TermsPage

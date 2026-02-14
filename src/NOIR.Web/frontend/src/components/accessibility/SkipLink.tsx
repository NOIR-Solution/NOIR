import { useTranslation } from 'react-i18next'

interface SkipLinkProps {
  /** ID of the element to skip to */
  targetId: string
  /** Custom label (defaults to i18n key) */
  children?: React.ReactNode
}

/**
 * SkipLink - Accessibility component for keyboard users to skip navigation
 *
 * Visually hidden by default, becomes visible on focus.
 * Should be placed at the very top of the page.
 */
export const SkipLink = ({ targetId, children }: SkipLinkProps) => {
  const { t } = useTranslation('common')

  return (
    <a
      href={`#${targetId}`}
      className="sr-only focus:not-sr-only focus:fixed focus:top-4 focus:left-4 focus:z-[100]
                 focus:px-4 focus:py-2 focus:bg-primary focus:text-primary-foreground
                 focus:rounded-md focus:shadow-lg focus:outline-none focus:ring-2 focus:ring-ring
                 focus:ring-offset-2 focus:ring-offset-background transition-all"
    >
      {children || t('accessibility.skipToMain', 'Skip to main content')}
    </a>
  )
}

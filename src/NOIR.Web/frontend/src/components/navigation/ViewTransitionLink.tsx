import { Link, type LinkProps, useNavigate } from 'react-router-dom'
import { startViewTransition, supportsViewTransitions } from '@/hooks/useViewTransition'

interface ViewTransitionLinkProps extends LinkProps {
  /** Direction hint for CSS animations. Defaults to 'forward'. */
  vtDirection?: 'forward' | 'back'
}

/**
 * ViewTransitionLink - Drop-in replacement for React Router's <Link>
 *
 * Wraps navigation in document.startViewTransition() for smooth
 * native browser page transitions. Falls back to regular <Link>
 * behavior in unsupported browsers.
 *
 * Supports all standard <Link> props including `onClick`.
 *
 * @example
 * <ViewTransitionLink to="/portal/products">Products</ViewTransitionLink>
 * <ViewTransitionLink to="/portal" vtDirection="back">Back</ViewTransitionLink>
 */
export function ViewTransitionLink({
  to,
  onClick,
  vtDirection = 'forward',
  children,
  ...props
}: ViewTransitionLinkProps) {
  const navigate = useNavigate()

  const handleClick = (e: React.MouseEvent<HTMLAnchorElement>) => {
    // Allow default browser behavior for modifier keys (new tab, etc.)
    if (e.metaKey || e.ctrlKey || e.shiftKey || e.altKey || e.button !== 0) {
      onClick?.(e)
      return
    }

    // Allow parent onClick to run (e.g. closing mobile sidebar)
    onClick?.(e)

    // If parent called preventDefault, respect it
    if (e.defaultPrevented) return

    e.preventDefault()

    if (!supportsViewTransitions) {
      navigate(to)
      return
    }

    startViewTransition(() => {
      navigate(to)
    }, vtDirection)
  }

  return (
    <Link to={to} onClick={handleClick} {...props}>
      {children}
    </Link>
  )
}

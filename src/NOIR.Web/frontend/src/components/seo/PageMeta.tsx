import { useHead, type MetaTag } from './useHead'
import { generateMetaTitle, generateMetaDescription } from '../../lib/seo'

export interface PageMetaProps {
  /**
   * Page title (will be suffixed with site name).
   * Example: "Products" → "Products | NOIR"
   */
  title: string

  /**
   * Meta description for SEO (150-160 characters recommended).
   * If not provided, uses a default description.
   */
  description?: string

  /**
   * Keywords for SEO (comma-separated).
   * Example: "ecommerce, products, shopping"
   */
  keywords?: string

  /**
   * Canonical URL (absolute URL).
   * If not provided, uses current window location.
   */
  canonicalUrl?: string

  /**
   * Open Graph image URL (absolute URL).
   * For social media sharing previews.
   */
  ogImage?: string

  /**
   * Open Graph image width (recommended: 1200).
   */
  ogImageWidth?: number

  /**
   * Open Graph image height (recommended: 630).
   */
  ogImageHeight?: number

  /**
   * Open Graph type.
   * Default: "website"
   */
  ogType?: 'website' | 'article' | 'product' | 'profile'

  /**
   * Whether to allow search engines to index this page.
   * Default: true
   */
  allowIndexing?: boolean

  /**
   * Site name for Open Graph.
   * Default: "NOIR"
   */
  siteName?: string

  /**
   * Twitter card type.
   * Default: "summary_large_image"
   */
  twitterCard?: 'summary' | 'summary_large_image' | 'app' | 'player'

  /**
   * Twitter site handle (e.g., "@yoursite").
   */
  twitterSite?: string

  /**
   * Additional custom meta tags.
   */
  customMeta?: MetaTag[]
}

/**
 * General-purpose SEO meta tags component for all pages.
 * Provides Open Graph, Twitter Cards, and standard meta tags.
 *
 * Usage:
 * ```tsx
 * <PageMeta
 *   title="Products"
 *   description="Browse our collection of products"
 *   keywords="ecommerce, products, shopping"
 *   ogImage="/images/og-products.jpg"
 * />
 * ```
 *
 * Features:
 * - Automatic title suffix with site name
 * - Open Graph tags for social sharing
 * - Twitter Card tags
 * - Canonical URL support
 * - Robots meta (index/noindex)
 * - Keywords meta
 */
export const PageMeta = ({
  title,
  description,
  keywords,
  canonicalUrl,
  ogImage,
  ogImageWidth = 1200,
  ogImageHeight = 630,
  ogType = 'website',
  allowIndexing = true,
  siteName = 'NOIR',
  twitterCard = 'summary_large_image',
  twitterSite,
  customMeta = []
}: PageMetaProps) => {
  // Generate full title with site name
  const fullTitle = generateMetaTitle(title, siteName)

  // Use provided description or generate default
  const metaDescription = description || generateMetaDescription(title)

  // Use provided canonical URL or current location
  const canonical = canonicalUrl || window.location.href

  // Build meta tags array
  const meta: MetaTag[] = [
    // Basic meta
    { name: 'description', content: metaDescription },

    // Robots
    {
      name: 'robots',
      content: allowIndexing ? 'index, follow' : 'noindex, nofollow'
    },

    // Open Graph
    { property: 'og:type', content: ogType },
    { property: 'og:title', content: title },
    { property: 'og:description', content: metaDescription },
    { property: 'og:url', content: canonical },
    { property: 'og:site_name', content: siteName },

    // Twitter Card
    { name: 'twitter:card', content: twitterCard },
    { name: 'twitter:title', content: title },
    { name: 'twitter:description', content: metaDescription }
  ]

  // Add keywords if provided
  if (keywords) {
    meta.push({ name: 'keywords', content: keywords })
  }

  // Add Open Graph image if provided
  if (ogImage) {
    meta.push(
      { property: 'og:image', content: ogImage },
      { property: 'og:image:width', content: String(ogImageWidth) },
      { property: 'og:image:height', content: String(ogImageHeight) },
      { name: 'twitter:image', content: ogImage }
    )
  }

  // Add Twitter site handle if provided
  if (twitterSite) {
    meta.push({ name: 'twitter:site', content: twitterSite })
  }

  // Add custom meta tags
  meta.push(...customMeta)

  useHead({
    title: fullTitle,
    meta,
    link: [{ rel: 'canonical', href: canonical }]
  })

  return null
}

export default PageMeta

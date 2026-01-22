import { useHead, type MetaTag } from './useHead'
import { generateMetaTitle, generateMetaDescription } from '@/lib/seo'

export interface BlogPostMetaProps {
  title: string
  slug: string
  excerpt?: string | null
  contentHtml?: string | null
  metaTitle?: string | null
  metaDescription?: string | null
  featuredImageUrl?: string | null
  featuredImageWidth?: number | null
  featuredImageHeight?: number | null
  authorName?: string | null
  publishedAt?: string | null
  categoryName?: string | null
  allowIndexing?: boolean
  canonicalUrl?: string | null
  siteUrl?: string
  siteName?: string
}

/**
 * Meta tags for blog posts including Open Graph and Twitter Cards.
 * Improves social sharing and SEO.
 *
 * Usage:
 * ```tsx
 * <BlogPostMeta
 *   title={post.title}
 *   slug={post.slug}
 *   metaTitle={post.metaTitle}
 *   metaDescription={post.metaDescription}
 *   excerpt={post.excerpt}
 *   featuredImageUrl={post.featuredImageUrl}
 * />
 * ```
 */
export function BlogPostMeta({
  title,
  slug,
  excerpt,
  contentHtml,
  metaTitle,
  metaDescription,
  featuredImageUrl,
  featuredImageWidth,
  featuredImageHeight,
  authorName,
  publishedAt,
  categoryName,
  allowIndexing = true,
  canonicalUrl,
  siteUrl = window.location.origin,
  siteName = 'NOIR'
}: BlogPostMetaProps) {
  // Use effective meta values (custom or auto-generated)
  const pageTitle = metaTitle || title
  const fullTitle = generateMetaTitle(pageTitle, siteName)
  const description = metaDescription || generateMetaDescription(excerpt, contentHtml)
  const articleUrl = canonicalUrl || `${siteUrl}/blog/${slug}`

  const meta: MetaTag[] = [
    // Basic meta
    { name: 'description', content: description },

    // Robots
    {
      name: 'robots',
      content: allowIndexing ? 'index, follow' : 'noindex, nofollow'
    },

    // Open Graph
    { property: 'og:type', content: 'article' },
    { property: 'og:title', content: pageTitle },
    { property: 'og:description', content: description },
    { property: 'og:url', content: articleUrl },
    { property: 'og:site_name', content: siteName },

    // Twitter Card
    { name: 'twitter:card', content: featuredImageUrl ? 'summary_large_image' : 'summary' },
    { name: 'twitter:title', content: pageTitle },
    { name: 'twitter:description', content: description }
  ]

  // Add image meta tags if featured image exists
  if (featuredImageUrl) {
    meta.push(
      { property: 'og:image', content: featuredImageUrl },
      { name: 'twitter:image', content: featuredImageUrl }
    )

    if (featuredImageWidth) {
      meta.push({ property: 'og:image:width', content: String(featuredImageWidth) })
    }
    if (featuredImageHeight) {
      meta.push({ property: 'og:image:height', content: String(featuredImageHeight) })
    }
  }

  // Add article-specific Open Graph tags
  if (publishedAt) {
    meta.push({ property: 'article:published_time', content: publishedAt })
  }

  if (authorName) {
    meta.push({ property: 'article:author', content: authorName })
  }

  if (categoryName) {
    meta.push({ property: 'article:section', content: categoryName })
  }

  useHead({
    title: fullTitle,
    meta
  })

  return null
}

export default BlogPostMeta

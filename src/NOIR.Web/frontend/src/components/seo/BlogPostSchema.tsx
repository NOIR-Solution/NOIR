import { useHead } from './useHead'

export interface BlogPostSchemaProps {
  title: string
  slug: string
  excerpt?: string | null
  publishedAt?: string | null
  modifiedAt?: string | null
  authorName?: string | null
  categoryName?: string | null
  featuredImageUrl?: string | null
  featuredImageWidth?: number | null
  featuredImageHeight?: number | null
  siteUrl?: string
  siteName?: string
}

/**
 * JSON-LD Schema.org structured data for blog posts.
 * Improves SEO and enables rich snippets in search results.
 *
 * Usage:
 * ```tsx
 * <BlogPostSchema
 *   title={post.title}
 *   slug={post.slug}
 *   excerpt={post.excerpt}
 *   publishedAt={post.publishedAt}
 *   authorName={post.authorName}
 *   featuredImageUrl={post.featuredImageUrl}
 * />
 * ```
 */
export const BlogPostSchema = ({
  title,
  slug,
  excerpt,
  publishedAt,
  modifiedAt,
  authorName,
  categoryName,
  featuredImageUrl,
  featuredImageWidth,
  featuredImageHeight,
  siteUrl = window.location.origin,
  siteName = 'NOIR'
}: BlogPostSchemaProps) => {
  const articleUrl = `${siteUrl}/blog/${slug}`

  const jsonLd: Record<string, unknown> = {
    '@context': 'https://schema.org',
    '@type': 'Article',
    headline: title,
    url: articleUrl,
    mainEntityOfPage: {
      '@type': 'WebPage',
      '@id': articleUrl
    }
  }

  if (excerpt) {
    jsonLd.description = excerpt
  }

  if (publishedAt) {
    jsonLd.datePublished = publishedAt
  }

  if (modifiedAt) {
    jsonLd.dateModified = modifiedAt
  }

  if (authorName) {
    jsonLd.author = {
      '@type': 'Person',
      name: authorName
    }
  }

  if (featuredImageUrl) {
    jsonLd.image = {
      '@type': 'ImageObject',
      url: featuredImageUrl,
      ...(featuredImageWidth && { width: featuredImageWidth }),
      ...(featuredImageHeight && { height: featuredImageHeight })
    }
  }

  if (categoryName) {
    jsonLd.articleSection = categoryName
  }

  jsonLd.publisher = {
    '@type': 'Organization',
    name: siteName,
    url: siteUrl
  }

  useHead({ jsonLd })

  return null
}

import { useHead } from './useHead'

export interface BreadcrumbItem {
  name: string
  url: string
}

export interface BreadcrumbSchemaProps {
  items: BreadcrumbItem[]
}

/**
 * JSON-LD BreadcrumbList schema for navigation.
 * Helps search engines understand site structure.
 *
 * Usage:
 * ```tsx
 * <BreadcrumbSchema
 *   items={[
 *     { name: 'Home', url: '/' },
 *     { name: 'Blog', url: '/blog' },
 *     { name: 'My Post Title', url: '/blog/my-post' }
 *   ]}
 * />
 * ```
 */
export function BreadcrumbSchema({ items }: BreadcrumbSchemaProps) {
  const siteUrl = window.location.origin

  const jsonLd = {
    '@context': 'https://schema.org',
    '@type': 'BreadcrumbList',
    itemListElement: items.map((item, index) => ({
      '@type': 'ListItem',
      position: index + 1,
      name: item.name,
      item: item.url.startsWith('http') ? item.url : `${siteUrl}${item.url}`
    }))
  }

  useHead({ jsonLd })

  return null
}

export default BreadcrumbSchema

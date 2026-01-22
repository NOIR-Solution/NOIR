import { useEffect } from 'react'

/**
 * Simple head management hook for SPAs.
 * Updates document title and meta tags.
 */
export function useHead(config: HeadConfig) {
  useEffect(() => {
    const { title, meta, jsonLd } = config

    // Update title
    if (title) {
      document.title = title
    }

    // Track added elements for cleanup
    const addedElements: HTMLElement[] = []

    // Update/add meta tags
    if (meta) {
      meta.forEach(({ name, property, content }) => {
        const selector = name
          ? `meta[name="${name}"]`
          : `meta[property="${property}"]`

        let element = document.querySelector(selector) as HTMLMetaElement | null

        if (!element) {
          element = document.createElement('meta')
          if (name) element.setAttribute('name', name)
          if (property) element.setAttribute('property', property)
          document.head.appendChild(element)
          addedElements.push(element)
        }

        element.setAttribute('content', content)
      })
    }

    // Add JSON-LD structured data
    if (jsonLd) {
      const script = document.createElement('script')
      script.type = 'application/ld+json'
      script.textContent = JSON.stringify(jsonLd)
      document.head.appendChild(script)
      addedElements.push(script)
    }

    // Cleanup on unmount
    return () => {
      addedElements.forEach(el => el.remove())
    }
  }, [config.title, JSON.stringify(config.meta), JSON.stringify(config.jsonLd)])
}

export interface MetaTag {
  name?: string
  property?: string
  content: string
}

export interface HeadConfig {
  title?: string
  meta?: MetaTag[]
  jsonLd?: Record<string, unknown>
}

export default useHead

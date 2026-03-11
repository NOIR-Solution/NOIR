/**
 * CodeBlock Component
 *
 * Renders syntax-highlighted code using Shiki with lazy loading.
 * Supports theme awareness (light/dark), LRU cache for rendered HTML,
 * line numbers, and copy-to-clipboard functionality.
 */

import { useState, useRef, useEffect, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { Copy, Check } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '../button/Button'
import { Skeleton } from '../skeleton/Skeleton'
import { Tooltip, TooltipContent, TooltipTrigger } from '../tooltip/Tooltip'

export interface CodeBlockProps {
  /** The code string to highlight */
  code: string
  /** Language identifier for syntax highlighting (defaults to 'text') */
  language?: string
  /** Additional CSS class names */
  className?: string
  /** Show line numbers alongside the code */
  showLineNumbers?: boolean
  /** Show copy-to-clipboard button in the top-right corner */
  showCopyButton?: boolean
}

// ---------------------------------------------------------------------------
// LRU Cache
// ---------------------------------------------------------------------------

const MAX_CACHE_SIZE = 200
const cache = new Map<string, string>()

const getCached = (key: string): string | undefined => {
  const val = cache.get(key)
  if (val !== undefined) {
    // Move to end (most recently used)
    cache.delete(key)
    cache.set(key, val)
  }
  return val
}

const setCache = (key: string, value: string) => {
  if (cache.size >= MAX_CACHE_SIZE) {
    // Delete oldest (first entry)
    const firstKey = cache.keys().next().value
    if (firstKey !== undefined) cache.delete(firstKey)
  }
  cache.set(key, value)
}

// ---------------------------------------------------------------------------
// String hash for cache keys
// ---------------------------------------------------------------------------

const hashCode = (str: string): number => {
  let hash = 0
  for (let i = 0; i < str.length; i++) {
    const char = str.charCodeAt(i)
    hash = ((hash << 5) - hash) + char
    hash |= 0
  }
  return hash
}

// ---------------------------------------------------------------------------
// Shiki singleton
// ---------------------------------------------------------------------------

type HighlighterCore = Awaited<ReturnType<typeof import('shiki')['createHighlighter']>>

let highlighterInstance: HighlighterCore | null = null
let highlighterPromise: Promise<HighlighterCore> | null = null
const loadedLanguages = new Set<string>()

const getHighlighter = async (): Promise<HighlighterCore> => {
  if (highlighterInstance) return highlighterInstance

  if (highlighterPromise) return highlighterPromise

  highlighterPromise = Promise.all([
    import('shiki'),
    import('@/lib/shiki-theme'),
  ]).then(async ([{ createHighlighter }, { noirDark, noirLight }]) => {
    const instance = await createHighlighter({
      themes: [noirDark, noirLight],
      langs: [],
    })
    highlighterInstance = instance
    return instance
  })

  return highlighterPromise
}

const ensureLanguage = async (highlighter: HighlighterCore, lang: string): Promise<boolean> => {
  if (lang === 'text' || loadedLanguages.has(lang)) return true

  try {
    await highlighter.loadLanguage(lang as Parameters<HighlighterCore['loadLanguage']>[0])
    loadedLanguages.add(lang)
    return true
  } catch {
    // Language not available in shiki
    return false
  }
}

// ---------------------------------------------------------------------------
// Theme detection
// ---------------------------------------------------------------------------

const detectTheme = (): 'noir-dark' | 'noir-light' => {
  return document.documentElement.classList.contains('dark') ? 'noir-dark' : 'noir-light'
}

// ---------------------------------------------------------------------------
// Component
// ---------------------------------------------------------------------------

export const CodeBlock = ({
  code,
  language = 'text',
  className,
  showLineNumbers = false,
  showCopyButton = true,
}: CodeBlockProps) => {
  const { t } = useTranslation('common')
  const [html, setHtml] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)
  const [copied, setCopied] = useState(false)
  const renderIdRef = useRef(0)

  const highlight = useCallback(async (source: string, lang: string) => {
    renderIdRef.current += 1
    const currentRenderId = renderIdRef.current

    const theme = detectTheme()
    const cacheKey = `${theme}:${lang}:${hashCode(source)}`

    // Check cache first
    const cached = getCached(cacheKey)
    if (cached) {
      if (renderIdRef.current !== currentRenderId) return
      setHtml(cached)
      setLoading(false)
      setError(false)
      return
    }

    setLoading(true)
    setError(false)

    try {
      const highlighter = await getHighlighter()
      if (renderIdRef.current !== currentRenderId) return

      const langLoaded = await ensureLanguage(highlighter, lang)
      if (renderIdRef.current !== currentRenderId) return

      const effectiveLang = langLoaded ? lang : 'text'
      const rendered = highlighter.codeToHtml(source, { lang: effectiveLang, theme })

      if (renderIdRef.current !== currentRenderId) return

      setCache(cacheKey, rendered)
      setHtml(rendered)
      setLoading(false)
      setError(false)
    } catch {
      if (renderIdRef.current !== currentRenderId) return
      setError(true)
      setHtml(null)
      setLoading(false)
    }
  }, [])

  // Highlight on code/language change
  useEffect(() => {
    highlight(code, language)
  }, [code, language, highlight])

  // Watch for theme changes via MutationObserver on <html> class
  useEffect(() => {
    const observer = new MutationObserver((mutations) => {
      for (const mutation of mutations) {
        if (mutation.type === 'attributes' && mutation.attributeName === 'class') {
          // Theme changed — clear cache entries (themes differ) and re-highlight
          highlight(code, language)
          break
        }
      }
    })

    observer.observe(document.documentElement, {
      attributes: true,
      attributeFilter: ['class'],
    })

    return () => observer.disconnect()
  }, [code, language, highlight])

  const handleCopy = useCallback(async () => {
    try {
      await navigator.clipboard.writeText(code)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch {
      // Clipboard not available
    }
  }, [code])

  // Loading skeleton
  if (loading) {
    return (
      <div className={cn('rounded-lg border bg-muted/30 p-4', className)}>
        <div className="space-y-2">
          <Skeleton className="h-4 w-3/4" />
          <Skeleton className="h-4 w-1/2" />
          <Skeleton className="h-4 w-5/6" />
          <Skeleton className="h-4 w-2/3" />
        </div>
      </div>
    )
  }

  // Error fallback — plain <pre><code>
  if (error || !html) {
    return (
      <div className={cn('relative group', className)}>
        {showCopyButton && (
          <div className="absolute top-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity z-10">
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-7 w-7 cursor-pointer"
                  onClick={handleCopy}
                  aria-label={t('buttons.copyToClipboard', 'Copy to clipboard')}
                >
                  {copied ? (
                    <Check className="h-3.5 w-3.5 text-green-500" />
                  ) : (
                    <Copy className="h-3.5 w-3.5" />
                  )}
                </Button>
              </TooltipTrigger>
              <TooltipContent side="bottom">{t('buttons.copyToClipboard', 'Copy to clipboard')}</TooltipContent>
            </Tooltip>
          </div>
        )}
        <pre className="rounded-lg border bg-muted/50 p-4 overflow-x-auto text-sm font-mono">
          <code>{code}</code>
        </pre>
      </div>
    )
  }

  // Highlighted output
  return (
    <div
      className={cn(
        'relative group rounded-lg border',
        'code-block-wrapper',
        showLineNumbers && 'code-block-line-numbers',
        className,
      )}
    >
      {showCopyButton && (
        <div className="absolute top-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity z-10">
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant="ghost"
                size="icon"
                className="h-7 w-7 bg-background/80 backdrop-blur-sm hover:bg-background cursor-pointer"
                onClick={handleCopy}
                aria-label={t('buttons.copyToClipboard', 'Copy to clipboard')}
              >
                {copied ? (
                  <Check className="h-3.5 w-3.5 text-green-500" />
                ) : (
                  <Copy className="h-3.5 w-3.5" />
                )}
              </Button>
            </TooltipTrigger>
            <TooltipContent side="bottom">{t('buttons.copyToClipboard', 'Copy to clipboard')}</TooltipContent>
          </Tooltip>
        </div>
      )}
      <div
        className={cn(
          'overflow-x-auto text-sm',
          '[&_pre]:!rounded-lg [&_pre]:!p-4 [&_pre]:!m-0',
          '[&_code]:font-mono',
          showLineNumbers && [
            '[&_.line]:before:content-[counter(line)]',
            '[&_.line]:before:counter-increment-[line]',
            '[&_.line]:before:inline-block',
            '[&_.line]:before:w-8',
            '[&_.line]:before:mr-4',
            '[&_.line]:before:text-right',
            '[&_.line]:before:text-muted-foreground/50',
            '[&_.line]:before:select-none',
            '[&_pre]:counter-reset-[line]',
          ],
        )}
        dangerouslySetInnerHTML={{ __html: html }}
      />
    </div>
  )
}

import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertCircle } from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'
import { Textarea } from '@/components/ui/textarea'

interface JsonEditorProps {
  value: Record<string, unknown>
  onChange: (value: Record<string, unknown>) => void
  readOnly?: boolean
}

export function JsonEditor({ value, onChange, readOnly = false }: JsonEditorProps) {
  const { t } = useTranslation()
  const [jsonText, setJsonText] = useState('')
  const [error, setError] = useState<string | null>(null)

  // Initialize JSON text from value
  useEffect(() => {
    setJsonText(JSON.stringify(value, null, 2))
    setError(null)
  }, [value])

  const handleChange = (text: string) => {
    setJsonText(text)

    // Try to parse and validate JSON
    try {
      const parsed = JSON.parse(text)
      setError(null)
      onChange(parsed)
    } catch (err) {
      // Show error but don't propagate invalid JSON
      setError(err instanceof Error ? err.message : t('platformSettings.invalidJson'))
    }
  }

  return (
    <div className="space-y-4">
      {error && (
        <div className="flex items-center gap-2 p-3 bg-destructive/10 border border-destructive/20 rounded-md text-sm text-destructive">
          <AlertCircle className="h-4 w-4 flex-shrink-0" />
          <p>{error}</p>
        </div>
      )}

      <Card>
        <CardContent className="p-4">
          <Textarea
            value={jsonText}
            onChange={(e) => handleChange(e.target.value)}
            readOnly={readOnly}
            className="font-mono text-sm min-h-[500px] resize-none"
            placeholder={readOnly ? t('platformSettings.forbiddenMessage') : '{}'}
          />
        </CardContent>
      </Card>

      {readOnly && (
        <div className="flex items-center gap-2 p-3 bg-muted rounded-md text-sm text-muted-foreground">
          <AlertCircle className="h-4 w-4 flex-shrink-0" />
          <p>{t('platformSettings.forbiddenMessage')}</p>
        </div>
      )}
    </div>
  )
}

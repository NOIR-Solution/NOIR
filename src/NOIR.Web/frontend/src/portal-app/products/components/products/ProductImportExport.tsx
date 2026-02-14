/**
 * ProductImportExport Component
 *
 * Provides enhanced CSV import/export functionality for products.
 * Supports variants, images (pipe-separated), and dynamic attributes.
 */
import { useState, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Download,
  Upload,
  FileSpreadsheet,
  Check,
  AlertTriangle,
  Loader2,
} from 'lucide-react'
import {
  Badge,
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  Progress,
  ScrollArea,
} from '@uikit'

import type { ProductListItem } from '@/types/product'
import { bulkImportProducts, exportProducts, type ImportProductDto } from '@/services/products'
import { toast } from 'sonner'

interface ProductImportExportProps {
  products: ProductListItem[]
  onImportComplete?: () => void
}

interface ImportResult {
  success: number
  errors: { row: number; message: string }[]
}

// Base CSV export fields
const BASE_EXPORT_FIELDS = [
  'name',
  'slug',
  'sku',
  'barcode',
  'basePrice',
  'currency',
  'status',
  'categoryName',
  'brand',
  'shortDescription',
  'variantName',
  'variantPrice',
  'compareAtPrice',
  'stock',
  'images',
]

export const ProductImportExport = ({
  products,
  onImportComplete,
}: ProductImportExportProps) => {
  const { t } = useTranslation('common')
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [isExporting, setIsExporting] = useState(false)
  const [isImporting, setIsImporting] = useState(false)
  const [importProgress, setImportProgress] = useState(0)
  const [importResult, setImportResult] = useState<ImportResult | null>(null)
  const [showImportDialog, setShowImportDialog] = useState(false)

  // Export products to CSV using the API
  const handleExport = async () => {
    setIsExporting(true)

    try {
      // Fetch export data from API
      const result = await exportProducts({
        includeAttributes: true,
        includeImages: true,
      })

      // Build headers with dynamic attribute columns
      const allHeaders = [...BASE_EXPORT_FIELDS]
      result.attributeColumns.forEach(attrCode => {
        allHeaders.push(`attr_${attrCode}`)
      })
      const headerRow = allHeaders.join(',')

      // Build rows
      const dataRows = result.rows.map((row) => {
        const values: string[] = []

        // Base fields
        values.push(escapeCSV(row.name))
        values.push(escapeCSV(row.slug))
        values.push(escapeCSV(row.sku || ''))
        values.push(escapeCSV(row.barcode || ''))
        values.push(String(row.basePrice))
        values.push(escapeCSV(row.currency))
        values.push(escapeCSV(row.status))
        values.push(escapeCSV(row.categoryName || ''))
        values.push(escapeCSV(row.brand || ''))
        values.push(escapeCSV(row.shortDescription || ''))
        values.push(escapeCSV(row.variantName || ''))
        values.push(row.variantPrice != null ? String(row.variantPrice) : '')
        values.push(row.compareAtPrice != null ? String(row.compareAtPrice) : '')
        values.push(String(row.stock))
        values.push(escapeCSV(row.images || ''))

        // Dynamic attribute columns
        result.attributeColumns.forEach(attrCode => {
          values.push(escapeCSV(row.attributes[attrCode] || ''))
        })

        return values.join(',')
      })

      const csvContent = [headerRow, ...dataRows].join('\n')

      // Download file
      const blob = new Blob(['\ufeff' + csvContent], { type: 'text/csv;charset=utf-8' })
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = `products-export-${new Date().toISOString().split('T')[0]}.csv`
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      URL.revokeObjectURL(url)

      toast.success(t('products.export.success', { count: result.rows.length, defaultValue: `${result.rows.length} products exported` }))
    } catch (error) {
      console.error('Export error:', error)
      toast.error(t('products.export.failed', 'Failed to export products'))
    } finally {
      setIsExporting(false)
    }
  }

  // Download CSV template with enhanced fields
  const handleDownloadTemplate = () => {
    const headers = [...BASE_EXPORT_FIELDS, 'attr_color', 'attr_material'].join(',')
    const exampleRows = [
      // Product row
      'Example Product,example-product,SKU-001,,29990000,VND,Draft,Electronics,Apple,Short description,Default,29990000,,100,https://example.com/img1.jpg|https://example.com/img2.jpg,Silver,Aluminum',
      // Variant row (same product slug)
      'Example Product,example-product,SKU-002,,29990000,VND,Draft,Electronics,Apple,,128GB Variant,32990000,34990000,50,,Silver,Aluminum',
    ].join('\n')

    const csvContent = [headers, exampleRows].join('\n')
    const blob = new Blob(['\ufeff' + csvContent], { type: 'text/csv;charset=utf-8' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = 'products-import-template.csv'
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    URL.revokeObjectURL(url)

    toast.success(t('products.import.templateDownloaded', 'Template downloaded'))
  }

  // Handle file selection
  const handleFileSelect = () => {
    fileInputRef.current?.click()
  }

  // Parse and import CSV file
  const handleFileChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    if (!file) return

    // Reset input
    event.target.value = ''

    setIsImporting(true)
    setImportProgress(0)
    setImportResult(null)
    setShowImportDialog(true)

    try {
      const text = await file.text()
      const { headers, rows } = parseCSV(text)

      if (rows.length === 0) {
        throw new Error(t('products.import.invalidFile', 'CSV file must have at least a header row and one data row'))
      }

      // Validate required fields
      const requiredFields = ['name', 'baseprice']
      const lowerHeaders = headers.map(h => h.toLowerCase())
      const missingFields = requiredFields.filter(f => !lowerHeaders.includes(f))
      if (missingFields.length > 0) {
        throw new Error(t('products.import.missingFields', { fields: missingFields.join(', '), defaultValue: `Missing required fields: ${missingFields.join(', ')}` }))
      }

      // Find attribute columns (start with attr_)
      const attrColumnIndexes: { index: number; code: string }[] = []
      headers.forEach((header, index) => {
        if (header.toLowerCase().startsWith('attr_')) {
          attrColumnIndexes.push({
            index,
            code: header.substring(5).toLowerCase(),
          })
        }
      })

      // Map column indexes for known fields
      const columnMap = new Map<string, number>()
      lowerHeaders.forEach((h, i) => columnMap.set(h, i))

      // Convert rows to ImportProductDto
      const products: ImportProductDto[] = rows.map(row => {
        const getValue = (field: string): string | undefined => {
          const index = columnMap.get(field.toLowerCase())
          return index !== undefined ? row[index]?.trim() || undefined : undefined
        }

        const getNumberValue = (field: string): number | undefined => {
          const val = getValue(field)
          if (!val) return undefined
          const num = parseFloat(val)
          return isNaN(num) ? undefined : num
        }

        // Parse attributes
        const attributes: Record<string, string> = {}
        attrColumnIndexes.forEach(({ index, code }) => {
          const value = row[index]?.trim()
          if (value) {
            attributes[code] = value
          }
        })

        return {
          name: getValue('name') || '',
          slug: getValue('slug'),
          basePrice: getNumberValue('baseprice') || 0,
          currency: getValue('currency'),
          shortDescription: getValue('shortdescription'),
          sku: getValue('sku'),
          barcode: getValue('barcode'),
          categoryName: getValue('categoryname'),
          brand: getValue('brand'),
          stock: getNumberValue('stock') ? Math.floor(getNumberValue('stock')!) : undefined,
          variantName: getValue('variantname'),
          variantPrice: getNumberValue('variantprice'),
          compareAtPrice: getNumberValue('compareatprice'),
          images: getValue('images'),
          attributes: Object.keys(attributes).length > 0 ? attributes : undefined,
        }
      })

      setImportProgress(50)

      // Call the API to import products
      const result = await bulkImportProducts(products)

      setImportProgress(100)
      setImportResult({
        success: result.success,
        errors: result.errors,
      })

      if (result.success > 0) {
        toast.success(t('products.import.success', { count: result.success, defaultValue: `${result.success} products imported` }))
        onImportComplete?.()
      }

      if (result.errors.length > 0) {
        toast.warning(t('products.import.partialSuccess', { errors: result.errors.length, defaultValue: `${result.errors.length} rows had errors` }))
      }
    } catch (error) {
      const message = error instanceof Error ? error.message : t('products.import.failed', 'Failed to import products')
      toast.error(message)
      setShowImportDialog(false)
    } finally {
      setIsImporting(false)
    }
  }

  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="outline" className="cursor-pointer">
            <FileSpreadsheet className="h-4 w-4 mr-2" />
            {t('products.importExport', 'Import/Export')}
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-48">
          <DropdownMenuItem
            className="cursor-pointer"
            onClick={handleExport}
            disabled={isExporting}
          >
            {isExporting ? (
              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
            ) : (
              <Download className="h-4 w-4 mr-2" />
            )}
            {t('products.export.button', 'Export CSV')}
            <Badge variant="secondary" className="ml-auto text-xs">
              {products.length}
            </Badge>
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuItem
            className="cursor-pointer"
            onClick={handleFileSelect}
          >
            <Upload className="h-4 w-4 mr-2" />
            {t('products.import.button', 'Import CSV')}
          </DropdownMenuItem>
          <DropdownMenuItem
            className="cursor-pointer"
            onClick={handleDownloadTemplate}
          >
            <FileSpreadsheet className="h-4 w-4 mr-2 text-muted-foreground" />
            {t('products.import.downloadTemplate', 'Download Template')}
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>

      {/* Hidden file input */}
      <input
        ref={fileInputRef}
        type="file"
        accept=".csv"
        onChange={handleFileChange}
        className="hidden"
      />

      {/* Import Progress Dialog */}
      <Dialog open={showImportDialog} onOpenChange={setShowImportDialog}>
        <DialogContent className="sm:max-w-[500px]">
          <DialogHeader>
            <DialogTitle>
              {isImporting
                ? t('products.import.importing', 'Importing Products...')
                : t('products.import.complete', 'Import Complete')}
            </DialogTitle>
            <DialogDescription>
              {isImporting
                ? t('products.import.pleaseWait', 'Please wait while we process your file.')
                : t('products.import.summary', 'Here is a summary of the import.')}
            </DialogDescription>
          </DialogHeader>

          <div className="py-4 space-y-4">
            {isImporting ? (
              <div className="space-y-2">
                <Progress value={importProgress} />
                <p className="text-sm text-center text-muted-foreground">
                  {importProgress}%
                </p>
              </div>
            ) : importResult ? (
              <>
                <div className="flex items-center gap-4">
                  <div className="flex items-center gap-2 p-3 rounded-lg bg-emerald-500/10 flex-1">
                    <Check className="h-5 w-5 text-emerald-600" />
                    <div>
                      <p className="font-medium text-emerald-600">
                        {importResult.success}
                      </p>
                      <p className="text-xs text-emerald-600/80">
                        {t('products.import.successLabel', 'Imported')}
                      </p>
                    </div>
                  </div>
                  {importResult.errors.length > 0 && (
                    <div className="flex items-center gap-2 p-3 rounded-lg bg-destructive/10 flex-1">
                      <AlertTriangle className="h-5 w-5 text-destructive" />
                      <div>
                        <p className="font-medium text-destructive">
                          {importResult.errors.length}
                        </p>
                        <p className="text-xs text-destructive/80">
                          {t('products.import.errorsLabel', 'Errors')}
                        </p>
                      </div>
                    </div>
                  )}
                </div>

                {importResult.errors.length > 0 && (
                  <div className="space-y-2">
                    <p className="text-sm font-medium">
                      {t('products.import.errorDetails', 'Error Details:')}
                    </p>
                    <ScrollArea className="h-[150px] rounded-md border p-2">
                      {importResult.errors.map((error, index) => (
                        <div
                          key={index}
                          className="text-sm py-1 border-b last:border-0"
                        >
                          <span className="text-muted-foreground">
                            Row {error.row}:
                          </span>{' '}
                          <span className="text-destructive">{error.message}</span>
                        </div>
                      ))}
                    </ScrollArea>
                  </div>
                )}
              </>
            ) : null}
          </div>

          <DialogFooter>
            <Button
              onClick={() => setShowImportDialog(false)}
              disabled={isImporting}
              className="cursor-pointer"
            >
              {isImporting ? t('buttons.cancel', 'Cancel') : t('buttons.close', 'Close')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}

/**
 * Escape a value for CSV output
 */
const escapeCSV = (value: string): string => {
  if (value.includes(',') || value.includes('"') || value.includes('\n')) {
    return `"${value.replace(/"/g, '""')}"`
  }
  return value
}

/**
 * Parse CSV text into headers and rows
 * Handles quoted fields with commas and newlines
 */
const parseCSV = (text: string): { headers: string[]; rows: string[][] } => {
  const lines: string[] = []
  let currentLine = ''
  let inQuotes = false

  for (let i = 0; i < text.length; i++) {
    const char = text[i]
    if (char === '"') {
      // Check for escaped quote
      if (inQuotes && text[i + 1] === '"') {
        currentLine += '"'
        i++ // Skip next quote
      } else {
        inQuotes = !inQuotes
      }
    } else if (char === '\n' && !inQuotes) {
      if (currentLine.trim()) {
        lines.push(currentLine)
      }
      currentLine = ''
    } else if (char === '\r' && !inQuotes) {
      // Skip carriage return
    } else {
      currentLine += char
    }
  }

  // Don't forget the last line
  if (currentLine.trim()) {
    lines.push(currentLine)
  }

  if (lines.length === 0) {
    return { headers: [], rows: [] }
  }

  // Parse a single line into values
  const parseLine = (line: string): string[] => {
    const values: string[] = []
    let currentValue = ''
    let inQuotes = false

    for (let i = 0; i < line.length; i++) {
      const char = line[i]
      if (char === '"') {
        if (inQuotes && line[i + 1] === '"') {
          currentValue += '"'
          i++
        } else {
          inQuotes = !inQuotes
        }
      } else if (char === ',' && !inQuotes) {
        values.push(currentValue)
        currentValue = ''
      } else {
        currentValue += char
      }
    }
    values.push(currentValue)

    return values
  }

  const headers = parseLine(lines[0])
  const rows = lines.slice(1).map(parseLine)

  return { headers, rows }
}

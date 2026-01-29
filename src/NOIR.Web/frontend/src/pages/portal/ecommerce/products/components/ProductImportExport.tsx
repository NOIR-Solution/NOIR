/**
 * ProductImportExport Component
 *
 * Provides CSV import/export functionality for products.
 * - Export: Downloads current products as CSV (fully functional)
 * - Import: Upload CSV file to bulk create/update products
 *
 * TODO: Backend Integration Required for Import
 * Currently import simulates processing. To complete:
 * 1. Create POST /api/products/import bulk endpoint
 * 2. Replace setTimeout simulation with actual API calls
 * 3. Consider using papaparse for robust CSV parsing
 */
import { useState, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { motion, AnimatePresence } from 'framer-motion'
import {
  Download,
  Upload,
  FileSpreadsheet,
  X,
  Check,
  AlertTriangle,
  Loader2,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Progress } from '@/components/ui/progress'
import { ScrollArea } from '@/components/ui/scroll-area'
import type { ProductListItem } from '@/types/product'
import { toast } from 'sonner'

interface ProductImportExportProps {
  products: ProductListItem[]
  onImportComplete?: () => void
}

interface ImportResult {
  success: number
  errors: { row: number; message: string }[]
}

// CSV export fields
const EXPORT_FIELDS = [
  'name',
  'slug',
  'sku',
  'barcode',
  'basePrice',
  'currency',
  'status',
  'categoryName',
  'brand',
  'totalStock',
  'shortDescription',
] as const

export function ProductImportExport({
  products,
  onImportComplete,
}: ProductImportExportProps) {
  const { t } = useTranslation('common')
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [isExporting, setIsExporting] = useState(false)
  const [isImporting, setIsImporting] = useState(false)
  const [importProgress, setImportProgress] = useState(0)
  const [importResult, setImportResult] = useState<ImportResult | null>(null)
  const [showImportDialog, setShowImportDialog] = useState(false)

  // Export products to CSV
  const handleExport = async () => {
    setIsExporting(true)

    try {
      // Build CSV content
      const headers = EXPORT_FIELDS.join(',')
      const rows = products.map((product) => {
        return EXPORT_FIELDS.map((field) => {
          const value = product[field as keyof ProductListItem]
          if (value === null || value === undefined) return ''
          // Escape quotes and wrap in quotes if contains comma
          const stringValue = String(value)
          if (stringValue.includes(',') || stringValue.includes('"') || stringValue.includes('\n')) {
            return `"${stringValue.replace(/"/g, '""')}"`
          }
          return stringValue
        }).join(',')
      })

      const csvContent = [headers, ...rows].join('\n')

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

      toast.success(t('products.export.success', { count: products.length, defaultValue: `${products.length} products exported` }))
    } catch (error) {
      toast.error(t('products.export.failed', 'Failed to export products'))
    } finally {
      setIsExporting(false)
    }
  }

  // Download CSV template
  const handleDownloadTemplate = () => {
    const headers = EXPORT_FIELDS.join(',')
    const exampleRow = [
      'Example Product',
      'example-product',
      'SKU-001',
      '',
      '29.99',
      'VND',
      'Draft',
      'Category Name',
      'Brand Name',
      '100',
      'Short description here',
    ].join(',')

    const csvContent = [headers, exampleRow].join('\n')
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
      const lines = text.split('\n').filter(line => line.trim())

      if (lines.length < 2) {
        throw new Error('CSV file must have at least a header row and one data row')
      }

      // Parse header
      const headers = lines[0].split(',').map(h => h.trim().toLowerCase())

      // Validate required fields
      const requiredFields = ['name', 'baseprice']
      const missingFields = requiredFields.filter(f => !headers.includes(f))
      if (missingFields.length > 0) {
        throw new Error(`Missing required fields: ${missingFields.join(', ')}`)
      }

      // Parse rows
      const result: ImportResult = { success: 0, errors: [] }
      const totalRows = lines.length - 1

      for (let i = 1; i < lines.length; i++) {
        const row = lines[i]
        const rowNumber = i + 1

        try {
          // Simple CSV parsing (doesn't handle all edge cases)
          const values = row.split(',').map(v => v.trim().replace(/^"|"$/g, ''))

          // Create product object
          const product: Record<string, string> = {}
          headers.forEach((header, index) => {
            product[header] = values[index] || ''
          })

          // Validate row
          if (!product.name) {
            result.errors.push({ row: rowNumber, message: 'Name is required' })
            continue
          }

          // Note: In a real implementation, you would call the API here to create/update the product
          // For now, we just simulate the import
          await new Promise(resolve => setTimeout(resolve, 50)) // Simulate API call

          result.success++
        } catch (err) {
          result.errors.push({
            row: rowNumber,
            message: err instanceof Error ? err.message : 'Unknown error',
          })
        }

        // Update progress
        setImportProgress(Math.round((i / totalRows) * 100))
      }

      setImportResult(result)

      if (result.success > 0) {
        toast.success(t('products.import.success', { count: result.success, defaultValue: `${result.success} products imported` }))
        onImportComplete?.()
      }

      if (result.errors.length > 0) {
        toast.warning(t('products.import.partialSuccess', { errors: result.errors.length, defaultValue: `${result.errors.length} rows had errors` }))
      }
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to import products'
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
            disabled={isExporting || products.length === 0}
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

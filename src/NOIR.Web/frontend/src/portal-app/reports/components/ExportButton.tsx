/**
 * Export Button component for downloading reports as CSV or Excel.
 */
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Download, FileSpreadsheet, FileText } from 'lucide-react'
import { toast } from 'sonner'
import {
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@uikit'
import { exportReport } from '@/services/reports'
import type { ReportType, ExportFormat } from '@/types/report'

export interface ExportButtonProps {
  reportType: ReportType
  startDate?: string
  endDate?: string
  disabled?: boolean
}

export const ExportButton = ({ reportType, startDate, endDate, disabled }: ExportButtonProps) => {
  const { t } = useTranslation('common')
  const [exporting, setExporting] = useState(false)

  const handleExport = async (format: ExportFormat) => {
    setExporting(true)
    try {
      await exportReport({ reportType, format, startDate, endDate })
      toast.success(t('reports.exportSuccess', 'Report exported successfully'))
    } catch {
      toast.error(t('reports.exportFailed', 'Failed to export report'))
    } finally {
      setExporting(false)
    }
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="outline"
          size="sm"
          disabled={disabled || exporting}
          className="cursor-pointer"
        >
          <Download className="h-4 w-4 mr-2" />
          {exporting
            ? t('labels.exporting', 'Exporting...')
            : t('buttons.export', 'Export')}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem
          className="cursor-pointer"
          onClick={() => handleExport('CSV')}
        >
          <FileText className="h-4 w-4 mr-2" />
          {t('reports.exportCsv', 'Export as CSV')}
        </DropdownMenuItem>
        <DropdownMenuItem
          className="cursor-pointer"
          onClick={() => handleExport('Excel')}
        >
          <FileSpreadsheet className="h-4 w-4 mr-2" />
          {t('reports.exportExcel', 'Export as Excel')}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

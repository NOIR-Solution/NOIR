import { useTranslation } from 'react-i18next'
import { Search } from 'lucide-react'
import {
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@uikit'

interface MediaToolbarProps {
  searchValue: string
  onSearchChange: (value: string) => void
  fileType: string | undefined
  onFileTypeChange: (value: string | undefined) => void
  folder: string | undefined
  onFolderChange: (value: string | undefined) => void
  sortBy: string
  sortOrder: string
  onSortChange: (sortBy: string, sortOrder: string) => void
  actionSlot?: React.ReactNode
  /** When true, hide the search input (e.g. when used inside DataTableToolbar which has its own search) */
  hideSearch?: boolean
}

const SORT_OPTIONS = [
  { value: 'createdAt-desc', sortBy: 'createdAt', sortOrder: 'desc' },
  { value: 'createdAt-asc', sortBy: 'createdAt', sortOrder: 'asc' },
  { value: 'originalFileName-asc', sortBy: 'originalFileName', sortOrder: 'asc' },
  { value: 'originalFileName-desc', sortBy: 'originalFileName', sortOrder: 'desc' },
  { value: 'sizeBytes-desc', sortBy: 'sizeBytes', sortOrder: 'desc' },
  { value: 'sizeBytes-asc', sortBy: 'sizeBytes', sortOrder: 'asc' },
] as const

export const MediaToolbar = ({
  searchValue,
  onSearchChange,
  fileType,
  onFileTypeChange,
  folder,
  onFolderChange,
  sortBy,
  sortOrder,
  onSortChange,
  actionSlot,
  hideSearch = false,
}: MediaToolbarProps) => {
  const { t } = useTranslation('common')

  const currentSortValue = `${sortBy}-${sortOrder}`

  const handleSortChange = (value: string) => {
    const option = SORT_OPTIONS.find(o => o.value === value)
    if (option) {
      onSortChange(option.sortBy, option.sortOrder)
    }
  }

  return (
    <div className="flex flex-wrap items-center gap-2">
      {!hideSearch && (
        <div className="relative flex-1 min-w-[200px]">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground pointer-events-none" />
          <Input
            placeholder={t('media.searchPlaceholder', 'Search media files...')}
            value={searchValue}
            onChange={(e) => onSearchChange(e.target.value)}
            className="pl-9 h-9"
            aria-label={t('media.searchMedia', 'Search media files')}
          />
        </div>
      )}

      <Select onValueChange={(v) => onFileTypeChange(v === 'all' ? undefined : v)} value={fileType || 'all'}>
        <SelectTrigger className="w-full sm:w-36 h-9 cursor-pointer transition-all duration-200 hover:border-primary/50" aria-label={t('media.filterByType', 'Filter by type')}>
          <SelectValue placeholder={t('media.allTypes', 'All Types')} />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all" className="cursor-pointer">{t('media.allTypes', 'All Types')}</SelectItem>
          <SelectItem value="image" className="cursor-pointer">{t('media.images', 'Images')}</SelectItem>
          <SelectItem value="video" className="cursor-pointer">{t('media.videos', 'Videos')}</SelectItem>
          <SelectItem value="document" className="cursor-pointer">{t('media.documents', 'Documents')}</SelectItem>
        </SelectContent>
      </Select>

      <Select onValueChange={(v) => onFolderChange(v === 'all' ? undefined : v)} value={folder || 'all'}>
        <SelectTrigger className="w-full sm:w-36 h-9 cursor-pointer transition-all duration-200 hover:border-primary/50" aria-label={t('media.filterByFolder', 'Filter by folder')}>
          <SelectValue placeholder={t('media.allFolders', 'All Folders')} />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all" className="cursor-pointer">{t('media.allFolders', 'All Folders')}</SelectItem>
          <SelectItem value="blog" className="cursor-pointer">{t('media.folders.blog', 'Blog')}</SelectItem>
          <SelectItem value="content" className="cursor-pointer">{t('media.folders.content', 'Content')}</SelectItem>
          <SelectItem value="avatars" className="cursor-pointer">{t('media.folders.avatars', 'Avatars')}</SelectItem>
          <SelectItem value="branding" className="cursor-pointer">{t('media.folders.branding', 'Branding')}</SelectItem>
          <SelectItem value="products" className="cursor-pointer">{t('media.folders.products', 'Products')}</SelectItem>
        </SelectContent>
      </Select>

      <Select onValueChange={handleSortChange} value={currentSortValue}>
        <SelectTrigger className="w-full sm:w-44 h-9 cursor-pointer transition-all duration-200 hover:border-primary/50" aria-label={t('media.sortBy', 'Sort by')}>
          <SelectValue placeholder={t('media.sortBy', 'Sort by')} />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="createdAt-desc" className="cursor-pointer">{t('media.sortNewest', 'Date (newest)')}</SelectItem>
          <SelectItem value="createdAt-asc" className="cursor-pointer">{t('media.sortOldest', 'Date (oldest)')}</SelectItem>
          <SelectItem value="originalFileName-asc" className="cursor-pointer">{t('media.sortNameAZ', 'Name A-Z')}</SelectItem>
          <SelectItem value="originalFileName-desc" className="cursor-pointer">{t('media.sortNameZA', 'Name Z-A')}</SelectItem>
          <SelectItem value="sizeBytes-desc" className="cursor-pointer">{t('media.sortLargest', 'Size (largest)')}</SelectItem>
          <SelectItem value="sizeBytes-asc" className="cursor-pointer">{t('media.sortSmallest', 'Size (smallest)')}</SelectItem>
        </SelectContent>
      </Select>

      {actionSlot}
    </div>
  )
}

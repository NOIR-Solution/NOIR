import { useState, useDeferredValue, useMemo, useTransition, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { ImageIcon, Plus, Trash2, Loader2, LayoutGrid, List as ListIcon } from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useSelection } from '@/hooks/useSelection'
import { useMediaFiles, useDeleteMediaFile, useRenameMediaFile, useBulkDeleteMediaFiles } from '@/hooks/useMediaFiles'
import { getPaginationRange } from '@/lib/utils/pagination'
import type { MediaFileListItem } from '@/types'
import {
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  EmptyState,
  PageHeader,
  Pagination,
  Skeleton,
  ViewModeToggle,
  type ViewModeOption,
} from '@uikit'
import { BulkActionToolbar } from '@/components/BulkActionToolbar'
import { MediaToolbar } from './MediaToolbar'
import { MediaGrid } from './MediaGrid'
import { MediaTable } from './MediaTable'
import { MediaDetailSheet } from './MediaDetailSheet'
import { MediaUploadDialog } from './MediaUploadDialog'
import { DeleteMediaDialog } from './DeleteMediaDialog'

const DEFAULT_PAGE_SIZE = 24

export const MediaLibraryPage = () => {
  const { t } = useTranslation('common')
  usePageContext('Media')

  // Search
  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch

  // Filters
  const [fileTypeFilter, setFileTypeFilter] = useState<string | undefined>()
  const [folderFilter, setFolderFilter] = useState<string | undefined>()
  const [sortBy, setSortBy] = useState('createdAt')
  const [sortOrder, setSortOrder] = useState('desc')
  const [currentPage, setCurrentPage] = useState(1)
  const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid')
  const viewModeOptions: ViewModeOption<'grid' | 'list'>[] = useMemo(() => [
    { value: 'grid', label: t('labels.grid', 'Grid'), icon: LayoutGrid, ariaLabel: t('labels.gridView', 'Grid view') },
    { value: 'list', label: t('labels.list', 'List'), icon: ListIcon, ariaLabel: t('labels.tableView', 'Table view') },
  ], [t])

  // Filter transitions
  const [isFilterPending, startFilterTransition] = useTransition()

  // Detail sheet
  const [detailFile, setDetailFile] = useState<MediaFileListItem | null>(null)

  // Delete dialog
  const [fileToDelete, setFileToDelete] = useState<MediaFileListItem | null>(null)
  const [showBulkDeleteConfirm, setShowBulkDeleteConfirm] = useState(false)

  // Upload dialog (URL-synced)
  const { isOpen: isUploadOpen, open: openUpload, onOpenChange: onUploadOpenChange } = useUrlDialog({ paramValue: 'upload-media' })

  // Query
  const queryParams = useMemo(() => ({
    search: deferredSearch || undefined,
    fileType: fileTypeFilter,
    folder: folderFilter,
    sortBy,
    sortOrder,
    page: currentPage,
    pageSize: DEFAULT_PAGE_SIZE,
  }), [deferredSearch, fileTypeFilter, folderFilter, sortBy, sortOrder, currentPage])

  const { data, isLoading, isPlaceholderData, refetch } = useMediaFiles(queryParams)

  // Selection
  const { selectedIds, setSelectedIds, handleSelectAll, handleSelectNone, handleToggleSelect, isAllSelected } = useSelection(data?.items)

  const handleCollectionUpdate = useCallback(() => {
    if (selectedIds.size === 0) refetch()
  }, [selectedIds.size, refetch])

  const { isReconnecting } = useEntityUpdateSignal({
    entityType: 'MediaFile',
    onCollectionUpdate: handleCollectionUpdate,
  })

  // Mutations
  const deleteMutation = useDeleteMediaFile()
  const renameMutation = useRenameMediaFile()
  const bulkDeleteMutation = useBulkDeleteMediaFiles()

  // Bulk transition
  const [isBulkPending, startBulkTransition] = useTransition()

  // Pagination
  const paginationRange = data
    ? getPaginationRange(currentPage, DEFAULT_PAGE_SIZE, data.totalCount)
    : { from: 0, to: 0 }

  // Filter handlers
  const handleFileTypeChange = (value: string | undefined) => {
    startFilterTransition(() => {
      setFileTypeFilter(value)
      setCurrentPage(1)
    })
  }

  const handleFolderChange = (value: string | undefined) => {
    startFilterTransition(() => {
      setFolderFilter(value)
      setCurrentPage(1)
    })
  }

  const handleSortChange = (newSortBy: string, newSortOrder: string) => {
    startFilterTransition(() => {
      setSortBy(newSortBy)
      setSortOrder(newSortOrder)
      setCurrentPage(1)
    })
  }

  const handleSearchChange = (value: string) => {
    setSearchInput(value)
    setCurrentPage(1)
  }

  const handlePageChange = (page: number) => {
    startFilterTransition(() => setCurrentPage(page))
  }

  // Delete handlers
  const handleSingleDelete = () => {
    if (!fileToDelete) return
    deleteMutation.mutate(fileToDelete.id, {
      onSuccess: () => {
        setFileToDelete(null)
        setDetailFile(null)
      },
    })
  }

  const handleBulkDelete = () => {
    const ids = Array.from(selectedIds)
    startBulkTransition(async () => {
      await bulkDeleteMutation.mutateAsync(ids)
      setSelectedIds(new Set())
      setShowBulkDeleteConfirm(false)
    })
  }

  // Rename handler
  const handleRename = (id: string, newFileName: string) => {
    renameMutation.mutate({ id, newFileName })
  }

  // Open rename from table actions
  const handleRenameFromTable = (item: MediaFileListItem) => {
    setDetailFile(item)
  }

  // Bulk file names for delete dialog
  const bulkFileNames = useMemo(() => {
    if (!data?.items) return []
    return data.items.filter(f => selectedIds.has(f.id)).map(f => f.originalFileName)
  }, [data?.items, selectedIds])

  return (
    <div className="space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <PageHeader
        icon={ImageIcon}
        title={t('media.title', 'Media Library')}
        description={t('media.description', 'Manage your uploaded images and files')}
        responsive
        action={
          <Button onClick={openUpload} className="group transition-all duration-300 cursor-pointer">
            <Plus className="h-4 w-4 mr-2 transition-transform group-hover:rotate-90 duration-300" />
            {t('media.upload', 'Upload')}
          </Button>
        }
      />

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="pb-4">
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <div>
                <CardTitle className="text-lg">{t('media.allFiles', 'All Files')}</CardTitle>
                <CardDescription className="text-sm">
                  {data
                    ? t('labels.showingOfItems', { from: paginationRange.from, to: paginationRange.to, total: data.totalCount })
                    : t('labels.loading', 'Loading...')}
                </CardDescription>
              </div>
              <ViewModeToggle options={viewModeOptions} value={viewMode} onChange={setViewMode} />
            </div>

            <MediaToolbar
              searchValue={searchInput}
              onSearchChange={handleSearchChange}
              fileType={fileTypeFilter}
              onFileTypeChange={handleFileTypeChange}
              folder={folderFilter}
              onFolderChange={handleFolderChange}
              sortBy={sortBy}
              sortOrder={sortOrder}
              onSortChange={handleSortChange}
            />
          </div>
        </CardHeader>

        <CardContent className={(isFilterPending || isSearchStale || isPlaceholderData) ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
          {/* Bulk Action Toolbar */}
          <BulkActionToolbar selectedCount={selectedIds.size} onClearSelection={handleSelectNone}>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setShowBulkDeleteConfirm(true)}
              disabled={isBulkPending}
              className="cursor-pointer text-destructive border-destructive/30 hover:bg-destructive/10"
            >
              {isBulkPending ? (
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
              ) : (
                <Trash2 className="h-4 w-4 mr-2" />
              )}
              {t('media.deleteCount', { count: selectedIds.size, defaultValue: `Delete ${selectedIds.size}` })}
            </Button>
          </BulkActionToolbar>

          {/* Content */}
          {isLoading ? (
            viewMode === 'grid' ? (
              <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4">
                {[...Array(DEFAULT_PAGE_SIZE)].map((_, i) => (
                  <div key={i} className="animate-pulse">
                    <div className="aspect-square bg-muted rounded-xl" />
                    <div className="p-2.5 space-y-2">
                      <Skeleton className="h-4 w-3/4" />
                      <Skeleton className="h-3 w-1/2" />
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="space-y-2">
                {[...Array(8)].map((_, i) => (
                  <div key={i} className="flex items-center gap-4 p-3 animate-pulse">
                    <Skeleton className="h-10 w-10 rounded-lg" />
                    <div className="flex-1 space-y-2">
                      <Skeleton className="h-4 w-1/3" />
                      <Skeleton className="h-3 w-1/5" />
                    </div>
                  </div>
                ))}
              </div>
            )
          ) : data?.items.length === 0 ? (
            <EmptyState
              icon={ImageIcon}
              title={t('media.noFilesFound', 'No media files found')}
              description={t('media.noFilesDescription', 'Upload your first file to get started.')}
              action={{
                label: t('media.upload', 'Upload'),
                onClick: openUpload,
              }}
              className="border-0 rounded-none px-4 py-12"
            />
          ) : viewMode === 'grid' ? (
            <MediaGrid
              items={data?.items || []}
              selectedIds={selectedIds}
              onToggleSelect={handleToggleSelect}
              onOpenDetail={setDetailFile}
            />
          ) : (
            <MediaTable
              items={data?.items || []}
              selectedIds={selectedIds}
              isAllSelected={isAllSelected}
              onSelectAll={handleSelectAll}
              onSelectNone={handleSelectNone}
              onToggleSelect={handleToggleSelect}
              onOpenDetail={setDetailFile}
              onRename={handleRenameFromTable}
              onDelete={setFileToDelete}
            />
          )}

          {/* Pagination */}
          {data && data.totalPages > 1 && (
            <Pagination
              currentPage={data.pageIndex}
              totalPages={data.totalPages}
              totalItems={data.totalCount}
              pageSize={DEFAULT_PAGE_SIZE}
              onPageChange={handlePageChange}
              showPageSizeSelector={false}
              className="mt-4 justify-center"
            />
          )}
        </CardContent>
      </Card>

      {/* Detail Sheet */}
      <MediaDetailSheet
        file={detailFile}
        open={!!detailFile}
        onOpenChange={(open) => { if (!open) setDetailFile(null) }}
        onRename={handleRename}
        onDelete={(file) => {
          setFileToDelete(file)
          setDetailFile(null)
        }}
        isRenaming={renameMutation.isPending}
      />

      {/* Upload Dialog */}
      <MediaUploadDialog
        open={isUploadOpen}
        onOpenChange={onUploadOpenChange}
      />

      {/* Single Delete Dialog */}
      <DeleteMediaDialog
        open={!!fileToDelete}
        onOpenChange={(open) => { if (!open) setFileToDelete(null) }}
        file={fileToDelete}
        onConfirm={handleSingleDelete}
        isPending={deleteMutation.isPending}
      />

      {/* Bulk Delete Dialog */}
      <DeleteMediaDialog
        open={showBulkDeleteConfirm}
        onOpenChange={setShowBulkDeleteConfirm}
        bulkCount={selectedIds.size}
        bulkFileNames={bulkFileNames}
        onConfirm={handleBulkDelete}
        isPending={bulkDeleteMutation.isPending}
      />
    </div>
  )
}

export default MediaLibraryPage

import { useState, useDeferredValue, useMemo, useTransition, useCallback } from 'react'
import { createColumnHelper } from '@tanstack/react-table'
import type { ColumnDef, RowSelectionState } from '@tanstack/react-table'
import { useTranslation } from 'react-i18next'
import { ImageIcon, Plus, Trash2, Loader2, LayoutGrid, List as ListIcon, Pencil } from 'lucide-react'
import { formatDistanceToNow } from 'date-fns'
import { usePageContext } from '@/hooks/usePageContext'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { useUrlDialog } from '@/hooks/useUrlDialog'
import { useServerTable, useSelectedIds } from '@/hooks/useServerTable'
import { createSelectColumn, createActionsColumn } from '@/lib/table/columnHelpers'
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
  DataTable,
  DataTablePagination,
  DataTableToolbar,
  DropdownMenuItem,
  DropdownMenuSeparator,
  EmptyState,
  FilePreviewTrigger,
  PageHeader,
  Pagination,
  Skeleton,
  ViewModeToggle,
  type ViewModeOption,
} from '@uikit'
import { BulkActionToolbar } from '@/components/BulkActionToolbar'
import { MediaToolbar } from './MediaToolbar'
import { MediaGrid } from './MediaGrid'
import { MediaDetailSheet } from './MediaDetailSheet'
import { MediaUploadDialog } from './MediaUploadDialog'
import { DeleteMediaDialog } from './DeleteMediaDialog'
import { formatFileSize, extractFolderName } from './media-utils'

const DEFAULT_PAGE_SIZE = 24

const ch = createColumnHelper<MediaFileListItem>()

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
  const [rowSelection, setRowSelection] = useState<RowSelectionState>({})
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

  const selectedIds = useSelectedIds(rowSelection)
  const selectedIdsSet = useMemo(() => new Set(selectedIds), [selectedIds])
  const clearSelection = useCallback(() => setRowSelection({}), [])

  const handleCollectionUpdate = useCallback(() => {
    if (selectedIds.length === 0) refetch()
  }, [selectedIds.length, refetch])

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
    const ids = [...selectedIds]
    startBulkTransition(async () => {
      await bulkDeleteMutation.mutateAsync(ids)
      setRowSelection({})
      setShowBulkDeleteConfirm(false)
    })
  }

  // Rename handler
  const handleRename = (id: string, newFileName: string) => {
    renameMutation.mutate({ id, newFileName })
  }

  // Bulk file names for delete dialog
  const bulkFileNames = useMemo(() => {
    if (!data?.items) return []
    return data.items.filter(f => selectedIdsSet.has(f.id)).map(f => f.originalFileName)
  }, [data?.items, selectedIdsSet])

  const columns = useMemo((): ColumnDef<MediaFileListItem, unknown>[] => [
    createActionsColumn<MediaFileListItem>((item) => (
      <>
        <DropdownMenuItem className="cursor-pointer" onClick={() => setDetailFile(item)}>
          <Pencil className="h-4 w-4 mr-2" />
          {t('media.rename', 'Rename')}
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          className="cursor-pointer text-destructive focus:text-destructive"
          onClick={() => setFileToDelete(item)}
        >
          <Trash2 className="h-4 w-4 mr-2" />
          {t('labels.delete', 'Delete')}
        </DropdownMenuItem>
      </>
    )),
    createSelectColumn<MediaFileListItem>(),
    ch.accessor('defaultUrl', {
      id: 'preview',
      header: t('labels.preview', 'Preview'),
      enableSorting: false,
      cell: ({ row }) => (
        <FilePreviewTrigger
          file={{ url: row.original.defaultUrl, name: row.original.originalFileName }}
          thumbnailWidth={40}
          thumbnailHeight={40}
          className="rounded-lg"
        />
      ),
    }) as ColumnDef<MediaFileListItem, unknown>,
    ch.accessor('originalFileName', {
      id: 'name',
      header: t('labels.name', 'Name'),
      enableSorting: false,
      cell: ({ row }) => (
        <div className="flex flex-col min-w-0">
          <span className="font-medium truncate max-w-[300px]">{row.original.originalFileName}</span>
          {row.original.width > 0 && row.original.height > 0 && (
            <span className="text-xs text-muted-foreground">{row.original.width} x {row.original.height}</span>
          )}
        </div>
      ),
    }) as ColumnDef<MediaFileListItem, unknown>,
    ch.accessor('folder', {
      id: 'folder',
      header: t('media.folder', 'Folder'),
      enableSorting: false,
      cell: ({ getValue }) => (
        <span className="text-sm">{getValue() ? extractFolderName(getValue()) : '\u2014'}</span>
      ),
    }) as ColumnDef<MediaFileListItem, unknown>,
    ch.accessor('format', {
      id: 'type',
      header: t('media.type', 'Type'),
      enableSorting: false,
      cell: ({ getValue }) => <span className="text-sm uppercase">{getValue()}</span>,
    }) as ColumnDef<MediaFileListItem, unknown>,
    ch.accessor('sizeBytes', {
      id: 'size',
      header: t('media.size', 'Size'),
      enableSorting: false,
      meta: { align: 'right' as const },
      cell: ({ getValue }) => <span className="text-sm">{formatFileSize(getValue())}</span>,
    }) as ColumnDef<MediaFileListItem, unknown>,
    ch.accessor('createdAt', {
      id: 'uploaded',
      header: t('labels.uploaded', 'Uploaded'),
      enableSorting: false,
      cell: ({ getValue }) => (
        <span className="text-sm text-muted-foreground">
          {formatDistanceToNow(new Date(getValue()), { addSuffix: true })}
        </span>
      ),
    }) as ColumnDef<MediaFileListItem, unknown>,
  ], [t])

  const tableData = useMemo(() => data?.items ?? [], [data?.items])
  const table = useServerTable({
    data: tableData,
    columns,
    rowCount: data?.totalCount ?? 0,
    columnVisibilityStorageKey: 'media',
    state: {
      pagination: { pageIndex: currentPage - 1, pageSize: DEFAULT_PAGE_SIZE },
      sorting: [],
      rowSelection,
    },
    onPaginationChange: (updater) => {
      const next = typeof updater === 'function'
        ? updater({ pageIndex: currentPage - 1, pageSize: DEFAULT_PAGE_SIZE })
        : updater
      if (next.pageIndex !== currentPage - 1) {
        startFilterTransition(() => setCurrentPage(next.pageIndex + 1))
      }
    },
    onRowSelectionChange: setRowSelection,
    enableRowSelection: true,
    getRowId: (row) => row.id,
  })

  const handleToggleSelect = useCallback((id: string) => {
    setRowSelection(prev => {
      const next = { ...prev }
      if (next[id]) delete next[id]
      else next[id] = true
      return next
    })
  }, [])

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

            {viewMode === 'list' ? (
              <DataTableToolbar
                table={table}
                searchInput={searchInput}
                onSearchChange={handleSearchChange}
                searchPlaceholder={t('media.searchPlaceholder', 'Search media files...')}
                isSearchStale={isSearchStale}
                showColumnToggle={true}
                onResetColumnVisibility={table.resetColumnVisibility}
                filterSlot={
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
                    hideSearch
                  />
                }
              />
            ) : (
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
            )}
          </div>
        </CardHeader>

        <CardContent className={(isFilterPending || isSearchStale || isPlaceholderData) ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
          {/* Bulk Action Toolbar */}
          <BulkActionToolbar selectedCount={selectedIds.length} onClearSelection={clearSelection}>
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
              {t('media.deleteCount', { count: selectedIds.length, defaultValue: `Delete ${selectedIds.length}` })}
            </Button>
          </BulkActionToolbar>

          {/* Content */}
          {isLoading && viewMode === 'grid' ? (
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
          ) : !isLoading && data?.items.length === 0 ? (
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
            <>
              <MediaGrid
                items={data?.items || []}
                selectedIds={selectedIdsSet}
                onToggleSelect={handleToggleSelect}
                onOpenDetail={setDetailFile}
              />
              {data && data.totalPages > 1 && (
                <Pagination
                  currentPage={currentPage}
                  totalPages={data.totalPages}
                  totalItems={data.totalCount}
                  pageSize={DEFAULT_PAGE_SIZE}
                  onPageChange={handlePageChange}
                  showPageSizeSelector={false}
                  className="mt-4 justify-center"
                />
              )}
            </>
          ) : (
            <div className="space-y-3">
              <DataTable
                table={table}
                isLoading={isLoading}
                isStale={isSearchStale || isFilterPending || isPlaceholderData}
                onRowClick={(item) => setDetailFile(item)}
                emptyState={
                  <EmptyState
                    icon={ImageIcon}
                    title={t('media.noFilesFound', 'No media files found')}
                    description={t('media.noFilesDescription', 'Upload your first file to get started.')}
                    action={{ label: t('media.upload', 'Upload'), onClick: openUpload }}
                    className="border-0 rounded-none px-4 py-12"
                  />
                }
              />
              {data && data.totalPages > 1 && (
                <DataTablePagination table={table} showPageSizeSelector={false} />
              )}
            </div>
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
        bulkCount={selectedIds.length}
        bulkFileNames={bulkFileNames}
        onConfirm={handleBulkDelete}
        isPending={bulkDeleteMutation.isPending}
      />
    </div>
  )
}

export default MediaLibraryPage

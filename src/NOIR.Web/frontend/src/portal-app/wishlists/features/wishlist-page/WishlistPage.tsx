import { useState, useCallback, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  Copy,
  Edit,
  ExternalLink,
  Heart,
  Loader2,
  MoreHorizontal,
  Package,
  Plus,
  Share2,
  ShoppingCart,
  Trash2,
} from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  EmptyState,
  PageHeader,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Skeleton,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@uikit'
import {
  useWishlistsQuery,
  useWishlistDetailQuery,
  useRemoveFromWishlist,
  useMoveToCart,
  useShareWishlist,
  useDeleteWishlist,
  useUpdateWishlistItemPriority,
} from '@/portal-app/wishlists/queries'
import { WishlistFormDialog } from '@/portal-app/wishlists/components/WishlistFormDialog'
import type { WishlistDto, WishlistItemDto, WishlistItemPriority } from '@/types/wishlist'
import { formatCurrency } from '@/lib/utils/currency'

const PRIORITY_OPTIONS: WishlistItemPriority[] = ['None', 'Low', 'Medium', 'High']

const getPriorityColor = (priority: WishlistItemPriority): string => {
  switch (priority) {
    case 'High':
      return 'bg-red-100 text-red-700 border-red-200 dark:bg-red-900/30 dark:text-red-400 dark:border-red-800'
    case 'Medium':
      return 'bg-amber-100 text-amber-700 border-amber-200 dark:bg-amber-900/30 dark:text-amber-400 dark:border-amber-800'
    case 'Low':
      return 'bg-blue-100 text-blue-700 border-blue-200 dark:bg-blue-900/30 dark:text-blue-400 dark:border-blue-800'
    default:
      return ''
  }
}

export const WishlistPage = () => {
  const { t } = useTranslation('common')
  usePageContext('Wishlists')

  const [activeWishlistId, setActiveWishlistId] = useState<string | undefined>(undefined)
  const [isTabPending, startTabTransition] = useTransition()
  const [formDialogOpen, setFormDialogOpen] = useState(false)
  const [editingWishlist, setEditingWishlist] = useState<WishlistDto | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<WishlistDto | null>(null)

  const { data: wishlists, isLoading: loadingWishlists } = useWishlistsQuery()
  const selectedId = activeWishlistId ?? wishlists?.[0]?.id
  const { data: wishlistDetail, isLoading: loadingDetail } = useWishlistDetailQuery(selectedId)

  const removeItemMutation = useRemoveFromWishlist()
  const moveToCartMutation = useMoveToCart()
  const shareWishlistMutation = useShareWishlist()
  const deleteWishlistMutation = useDeleteWishlist()
  const updatePriorityMutation = useUpdateWishlistItemPriority()

  const handleTabChange = useCallback((value: string) => {
    startTabTransition(() => setActiveWishlistId(value))
  }, [])

  const handleCreateNew = useCallback(() => {
    setEditingWishlist(null)
    setFormDialogOpen(true)
  }, [])

  const handleEdit = useCallback((wishlist: WishlistDto) => {
    setEditingWishlist(wishlist)
    setFormDialogOpen(true)
  }, [])

  const handleDelete = useCallback(async () => {
    if (!deleteTarget) return
    try {
      await deleteWishlistMutation.mutateAsync(deleteTarget.id)
      toast.success(t('wishlists.deleteSuccess', 'Wishlist deleted successfully'))
      setDeleteTarget(null)
      // Reset active tab if the deleted one was selected
      if (activeWishlistId === deleteTarget.id) {
        setActiveWishlistId(undefined)
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : t('wishlists.deleteFailed', 'Failed to delete wishlist')
      toast.error(message)
    }
  }, [deleteTarget, deleteWishlistMutation, t, activeWishlistId])

  const handleRemoveItem = useCallback(async (item: WishlistItemDto) => {
    try {
      await removeItemMutation.mutateAsync(item.id)
      toast.success(t('wishlists.itemRemoved', 'Item removed from wishlist'))
    } catch (err) {
      const message = err instanceof Error ? err.message : t('wishlists.removeFailed', 'Failed to remove item')
      toast.error(message)
    }
  }, [removeItemMutation, t])

  const handleMoveToCart = useCallback(async (item: WishlistItemDto) => {
    try {
      await moveToCartMutation.mutateAsync(item.id)
      toast.success(t('wishlists.movedToCart', 'Item moved to cart'))
    } catch (err) {
      const message = err instanceof Error ? err.message : t('wishlists.moveToCartFailed', 'Failed to move item to cart')
      toast.error(message)
    }
  }, [moveToCartMutation, t])

  const handleShare = useCallback(async (wishlist: WishlistDto) => {
    try {
      const result = await shareWishlistMutation.mutateAsync(wishlist.id)
      if (result.shareUrl) {
        await navigator.clipboard.writeText(result.shareUrl)
        toast.success(t('wishlists.shareLinkCopied', 'Share link copied to clipboard'))
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : t('wishlists.shareFailed', 'Failed to share wishlist')
      toast.error(message)
    }
  }, [shareWishlistMutation, t])

  const handlePriorityChange = useCallback(async (itemId: string, priority: WishlistItemPriority) => {
    try {
      await updatePriorityMutation.mutateAsync({ itemId, priority })
    } catch (err) {
      const message = err instanceof Error ? err.message : t('wishlists.priorityUpdateFailed', 'Failed to update priority')
      toast.error(message)
    }
  }, [updatePriorityMutation, t])

  const items = wishlistDetail?.items ?? []

  return (
    <div className="space-y-6">
      <PageHeader
        icon={Heart}
        title={t('wishlists.title', 'My Wishlists')}
        description={t('wishlists.description', 'Manage your saved products and wishlists')}
        responsive
        action={
          <Button onClick={handleCreateNew} className="cursor-pointer">
            <Plus className="mr-2 h-4 w-4" />
            {t('wishlists.newWishlist', 'New Wishlist')}
          </Button>
        }
      />

      {loadingWishlists ? (
        <Card className="shadow-sm">
          <CardContent className="p-6">
            <div className="space-y-4">
              <Skeleton className="h-10 w-full max-w-md" />
              <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3">
                {[...Array(3)].map((_, i) => (
                  <Skeleton key={i} className="h-48 rounded-xl" />
                ))}
              </div>
            </div>
          </CardContent>
        </Card>
      ) : !wishlists || wishlists.length === 0 ? (
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
          <CardContent className="p-6">
            <EmptyState
              icon={Heart}
              title={t('wishlists.noWishlists', 'No wishlists yet')}
              description={t('wishlists.noWishlistsDescription', 'Create your first wishlist to start saving products you love.')}
              action={{
                label: t('wishlists.createFirstWishlist', 'Create Wishlist'),
                onClick: handleCreateNew,
              }}
            />
          </CardContent>
        </Card>
      ) : (
        <Tabs value={selectedId} onValueChange={handleTabChange}>
          <div className="flex items-center justify-between gap-4 flex-wrap">
            <TabsList className="flex-wrap h-auto gap-1">
              {wishlists.map((wishlist) => (
                <TabsTrigger
                  key={wishlist.id}
                  value={wishlist.id}
                  className="cursor-pointer"
                >
                  {wishlist.name}
                  <Badge variant="secondary" className="ml-2 text-xs">
                    {wishlist.itemCount}
                  </Badge>
                  {wishlist.isDefault && (
                    <Badge variant="outline" className="ml-1 text-xs">
                      {t('wishlists.default', 'Default')}
                    </Badge>
                  )}
                </TabsTrigger>
              ))}
            </TabsList>
          </div>

          {wishlists.map((wishlist) => (
            <TabsContent key={wishlist.id} value={wishlist.id} className="mt-4">
              <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
                <CardHeader>
                  <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                    <div className="space-y-1">
                      <CardTitle className="flex items-center gap-2">
                        {wishlist.name}
                        {wishlist.isPublic && (
                          <Badge variant="outline" className="text-xs">
                            {t('wishlists.public', 'Public')}
                          </Badge>
                        )}
                      </CardTitle>
                      <CardDescription>
                        {t('wishlists.itemsCount', '{{count}} items', { count: wishlist.itemCount })}
                      </CardDescription>
                    </div>
                    <div className="flex items-center gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        className="cursor-pointer"
                        onClick={() => handleShare(wishlist)}
                        disabled={shareWishlistMutation.isPending}
                      >
                        {shareWishlistMutation.isPending ? (
                          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        ) : (
                          <Share2 className="mr-2 h-4 w-4" />
                        )}
                        {t('wishlists.share', 'Share')}
                      </Button>
                      {wishlist.shareUrl && (
                        <Button
                          variant="ghost"
                          size="icon"
                          className="cursor-pointer h-8 w-8"
                          onClick={() => {
                            navigator.clipboard.writeText(wishlist.shareUrl!)
                            toast.success(t('messages.copySuccess', 'Copied to clipboard'))
                          }}
                          aria-label={t('wishlists.copyShareLink', 'Copy share link')}
                        >
                          <Copy className="h-4 w-4" />
                        </Button>
                      )}
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <Button
                            variant="ghost"
                            size="icon"
                            className="cursor-pointer h-8 w-8"
                            aria-label={t('labels.actionsFor', { name: wishlist.name, defaultValue: `Actions for ${wishlist.name}` })}
                          >
                            <MoreHorizontal className="h-4 w-4" />
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end">
                          <DropdownMenuItem
                            className="cursor-pointer"
                            onClick={() => handleEdit(wishlist)}
                          >
                            <Edit className="mr-2 h-4 w-4" />
                            {t('buttons.edit', 'Edit')}
                          </DropdownMenuItem>
                          {wishlist.shareUrl && (
                            <DropdownMenuItem
                              className="cursor-pointer"
                              onClick={() => window.open(wishlist.shareUrl!, '_blank')}
                            >
                              <ExternalLink className="mr-2 h-4 w-4" />
                              {t('wishlists.viewShared', 'View Shared')}
                            </DropdownMenuItem>
                          )}
                          {!wishlist.isDefault && (
                            <>
                              <DropdownMenuSeparator />
                              <DropdownMenuItem
                                className="cursor-pointer text-destructive focus:text-destructive"
                                onClick={() => setDeleteTarget(wishlist)}
                              >
                                <Trash2 className="mr-2 h-4 w-4" />
                                {t('buttons.delete', 'Delete')}
                              </DropdownMenuItem>
                            </>
                          )}
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </div>
                  </div>
                </CardHeader>
                <CardContent className={isTabPending ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
                  {loadingDetail && selectedId === wishlist.id ? (
                    <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3">
                      {[...Array(3)].map((_, i) => (
                        <div key={i} className="rounded-xl border border-border/50 p-4 space-y-3">
                          <Skeleton className="h-32 w-full rounded-lg" />
                          <Skeleton className="h-4 w-3/4" />
                          <Skeleton className="h-4 w-1/2" />
                          <div className="flex gap-2">
                            <Skeleton className="h-8 w-20" />
                            <Skeleton className="h-8 w-20" />
                          </div>
                        </div>
                      ))}
                    </div>
                  ) : items.length === 0 ? (
                    <EmptyState
                      icon={Heart}
                      title={t('wishlists.noItems', 'No items in this wishlist')}
                      description={t('wishlists.noItemsDescription', 'Add products to this wishlist to save them for later.')}
                      className="py-12"
                    />
                  ) : (
                    <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3">
                      {items.map((item) => (
                        <WishlistItemCard
                          key={item.id}
                          item={item}
                          onRemove={handleRemoveItem}
                          onMoveToCart={handleMoveToCart}
                          onPriorityChange={handlePriorityChange}
                          isRemoving={removeItemMutation.isPending && removeItemMutation.variables === item.id}
                          isMoving={moveToCartMutation.isPending && moveToCartMutation.variables === item.id}
                          t={t}
                        />
                      ))}
                    </div>
                  )}
                </CardContent>
              </Card>
            </TabsContent>
          ))}
        </Tabs>
      )}

      {/* Create/Edit Wishlist Dialog */}
      <WishlistFormDialog
        open={formDialogOpen}
        onOpenChange={setFormDialogOpen}
        wishlist={editingWishlist}
      />

      {/* Delete Confirmation */}
      <AlertDialog open={!!deleteTarget} onOpenChange={(open) => !open && setDeleteTarget(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <Trash2 className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <AlertDialogTitle>
                  {t('wishlists.deleteTitle', 'Delete Wishlist')}
                </AlertDialogTitle>
                <AlertDialogDescription>
                  {t('wishlists.deleteDescription', 'Are you sure you want to delete "{{name}}"? All items in this wishlist will be lost.', { name: deleteTarget?.name })}
                </AlertDialogDescription>
              </div>
            </div>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel className="cursor-pointer">
              {t('buttons.cancel', 'Cancel')}
            </AlertDialogCancel>
            <AlertDialogAction
              className="cursor-pointer bg-destructive text-destructive-foreground hover:bg-destructive/90 border-destructive/30"
              onClick={handleDelete}
              disabled={deleteWishlistMutation.isPending}
            >
              {deleteWishlistMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {t('buttons.delete', 'Delete')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}

// ---------------------------------------------------------------------------
// WishlistItemCard sub-component
// ---------------------------------------------------------------------------

interface WishlistItemCardProps {
  item: WishlistItemDto
  onRemove: (item: WishlistItemDto) => void
  onMoveToCart: (item: WishlistItemDto) => void
  onPriorityChange: (itemId: string, priority: WishlistItemPriority) => void
  isRemoving: boolean
  isMoving: boolean
  t: ReturnType<typeof useTranslation<'common'>>['t']
}

const WishlistItemCard = ({
  item,
  onRemove,
  onMoveToCart,
  onPriorityChange,
  isRemoving,
  isMoving,
  t,
}: WishlistItemCardProps) => {
  return (
    <div className="group rounded-xl border border-border/50 p-4 transition-all duration-300 hover:shadow-md hover:border-border">
      {/* Product Image */}
      <div className="relative mb-3">
        {item.productImage ? (
          <img
            src={item.productImage}
            alt={item.productName}
            className="w-full h-36 object-cover rounded-lg border border-border/50"
          />
        ) : (
          <div className="w-full h-36 rounded-lg bg-muted flex items-center justify-center border border-border/50">
            <Package className="h-8 w-8 text-muted-foreground" />
          </div>
        )}
        {!item.isInStock && (
          <Badge variant="destructive" className="absolute top-2 left-2 text-xs">
            {t('wishlists.outOfStock', 'Out of Stock')}
          </Badge>
        )}
        {item.priority !== 'None' && (
          <Badge
            variant="outline"
            className={`absolute top-2 right-2 text-xs ${getPriorityColor(item.priority)}`}
          >
            {t(`wishlists.priority.${item.priority.toLowerCase()}`, item.priority)}
          </Badge>
        )}
      </div>

      {/* Product Info */}
      <div className="space-y-2">
        <h4 className="font-medium text-sm line-clamp-2 leading-snug">{item.productName}</h4>
        {item.variantName && (
          <p className="text-xs text-muted-foreground">{item.variantName}</p>
        )}
        <p className="text-lg font-semibold tracking-tight">
          {formatCurrency(item.price)}
        </p>
        {item.note && (
          <p className="text-xs text-muted-foreground italic line-clamp-2">{item.note}</p>
        )}
      </div>

      {/* Priority Selector */}
      <div className="mt-3">
        <Select
          value={item.priority}
          onValueChange={(value) => onPriorityChange(item.id, value as WishlistItemPriority)}
        >
          <SelectTrigger className="h-8 text-xs cursor-pointer">
            <SelectValue placeholder={t('wishlists.selectPriority', 'Priority')} />
          </SelectTrigger>
          <SelectContent>
            {PRIORITY_OPTIONS.map((priority) => (
              <SelectItem key={priority} value={priority} className="text-xs cursor-pointer">
                {t(`wishlists.priority.${priority.toLowerCase()}`, priority)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {/* Actions */}
      <div className="mt-3 flex items-center gap-2">
        <Button
          variant="default"
          size="sm"
          className="flex-1 cursor-pointer text-xs"
          onClick={() => onMoveToCart(item)}
          disabled={isMoving || !item.isInStock}
        >
          {isMoving ? (
            <Loader2 className="mr-1.5 h-3.5 w-3.5 animate-spin" />
          ) : (
            <ShoppingCart className="mr-1.5 h-3.5 w-3.5" />
          )}
          {t('wishlists.moveToCart', 'Move to Cart')}
        </Button>
        <Button
          variant="ghost"
          size="icon"
          className="cursor-pointer h-8 w-8 text-destructive hover:text-destructive hover:bg-destructive/10"
          onClick={() => onRemove(item)}
          disabled={isRemoving}
          aria-label={t('wishlists.removeItem', { name: item.productName, defaultValue: `Remove ${item.productName}` })}
        >
          {isRemoving ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <Trash2 className="h-4 w-4" />
          )}
        </Button>
      </div>
    </div>
  )
}

export default WishlistPage

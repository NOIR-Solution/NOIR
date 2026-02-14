import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Tags, Plus, Trash2, GripVertical, Check } from 'lucide-react'
import {
  useCategoryAttributesQuery,
  useActiveProductAttributesQuery,
  useAssignCategoryAttributeMutation,
  useUpdateCategoryAttributeMutation,
  useRemoveCategoryAttributeMutation,
} from '@/portal-app/products/queries'
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
  Checkbox,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  EmptyState,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@uikit'

import { toast } from 'sonner'
import type { ProductCategoryListItem } from '@/types/product'
import type { CategoryAttribute } from '@/types/productAttribute'

interface ProductCategoryAttributesDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  category: ProductCategoryListItem | null
}

export const ProductCategoryAttributesDialog = ({
  open,
  onOpenChange,
  category,
}: ProductCategoryAttributesDialogProps) => {
  const { t } = useTranslation('common')
  const [showAddAttribute, setShowAddAttribute] = useState(false)
  const [selectedAttributeId, setSelectedAttributeId] = useState<string>('')
  const [selectedIsRequired, setSelectedIsRequired] = useState(false)
  const [attributeToRemove, setAttributeToRemove] = useState<CategoryAttribute | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const { data: categoryAttributes = [], isLoading: loading } = useCategoryAttributesQuery(category?.id)
  const { data: allAttributes = [], isLoading: loadingAttributes } = useActiveProductAttributesQuery()
  const assignMutation = useAssignCategoryAttributeMutation()
  const updateMutation = useUpdateCategoryAttributeMutation()
  const removeMutation = useRemoveCategoryAttributeMutation()

  // Filter out already assigned attributes
  const availableAttributes = allAttributes.filter(
    (attr) => !categoryAttributes.find((ca) => ca.attributeId === attr.id)
  )

  const handleAddAttribute = async () => {
    if (!selectedAttributeId || !category) return

    setIsSubmitting(true)
    try {
      await assignMutation.mutateAsync({
        categoryId: category.id,
        request: {
          attributeId: selectedAttributeId,
          isRequired: selectedIsRequired,
          sortOrder: categoryAttributes.length,
        },
      })
      toast.success(t('categoryAttributes.assignSuccess', 'Attribute assigned successfully'))
      setShowAddAttribute(false)
      setSelectedAttributeId('')
      setSelectedIsRequired(false)
    } catch (err) {
      const message = err instanceof Error ? err.message : t('categoryAttributes.assignError', 'Failed to assign attribute')
      toast.error(message)
    }
    setIsSubmitting(false)
  }

  const handleToggleRequired = async (ca: CategoryAttribute) => {
    if (!category) return
    try {
      await updateMutation.mutateAsync({
        categoryId: category.id,
        attributeId: ca.attributeId,
        request: {
          isRequired: !ca.isRequired,
          sortOrder: ca.sortOrder,
        },
      })
      toast.success(t('categoryAttributes.updateSuccess', 'Attribute updated'))
    } catch (err) {
      const message = err instanceof Error ? err.message : t('categoryAttributes.updateError', 'Failed to update attribute')
      toast.error(message)
    }
  }

  const confirmRemoveAttribute = async () => {
    if (!attributeToRemove || !category) return

    try {
      await removeMutation.mutateAsync({
        categoryId: category.id,
        attributeId: attributeToRemove.attributeId,
      })
      toast.success(t('categoryAttributes.removeSuccess', 'Attribute removed'))
      setAttributeToRemove(null)
    } catch (err) {
      const message = err instanceof Error ? err.message : t('categoryAttributes.removeError', 'Failed to remove attribute')
      toast.error(message)
    }
  }

  if (!category) return null

  return (
    <>
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent className="sm:max-w-[600px]">
          <DialogHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-primary/10 border border-primary/20">
                <Tags className="h-5 w-5 text-primary" />
              </div>
              <div>
                <DialogTitle>
                  {t('categoryAttributes.title', 'Category Attributes')}
                </DialogTitle>
                <DialogDescription>
                  {t('categoryAttributes.description', 'Manage which attributes are available for products in "{name}"', { name: category.name })}
                </DialogDescription>
              </div>
            </div>
          </DialogHeader>

          <div className="space-y-4">
            {/* Add Attribute Section */}
            {showAddAttribute ? (
              <div className="flex items-end gap-2 p-4 bg-muted/50 rounded-lg">
                <div className="flex-1 space-y-2">
                  <label className="text-sm font-medium">
                    {t('categoryAttributes.selectAttribute', 'Select Attribute')}
                  </label>
                  <Select
                    value={selectedAttributeId}
                    onValueChange={setSelectedAttributeId}
                    disabled={loadingAttributes}
                  >
                    <SelectTrigger className="cursor-pointer" aria-label={t('categoryAttributes.selectAttribute', 'Select Attribute')}>
                      <SelectValue placeholder={t('categoryAttributes.selectPlaceholder', 'Choose an attribute...')} />
                    </SelectTrigger>
                    <SelectContent>
                      {availableAttributes.map((attr) => (
                        <SelectItem key={attr.id} value={attr.id} className="cursor-pointer">
                          <div className="flex items-center gap-2">
                            <span>{attr.name}</span>
                            <Badge variant="outline" className="text-xs">{attr.type}</Badge>
                          </div>
                        </SelectItem>
                      ))}
                      {availableAttributes.length === 0 && (
                        <SelectItem value="none" disabled>
                          {t('categoryAttributes.noAvailableAttributes', 'No available attributes')}
                        </SelectItem>
                      )}
                    </SelectContent>
                  </Select>
                </div>
                <div className="flex items-center gap-2 pb-0.5">
                  <Checkbox
                    id="isRequired"
                    checked={selectedIsRequired}
                    onCheckedChange={(checked) => setSelectedIsRequired(checked === true)}
                    className="cursor-pointer"
                  />
                  <label htmlFor="isRequired" className="text-sm cursor-pointer">
                    {t('labels.required', 'Required')}
                  </label>
                </div>
                <Button
                  size="sm"
                  onClick={handleAddAttribute}
                  disabled={!selectedAttributeId || isSubmitting}
                  className="cursor-pointer"
                >
                  <Check className="h-4 w-4 mr-1" />
                  {t('labels.add', 'Add')}
                </Button>
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => {
                    setShowAddAttribute(false)
                    setSelectedAttributeId('')
                    setSelectedIsRequired(false)
                  }}
                  className="cursor-pointer"
                >
                  {t('labels.cancel', 'Cancel')}
                </Button>
              </div>
            ) : (
              <Button
                variant="outline"
                size="sm"
                onClick={() => setShowAddAttribute(true)}
                disabled={availableAttributes.length === 0}
                className="cursor-pointer"
              >
                <Plus className="h-4 w-4 mr-2" />
                {t('categoryAttributes.addAttribute', 'Add Attribute')}
              </Button>
            )}

            {/* Attributes List */}
            <div className="rounded-lg border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-8"></TableHead>
                    <TableHead>{t('labels.name', 'Name')}</TableHead>
                    <TableHead>{t('labels.code', 'Code')}</TableHead>
                    <TableHead className="text-center">{t('labels.required', 'Required')}</TableHead>
                    <TableHead className="text-right">{t('labels.actions', 'Actions')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {loading ? (
                    [...Array(3)].map((_, i) => (
                      <TableRow key={i}>
                        <TableCell><Skeleton className="h-4 w-4" /></TableCell>
                        <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                        <TableCell><Skeleton className="h-4 w-16" /></TableCell>
                        <TableCell className="text-center"><Skeleton className="h-4 w-4 mx-auto" /></TableCell>
                        <TableCell className="text-right"><Skeleton className="h-8 w-8 ml-auto" /></TableCell>
                      </TableRow>
                    ))
                  ) : categoryAttributes.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={5} className="p-0">
                        <EmptyState
                          icon={Tags}
                          title={t('categoryAttributes.noAttributes', 'No attributes assigned')}
                          description={t('categoryAttributes.noAttributesDescription', 'Add attributes to define what information products in this category should have.')}
                          className="border-0 rounded-none py-8"
                        />
                      </TableCell>
                    </TableRow>
                  ) : (
                    categoryAttributes.map((ca) => (
                      <TableRow key={ca.id} className="group">
                        <TableCell>
                          <GripVertical className="h-4 w-4 text-muted-foreground cursor-grab" />
                        </TableCell>
                        <TableCell>
                          <span className="font-medium">{ca.attributeName}</span>
                        </TableCell>
                        <TableCell>
                          <code className="text-sm bg-muted px-1.5 py-0.5 rounded">
                            {ca.attributeCode}
                          </code>
                        </TableCell>
                        <TableCell className="text-center">
                          <Checkbox
                            checked={ca.isRequired}
                            onCheckedChange={() => handleToggleRequired(ca)}
                            className="cursor-pointer"
                            aria-label={t('categoryAttributes.toggleRequired', 'Toggle required for {name}', { name: ca.attributeName })}
                          />
                        </TableCell>
                        <TableCell className="text-right">
                          <Button
                            variant="ghost"
                            size="sm"
                            className="cursor-pointer h-8 w-8 p-0 text-destructive hover:text-destructive hover:bg-destructive/10"
                            onClick={() => setAttributeToRemove(ca)}
                            aria-label={t('categoryAttributes.removeAttribute', 'Remove {name}', { name: ca.attributeName })}
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      {/* Remove Confirmation Dialog */}
      <AlertDialog open={!!attributeToRemove} onOpenChange={(open) => !open && setAttributeToRemove(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <Trash2 className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <AlertDialogTitle>{t('categoryAttributes.removeTitle', 'Remove Attribute')}</AlertDialogTitle>
                <AlertDialogDescription>
                  {t('categoryAttributes.removeDescription', 'Are you sure you want to remove "{name}" from this category?', { name: attributeToRemove?.attributeName })}
                </AlertDialogDescription>
              </div>
            </div>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel className="cursor-pointer">{t('labels.cancel', 'Cancel')}</AlertDialogCancel>
            <AlertDialogAction
              onClick={confirmRemoveAttribute}
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
            >
              {t('labels.remove', 'Remove')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  )
}

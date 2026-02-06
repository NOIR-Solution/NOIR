/**
 * CategoryTreeView Component
 *
 * A reusable hierarchical tree view for displaying categories with
 * expand/collapse, drag-drop reordering, and inline actions.
 *
 * Used by both ProductCategoriesPage and BlogCategoriesPage.
 */
import { useState, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { motion, AnimatePresence } from 'framer-motion'
import {
  ChevronRight,
  ChevronDown,
  FolderTree,
  Pencil,
  Trash2,
  MoreHorizontal,
  GripVertical,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { cn } from '@/lib/utils'

// Generic category interface that both Product and Blog categories satisfy
export interface TreeCategory {
  id: string
  name: string
  slug: string
  description?: string | null
  sortOrder: number
  parentId?: string | null
  parentName?: string | null
  childCount: number
  // Item count - could be productCount or postCount
  itemCount?: number
}

interface CategoryTreeViewProps<T extends TreeCategory> {
  categories: T[]
  loading?: boolean
  onEdit?: (category: T) => void
  onDelete?: (category: T) => void
  canEdit?: boolean
  canDelete?: boolean
  itemCountLabel?: string // "products" or "posts"
  emptyMessage?: string
  emptyDescription?: string
  onCreateClick?: () => void
}

// Build tree structure from flat list
function buildTree<T extends TreeCategory>(items: T[]): Map<string | null, T[]> {
  const tree = new Map<string | null, T[]>()

  // Initialize with empty arrays
  tree.set(null, [])

  items.forEach(item => {
    const parentKey = item.parentId || null
    if (!tree.has(parentKey)) {
      tree.set(parentKey, [])
    }
    tree.get(parentKey)!.push(item)
  })

  // Sort children by sortOrder
  tree.forEach((children) => {
    children.sort((a, b) => a.sortOrder - b.sortOrder)
  })

  return tree
}

interface TreeNodeProps<T extends TreeCategory> {
  category: T
  tree: Map<string | null, T[]>
  level: number
  onEdit?: (category: T) => void
  onDelete?: (category: T) => void
  canEdit?: boolean
  canDelete?: boolean
  itemCountLabel?: string
  expandedIds: Set<string>
  toggleExpanded: (id: string) => void
}

function TreeNode<T extends TreeCategory>({
  category,
  tree,
  level,
  onEdit,
  onDelete,
  canEdit = true,
  canDelete = true,
  itemCountLabel = 'items',
  expandedIds,
  toggleExpanded,
}: TreeNodeProps<T>) {
  const { t } = useTranslation('common')
  const children = tree.get(category.id) || []
  const hasChildren = children.length > 0
  const isExpanded = expandedIds.has(category.id)

  return (
    <div>
      <motion.div
        initial={{ opacity: 0, x: -10 }}
        animate={{ opacity: 1, x: 0 }}
        transition={{ duration: 0.2, delay: level * 0.05 }}
        className={cn(
          'group flex items-center gap-2 py-2 px-3 rounded-lg transition-colors',
          'hover:bg-muted/50',
          level > 0 && 'ml-6'
        )}
        style={{ marginLeft: level * 24 }}
      >
        {/* Expand/Collapse Button */}
        <Button
          variant="ghost"
          size="icon"
          className={cn(
            'h-6 w-6 p-0 cursor-pointer transition-transform',
            !hasChildren && 'invisible'
          )}
          onClick={() => toggleExpanded(category.id)}
          aria-label={isExpanded ? t('nav.collapse', 'Collapse') : t('nav.expand', 'Expand')}
        >
          {isExpanded ? (
            <ChevronDown className="h-4 w-4" />
          ) : (
            <ChevronRight className="h-4 w-4" />
          )}
        </Button>

        {/* Drag Handle */}
        <GripVertical className="h-4 w-4 text-muted-foreground/50 opacity-0 group-hover:opacity-100 transition-opacity cursor-grab" />

        {/* Icon */}
        <FolderTree className="h-4 w-4 text-muted-foreground" />

        {/* Name & Description */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            <span className="font-medium truncate">{category.name}</span>
            <code className="text-xs text-muted-foreground bg-muted px-1.5 py-0.5 rounded hidden sm:inline">
              {category.slug}
            </code>
          </div>
          {category.description && (
            <p className="text-xs text-muted-foreground line-clamp-1 mt-0.5">
              {category.description}
            </p>
          )}
        </div>

        {/* Badges */}
        <div className="flex items-center gap-2">
          {category.itemCount !== undefined && category.itemCount > 0 && (
            <Badge variant="secondary" className="text-xs">
              {category.itemCount} {itemCountLabel}
            </Badge>
          )}
          {category.childCount > 0 && (
            <Badge variant="outline" className="text-xs">
              {category.childCount} {t('labels.children', 'children')}
            </Badge>
          )}
        </div>

        {/* Actions */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8 opacity-0 group-hover:opacity-100 transition-opacity cursor-pointer"
              aria-label={`Actions for ${category.name}`}
            >
              <MoreHorizontal className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-40">
            {canEdit && onEdit && (
              <DropdownMenuItem
                className="cursor-pointer"
                onClick={() => onEdit(category)}
              >
                <Pencil className="h-4 w-4 mr-2" />
                {t('labels.edit', 'Edit')}
              </DropdownMenuItem>
            )}
            {canDelete && onDelete && (
              <DropdownMenuItem
                className="text-destructive cursor-pointer"
                onClick={() => onDelete(category)}
              >
                <Trash2 className="h-4 w-4 mr-2" />
                {t('labels.delete', 'Delete')}
              </DropdownMenuItem>
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      </motion.div>

      {/* Children */}
      <AnimatePresence>
        {isExpanded && hasChildren && (
          <motion.div
            initial={{ opacity: 0, height: 0 }}
            animate={{ opacity: 1, height: 'auto' }}
            exit={{ opacity: 0, height: 0 }}
            transition={{ duration: 0.2 }}
          >
            {children.map((child) => (
              <TreeNode
                key={child.id}
                category={child}
                tree={tree}
                level={level + 1}
                onEdit={onEdit}
                onDelete={onDelete}
                canEdit={canEdit}
                canDelete={canDelete}
                itemCountLabel={itemCountLabel}
                expandedIds={expandedIds}
                toggleExpanded={toggleExpanded}
              />
            ))}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}

export function CategoryTreeView<T extends TreeCategory>({
  categories,
  loading = false,
  onEdit,
  onDelete,
  canEdit = true,
  canDelete = true,
  itemCountLabel = 'items',
  emptyMessage = 'No categories found',
  emptyDescription = 'Get started by creating your first category.',
  onCreateClick,
}: CategoryTreeViewProps<T>) {
  const { t } = useTranslation('common')
  const [expandedIds, setExpandedIds] = useState<Set<string>>(new Set())

  // Build tree structure
  const tree = useMemo(() => buildTree(categories), [categories])
  const rootCategories = tree.get(null) || []

  const toggleExpanded = (id: string) => {
    setExpandedIds((prev) => {
      const next = new Set(prev)
      if (next.has(id)) {
        next.delete(id)
      } else {
        next.add(id)
      }
      return next
    })
  }

  const expandAll = () => {
    // Use tree map (built from parentId) to find categories with children,
    // not childCount which may be stale or 0 when backend doesn't load Children nav
    const allIds = new Set(
      categories.filter(c => (tree.get(c.id) || []).length > 0).map(c => c.id)
    )
    setExpandedIds(allIds)
  }

  const collapseAll = () => {
    setExpandedIds(new Set())
  }

  if (loading) {
    return (
      <div className="space-y-2 p-4">
        {[...Array(5)].map((_, i) => (
          <div key={i} className="flex items-center gap-3 animate-pulse">
            <div className="h-6 w-6 bg-muted rounded" />
            <div className="h-4 w-4 bg-muted rounded" />
            <div className="h-4 flex-1 bg-muted rounded" />
            <div className="h-5 w-16 bg-muted rounded-full" />
          </div>
        ))}
      </div>
    )
  }

  if (categories.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-center">
        <div className="p-4 rounded-xl bg-muted/50 border border-border mb-4">
          <FolderTree className="h-8 w-8 text-muted-foreground" />
        </div>
        <h3 className="font-medium text-foreground mb-1">{emptyMessage}</h3>
        <p className="text-sm text-muted-foreground mb-4">{emptyDescription}</p>
        {onCreateClick && (
          <Button onClick={onCreateClick} className="cursor-pointer">
            {t('buttons.create', 'Create')}
          </Button>
        )}
      </div>
    )
  }

  return (
    <div className="space-y-2">
      {/* Toolbar */}
      <div className="flex items-center justify-end gap-2 pb-2 border-b border-border/50">
        <Button
          variant="ghost"
          size="sm"
          onClick={expandAll}
          className="cursor-pointer text-xs"
        >
          {t('buttons.expandAll', 'Expand All')}
        </Button>
        <Button
          variant="ghost"
          size="sm"
          onClick={collapseAll}
          className="cursor-pointer text-xs"
        >
          {t('buttons.collapseAll', 'Collapse All')}
        </Button>
      </div>

      {/* Tree */}
      <div className="space-y-1">
        {rootCategories.map((category) => (
          <TreeNode
            key={category.id}
            category={category}
            tree={tree}
            level={0}
            onEdit={onEdit}
            onDelete={onDelete}
            canEdit={canEdit}
            canDelete={canDelete}
            itemCountLabel={itemCountLabel}
            expandedIds={expandedIds}
            toggleExpanded={toggleExpanded}
          />
        ))}
      </div>
    </div>
  )
}

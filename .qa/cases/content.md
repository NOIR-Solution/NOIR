# Content Domain — Test Cases

> Pages: /portal/blog/posts, /portal/blog/posts/:id/edit, /portal/blog/posts/new, /portal/blog/categories, /portal/blog/tags, /portal/media | Last updated: 2026-04-03 | Git ref: f6f5cd3
> Total: 82 cases | P0: 5 | P1: 39 | P2: 28 | P3: 10

---

## Page: Blog Posts List (`/portal/blog/posts`)

### Happy Path

#### TC-CON-001: View blog posts list [P1] [smoke]
- **Pre**: Blog posts exist with various statuses
- **Steps**:
  1. Navigate to `/portal/blog/posts`
- **Expected**: DataTable with columns: Actions, Select, Title (with FilePreviewTrigger thumbnail + excerpt), Status, Category, Views, audit columns. CardDescription shows "Showing X of Y". "New Post" button links to `/portal/blog/posts/new`.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Status badges: Draft=gray, Published=green, Scheduled=blue, Archived=yellow

#### TC-CON-002: Search blog posts [P1] [smoke]
- **Pre**: Multiple posts exist
- **Steps**:
  1. Type post title fragment in search
  2. Observe table filter
- **Expected**: Table filters reactively. Search full-width. Content opacity transition during search.

#### TC-CON-003: Filter by status [P1] [regression]
- **Pre**: Posts with Draft, Published, Scheduled, Archived statuses
- **Steps**:
  1. Select "Published" from status filter
  2. Verify only published posts shown
  3. Reset to "All"
- **Expected**: Status filter works. Pagination resets on filter change.

#### TC-CON-004: Filter by category [P1] [regression]
- **Pre**: Posts assigned to different categories; categories exist
- **Steps**:
  1. Select a category from category filter
  2. Verify filtered results
- **Expected**: Only posts in selected category shown. Dropdown populated from `useBlogCategoriesQuery`.

#### TC-CON-005: Featured image thumbnail with preview [P0] [smoke]
- **Pre**: Post with featured image exists
- **Steps**:
  1. Observe Title column — thumbnail on left, title + excerpt on right
  2. Hover over thumbnail — popover preview
  3. Click thumbnail — lightbox opens
- **Expected**: FilePreviewTrigger renders 48x48 thumbnail. Hover shows popover. Click opens full-size preview. `viewTransitionName` set for smooth navigation.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CON-006: Navigate to edit page via row click [P1] [smoke]
- **Pre**: Post exists; no rows selected
- **Steps**:
  1. Click on a post row
- **Expected**: Navigates to `/portal/blog/posts/<id>/edit`. When rows selected, row click does NOT navigate.

#### TC-CON-007: Navigate to edit page via actions menu [P1] [regression]
- **Pre**: Post exists
- **Steps**:
  1. Click EllipsisVertical on post row
  2. Click "Edit" (ViewTransitionLink)
- **Expected**: Navigates to edit page with view transition.

#### TC-CON-008: Delete post with confirmation [P1] [regression]
- **Pre**: Has BlogPostsDelete permission; post exists
- **Steps**:
  1. Actions > "Delete"
  2. DeleteBlogPostDialog opens
  3. Confirm
- **Expected**: Post deleted. Row fades out via `fadeOutRow`. Toast success.

#### TC-CON-009: Sort by column [P2] [regression]
- **Pre**: Multiple posts
- **Steps**:
  1. Click "Views" column header to sort
- **Expected**: Posts sorted by view count. Sort indicator visible.

#### TC-CON-010: Group by status [P2] [regression]
- **Pre**: Posts with different statuses
- **Steps**:
  1. Enable "Status" grouping
- **Expected**: Rows grouped by status. Group headers show translated status via `groupValueFormatter`. Aggregated cells show count.

#### TC-CON-011: Group by category [P2] [regression]
- **Pre**: Posts in different categories
- **Steps**:
  1. Enable "Category" grouping
- **Expected**: Rows grouped by category name.

### Bulk Operations

#### TC-CON-012: Bulk publish draft posts [P1] [regression]
- **Pre**: Has BlogPostsPublish permission; draft posts exist
- **Steps**:
  1. Select 3 draft posts
  2. BulkActionToolbar shows "Publish (3)"
  3. Click Publish
- **Expected**: Toast success with count. Status changes to Published. Selection cleared.

#### TC-CON-013: Bulk unpublish published posts [P1] [regression]
- **Pre**: Published posts exist
- **Steps**:
  1. Select 2 published posts
  2. Click "Unpublish (2)"
- **Expected**: Toast success. Status reverts to Draft.

#### TC-CON-014: Bulk publish warns when no drafts selected [P2] [edge-case]
- **Pre**: Select only published posts
- **Steps**:
  1. Observe BulkActionToolbar
- **Expected**: "Publish" button NOT shown (selectedDraftCount === 0). Only "Unpublish" and "Delete" visible.

#### TC-CON-015: Bulk delete with confirmation [P1] [regression]
- **Pre**: Has BlogPostsDelete permission; posts selected
- **Steps**:
  1. Select 3 posts
  2. Click "Delete (3)"
  3. Confirmation dialog with destructive styling
  4. Confirm
- **Expected**: Dialog shows count. Posts deleted. Toast with results.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Edge Cases

#### TC-CON-016: Empty state when no posts [P2] [edge-case]
- **Pre**: No blog posts
- **Steps**:
  1. Navigate to blog posts page
- **Expected**: EmptyState with FileText icon, "No posts found", "New Post" action button.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CON-017: Pagination with defaultPageSize 10 [P2] [regression]
- **Pre**: More than 10 posts
- **Steps**:
  1. Verify page 1 shows 10 items
  2. Navigate to page 2
- **Expected**: Pagination works. `defaultPageSize` is 10 (unlike other pages at 20).

---

## Page: Blog Post Edit (`/portal/blog/posts/:id/edit` or `/portal/blog/posts/new`)

### Happy Path

#### TC-CON-018: Create new blog post with all fields [P0] [smoke]
- **Pre**: Categories and tags exist
- **Steps**:
  1. Navigate to `/portal/blog/posts/new`
  2. Fill title: "Test Post"
  3. Verify slug auto-generated: "test-post"
  4. Write content in Tiptap RichTextEditor
  5. Select category from dropdown
  6. Click tags to toggle selection (Badge click)
  7. Upload featured image via drag zone
  8. Fill SEO fields (meta title, meta description)
  9. Select "Save as draft" publish option
  10. Click Save
- **Expected**: Post created. Toast "Post created". Navigates back to `/portal/blog/posts`.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Slug matches title | ☐ Content HTML saved | ☐ Category assigned | ☐ Tags assigned | ☐ Featured image uploaded

#### TC-CON-019: Tiptap RichTextEditor — text formatting [P0] [smoke]
- **Pre**: On new post page
- **Steps**:
  1. Click in editor area
  2. Type text
  3. Select text, apply bold, italic, heading
  4. Add a link
  5. Create a bullet list
  6. Insert a code block
- **Expected**: RichTextEditor preset="full" with toolbar. All formatting applied. Content saved as HTML (`contentHtml`). Editor height 500px.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CON-020: Tiptap RichTextEditor — image upload [P0] [smoke]
- **Pre**: On post edit page
- **Steps**:
  1. In editor, use image upload
  2. Select an image file
- **Expected**: Image uploaded via `onImageUpload` handler (POST /api/media/upload?folder=blog). Image appears inline in editor content.
- **Data**: ☐ Image URL returned and embedded in HTML

#### TC-CON-021: Auto-generate slug from title (new post only) [P1] [regression]
- **Pre**: On new post page
- **Steps**:
  1. Type title "My Test Post!"
  2. Observe slug field
- **Expected**: Slug auto-generates: "my-test-post" (lowercase, special chars removed, spaces to hyphens). Slug does NOT auto-update on edit mode.

#### TC-CON-022: Edit existing post — pre-fill all fields [P1] [regression]
- **Pre**: Published post with all fields populated
- **Steps**:
  1. Navigate to `/portal/blog/posts/<id>/edit`
  2. Verify all fields pre-filled
- **Expected**: Title, slug, excerpt, content (in Tiptap), category, tags (selected badges), featured image (preview shown), SEO fields, publish option set to "Publish now" (since status is Published).
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CON-023: Publishing options — draft/publish/schedule [P1] [smoke]
- **Pre**: On post edit page
- **Steps**:
  1. Observe 3 radio options: "Save as draft", "Publish now", "Schedule"
  2. Select "Schedule"
  3. DatePicker and TimePicker appear
  4. Select future date and time
  5. Save
- **Expected**: Post saved with Scheduled status. Toast shows scheduled date. DatePicker uses `formatDate` from `useRegionalSettings`. TimePicker interval is 30 min.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CON-024: Schedule validation — date required [P1] [edge-case]
- **Pre**: Select "Schedule" option without picking a date
- **Steps**:
  1. Click Save
- **Expected**: Error message "Please select a date" shown below date picker. Save blocked.

#### TC-CON-025: Schedule validation — must be future [P1] [edge-case]
- **Pre**: Select "Schedule" option
- **Steps**:
  1. Pick a past date
  2. Click Save
- **Expected**: Error "Schedule must be in the future" shown. Save blocked.

#### TC-CON-026: Unpublish a published post by switching to draft [P2] [regression]
- **Pre**: Published post exists
- **Steps**:
  1. Edit the post
  2. Change publish option from "Publish now" to "Save as draft"
  3. Save
- **Expected**: Post unpublished. Toast "Saved as draft". Status badge in header changes to Draft (gray).

#### TC-CON-027: Featured image upload [P1] [regression]
- **Pre**: On post edit page
- **Steps**:
  1. Click dashed upload zone
  2. Select image file
  3. Wait for upload
- **Expected**: Upload spinner shown during processing. After upload, image preview displayed with X button to clear. "Replace Image" button appears below.
- **Data**: ☐ featuredImageId and featuredImageUrl populated in form

#### TC-CON-028: Featured image — non-image file rejected [P2] [edge-case]
- **Pre**: On post edit page
- **Steps**:
  1. Try uploading a .txt file
- **Expected**: Toast error "Select an image file". No upload.

#### TC-CON-029: Featured image — file too large (>10MB) [P2] [edge-case]
- **Pre**: On post edit page
- **Steps**:
  1. Try uploading a 15MB image
- **Expected**: Toast error "Image too large". No upload.

#### TC-CON-030: Clear featured image [P2] [regression]
- **Pre**: Post has featured image
- **Steps**:
  1. Click X button on image preview
- **Expected**: Image cleared. Upload zone reappears. Alt text cleared.

#### TC-CON-031: SEO fields with character counters [P2] [regression]
- **Pre**: On post edit page
- **Steps**:
  1. Type in Meta Title field (max 60 chars)
  2. Observe character counter
  3. Type in Meta Description (max 160 chars)
  4. Observe counter
- **Expected**: Counters show "X/60" and "X/160" format. Validation prevents exceeding limits.

#### TC-CON-032: Canonical URL validation [P2] [edge-case]
- **Pre**: On post edit page
- **Steps**:
  1. Enter invalid URL in Canonical URL field
  2. Blur field
- **Expected**: Validation error for invalid URL format. Empty string allowed.

#### TC-CON-033: Allow Indexing toggle [P3] [regression]
- **Pre**: On post edit page
- **Steps**:
  1. Toggle "Allow Indexing" switch off
  2. Save
- **Expected**: `allowIndexing: false` saved. Switch has cursor-pointer.

#### TC-CON-034: Tag selection via badge click [P1] [regression]
- **Pre**: Tags exist
- **Steps**:
  1. In Organization card, observe tag badges
  2. Click a tag to select (becomes default variant with color)
  3. Click again to deselect (becomes outline variant)
- **Expected**: Tags toggle on/off. Selected tags have background color from `tag.color`. Multiple tags selectable.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CON-035: Category selection with "No category" option [P2] [regression]
- **Pre**: Categories exist
- **Steps**:
  1. Open Category dropdown
  2. Verify "No category" option at top
  3. Select a category
  4. Change to "No category"
- **Expected**: Category assigned/unassigned correctly. Uses `__none__` sentinel value.

#### TC-CON-036: Form validation — title and slug required [P1] [regression]
- **Pre**: On new post page
- **Steps**:
  1. Leave title empty, click Save
- **Expected**: Validation error on Title field "Required". Slug also required.

#### TC-CON-037: Server error handling [P2] [edge-case]
- **Pre**: On post edit page
- **Steps**:
  1. Submit with duplicate slug
- **Expected**: FormErrorBanner at top of form shows server error. Field-specific errors shown inline under slug field via `handleFormError`.

#### TC-CON-038: Entity conflict dialog during edit [P2] [edge-case]
- **Pre**: Post open for edit; form dirty (isDirty=true)
- **Steps**:
  1. Another user edits and saves the same post
- **Expected**: EntityConflictDialog appears (via `useEntityUpdateSignal` with `isDirty`). Options: "Continue Editing" or "Reload".

#### TC-CON-039: Status badge in header for existing posts [P3] [visual]
- **Pre**: Editing a Published post
- **Steps**:
  1. Observe header area
- **Expected**: Badge shows "Published" (green). For Scheduled: shows date. For Draft: gray badge.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

---

## Page: Blog Categories (`/portal/blog/categories`)

### Happy Path

#### TC-CON-040: View categories with tree/table toggle [P1] [smoke]
- **Pre**: Blog categories exist (some with parent-child hierarchy)
- **Steps**:
  1. Navigate to `/portal/blog/categories`
  2. Default view is "Tree"
  3. Toggle to "Table" view via ViewModeToggle
- **Expected**: Tree view shows CategoryTreeView with hierarchy, post counts, edit/delete/add-child actions. Table view shows DataTable with columns: Actions, Name, Slug, Parent, Posts, Children, Sort Order, audit columns. ViewModeToggle shows Tree and List icons.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CON-041: Create category [P1] [smoke]
- **Pre**: On categories page
- **Steps**:
  1. Click "New Category"
  2. URL changes to `?dialog=create-blog-category`
  3. Fill name, slug, description
  4. Optionally set parent category
  5. Save
- **Expected**: Category created. Tree/table refreshes. Toast success.
- **Data**: ☐ Slug correct | ☐ Parent assignment correct

#### TC-CON-042: Create child category from tree [P1] [regression]
- **Pre**: Parent category exists; in tree view
- **Steps**:
  1. Click "Add child" action on a category node
  2. Dialog opens with parentId pre-set
  3. Fill name
  4. Save
- **Expected**: Child category created under parent. Tree updates to show new child.

#### TC-CON-043: Edit category via tree or table [P1] [regression]
- **Pre**: Category exists
- **Steps**:
  1. In tree: click Edit action. In table: click row or actions > Edit
  2. URL changes to `?edit=<categoryId>`
  3. Modify name
  4. Save
- **Expected**: Category updated in both views.

#### TC-CON-044: Delete category with confirmation [P1] [regression]
- **Pre**: Category exists
- **Steps**:
  1. Click Delete action
  2. DeleteBlogCategoryDialog opens
  3. Confirm
- **Expected**: Category deleted. Row fades out (table) or node removed (tree).

#### TC-CON-045: Reorder categories via drag-and-drop (tree view) [P2] [regression]
- **Pre**: Multiple categories in tree view
- **Steps**:
  1. Drag a category node to a new position
  2. Drop
- **Expected**: `handleReorder` called with new positions. Categories reordered. Mutation fires.

#### TC-CON-046: Search categories [P2] [regression]
- **Pre**: Categories exist
- **Steps**:
  1. Type in search field
- **Expected**: Both tree and table views filter by search term.

#### TC-CON-047: Column toggle hidden in tree view [P3] [visual]
- **Pre**: In tree view mode
- **Steps**:
  1. Observe DataTableToolbar
- **Expected**: `showColumnToggle={false}` when viewMode=tree. Column toggle only shown in table view.

#### TC-CON-048: Empty state for categories [P2] [edge-case]
- **Pre**: No blog categories
- **Steps**:
  1. Navigate to page
- **Expected**: Tree: emptyMessage + "Create" button. Table: EmptyState with FolderTree icon.

---

## Page: Blog Tags (`/portal/blog/tags`)

### Happy Path

#### TC-CON-049: View blog tags list [P1] [smoke]
- **Pre**: Tags exist with colors and post counts
- **Steps**:
  1. Navigate to `/portal/blog/tags`
- **Expected**: DataTable with columns: Actions, Name (with ColorPopover dot + name), Slug, Posts (Badge count), audit columns. All items loaded client-side (pageSize 1000). No pagination component visible.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Post counts match actual tagged posts

#### TC-CON-050: Color popover on tag name [P2] [visual]
- **Pre**: Tag with color set
- **Steps**:
  1. Observe Name column — colored circle via ColorPopover
  2. Tags without color show muted circle
- **Expected**: ColorPopover renders correct color. No-color tags show default gray circle.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CON-051: Create blog tag [P1] [smoke]
- **Pre**: On tags page
- **Steps**:
  1. Click "New Tag"
  2. URL changes to `?dialog=create-tag`
  3. Fill name, slug, color
  4. Save
- **Expected**: Tag created. Table refreshes. Toast success.

#### TC-CON-052: Edit blog tag via row click [P1] [regression]
- **Pre**: Tag exists
- **Steps**:
  1. Click on tag row
  2. URL changes to `?edit=<tagId>`
  3. Edit dialog opens with pre-filled data
  4. Modify color
  5. Save
- **Expected**: Tag updated. ColorPopover in table reflects new color.

#### TC-CON-053: Delete blog tag with confirmation [P1] [regression]
- **Pre**: Tag exists
- **Steps**:
  1. Actions > "Delete"
  2. DeleteBlogTagDialog opens
  3. Confirm
- **Expected**: Tag deleted. Row fades out. Toast success.

#### TC-CON-054: Search tags [P2] [regression]
- **Pre**: Tags exist
- **Steps**:
  1. Type in search field
- **Expected**: Table filters client-side by tag name.

#### TC-CON-055: Empty state for tags [P3] [visual]
- **Pre**: No tags
- **Steps**:
  1. Navigate to page
- **Expected**: EmptyState with Tag icon, "No tags found", "New Tag" action.

#### TC-CON-056: Client-side sorting [P2] [regression]
- **Pre**: Multiple tags
- **Steps**:
  1. Click "Posts" column header
- **Expected**: Sorts by post count client-side (`manualSorting: false`).

---

## Page: Media Library (`/portal/media`)

### Happy Path

#### TC-CON-057: View media library in grid mode (default) [P1] [smoke]
- **Pre**: Media files uploaded
- **Steps**:
  1. Navigate to `/portal/media`
  2. Default view is Grid
- **Expected**: Grid of thumbnails (2 cols mobile, 3-6 cols desktop). Stats show "Showing X-Y of Z items". ViewModeToggle with Grid/List options. Upload button in header.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CON-058: Switch to list view [P1] [regression]
- **Pre**: Media files exist
- **Steps**:
  1. Click "List" in ViewModeToggle
- **Expected**: DataTable appears with columns: Actions, Select, Preview (FilePreviewTrigger + Copy URL), Name (+ dimensions), Folder, Type, Size, audit columns. DataTableToolbar with search and filters.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CON-059: Upload media file via dialog [P0] [smoke]
- **Pre**: On media page
- **Steps**:
  1. Click "Upload" button
  2. URL changes to `?dialog=upload-media`
  3. MediaUploadDialog opens
  4. Select image file
  5. Upload completes
- **Expected**: File uploaded. Grid/list refreshes with new file.
- **Data**: ☐ File appears in list | ☐ Thumbnail generated | ☐ File size correct

#### TC-CON-060: FilePreviewTrigger in list view [P0] [smoke]
- **Pre**: Image file in list view
- **Steps**:
  1. Observe Preview column — 40x40 thumbnail
  2. Hover over thumbnail — popover preview
  3. Click thumbnail — lightbox opens
- **Expected**: FilePreviewTrigger renders thumbnail. Hover shows preview popover. Click opens full-size lightbox with file name.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CON-061: Copy file URL in list view [P2] [regression]
- **Pre**: File in list view
- **Steps**:
  1. Click Copy button (Copy icon) next to thumbnail
- **Expected**: URL copied to clipboard. Toast "URL copied to clipboard".

#### TC-CON-062: Open file detail sheet [P1] [regression]
- **Pre**: File exists
- **Steps**:
  1. Click on file card (grid) or row (list)
  2. MediaDetailSheet opens from right
- **Expected**: Sheet shows: file preview, file name (editable), dimensions, format, size, folder, upload date. Actions: rename, delete.

#### TC-CON-063: Rename file via detail sheet [P2] [regression]
- **Pre**: Detail sheet open for a file
- **Steps**:
  1. Edit file name in sheet
  2. Save/confirm rename
- **Expected**: File renamed. List/grid updates.

#### TC-CON-064: Delete single file with confirmation [P1] [regression]
- **Pre**: File exists
- **Steps**:
  1. In list: Actions > Delete. In grid: from detail sheet
  2. DeleteMediaDialog opens
  3. Confirm
- **Expected**: File deleted. Row fades out (list) or card removed (grid).

#### TC-CON-065: Search media files [P1] [regression]
- **Pre**: Multiple files exist
- **Steps**:
  1. Type file name in search
- **Expected**: Results filter. Uses `useDeferredValue` for smooth UX. Page resets to 1.

#### TC-CON-066: Filter by file type [P2] [regression]
- **Pre**: Files of different types (image, document, etc.)
- **Steps**:
  1. Select file type from filter
- **Expected**: Only matching files shown. Page resets to 1.

#### TC-CON-067: Filter by folder [P2] [regression]
- **Pre**: Files in different folders (blog, products, etc.)
- **Steps**:
  1. Select folder from filter
- **Expected**: Only files in selected folder shown.

#### TC-CON-068: Sort media files [P2] [regression]
- **Pre**: Multiple files
- **Steps**:
  1. Change sort to "Name" ascending
- **Expected**: Files reorder by name. Default sort is createdAt desc.

### Bulk Operations

#### TC-CON-069: Select files in grid view [P1] [regression]
- **Pre**: Grid view; files exist
- **Steps**:
  1. Click checkbox on file cards
  2. BulkActionToolbar appears with count
- **Expected**: Selected files have visual indicator. Toolbar shows "Delete (N)" button.

#### TC-CON-070: Select files in list view [P1] [regression]
- **Pre**: List view
- **Steps**:
  1. Click row checkboxes
  2. BulkActionToolbar shows
- **Expected**: Row selection works. Count matches.

#### TC-CON-071: Bulk delete files with confirmation [P1] [regression]
- **Pre**: Multiple files selected
- **Steps**:
  1. Click "Delete (N)" in BulkActionToolbar
  2. Bulk DeleteMediaDialog shows file names
  3. Confirm
- **Expected**: Files deleted. Selection cleared. Grid/list refreshes.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Pagination

#### TC-CON-072: Grid pagination [P1] [regression]
- **Pre**: More than 24 files (DEFAULT_PAGE_SIZE)
- **Steps**:
  1. Observe Pagination component at bottom
  2. Navigate to page 2
- **Expected**: Grid shows next 24 files. Pagination shows page numbers. `showPageSizeSelector=false` in grid mode.

#### TC-CON-073: List pagination [P2] [regression]
- **Pre**: More than 24 files; list view
- **Steps**:
  1. Observe DataTablePagination
- **Expected**: DataTablePagination component with page size selector.

### Edge Cases

#### TC-CON-074: Empty state when no files [P2] [edge-case]
- **Pre**: No media files
- **Steps**:
  1. Navigate to media page
- **Expected**: EmptyState with ImageIcon, "No media files found", "Upload" action. Shown in both grid and list views.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CON-075: Loading skeleton in grid view [P3] [visual]
- **Pre**: Slow network / loading state
- **Steps**:
  1. Navigate to media page while loading
- **Expected**: Grid shows 24 skeleton cards (aspect-square + text lines).

#### TC-CON-076: Real-time updates via SignalR [P2] [edge-case]
- **Pre**: Media library open; another user uploads a file
- **Steps**:
  1. Observe page
- **Expected**: Page auto-refreshes via `useEntityUpdateSignal` (only when no rows selected).

### i18n & Visual

#### TC-CON-077: Vietnamese locale on media page [P2] [i18n]
- **Pre**: UI set to Vietnamese
- **Steps**:
  1. Navigate to media page
  2. Verify all labels translated
- **Expected**: "Thu vien tai nguyen" header, filter labels, empty state text all in Vietnamese.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CON-078: Dark mode — all content pages [P3] [dark-mode]
- **Pre**: Dark mode enabled
- **Steps**:
  1. Visit blog posts, categories, tags, media pages
- **Expected**: All cards have correct shadow. Badges readable. FilePreviewTrigger thumbnails visible. Skeleton dark variant.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CON-079: Responsive — blog post edit page at 768px [P2] [responsive]
- **Pre**: Blog post edit page
- **Steps**:
  1. Resize to 768px width
- **Expected**: Layout changes from 3-column (2+1) to single column. Sidebar cards stack below main content. Editor and all form fields remain functional.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

### Security

#### TC-CON-080: No publish/unpublish buttons without BlogPostsPublish [P1] [security]
- **Pre**: User without BlogPostsPublish permission; has BlogPostsRead
- **Steps**:
  1. Select draft posts via checkboxes
  2. Observe BulkActionToolbar
- **Expected**: "Publish" and "Unpublish" bulk buttons NOT rendered. Only "Delete" if has delete permission.

#### TC-CON-081: No delete actions without permission [P1] [security]
- **Pre**: User without BlogPostsDelete/PromotionsDelete
- **Steps**:
  1. Click actions menu on blog post
- **Expected**: "Delete" option NOT shown in dropdown. Bulk delete button NOT rendered.

#### TC-CON-082: URL dialog param preserved on page load [P3] [regression]
- **Pre**: Direct URL with dialog param
- **Steps**:
  1. Navigate to `/portal/blog/tags?dialog=create-tag`
- **Expected**: Page loads and create dialog auto-opens from URL state.

# Image Preview in Lists Convention

All list/grid views that display images (thumbnails, featured images, product photos) MUST support click-to-preview behavior.

## Pattern

### Table/List views → Use `FilePreviewTrigger`
```tsx
import { FilePreviewTrigger } from '@uikit'

<FilePreviewTrigger
  file={{ url: item.imageUrl, name: item.name, thumbnailUrl: item.thumbnailUrl }}
  thumbnailWidth={48}
  thumbnailHeight={48}
/>
```

`FilePreviewTrigger` provides: thumbnail display, hover popover preview, click-to-open lightbox.

### Card/Grid views → Use `FilePreviewModal` with click handler
```tsx
import { FilePreviewModal } from '@uikit'

const [previewOpen, setPreviewOpen] = useState(false)

<div onClick={() => { if (item.imageUrl) setPreviewOpen(true) }} className={item.imageUrl ? 'cursor-pointer' : ''}>
  <img src={item.imageUrl} alt={item.name} />
</div>

{item.imageUrl && (
  <FilePreviewModal
    open={previewOpen}
    onOpenChange={setPreviewOpen}
    files={[{ url: item.imageUrl, name: item.name }]}
  />
)}
```

## Reference implementations
- **Table**: `BlogPostsPage.tsx` — `FilePreviewTrigger` for featured image
- **Grid**: `EnhancedProductCard.tsx` — `FilePreviewModal` for product image

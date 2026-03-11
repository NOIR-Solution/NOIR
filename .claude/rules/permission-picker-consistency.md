# Permission Picker UI Consistency

## Rule

ALL permission selection UIs MUST use the shared `PermissionPicker` component from `src/components/PermissionPicker.tsx`.

**Never** build a custom permission picker with raw permission strings. The shared component provides:
- Category grouping with collapsible sections and icons
- Translated display names and descriptions (via `permissionTranslation.ts`)
- Search, Select All/Clear All, category-level toggle
- Indeterminate checkbox state for partially-selected categories

## Usage

```tsx
import { PermissionPicker } from '@/components/PermissionPicker'

// Role dialog — shows all permissions
<PermissionPicker
  selectedPermissions={selectedSet}
  onPermissionsChange={setSelectedSet}
/>

// API Key dialog — only show user's own permissions
<PermissionPicker
  selectedPermissions={selectedSet}
  onPermissionsChange={handleChange}
  allowedPermissions={new Set(userPermissions)}
/>
```

## When Adding New Permissions

1. Add permission constant to `Domain/Common/Permissions.cs`
2. Add metadata (displayName, description, category) to `PermissionDtoFactory._metadata`
3. Add category to `permissionTranslation.ts` CATEGORY_SORT_ORDER and CATEGORY_ICONS if new
4. Add i18n category key to both EN and VI `permissions.categories.*` (key = `category.toLowerCase().replace(/[^a-z0-9]/g, '')`)

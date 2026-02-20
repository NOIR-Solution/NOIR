# Localization Check Rules

## MANDATORY: After Any Frontend Code Change

When implementing or modifying frontend code in `src/NOIR.Web/frontend/src/`, verify localization completeness.

---

## Checklist

### 1. No Hardcoded User-Facing Strings
All visible text must use `t('namespace.key')` from `react-i18next`:
labels, placeholders, button text, error messages, tooltips, titles, descriptions.

```tsx
// BAD
<Input placeholder="Search products..." />
<Button>Save</Button>

// GOOD
<Input placeholder={t('products.searchPlaceholder')} />
<Button>{t('buttons.save')}</Button>
```

### 2. Both EN and VI Must Have the Same Keys
For every key added to `public/locales/en/*.json`, add the same key to `public/locales/vi/*.json`.

### 3. Backend-Driven Display Text (CRITICAL)

Some UI text comes from backend entities and bypasses `t()`. These MUST have localized equivalents in the translation files:

| Backend Source | Display Fields | Locale Key |
|---------------|---------------|------------|
| Permission list (Roles, Users) | `displayName`, `description`, category headers | `common.permissions.categories.*` |
| Role names | `name`, `description` | `common.roles.*` |
| Status badges | Enum display values (OrderStatus, ProductStatus) | `common.statuses.*` |
| Error codes | Backend error messages | `errors.*` |

**Pattern:** When permission categories, role names, or enum values are displayed from API data, map them through translation keys rather than rendering raw English strings.

### 4. Localization File Locations
```
src/NOIR.Web/frontend/public/locales/
├── en/   (auth.json, common.json, errors.json, nav.json)
└── vi/   (auth.json, common.json, errors.json, nav.json)
```

### 5. Namespace Conventions
| Namespace | Use For |
|-----------|---------|
| `auth.*` | Login, logout, password reset, profile |
| `nav.*` | Navigation menu, sidebar |
| `buttons.*` | Common button labels |
| `labels.*` | Common field labels |
| `errors.*` | Error messages, error codes |
| `validation.*` | Form validation messages |
| `permissions.*` | Permission categories, display names |
| `products.*` | Product management |
| `categories.*` | Category management |
| `users.*` | User management |
| `roles.*` | Role management |
| `tenants.*` | Tenant management |
| `commandPalette.*` | Command palette |
| `activityTimeline.*` | Activity timeline |
| `developerLogs.*` | Developer logs |

---

## Common Patterns Requiring Localization

| Context | Key Pattern | Example |
|---------|-------------|---------|
| Search inputs | `{namespace}.searchPlaceholder` | `products.searchPlaceholder` |
| Filter dropdowns | `{namespace}.filterBy{Field}` | `users.filterByRole` |
| Empty states | `{namespace}.no{Items}Found` | `products.noProductsFound` |
| Create dialogs | `{namespace}.create{Item}` | `roles.createRole` |
| Delete confirmations | `{namespace}.deleteConfirmation` | `products.deleteConfirmation` |
| Permission categories | `permissions.categories.{name}` | `permissions.categories.usermanagement` |
| Status badges | `statuses.{enumValue}` | `statuses.active` |

---

## When Adding New Features

1. Plan localization keys before implementation
2. Add keys to both EN and VI simultaneously
3. Use descriptive key names that match the UI context
4. For backend-driven text, add translation mapping in the component


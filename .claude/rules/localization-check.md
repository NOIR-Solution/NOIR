# Localization Check Rules

## MANDATORY: After Any Frontend Code Change

When implementing or modifying frontend code (React/TypeScript files in `src/NOIR.Web/frontend/src/`), you MUST verify localization completeness.

---

## Checklist

### 1. No Hardcoded User-Facing Strings
- **All visible text** must use `t('namespace.key')` from `react-i18next`
- This includes: labels, placeholders, button text, error messages, tooltips, titles, descriptions

**BAD:**
```tsx
<Input placeholder="Search products..." />
<Button>Save</Button>
```

**GOOD:**
```tsx
<Input placeholder={t('products.searchPlaceholder')} />
<Button>{t('buttons.save')}</Button>
```

### 2. Both EN and VI Must Have the Same Keys
- For every key added to `public/locales/en/*.json`, add the same key to `public/locales/vi/*.json`
- Keys must be identical in structure (nested objects must match)

### 3. Localization File Locations
```
src/NOIR.Web/frontend/public/locales/
├── en/
│   ├── auth.json      # Authentication-related
│   ├── common.json    # General UI, navigation, buttons, labels
│   ├── errors.json    # Error codes and messages
│   └── nav.json       # Navigation menu items
└── vi/
    ├── auth.json
    ├── common.json
    ├── errors.json
    └── nav.json
```

### 4. Namespace Conventions
| Namespace | Use For |
|-----------|---------|
| `auth.*` | Login, logout, password reset, profile |
| `nav.*` | Navigation menu, sidebar |
| `buttons.*` | Common button labels |
| `labels.*` | Common field labels |
| `errors.*` | Error messages |
| `validation.*` | Form validation messages |
| `products.*` | Product management |
| `categories.*` | Category management |
| `users.*` | User management |
| `roles.*` | Role management |
| `tenants.*` | Tenant management |
| `commandPalette.*` | Command palette |
| `activityTimeline.*` | Activity timeline |
| `developerLogs.*` | Developer logs |

---

## Verification Steps

After making frontend changes:

1. **Search for hardcoded strings:**
   ```bash
   grep -r "placeholder=\"[A-Z]" src/NOIR.Web/frontend/src/portal-app/ src/NOIR.Web/frontend/src/layouts/
   grep -r ">[A-Z][a-z].*</" src/NOIR.Web/frontend/src/portal-app/ src/NOIR.Web/frontend/src/layouts/
   ```

2. **Compare EN and VI keys:**
   - Open both `en/common.json` and `vi/common.json`
   - Ensure all keys exist in both files

3. **Test language switching:**
   - Run the frontend
   - Switch between English and Vietnamese
   - Verify no untranslated text appears

---

## Common Placeholders Requiring Localization

| Context | Key Pattern | Example |
|---------|-------------|---------|
| Search inputs | `{namespace}.searchPlaceholder` | `products.searchPlaceholder` |
| Filter dropdowns | `{namespace}.filterBy{Field}` | `users.filterByRole` |
| Empty states | `{namespace}.no{Items}Found` | `products.noProductsFound` |
| Create dialogs | `{namespace}.create{Item}` | `roles.createRole` |
| Delete confirmations | `{namespace}.deleteConfirmation` | `products.deleteConfirmation` |

---

## When Adding New Features

1. **Plan localization keys** before implementation
2. **Add keys to both EN and VI** simultaneously
3. **Use descriptive key names** that match the UI context
4. **Group related keys** under the same namespace

---

**Last Updated:** 2026-02-13

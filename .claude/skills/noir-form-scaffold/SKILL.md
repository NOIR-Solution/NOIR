---
name: noir-form-scaffold
description: Scaffold a React form in NOIR following the 4-rule validation standard (useValidatedForm + Zod schema + FormErrorBanner + auto-detected required fields). Use when the user asks to create, add, or build a new form, dialog form, settings form, or edit form on the frontend. Prevents the common mistakes covered in `.claude/rules/form-validation-standard.md`.
---

# noir-form-scaffold — Forms that pass the 4-rule standard

NOIR has a mandatory form validation standard. Forms built without it produce bugs like: errors shown while user is typing, required asterisks missing, server errors shown as toasts instead of inline, or errors persisting after dialog re-open. This skill walks the canonical pattern.

## Inputs to collect

Before writing code, confirm:

1. **Form location**: dialog (`CredenzaBody`) vs page (`src/portal-app/...FormPage.tsx`) vs settings tab (`...SettingsTab.tsx`)
2. **Entity name** (PascalCase, singular): `Brand`, `Promotion`, `Employee`
3. **Operation**: create-only, edit-only, or combined (same component handles both)
4. **Backend contract**: the Zod schema — is it auto-generated from FluentValidation via `pnpm run generate:api`, or hand-written?
5. **Special fields**: rich text (RichTextEditor), file upload, date picker, color picker, multi-select (Combobox), tags

If the backend command/DTO doesn't exist yet, STOP and invoke `noir-feature-add` first.

## Step 1 — Generate / locate the Zod schema

If backend has FluentValidation:
```bash
cd src/NOIR.Web/frontend && pnpm run generate:api
```
Look for the schema at `src/validation/generated/{Entity}{Operation}Schema.ts`.

If hand-writing: follow the pattern in `src/validation/` — use `z.string().min(1, t('validation.required'))` etc. For i18n:
```ts
import { z } from 'zod'
import type { TFunction } from 'i18next'

export const createBrandSchema = (t: TFunction) => z.object({
  name: z.string().min(1, t('validation.required')).max(200, t('validation.maxLength', { max: 200 })),
  slug: z.string().optional(),
  isActive: z.boolean().default(true),
})
export type CreateBrandFormData = z.infer<ReturnType<typeof createBrandSchema>>
```

## Step 2 — Component skeleton (canonical)

Reference implementations (read one before writing yours — Rule 1):
- **Dialog form (gold standard)**: `src/portal-app/catalog/brands/BrandDialog.tsx`
- **Page-level form**: `src/portal-app/catalog/products/ProductFormPage.tsx`
- **Settings tab**: `src/portal-app/settings/tabs/SmtpSettingsTab.tsx`

Skeleton:

```tsx
import { useTranslation } from 'react-i18next'
import { zodResolver } from '@hookform/resolvers/zod'
import type { Resolver } from 'react-hook-form'
import { useValidatedForm } from '@/hooks/useValidatedForm'
import { Form, FormField, FormItem, FormLabel, FormControl, FormMessage } from '@uikit/form'
import { FormErrorBanner, Input, Button, Credenza, CredenzaContent, CredenzaHeader, CredenzaTitle, CredenzaBody, CredenzaFooter } from '@uikit'
import { createBrandSchema, type CreateBrandFormData } from '@/validation/brandSchema'
import { useCreateBrand } from '@/queries/useBrandMutations'
import { toast } from 'sonner'

interface BrandDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  brand?: BrandDto   // undefined = create, defined = edit
}

export const BrandDialog = ({ open, onOpenChange, brand }: BrandDialogProps) => {
  const { t } = useTranslation()
  const createMutation = useCreateBrand()
  const updateMutation = useUpdateBrand()
  const isEdit = !!brand

  const { form, handleSubmit, serverErrors, dismissServerErrors, requiredFields } = useValidatedForm({
    schema: createBrandSchema(t),
    defaultValues: {
      name: brand?.name ?? '',
      slug: brand?.slug ?? '',
      isActive: brand?.isActive ?? true,
    },
    onSubmit: async (data) => {
      if (isEdit) {
        await updateMutation.mutateAsync({ id: brand.id, ...data })
        toast.success(t('brands.updateSuccess'))
      } else {
        await createMutation.mutateAsync(data)
        toast.success(t('brands.createSuccess'))
      }
      onOpenChange(false)
    },
  })

  // Reset form + clear server errors when dialog opens
  useEffect(() => {
    if (open) {
      form.reset({
        name: brand?.name ?? '',
        slug: brand?.slug ?? '',
        isActive: brand?.isActive ?? true,
      })
      dismissServerErrors()
    }
  }, [open, brand])

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent>
        <CredenzaHeader>
          <CredenzaTitle>{isEdit ? t('brands.editBrand') : t('brands.createBrand')}</CredenzaTitle>
        </CredenzaHeader>
        <Form {...form} requiredFields={requiredFields}>
          <form onSubmit={handleSubmit}>
            <CredenzaBody className="space-y-4">
              <FormErrorBanner
                errors={serverErrors}
                onDismiss={dismissServerErrors}
                title={t('validation.unableToSave')}
              />
              <FormField
                control={form.control}
                name="name"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('brands.name')}</FormLabel>
                    <FormControl><Input {...field} /></FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              {/* more fields... */}
            </CredenzaBody>
            <CredenzaFooter>
              <Button variant="outline" className="cursor-pointer" onClick={() => onOpenChange(false)}>
                {t('buttons.cancel')}
              </Button>
              <Button type="submit" className="cursor-pointer" disabled={form.formState.isSubmitting}>
                {isEdit ? t('buttons.save') : t('buttons.create')}
              </Button>
            </CredenzaFooter>
          </form>
        </Form>
      </CredenzaContent>
    </Credenza>
  )
}
```

## Step 3 — The 4 non-negotiable rules (from `form-validation-standard.md`)

1. **Required field asterisks** — `requiredFields={requiredFields}` on `<Form>`. Never hardcode `required` on labels.
2. **Validation timing** — `useValidatedForm` sets `mode: 'onBlur'` + `reValidateMode: 'onChange'`. Never override to `'onBlur'`.
3. **Server errors** — `FormErrorBanner` as first child of `CredenzaBody`/form body. Never `toast.error()` for form submit failures. `handleFormError` is wired into `useValidatedForm` automatically.
4. **BE ↔ FE sync** — schema mirrors FluentValidation. Regenerate after backend changes: `pnpm run generate:api`.

## Step 4 — i18n keys

Add to BOTH `en/common.json` and `vi/common.json`:
```json
// en
"brands": {
  "createBrand": "Create Brand",
  "editBrand": "Edit Brand",
  "name": "Name",
  "createSuccess": "Brand created successfully",
  "updateSuccess": "Brand updated successfully"
}

// vi — sentence case, pure Vietnamese (no mixing)
"brands": {
  "createBrand": "Tạo thương hiệu",
  "editBrand": "Chỉnh sửa thương hiệu",
  "name": "Tên",
  "createSuccess": "Đã tạo thương hiệu",
  "updateSuccess": "Đã cập nhật thương hiệu"
}
```

## Step 5 — URL-synced dialog state (if list page)

Wire the dialog open state to URL params (per `url-tab-state.md`):
```tsx
const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-brand' })
const { editItem, openEdit, onEditOpenChange } = useUrlEditDialog<BrandDto>(brands)

<BrandDialog
  open={isCreateOpen || !!editItem}
  onOpenChange={(open) => {
    if (!open) {
      if (isCreateOpen) onCreateOpenChange(false)
      if (editItem) onEditOpenChange(false)
    }
  }}
  brand={editItem}
/>
```

The **conditional close** is critical — calling both setters in the same tick causes the second to overwrite the first.

## Step 6 — Verify

```bash
cd src/NOIR.Web/frontend && pnpm run build    # must pass — strict mode
pnpm test                                      # if form has unit tests
```

Browser check:
- Submit empty form → inline errors under required fields, red asterisks on labels
- Focus a field with an error → error text hides while focused
- Type a valid value and blur → error clears
- Submit with backend 400 (e.g. duplicate slug) → `FormErrorBanner` shows at top; field-specific errors inline
- Reopen dialog → no stale errors

## Common mistakes this skill prevents

- Missing `requiredFields` on `<Form>` → no red asterisks
- Missing `reValidateMode: 'onChange'` → errors persist while user types the fix
- `toast.error()` on submit failure → user loses the message, can't re-read it
- Forgetting `setServerErrors([])` / `dismissServerErrors()` on dialog open → stale errors from previous submit
- `result.error.errors` in Zod → throws; must be `result.error.issues` (Zod v3+)
- `zodResolver(createSchema(t))` without `as unknown as Resolver<FormData>` → type mismatch due to `z.default()`
- Hand-writing `useForm({...})` instead of `useValidatedForm` → drops one of the 4 rules
- Raw `<input>` instead of `<Input>` from `@uikit` (violates `component-based-design.md`)
- VI labels mixing English (e.g. "Tên Brand" instead of "Tên thương hiệu")

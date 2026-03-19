# Form Validation Standard

## 4 Rules Every Form Must Follow

### 1. Required Field Indicator (Auto-Detect from Schema)

FormLabel auto-renders a red `*` for required fields — no manual `required` prop needed.

```tsx
const schema = useMemo(() => createXxxSchema(t), [t])
const requiredFields = useMemo(() => getRequiredFields(schema), [schema])

// Pass to Form wrapper — that's all
<Form {...form} requiredFields={requiredFields}>
```

`getRequiredFields(schema)` from `@/lib/form` introspects Zod: a field is required if it is NOT wrapped in `.optional()`, `.default()`, or `.nullable().optional()`. Fields that are only `.nullable()` are still required.

**Dynamic required fields** (e.g. field B required when dropdown A = "X"): change the schema via `watch()` + `useMemo` depending on watched value → `requiredFields` auto-updates.

### 2. Validation Timing — "Reward Early, Punish Late"

| User action | Error shown? |
|---|---|
| Focus field → leave WITHOUT editing | ❌ No — `isDirty=false` |
| Edit field → leave with invalid value | ✅ Yes — `isDirty=true`, unfocused |
| **Focus field that has an error** | ❌ **Hidden while focused** — `isFocused=true` |
| **Type in focused field (even invalid)** | ❌ **No error while typing** |
| **Blur field with valid value** | ❌ Error gone — valid, re-validated by `onChange` |
| **Blur field with still-invalid value** | ✅ Error re-appears |
| Click Submit | ✅ All errors — `isSubmitted=true` (hidden for whichever field has focus) |
| Server error (`type="server"`) | ✅ Shown when unfocused |

**How it works — two gates in `FormItem` / `FormMessage`:**
1. `hasBeenValidated`: `isDirty || isSubmitted || isServerError` — has the user "earned" seeing this error?
2. `!isFocused`: error is always suppressed while the field has focus

`FormItem` tracks focus via `onFocusCapture`/`onBlurCapture` with a `setTimeout(0)` debounce (handles Radix focus shifts between popover triggers and their content).

**Every `useForm` call MUST have:**
```tsx
useForm({
  mode: 'onBlur',
  reValidateMode: 'onChange',  // ← REQUIRED: clears errors in real-time when user fixes value
})
```

**NEVER use `reValidateMode: 'onBlur'`** — it causes old errors to persist while typing (re-validation only runs on blur), making the UX worse than `onChange`.

`FormMessage` in `Form.tsx` handles both gates automatically. No per-field code needed.

### 3. Server Error Display — FormErrorBanner (NOT `toast.error`)

| Error type | Where to show | How |
|---|---|---|
| Field-specific (email taken, slug exists) | Inline under field | `form.setError('field', { type: 'server', message })` |
| Form-level (business rule violation) | Banner at top of form | `FormErrorBanner` |
| Non-form action failure (delete, bulk) | Toast | `toast.error()` — only acceptable for non-form actions |

**Pattern — every form submit catch block:**
```tsx
import { handleFormError } from '@/lib/form'
import { FormErrorBanner } from '@uikit'

const [serverErrors, setServerErrors] = useState<string[]>([])

// catch block:
catch (err) {
  handleFormError(err, form, setServerErrors, t)
}

// JSX — FIRST child inside form body (CredenzaBody or form container):
<FormErrorBanner
  errors={serverErrors}
  onDismiss={() => setServerErrors([])}
  title={t('validation.unableToSave', 'Unable to save')}
/>
```

**Clear on open:** Add `setServerErrors([])` in the `useEffect` that resets the form when dialog opens.

### 4. BE ↔ FE Validation Sync

- FluentValidation validators auto-generate Zod schemas: `pnpm run generate:api`
- Hand-written Zod schemas MUST mirror FluentValidation (min/max, required, regex)
- `handleFormError` auto-maps `ValidationProblemDetails.Errors` PascalCase → `form.setError()` camelCase
- When modifying FluentValidation → regenerate schemas → verify Zod matches

---

## `useValidatedForm` (Convenience Hook)

For new forms, `useValidatedForm` from `@/hooks/useValidatedForm` bundles all 4 rules:

```tsx
const { form, handleSubmit, serverErrors, dismissServerErrors, requiredFields } = useValidatedForm({
  schema: createXxxSchema(t),
  defaultValues: { ... },
  onSubmit: async (data) => {
    await mutation.mutateAsync(data)
    toast.success(t('xxx.createSuccess'))
    onOpenChange(false)
  },
})

<Form {...form} requiredFields={requiredFields}>
  <form onSubmit={handleSubmit}>
    <FormErrorBanner errors={serverErrors} onDismiss={dismissServerErrors}
      title={t('validation.unableToSave')} />
    ...
  </form>
</Form>
```

---

## Reference Implementations

| Pattern | File |
|---|---|
| Dialog form (canonical) | `BrandDialog.tsx` |
| Page-level form | `ProductFormPage.tsx` |
| Settings inline form | `SmtpSettingsTab.tsx` |

---

## Common Mistakes

| Mistake | Fix |
|---|---|
| `toast.error()` for form submit error | Replace with `handleFormError()` + `FormErrorBanner` |
| Missing `requiredFields` on `<Form>` | Labels won't show asterisk |
| Missing `reValidateMode: 'onChange'` | Errors won't clear when user fixes input |
| `reValidateMode: 'onBlur'` | **DELETE IT** — old errors persist while typing; use `'onChange'` |
| Missing `setServerErrors([])` on dialog open | Stale errors persist across open/close cycles |
| Hardcoded `required` attribute | Remove — auto-detected from schema |
| Error shows while user is typing in focused field | **Bug**: `FormItem`/`FormField` focus tracking handles this — do NOT add custom focus logic |
| Label turns red after focus+blur without typing | **Bug**: `FormLabel` must use the same `hasBeenValidated` gate as `FormMessage` — all three (label, border, message) must stay in sync |

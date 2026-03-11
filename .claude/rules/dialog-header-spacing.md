# Dialog Close Button Convention

## Rule

Dialogs do NOT have a built-in X close button. Users close via:
- **Footer Close/Cancel button** (required on every dialog)
- Click outside (overlay dismiss)
- ESC key

## Every Dialog MUST Have a Footer

```tsx
// View-only dialogs
<CredenzaFooter>
  <Button variant="outline" className="cursor-pointer" onClick={() => onOpenChange(false)}>
    {t('buttons.close', 'Close')}
  </Button>
</CredenzaFooter>

// Form dialogs
<CredenzaFooter>
  <Button variant="outline" className="cursor-pointer" onClick={() => onOpenChange(false)}>
    {t('buttons.cancel', 'Cancel')}
  </Button>
  <Button type="submit" className="cursor-pointer">
    {t('buttons.save', 'Save')}
  </Button>
</CredenzaFooter>
```

## No Native `title=` Tooltips

Use Radix Tooltip (from `@uikit`) instead of HTML `title` attribute:

```tsx
// BAD — native browser tooltip, inconsistent styling
<Button title={t('buttons.copy')}>...</Button>

// GOOD — styled Radix tooltip
<Tooltip>
  <TooltipTrigger asChild>
    <Button aria-label={t('buttons.copy')}>...</Button>
  </TooltipTrigger>
  <TooltipContent side="bottom">{t('buttons.copy')}</TooltipContent>
</Tooltip>
```

## Credenza stableChildrenRef Gotcha

Credenza freezes children when `open` becomes `false` (prevents content flash during close animation). If a `CredenzaTrigger` button needs to update its text when the dialog closes (e.g., filter count), place the button **outside** Credenza:

```tsx
// BAD — button text frozen on close
<Credenza open={open} onOpenChange={setOpen}>
  <CredenzaTrigger asChild>
    <Button>{count} selected</Button>  {/* stale! */}
  </CredenzaTrigger>
  <CredenzaContent>...</CredenzaContent>
</Credenza>

// GOOD — button renders independently
<Button onClick={() => setOpen(true)}>{count} selected</Button>
<Credenza open={open} onOpenChange={setOpen}>
  <CredenzaContent>...</CredenzaContent>
</Credenza>
```

## Bug Reference

- X button overlapped with action buttons in JsonViewer fullscreen dialog (2026-03-11)
- Native `title=` tooltips inconsistent with styled Radix tooltips (2026-03-11)
- Credenza stableChildrenRef froze AttributeFilterDialog button text on Apply (2026-03-11)

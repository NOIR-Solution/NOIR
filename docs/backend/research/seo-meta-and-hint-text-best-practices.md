# SEO Meta Fields & Hint Text Best Practices

> Research Report | January 2026

## Executive Summary

This research covers best practices for SEO meta fields (title, description) and CMS form hint text UX patterns. Key findings include recommended character limits, auto-generation strategies, and guidelines for consistent, accessible hint text.

---

## 1. SEO Meta Title Best Practices

### Character Limits

| Guideline | Recommendation |
|-----------|----------------|
| **Optimal Length** | 50-60 characters |
| **Maximum Display** | ~60 characters (truncated after) |
| **Minimum** | 30 characters |

**Source:** Google doesn't specify a hard limit, but titles are "truncated in Google Search results as needed, typically to fit the device width." Industry consensus is 50-60 characters.

### Best Practices

1. **Unique & Descriptive** - Every page should have a unique, specific title
2. **Front-load Keywords** - Place important terms at the beginning
3. **Avoid Keyword Stuffing** - Don't repeat identical terms
4. **Brand Positioning** - Site name at end, separated by delimiter (` | ` or ` - `)
5. **Match Content** - Title must accurately reflect page content

### Auto-Generation Strategy

When left blank, generate from:
```
{Post Title} | {Site Name}
```

Truncate post title if combined length exceeds 60 characters.

---

## 2. SEO Meta Description Best Practices

### Character Limits

| Guideline | Recommendation |
|-----------|----------------|
| **Optimal Range** | 120-155 characters |
| **Desktop Display** | ~155 characters |
| **Mobile Display** | ~135 characters |
| **Safe Maximum** | 155 characters |

**Source:** Yoast recommends 120-156 characters. Semrush recommends max 135 characters for mobile-first.

### Best Practices

1. **Include Primary Keyword** - Natural placement, enables bold matching in SERPs
2. **Address Search Intent** - Answer the user's likely question
3. **Action-Oriented Language** - Start with verbs ("Learn", "Discover", "Find out")
4. **Unique Per Page** - Never duplicate across pages
5. **Compelling Summary** - Treat as an ad for your content

### Auto-Generation Strategy

When left blank, generate from:
1. **First priority:** Post excerpt (if available)
2. **Second priority:** First ~150 characters of content (strip HTML)
3. **Truncate intelligently:** End at word boundary with ellipsis if needed

**Note:** Google uses provided meta descriptions only ~28% of the time, generating alternatives from page content for the rest.

---

## 3. Hint Text UX Best Practices

### Key Principles (NN/g Research)

| DO | DON'T |
|----|-------|
| Use persistent helper text below fields | Use placeholder as only hint |
| Keep hints visible at all times | Hide important instructions |
| Provide examples and format guidance | Use vague descriptions |
| Ensure sufficient color contrast | Use light gray that fails WCAG |

### Problems with Placeholder-Only Hints

1. **Memory Burden** - Disappears when typing, users forget
2. **No Verification** - Can't review instructions after entering
3. **Accessibility Issues** - Screen readers don't reliably announce placeholders
4. **Confusion with Auto-fill** - Users may skip thinking field is pre-filled

### Recommended Pattern

```
┌─────────────────────────────────────┐
│ Label                               │
├─────────────────────────────────────┤
│ [Placeholder: example or format]    │
├─────────────────────────────────────┤
│ Helper text always visible below    │
│ Character count: 0/60               │
└─────────────────────────────────────┘
```

### Hint Text Content Guidelines

| Field Type | Hint Should Include |
|------------|---------------------|
| **Character-limited** | Current count + max (e.g., "0/60 characters") |
| **Optional with default** | What happens if empty (e.g., "Leave empty to auto-generate from title") |
| **Format-specific** | Example format (e.g., "https://example.com/page") |
| **Toggle/Switch** | What enabling/disabling does |

---

## 4. Recommendations for NOIR

### Current State Analysis

The current SEO section in PostEditorPage.tsx uses:
- Basic placeholder text ("SEO title", "SEO description")
- Character counters ("0/60 characters")
- Simple helper text for optional fields

### Recommended Improvements

#### 4.1 Enhanced Hint Text Pattern

```tsx
// Meta Title
<FormDescription className="space-y-1">
  <span className="block">
    {field.value?.length || 0}/60 characters
  </span>
  <span className="block text-muted-foreground/70">
    Leave empty to use post title
  </span>
</FormDescription>

// Meta Description
<FormDescription className="space-y-1">
  <span className="block">
    {field.value?.length || 0}/160 characters
  </span>
  <span className="block text-muted-foreground/70">
    Leave empty to use excerpt
  </span>
</FormDescription>
```

#### 4.2 Consistent Hint Text Format System-Wide

| Scenario | Format |
|----------|--------|
| Character limit | `{current}/{max} characters` |
| Auto-default | `Leave empty to {action}` |
| Format example | `Format: {example}` |
| Optional field | `Optional. {what it does}` |
| Toggle explanation | `{What happens when enabled}` |

#### 4.3 Auto-Generation Logic (Backend)

```csharp
// In CreatePostCommandHandler or UpdatePostCommandHandler
public string GenerateMetaTitle(string? metaTitle, string postTitle, string siteName)
{
    if (!string.IsNullOrWhiteSpace(metaTitle))
        return metaTitle;

    var maxTitleLength = 60 - siteName.Length - 3; // " | " separator
    var truncatedTitle = postTitle.Length > maxTitleLength
        ? postTitle[..maxTitleLength].TrimEnd() + "…"
        : postTitle;

    return $"{truncatedTitle} | {siteName}";
}

public string GenerateMetaDescription(string? metaDescription, string? excerpt, string? content)
{
    if (!string.IsNullOrWhiteSpace(metaDescription))
        return metaDescription;

    if (!string.IsNullOrWhiteSpace(excerpt))
        return TruncateToWords(excerpt, 155);

    if (!string.IsNullOrWhiteSpace(content))
    {
        var plainText = StripHtml(content);
        return TruncateToWords(plainText, 155);
    }

    return string.Empty;
}
```

---

## 5. Character Limit Summary

| Field | Min | Optimal | Max | Display Truncation |
|-------|-----|---------|-----|-------------------|
| **Meta Title** | 30 | 50-60 | 60 | ~60 chars |
| **Meta Description** | 70 | 120-155 | 160 | ~155 desktop, ~135 mobile |

---

---

## 6. Visual Styling Specifications (CSS/UI/UX)

### Typography

| Property | Value | Tailwind Class |
|----------|-------|----------------|
| **Font Size** | 0.875rem (14px) | `text-sm` |
| **Line Height** | 1.43 (20px) | (inherent to `text-sm`) |
| **Font Weight** | 400 (Regular) | `font-normal` |
| **Letter Spacing** | Normal | - |

### Colors (NOIR Design System)

| State | Light Mode | Dark Mode | Tailwind |
|-------|------------|-----------|----------|
| **Default Hint** | `oklch(0.556 0 0)` ≈ #71717a (gray-500) | `oklch(0.708 0 0)` ≈ #a1a1aa (gray-400) | `text-muted-foreground` |
| **Secondary Hint** | 70% opacity of above | 70% opacity of above | `text-muted-foreground/70` |
| **Character Counter** | Same as default | Same as default | `text-muted-foreground` |
| **Warning (near limit)** | `oklch(0.75 0.15 70)` ≈ amber-500 | Same | `text-amber-500` |
| **Error** | `oklch(0.577 0.245 27)` ≈ red-500 | `oklch(0.704 0.191 22)` | `text-destructive` |
| **Success (optimal)** | `oklch(0.60 0.15 145)` ≈ green-600 | Same | `text-green-600` |

### Spacing

| Property | Value | Tailwind |
|----------|-------|----------|
| **Top Margin** (below input) | 0.375rem (6px) | `mt-1.5` |
| **Between hint lines** | 0.25rem (4px) | `space-y-1` |
| **Icon to text** | 0.25rem (4px) | `gap-1` |

### Visual Hierarchy

```
┌─────────────────────────────────────────────────────┐
│ Label                               text-sm font-medium
├─────────────────────────────────────────────────────┤
│                                                     │
│  Input Field                        text-base       │
│                                                     │
├─────────────────────────────────────────────────────┤
│ Primary hint (char count)           text-sm muted   │  ← mt-1.5
│ Secondary hint (auto-gen info)      text-sm muted/70│  ← space-y-1
└─────────────────────────────────────────────────────┘
```

### Character Counter Styling

Visual feedback based on character count:

```tsx
// Utility function for character counter color
const getCharCountColor = (current: number, optimal: number, max: number) => {
  if (current === 0) return 'text-muted-foreground'      // Empty - neutral
  if (current < optimal * 0.5) return 'text-amber-500'   // Too short - warning
  if (current <= optimal) return 'text-green-600'        // Optimal - success
  if (current <= max) return 'text-amber-500'            // Near limit - warning
  return 'text-destructive'                               // Over limit - error
}

// Usage for Meta Title (optimal: 50-60, max: 60)
<span className={getCharCountColor(length, 50, 60)}>
  {length}/60 characters
</span>

// Usage for Meta Description (optimal: 120-155, max: 160)
<span className={getCharCountColor(length, 120, 160)}>
  {length}/160 characters
</span>
```

### Recommended Component Pattern

```tsx
// FormHint component with consistent styling
interface FormHintProps {
  charCount?: { current: number; optimal: number; max: number }
  autoGenHint?: string
}

const FormHint = ({ charCount, autoGenHint }: FormHintProps) => (
  <FormDescription className="space-y-1">
    {charCount && (
      <span className={cn(
        "block tabular-nums",
        getCharCountColor(charCount.current, charCount.optimal, charCount.max)
      )}>
        {charCount.current}/{charCount.max} characters
      </span>
    )}
    {autoGenHint && (
      <span className="block text-muted-foreground/70">
        {autoGenHint}
      </span>
    )}
  </FormDescription>
)

// Usage
<FormHint
  charCount={{ current: field.value?.length || 0, optimal: 50, max: 60 }}
  autoGenHint="Leave empty to use post title"
/>
```

### Accessibility Requirements

| Requirement | Implementation |
|-------------|----------------|
| **Color Contrast** | Minimum 4.5:1 ratio (WCAG AA) |
| **ARIA** | `aria-describedby` links input to hint |
| **Screen Readers** | Hint text announced after label |
| **Focus States** | Hint remains visible when input focused |

### Design Tokens (CSS Custom Properties)

```css
:root {
  /* Hint text colors */
  --hint-default: var(--muted-foreground);
  --hint-secondary: oklch(from var(--muted-foreground) l c h / 0.7);
  --hint-success: oklch(0.60 0.15 145);
  --hint-warning: oklch(0.75 0.15 70);
  --hint-error: var(--destructive);

  /* Spacing */
  --hint-margin-top: 0.375rem;
  --hint-line-gap: 0.25rem;
}
```

---

## Sources

1. Google Search Central - Title Links: https://developers.google.com/search/docs/appearance/title-link
2. Google Search Central - Snippets: https://developers.google.com/search/docs/appearance/snippet
3. Nielsen Norman Group - Form Placeholders: https://www.nngroup.com/articles/form-design-placeholders/
4. Yoast - Meta Descriptions: https://yoast.com/meta-descriptions/
5. Semrush - Meta Description Guide: https://www.semrush.com/blog/meta-description/
6. Carbon Design System - Text Input: https://carbondesignsystem.com/components/text-input/style/
7. Adobe Spectrum - Help Text: https://spectrum.adobe.com/page/help-text/
8. Tailwind CSS - Font Size: https://tailwindcss.com/docs/font-size

---

## Implementation Checklist

- [ ] Update character limits: Title 60, Description 160
- [ ] Add auto-generation hint text to SEO fields
- [ ] Implement backend auto-generation from title/excerpt
- [ ] Create consistent hint text component/pattern
- [ ] Apply pattern to all forms system-wide
- [ ] Add visual feedback for optimal character ranges (green/yellow/red)

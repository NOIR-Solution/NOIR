# NOIR Color Schema Guide

> Universal, Accessible Color System for Enterprise SaaS Applications

**Last Updated:** January 2026
**Research Basis:** WCAG 2.1/2.2, Color Blindness Studies, Enterprise UX Best Practices

---

## Executive Summary

This document defines NOIR's color schema strategy, designed to be **universally accessible** for all users including those with color vision deficiencies. The chosen **Blue-Teal** color palette provides:

- **Trust & Professionalism** - Blue is universally associated with reliability (42% of users associate blue with trust)
- **Colorblind Accessibility** - Avoids red-green combinations that affect 8% of men
- **WCAG Compliance** - Minimum 4.5:1 contrast ratios for text
- **Modern Technology** - OKLCH color space for perceptual uniformity

---

## Color Blindness: The Facts

### Statistics
- **8% of men** and **0.5% of women** have some form of color vision deficiency
- **~360 million people** worldwide are affected
- Many people with CVD are unaware of their condition

### Types of Color Blindness

| Type | Affected Population | Color Confusion |
|------|---------------------|-----------------|
| **Deuteranomaly** | 62.5% of CVD (5% of men) | Green appears more red |
| **Protanomaly** | 12.5% of CVD (1% of men) | Red appears more green, less bright |
| **Deuteranopia** | 12.5% of CVD (1% of men) | Cannot distinguish red/green |
| **Protanopia** | 12.5% of CVD (1% of men) | Cannot perceive red light |
| **Tritanopia** | Very rare (~1 in 10,000) | Blue-yellow confusion |
| **Monochromacy** | Extremely rare | Grayscale only |

### Colors to Avoid Together
- ❌ **Red + Green** - Most common CVD confusion
- ❌ **Orange + Red** - Hard to distinguish for protans/deutans
- ❌ **Blue + Purple** - Difficult for red-green CVD
- ❌ **Pink + Gray** - Problematic for many CVD types
- ❌ **Yellow + Light Green** - Low contrast confusion

### Safe Color Combinations
- ✅ **Blue + Orange** - High contrast, CVD-friendly
- ✅ **Blue + Yellow** - Excellent contrast across all CVD types
- ✅ **Blue + White** - Universal readability
- ✅ **Dark Blue + Light Teal** - Monochromatic safe
- ✅ **Navy + Amber** - High contrast pairing

---

## Why Blue?

### Psychological Research

Blue is the **most universally preferred color** for enterprise software:

1. **Trust & Security** - Banks, healthcare, and enterprise software use blue because it conveys reliability
2. **Cognitive Performance** - Research shows blue enhances creativity and calm focus
3. **Cross-Cultural Acceptance** - Positive associations in North America, UK, Latin America, and Asia
4. **Gender Neutral** - Equally appealing across demographics
5. **Industry Standard** - Used by PayPal, LinkedIn, Facebook, IBM, Salesforce, Ford, Samsung

### Blue in Enterprise Software

| Company | Industry | Blue Usage |
|---------|----------|------------|
| PayPal | Finance | Primary brand color |
| LinkedIn | Professional | Trust & networking |
| Salesforce | Enterprise SaaS | Reliability |
| IBM | Technology | "Big Blue" - authority |
| Zoom | Communication | Calm, professional |
| Jira/Trello | Productivity | Focus & organization |

---

## NOIR's Color System

### Primary Color: Blue-Teal

We use a **Blue-Teal** hybrid (OKLCH hue ~250) that combines:
- Blue's trust and professionalism
- Teal's modern, fresh feel
- Maximum colorblind accessibility

**Important:** The hue was adjusted from 220 to 250 in January 2026 to achieve a deeper, more saturated blue that better aligns with Tailwind's `blue-600` level.

### OKLCH Color Space

NOIR uses **OKLCH** (Oklab Lightness Chroma Hue) for color definitions:

```css
/* Format: oklch(Lightness Chroma Hue) */
--sidebar-primary: oklch(0.50 0.20 250);
```

**Why OKLCH?**
- **Perceptually Uniform** - Equal numeric changes = equal visual changes
- **Human-Readable** - Easy to understand and modify
- **Accessibility-Friendly** - Consistent lightness for contrast calculation
- **Browser Support** - All modern browsers since September 2023
- **Better Gradients** - No muddy grays in transitions

### Color Palette Definition

#### Light Theme
```css
:root {
  /* Core UI */
  --background: oklch(1 0 0);           /* Pure white */
  --foreground: oklch(0.145 0 0);       /* Near black */
  --border: oklch(0.922 0 0);           /* Light gray */
  --muted-foreground: oklch(0.556 0 0); /* Medium gray */

  /* Primary Accent: Deep Blue (matches blue-600 level) */
  --sidebar-primary: oklch(0.50 0.20 250);
  --sidebar-ring: oklch(0.50 0.20 250);

  /* Destructive: Red (with caution) */
  --destructive: oklch(0.577 0.245 27.325);
}
```

#### Dark Theme
```css
.dark {
  /* Core UI */
  --background: oklch(0.145 0 0);       /* Near black */
  --foreground: oklch(0.985 0 0);       /* Near white */
  --border: oklch(1 0 0 / 10%);         /* Subtle white */
  --muted-foreground: oklch(0.708 0 0); /* Light gray */

  /* Primary Accent: Deep Blue (slightly lighter for dark bg) */
  --sidebar-primary: oklch(0.60 0.20 250);
  --sidebar-ring: oklch(0.60 0.20 250);

  /* Destructive: Red (adjusted for dark) */
  --destructive: oklch(0.704 0.191 22.216);
}
```

### Tailwind Color Mappings

For components using Tailwind classes:

| Purpose | Light Mode | Dark Mode | OKLCH Equivalent |
|---------|------------|-----------|------------------|
| Primary Blue | `blue-600` | `blue-500` | `oklch(0.50 0.20 250)` |
| Primary Hover | `blue-700` | `blue-400` | Darker/lighter variant |
| Accent Cyan | `cyan-600` | `cyan-500` | `oklch(0.65 0.18 195)` |
| Accent Teal | `teal-600` | `teal-500` | `oklch(0.60 0.15 175)` |
| Destructive | `red-600` | `red-500` | `oklch(0.58 0.24 27)` |

**Note:** Gradients use the `-600` to `-700` range (not `-500` to `-600`) for deeper, more professional appearance.

---

## WCAG Compliance

### Contrast Requirements

| Element Type | Minimum Ratio | NOIR Implementation |
|--------------|---------------|---------------------|
| Normal Text | 4.5:1 | ✅ All text meets this |
| Large Text (24px+) | 3:1 | ✅ Headlines compliant |
| UI Components | 3:1 | ✅ Buttons, inputs |
| Focus Indicators | 3:1 | ✅ Ring states |

### Testing Tools

1. **WebAIM Contrast Checker** - https://webaim.org/resources/contrastchecker/
2. **Chrome DevTools** - Accessibility panel
3. **Adobe Color** - Contrast checker with CVD simulation
4. **Coolors** - Palette generation with accessibility check

### The "Don't Rely on Color Alone" Rule

**WCAG SC 1.4.1**: Color must not be the only means of conveying information.

NOIR implements this through:
- ✅ Icons alongside colored elements
- ✅ Text labels for status indicators
- ✅ Underlines for links
- ✅ Border/shape changes for states
- ✅ Patterns/textures where needed

---

## Implementation Guidelines

### 1. Primary Actions

```tsx
// Use blue-teal gradient for primary CTAs (use -700 level for deeper blue)
<Button className="bg-gradient-to-r from-blue-700 to-cyan-700 hover:from-blue-800 hover:to-cyan-800 text-white">
  Primary Action
</Button>
```

### 2. Destructive Actions

```tsx
// Red with text label and icon
<Button variant="destructive" className="text-red-600">
  <Trash className="mr-2 h-4 w-4" />
  Delete
</Button>
```

### 3. Status Indicators

```tsx
// Never use color alone - always include icon/text
<Badge variant="success">
  <Check className="mr-1 h-3 w-3" />
  Completed
</Badge>

<Badge variant="warning">
  <AlertTriangle className="mr-1 h-3 w-3" />
  Pending
</Badge>

<Badge variant="error">
  <XCircle className="mr-1 h-3 w-3" />
  Failed
</Badge>
```

### 4. Form Validation

```tsx
// Error state with icon and text, not just red border
<Input
  className="border-red-500"
  aria-invalid="true"
/>
<p className="text-red-600 flex items-center mt-1">
  <AlertCircle className="mr-1 h-4 w-4" />
  This field is required
</p>
```

### 5. Charts and Data Visualization

For charts, use a colorblind-safe palette:

```typescript
// Colorblind-safe chart colors (validated for all CVD types)
export const chartColors = {
  primary: '#3b82f6',   // Blue-500
  secondary: '#f59e0b', // Amber-500
  tertiary: '#06b6d4',  // Cyan-500
  quaternary: '#8b5cf6', // Violet-500
  quinary: '#ec4899',   // Pink-500
}
```

---

## Theme Configuration

NOIR uses **database-driven Branding Settings** for theming, configured per-tenant via the Admin UI. The **blue** theme is the default and most accessible. Theme colors are applied as CSS custom properties at runtime based on tenant configuration.

---

## Gradient Usage

### Approved Gradients

```css
/* Primary gradient: Blue → Cyan (colorblind-safe, use -600/-700 range) */
.gradient-primary {
  background: linear-gradient(to right, #2563eb, #0891b2); /* blue-600 → cyan-600 */
}

/* Accent gradient: Blue → Cyan → Teal */
.gradient-accent {
  background: linear-gradient(135deg, #1d4ed8, #0e7490, #0d9488); /* blue-700 → cyan-700 → teal-700 */
}

/* Avoid: Red → Green (colorblind problematic) */
/* Avoid: Purple → Orange (low contrast for some CVD) */
```

### Animated Backgrounds

For landing pages, use blue-teal blob animations with `-600` level colors:

```tsx
<div className="bg-blue-600/30 blur-3xl animate-blob" />
<div className="bg-cyan-600/30 blur-3xl animate-blob animation-delay-2000" />
<div className="bg-teal-600/30 blur-3xl animate-blob animation-delay-4000" />
```

### Dashboard Card Styling

For dashboard cards, use subtle blue accents:

```tsx
// Card with blue border and shadow
<Card className="border-blue-600/10 shadow-sm shadow-blue-600/5">
  <CardContent>...</CardContent>
</Card>

// Interactive link with blue icon
<a className="group flex items-center gap-3 p-3 rounded-lg border hover:border-blue-600/30">
  <div className="w-8 h-8 rounded-lg bg-blue-600/10 flex items-center justify-center">
    <BookOpen className="h-4 w-4 text-blue-600" />
  </div>
  <span className="group-hover:text-blue-600 transition-colors">Link Text</span>
</a>
```

---

## Testing Checklist

Before deploying UI changes, verify:

- [ ] **Contrast Ratio**: Text meets 4.5:1 minimum
- [ ] **Color Independence**: Information not conveyed by color alone
- [ ] **CVD Simulation**: Test with Sim Daltonism or similar
- [ ] **Dark Mode**: All elements visible and contrasted
- [ ] **Focus States**: Visible keyboard focus indicators
- [ ] **Hover States**: Clear interactive feedback

### Recommended Browser Extensions

1. **NoCoffee Vision Simulator** (Chrome)
2. **Sim Daltonism** (macOS)
3. **Color Oracle** (Windows/macOS/Linux)

---

## Quick Reference Card

### Do's
✅ Use blue as primary accent color
✅ Pair colors with icons and text labels
✅ Maintain 4.5:1 contrast for text
✅ Test with colorblind simulation tools
✅ Use OKLCH for color definitions
✅ Provide dark mode alternative

### Don'ts
❌ Use red-green combinations for meaning
❌ Rely on color alone for information
❌ Use low-contrast text
❌ Mix purple with similar blues
❌ Use saturated colors on large areas
❌ Forget to test in both light/dark modes

---

## Resources

### Official Standards
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [WCAG 2.2 What's New](https://www.w3.org/WAI/standards-guidelines/wcag/new-in-22/)

### Color Tools
- [OKLCH Color Picker](https://oklch.com/)
- [Coolors Palette Generator](https://coolors.co/)
- [Adobe Color Accessibility](https://color.adobe.com/create/color-accessibility)
- [WebAIM Contrast Checker](https://webaim.org/resources/contrastchecker/)

### Research
- [Colour Blind Awareness](https://www.colourblindawareness.org/)
- [EnChroma Types of CVD](https://enchroma.com/pages/types-of-color-blindness)
- [Evil Martians OKLCH Guide](https://evilmartians.com/chronicles/oklch-in-css-why-quit-rgb-hsl)

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | Jan 2026 | Initial blue-teal color system |
| 1.1 | Jan 2026 | Deepened colors: OKLCH hue 220→250, chroma 0.15→0.20; Updated gradients to use -600/-700 range; Added dashboard card styling guidelines |

---

*This document should be reviewed annually and updated based on new WCAG standards and colorblind research.*

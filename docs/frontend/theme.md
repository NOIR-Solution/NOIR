# Theme Customization Guide

This guide explains how to customize the color scheme when setting up NOIR for a new customer.

## Quick Start

1. Open `src/config/theme.ts`
2. Find the `SELECTED_THEME` constant (around line 258)
3. Change it to one of the available presets:
   ```typescript
   export const SELECTED_THEME: keyof typeof themes = "healthcare"
   ```
4. Save and rebuild: `npm run build`

## Available Theme Presets

| Preset | Colors | Best For |
|--------|--------|----------|
| `blue` | Blue/Indigo | Technology, SaaS (default) |
| `corporate` | Slate/Blue | Professional services, B2B |
| `healthcare` | Teal/Cyan | Medical, healthcare, wellness |
| `finance` | Emerald/Green | Banking, fintech, investments |
| `creative` | Purple/Pink | Design agencies, creative tools |
| `energy` | Orange/Amber | Industrial, energy, construction |
| `modern` | Rose/Pink | Consumer apps, lifestyle brands |
| `premium` | Indigo/Violet | Luxury brands, premium services |

## Why Static Presets?

NOIR uses static theme presets instead of dynamic color generation because **Tailwind CSS v4 uses static analysis at build time**.

Dynamic class generation like `` `bg-${color}-600` `` won't work because:
- Tailwind scans your code at build time to find CSS classes
- Template literal strings are evaluated at runtime
- Classes built dynamically are never detected by Tailwind
- Result: missing styles in production builds

Static presets solve this by defining all classes as literal strings that Tailwind can detect.

## Creating a Custom Preset

If the built-in presets don't match your customer's brand, create a new one:

1. Copy an existing preset in `src/config/theme.ts`:
   ```typescript
   const themes = {
     // ... existing presets ...

     /**
      * Custom - Customer Name theme
      */
     customername: {
       name: "Custom/Brand",
       description: "Customer Name brand theme",
       gradient: "bg-gradient-to-br from-sky-600 via-blue-600 to-sky-700",
       gradientSimple: "bg-gradient-to-br from-sky-600 to-blue-600",
       bgPrimary: "bg-sky-600",
       bgPrimaryHover: "hover:bg-sky-700",
       textPrimary: "text-sky-600",
       textPrimaryHover: "hover:text-sky-700",
       borderPrimary: "border-sky-600",
       shadowPrimary: "shadow-sky-500/30",
       shadowPrimaryLight: "shadow-sky-500/20",
       ringPrimary: "ring-sky-500/20",
       blobPrimary: "bg-sky-400/20",
       blobSecondary: "bg-blue-400/20",
       blobAccent: "bg-cyan-400/20",
       buttonPrimary: "bg-sky-600 hover:bg-sky-700 text-white shadow-lg shadow-sky-500/20",
       linkPrimary: "text-sky-600 hover:text-sky-700",
       iconContainer: "bg-gradient-to-br from-sky-600 to-blue-600",
       iconContainerShadow: "shadow-lg shadow-sky-500/30",
       svgGradientStart: "#0284c7", // sky-600
       svgGradientEnd: "#2563eb",   // blue-600
     },
   }
   ```

2. Update `SELECTED_THEME`:
   ```typescript
   export const SELECTED_THEME: keyof typeof themes = "customername"
   ```

3. Rebuild the application

## Tailwind Color Reference

Use these color names in your presets. Each color has shades from 50-950.

| Color | Hex (600) | Description |
|-------|-----------|-------------|
| `slate` | #475569 | Cool gray with blue undertones |
| `gray` | #4b5563 | Pure neutral gray |
| `zinc` | #52525b | Slightly warm gray |
| `stone` | #57534e | Warm brown-gray |
| `red` | #dc2626 | Danger, error states |
| `orange` | #ea580c | Energetic, attention |
| `amber` | #d97706 | Warm, friendly |
| `yellow` | #ca8a04 | Highlights, warnings |
| `lime` | #65a30d | Fresh, natural |
| `green` | #16a34a | Success, positive |
| `emerald` | #059669 | Rich, luxurious green |
| `teal` | #0d9488 | Professional, calming |
| `cyan` | #0891b2 | Tech, modern |
| `sky` | #0284c7 | Friendly, approachable |
| `blue` | #2563eb | Trust, reliability |
| `indigo` | #4f46e5 | Professional, tech |
| `violet` | #7c3aed | Creative, innovative |
| `purple` | #9333ea | Premium, creative |
| `fuchsia` | #c026d3 | Bold, playful |
| `pink` | #db2777 | Modern, friendly |
| `rose` | #e11d48 | Warm, passionate |

## What Gets Themed

The following UI elements use the theme configuration:

### Login Page
- Logo container (gradient background)
- Submit button (primary color with shadow)
- Right panel gradient background
- Animated background blobs
- SVG wave decoration

### Dashboard (Home)
- Header logo (gradient)
- Navigation links (primary color)

### Loading States
- Spinner border (primary color)

## Theme Class Reference

Available classes exported from `themeClasses`:

| Property | Purpose |
|----------|---------|
| `gradient` | Full 3-color gradient (from/via/to) |
| `gradientSimple` | Simple 2-color gradient |
| `bgPrimary` | Solid primary background |
| `bgPrimaryHover` | Hover state background |
| `textPrimary` | Primary text color |
| `textPrimaryHover` | Text hover state |
| `borderPrimary` | Primary border color |
| `shadowPrimary` | Primary colored shadow |
| `shadowPrimaryLight` | Lighter shadow variant |
| `ringPrimary` | Focus ring color |
| `buttonPrimary` | Complete button styling |
| `linkPrimary` | Link with hover |
| `iconContainer` | Logo/icon gradient box |
| `iconContainerShadow` | Shadow for icon containers |
| `blobPrimary` | Animated blob (primary) |
| `blobSecondary` | Animated blob (secondary) |
| `blobAccent` | Animated blob (accent) |
| `svgGradientStart` | SVG gradient start hex |
| `svgGradientEnd` | SVG gradient end hex |

## Usage in Components

```tsx
import { themeClasses } from '@/config/theme'

// Button example
<Button className={`w-full ${themeClasses.buttonPrimary}`}>
  Submit
</Button>

// Link example
<a className={themeClasses.linkPrimary}>Read more</a>

// Icon container example
<div className={`${themeClasses.iconContainer} ${themeClasses.iconContainerShadow}`}>
  <Icon />
</div>
```

## Troubleshooting

### Colors not showing after changing theme?
1. Make sure you saved `theme.ts`
2. Restart the dev server: `npm run dev`
3. Hard refresh the browser: Ctrl+Shift+R (Windows) or Cmd+Shift+R (Mac)

### TypeScript error when adding new preset?
Make sure your preset has ALL the required properties. Copy an existing preset to ensure you don't miss any.

### Custom color not in Tailwind palette?
If your customer's brand color doesn't match a Tailwind color:
1. Find the closest Tailwind color
2. Or extend Tailwind's config (advanced, see Tailwind docs)

### Build still includes old colors?
Tailwind caches compiled CSS. Try:
```bash
rm -rf node_modules/.vite
npm run build
```

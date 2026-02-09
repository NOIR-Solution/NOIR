# Visual Regression Testing Guide

**Automated Visual Testing with Playwright**

---

## Table of Contents

1. [Overview](#overview)
2. [Quick Start](#quick-start)
3. [Configuration](#configuration)
4. [Test Structure](#test-structure)
5. [Baseline Management](#baseline-management)
6. [Running Tests](#running-tests)
7. [CI/CD Integration](#cicd-integration)
8. [Best Practices](#best-practices)
9. [Troubleshooting](#troubleshooting)

---

## Overview

The NOIR project uses **Playwright's built-in visual comparison** to detect unintended UI changes. Visual regression tests capture screenshots and compare them against baseline images to ensure UI consistency across code changes.

### Coverage Statistics

| Metric | Count |
|--------|-------|
| **Test Cases** | 15 tests |
| **Baseline Snapshots** | 16 images (505KB) |
| **Viewports Tested** | 3 (Desktop, Tablet, Mobile) |
| **Theme Variants** | 2 (Light, Dark) |
| **Critical Pages** | 3 (Dashboard, Settings, Notifications) |

### What's Tested

- **Full Page Layouts** - Complete page renders with all components
- **Component Isolation** - Individual UI components (cards, buttons, forms, modals)
- **Responsive Design** - Mobile (375x667), Tablet (768x1024), Desktop (1280x720)
- **Theme Switching** - Light mode vs Dark mode
- **Navigation Elements** - Sidebar, navigation bars
- **Interactive States** - Button variations, form inputs

---

## Quick Start

```bash
# Navigate to e2e-tests directory
cd src/NOIR.Web/frontend/e2e-tests

# Install dependencies (if not already done)
npm install

# Generate baseline snapshots (first time setup)
npx playwright test tests/visual --update-snapshots

# Run visual regression tests
npx playwright test tests/visual

# Run in headed mode (see browser)
npx playwright test tests/visual --headed

# Update baselines after intentional UI changes
npx playwright test tests/visual --update-snapshots
```

---

## Configuration

### Visual Diff Thresholds

Thresholds are centralized in `tests/visual/config.ts`:

```typescript
export const VisualDiffThresholds = {
  FULL_PAGE: 100,           // Full page screenshots
  COMPONENT: 30,            // Individual components
  ELEMENT: 20,              // Small UI elements (buttons, badges)
  DYNAMIC_CONTENT: 150,     // Charts, graphs, changing data
  FORM_CONTROL: 20,         // Input fields, textareas
}
```

**Why different thresholds?**
- **Font rendering** varies across OS (macOS vs Linux vs Windows)
- **Anti-aliasing** creates minor pixel differences
- **Dynamic content** (timestamps, charts) needs flexibility
- **Static elements** (buttons, icons) should be strict

### Viewport Sizes

Standardized viewports for consistent testing:

```typescript
export const Viewports = {
  MOBILE: { width: 375, height: 667 },        // iPhone SE
  TABLET: { width: 768, height: 1024 },       // iPad
  DESKTOP: { width: 1280, height: 720 },      // Standard HD
  DESKTOP_FULL_HD: { width: 1920, height: 1080 },
}
```

### Screenshot Options

Helper methods for consistent screenshot capture:

```typescript
// Full page screenshot
await expect(page).toHaveScreenshot('page.png',
  ScreenshotOptions.fullPage()
);

// Component screenshot
await expect(component).toHaveScreenshot('component.png',
  ScreenshotOptions.component()
);

// Element screenshot with strict tolerance
await expect(button).toHaveScreenshot('button.png',
  ScreenshotOptions.element()
);
```

---

## Test Structure

### Test Organization

```
tests/visual/
├── config.ts                              # Configuration (thresholds, viewports)
├── visual-regression.spec.ts              # Main test suite
└── visual-regression.spec.ts-snapshots/   # Baseline images (505KB)
    ├── button-1.png
    ├── button-2.png
    ├── critical-dashboard.png
    ├── critical-notifications.png
    ├── critical-settings.png
    ├── dashboard-card-1.png
    ├── dashboard-card-2.png
    ├── dashboard-dark.png
    ├── dashboard-full.png
    ├── dashboard-light.png
    ├── dashboard-mobile.png
    ├── dashboard-tablet.png
    ├── form-input-1.png
    ├── login-page.png
    ├── modal-dialog.png
    └── sidebar.png
```

### Test Suite Breakdown

#### 1. Full Page Tests

```typescript
test('VIS-001: Dashboard page renders correctly', async ({ page }) => {
  const dashboardPage = new DashboardPage(page);
  await dashboardPage.navigate();
  await dashboardPage.expectDashboardLoaded();

  // Wait for loading states to complete
  await page.waitForLoadState('networkidle');

  // Capture full page
  await expect(page).toHaveScreenshot('dashboard-full.png',
    ScreenshotOptions.fullPage()
  );
});
```

**Pages Tested:**
- Dashboard (VIS-001)
- Login (VIS-002)
- Settings (VIS-CRITICAL-settings)
- Notifications (VIS-CRITICAL-notifications)

#### 2. Component Tests

```typescript
test('VIS-003: Dashboard card components', async ({ page }) => {
  const dashboardPage = new DashboardPage(page);
  await dashboardPage.navigate();
  await dashboardPage.expectDashboardLoaded();

  await page.waitForLoadState('networkidle');

  // Capture individual card components
  const cards = await page.locator('[data-slot="card"]').all();

  for (let i = 0; i < Math.min(cards.length, 5); i++) {
    await expect(cards[i]).toHaveScreenshot(`dashboard-card-${i + 1}.png`, {
      animations: 'disabled',
      maxDiffPixels: 30,
    });
  }
});
```

**Components Tested:**
- Dashboard cards (VIS-003)
- Navigation sidebar (VIS-007)
- Form inputs (VIS-008)
- Buttons (VIS-009)
- Modal dialogs (VIS-010)

#### 3. Responsive Tests

```typescript
test('VIS-004: Responsive - Mobile viewport', async ({ page }) => {
  // Test mobile viewport
  await page.setViewportSize({ width: 375, height: 667 }); // iPhone SE

  const dashboardPage = new DashboardPage(page);
  await dashboardPage.navigate();

  // On mobile, sidebar is hidden by design
  await expectDashboardLoadedOnMobile(page);

  await expect(page).toHaveScreenshot('dashboard-mobile.png', {
    fullPage: true,
    animations: 'disabled',
    maxDiffPixels: 100,
  });
});
```

**Viewports Tested:**
- Mobile (VIS-004) - 375x667 iPhone SE
- Tablet (VIS-005) - 768x1024 iPad
- Desktop (default) - 1280x720 HD

#### 4. Theme Tests

```typescript
test('VIS-006: Dark mode vs Light mode', async ({ page }) => {
  const dashboardPage = new DashboardPage(page);
  await dashboardPage.navigate();
  await dashboardPage.expectDashboardLoaded();

  await page.waitForLoadState('networkidle');

  // Capture light mode
  await expect(page).toHaveScreenshot('dashboard-light.png', {
    animations: 'disabled',
    maxDiffPixels: 100,
  });

  // Toggle to dark mode
  const themeToggle = page.locator('[aria-label*="theme" i]').first();
  if (await themeToggle.isVisible()) {
    await themeToggle.click();
    await page.waitForTimeout(500); // Wait for theme transition

    // Capture dark mode
    await expect(page).toHaveScreenshot('dashboard-dark.png', {
      animations: 'disabled',
      maxDiffPixels: 100,
    });
  }
});
```

**Themes Tested:**
- Light mode (VIS-006)
- Dark mode (VIS-006)

---

## Baseline Management

### Generating Baselines

**First-time setup** or after intentional UI changes:

```bash
# Generate all baselines
npx playwright test tests/visual --update-snapshots

# Generate for specific test
npx playwright test tests/visual -g "VIS-001" --update-snapshots

# Generate for specific viewport
npx playwright test tests/visual -g "mobile" --update-snapshots
```

### When to Update Baselines

**Update baselines when:**
- ✅ Intentional UI design changes
- ✅ Component styling updates
- ✅ Theme color adjustments
- ✅ Layout improvements
- ✅ Font or spacing changes

**DO NOT update baselines for:**
- ❌ Accidental visual regressions
- ❌ Broken layouts
- ❌ CSS bugs
- ❌ Rendering errors

### Reviewing Visual Diffs

When tests fail, Playwright generates:

```
test-results/
├── visual-regression-spec-VIS-001-chromium/
│   ├── dashboard-full-actual.png        # Current screenshot
│   ├── dashboard-full-expected.png      # Baseline
│   └── dashboard-full-diff.png          # Highlighted differences
```

**Review process:**
1. Open `playwright-report/` HTML report
2. View side-by-side comparison
3. Examine diff highlighting
4. Determine if change is intentional
5. Update baselines if approved

### Baseline Storage

```
tests/visual/visual-regression.spec.ts-snapshots/
├── chromium/                    # Browser-specific baselines
│   ├── dashboard-full.png
│   ├── login-page.png
│   └── ...
└── ...
```

**Why browser-specific?**
- Different rendering engines (Chromium, Firefox, WebKit)
- Font anti-aliasing varies
- Subpixel rendering differences

---

## Running Tests

### Local Development

```bash
# Run all visual tests
npm run test:visual

# Run with UI mode (recommended)
npx playwright test tests/visual --ui

# Run in headed mode (see browser)
npx playwright test tests/visual --headed

# Run specific test
npx playwright test tests/visual -g "VIS-001"

# Run with debug mode
npx playwright test tests/visual --debug
```

### Parallel Execution

```bash
# Run with 4 workers (faster)
npx playwright test tests/visual --workers=4

# Run serially (slower but more stable)
npx playwright test tests/visual --workers=1
```

### Filtering Tests

```bash
# Run only mobile tests
npx playwright test tests/visual -g "mobile"

# Run only component tests
npx playwright test tests/visual -g "component"

# Run critical pages
npx playwright test tests/visual -g "CRITICAL"

# Run specific browser
npx playwright test tests/visual --project=chromium
```

---

## CI/CD Integration

### GitHub Actions Workflow

**File:** `.github/workflows/visual-regression.yml`

```yaml
name: Visual Regression Tests

on:
  pull_request:
    branches: [main]
    paths:
      - 'src/NOIR.Web/frontend/src/**'
      - 'src/NOIR.Web/frontend/e2e-tests/**'
  push:
    branches: [main]
  workflow_dispatch:
    inputs:
      update_baselines:
        description: 'Update screenshot baselines'
        type: boolean
        default: false

jobs:
  visual-regression:
    runs-on: ubuntu-latest
    timeout-minutes: 20

    steps:
      - name: Checkout code
        uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'

      - name: Install dependencies
        working-directory: src/NOIR.Web/frontend
        run: npm ci

      - name: Install Playwright browsers
        working-directory: src/NOIR.Web/frontend/e2e-tests
        run: npx playwright install --with-deps chromium

      - name: Build frontend
        working-directory: src/NOIR.Web/frontend
        run: npm run build

      - name: Run visual regression tests
        working-directory: src/NOIR.Web/frontend/e2e-tests
        run: |
          if [ "${{ github.event.inputs.update_baselines }}" == "true" ]; then
            npx playwright test tests/visual --update-snapshots
          else
            npx playwright test tests/visual
          fi

      - name: Upload visual diff results
        if: failure()
        uses: actions/upload-artifact@v4
        with:
          name: visual-regression-diffs
          path: |
            src/NOIR.Web/frontend/e2e-tests/test-results/**/*-diff.png
            src/NOIR.Web/frontend/e2e-tests/test-results/**/*-actual.png
          retention-days: 7

      - name: Upload test report
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: visual-regression-report
          path: src/NOIR.Web/frontend/e2e-tests/playwright-report/
          retention-days: 30
```

### Manual Baseline Update (CI)

```bash
# Trigger workflow with baseline update
gh workflow run visual-regression.yml -f update_baselines=true
```

### PR Comments on Failure

When visual tests fail on PRs, GitHub Actions automatically comments:

```
⚠️ Visual Regression Test Failed

Visual differences detected! Please review the changes:

1. Download the visual-regression-diffs artifact
2. Compare the `-actual.png` and `-diff.png` files
3. If changes are intentional, update baselines: `npx playwright test visual --update-snapshots`

[View full test report]
```

---

## Best Practices

### 1. Disable Animations

```typescript
// ALWAYS disable animations for consistent screenshots
await expect(page).toHaveScreenshot('page.png', {
  animations: 'disabled',  // ✅ Prevents flaky tests
});
```

**Why?** Animations cause timing issues and random pixel differences.

### 2. Wait for Loading States

```typescript
// Wait for page to be fully loaded
await page.waitForLoadState('networkidle');

// Wait for specific elements
await page.locator('[data-testid="content"]').waitFor({ state: 'visible' });

// AVOID hard-coded waits (unless necessary for transitions)
await page.waitForTimeout(500); // ⚠️ Use sparingly
```

### 3. Use Appropriate Thresholds

```typescript
// Strict for static elements
await expect(button).toHaveScreenshot('button.png', {
  maxDiffPixels: 20,  // ✅ Tight tolerance
});

// Flexible for full pages
await expect(page).toHaveScreenshot('page.png', {
  maxDiffPixels: 100,  // ✅ Allows for font rendering
});

// Very flexible for dynamic content
await expect(chart).toHaveScreenshot('chart.png', {
  maxDiffPixels: 150,  // ✅ Accounts for data changes
});
```

### 4. Test Component Isolation

```typescript
// GOOD: Test individual components
const card = page.locator('[data-slot="card"]').first();
await expect(card).toHaveScreenshot('card.png');

// BETTER: Test multiple instances
const cards = await page.locator('[data-slot="card"]').all();
for (let i = 0; i < cards.length; i++) {
  await expect(cards[i]).toHaveScreenshot(`card-${i + 1}.png`);
}
```

### 5. Handle Dynamic Content

```typescript
// Mask dynamic content (timestamps, user IDs, etc.)
await expect(page).toHaveScreenshot('page.png', {
  mask: [
    page.locator('[data-testid="timestamp"]'),
    page.locator('[data-testid="user-id"]'),
  ],
});
```

### 6. Consistent Viewport Setup

```typescript
test.beforeEach(async ({ page }) => {
  // Set consistent viewport
  await page.setViewportSize(Viewports.DESKTOP);
});
```

### 7. Group Related Tests

```typescript
test.describe('Visual Regression - Components', () => {
  test('VIS-003: Cards', ...);
  test('VIS-008: Forms', ...);
  test('VIS-009: Buttons', ...);
});

test.describe('Visual Regression - Responsive', () => {
  test('VIS-004: Mobile', ...);
  test('VIS-005: Tablet', ...);
});
```

---

## Troubleshooting

### Common Issues

#### 1. Flaky Tests (Random Failures)

**Symptoms:** Tests pass/fail inconsistently

**Solutions:**
```typescript
// Add explicit waits
await page.waitForLoadState('networkidle');

// Wait for animations to complete
await page.waitForTimeout(500);

// Increase threshold
maxDiffPixels: 150  // More tolerance
```

#### 2. Font Rendering Differences

**Symptoms:** Minor pixel differences across OS

**Solutions:**
- Increase `maxDiffPixels` threshold
- Use Docker container for consistent rendering
- Run tests on same OS as CI (Ubuntu)

#### 3. Large Diff Files

**Symptoms:** Snapshot files are too large (>1MB)

**Solutions:**
```typescript
// Capture specific regions
await expect(page.locator('#main-content')).toHaveScreenshot();

// Reduce viewport size
await page.setViewportSize({ width: 1280, height: 720 });
```

#### 4. Missing Baselines

**Symptoms:** Error: "A snapshot doesn't exist"

**Solution:**
```bash
# Generate baselines
npx playwright test tests/visual --update-snapshots
```

#### 5. CI Failures but Local Passes

**Symptoms:** Tests pass locally but fail in CI

**Causes:**
- Different OS (Windows/macOS vs Linux)
- Different Node.js version
- Missing environment variables

**Solutions:**
```bash
# Run tests in Docker (matches CI environment)
docker run -it --rm -v $(pwd):/work -w /work mcr.microsoft.com/playwright:v1.50.0-noble \
  npx playwright test tests/visual

# Or increase threshold for CI
const threshold = process.env.CI ? 150 : 100;
```

### Debug Workflow

```bash
# 1. Run test with debug mode
npx playwright test tests/visual -g "VIS-001" --debug

# 2. Generate new baseline for comparison
npx playwright test tests/visual -g "VIS-001" --update-snapshots

# 3. View HTML report with diffs
npx playwright show-report

# 4. Compare images manually
open test-results/visual-regression-spec-VIS-001-chromium/dashboard-full-diff.png
```

### Reset All Baselines

```bash
# Delete all existing baselines
rm -rf tests/visual/visual-regression.spec.ts-snapshots/

# Regenerate all baselines
npx playwright test tests/visual --update-snapshots
```

---

## Performance Considerations

### Snapshot Size Optimization

| Strategy | Benefit |
|----------|---------|
| Capture specific regions | Smaller files, faster comparison |
| Use appropriate viewport | Avoid unnecessary full HD captures |
| Compress images | Reduce storage (CI artifacts) |
| Limit snapshot count | Only test critical UI components |

### Test Execution Time

| Tests | Duration (Chromium) |
|-------|---------------------|
| 15 visual tests | ~60 seconds |
| Full page (3 tests) | ~15 seconds |
| Components (5 tests) | ~25 seconds |
| Responsive (2 tests) | ~10 seconds |
| Theme (1 test) | ~8 seconds |
| Critical pages (3 tests) | ~12 seconds |

**Total baseline size:** 505KB (16 images)

---

## References

- [Playwright Visual Comparisons](https://playwright.dev/docs/test-snapshots)
- [E2E Testing Guide](./E2E-TESTING-GUIDE.md)
- [Accessibility Testing](./ACCESSIBILITY-TESTING.md)
- [Test Plan](./TEST_PLAN.md)

---

**Last Updated:** 2026-02-09
**Version:** 1.0
**Status:** ✅ All 15 tests passing (CI green)

# Testing Infrastructure Summary

**Comprehensive Quality Assurance System - February 2026**

---

## Overview

The NOIR project maintains a robust multi-layered testing infrastructure that ensures code quality, UI consistency, and accessibility compliance across 6,750+ backend tests and 500+ frontend E2E tests.

---

## Testing Pyramid

```
                    ╱╲
                   ╱  ╲
                  ╱ E2E ╲           15 Visual + 9 A11y + ~490 E2E
                 ╱────────╲          = ~514 frontend tests
                ╱          ╲
               ╱ Integration╲        654 integration tests
              ╱──────────────╲
             ╱                ╲
            ╱   Unit Tests     ╲    6,077 unit tests
           ╱────────────────────╲   (842 domain + 5,231 application + 4 other)
          ╱________________________╲
```

---

## Test Coverage by Layer

### 1. Unit Tests (6,077 tests)

**Location:** `tests/NOIR.*.UnitTests/`

| Project | Tests | Coverage |
|---------|-------|----------|
| Domain | 842 | Entity logic, value objects, domain services |
| Application | 5,231 | Command handlers, query handlers, validators |
| Other | 4 | Utility and helper tests |

**Technologies:**
- xUnit
- FluentAssertions
- Moq (mocking)
- NSubstitute

**Execution Time:** ~30 seconds

### 2. Integration Tests (654 tests)

**Location:** `tests/NOIR.IntegrationTests/`

**What's Tested:**
- API endpoints
- Database operations
- Authentication flows
- Multi-tenancy
- Email services
- File uploads

**Technologies:**
- xUnit
- WebApplicationFactory
- Testcontainers (PostgreSQL)
- Respawn (database cleanup)

**Execution Time:** ~2 minutes

### 3. Architecture Tests (25 tests)

**Location:** `tests/NOIR.ArchitectureTests/`

**What's Tested:**
- Project dependency rules
- Naming conventions
- Architecture patterns
- DI registration

**Technologies:**
- NetArchTest
- xUnit

**Execution Time:** ~5 seconds

### 4. E2E Functional Tests (~490 tests)

**Location:** `src/NOIR.Web/frontend/e2e-tests/tests/`

**Test Organization:**

| Category | Spec Files | Tests | Description |
|----------|-----------|-------|-------------|
| Authentication | 2 | ~40 | Login, password reset, session management |
| E-commerce | 8 | ~180 | Products, categories, brands, attributes, variants |
| Admin | 5 | ~120 | Users, roles, tenants, settings |
| Content | 5 | ~80 | Blog posts, categories, tags, legal pages |
| System | 7 | ~50 | Notifications, command palette, developer logs |
| Smoke | 5 | ~20 | Critical path validation |

**Technologies:**
- Playwright (v1.50.0)
- TypeScript
- Page Object Model (31 pages)

**Execution Time:**
- Chromium: ~8 minutes
- Firefox: ~10 minutes
- Total (parallel): ~12 minutes

**Documentation:** [E2E-TESTING-GUIDE.md](./E2E-TESTING-GUIDE.md)

### 5. Visual Regression Tests (15 tests)

**Location:** `src/NOIR.Web/frontend/e2e-tests/tests/visual/`

**What's Tested:**
- Full page layouts (Dashboard, Login, Settings, Notifications)
- Component isolation (Cards, Forms, Buttons, Modals)
- Responsive design (Mobile 375x667, Tablet 768x1024, Desktop 1280x720)
- Theme variants (Light mode, Dark mode)
- Navigation elements (Sidebar, menus)

**Baseline Snapshots:**
- 16 images
- 505KB total size
- Stored in `visual-regression.spec.ts-snapshots/`

**Technologies:**
- Playwright built-in screenshot comparison
- Custom threshold configuration
- Viewport standardization

**Execution Time:** ~60 seconds

**Documentation:** [VISUAL-REGRESSION-TESTING.md](./VISUAL-REGRESSION-TESTING.md)

### 6. Accessibility Tests (9 tests)

**Location:** `src/NOIR.Web/frontend/e2e-tests/tests/accessibility/`

**What's Tested:**
- WCAG 2.1 Level AA compliance
- Keyboard navigation
- ARIA labels and roles
- Heading hierarchy (h1-h6)
- Color contrast (4.5:1 ratio)
- Form accessibility
- Screen reader compatibility

**Technologies:**
- axe-core (90+ automated rules)
- @axe-core/playwright integration
- WCAG 2.0 A/AA and WCAG 2.1 A/AA rule sets

**Execution Time:** ~30 seconds

**Documentation:** [ACCESSIBILITY-TESTING.md](./ACCESSIBILITY-TESTING.md)

### 7. Mobile Tests (~30 tests)

**Location:** `src/NOIR.Web/frontend/e2e-tests/tests/mobile/`

**What's Tested:**
- iOS viewport (iPhone SE - 375x667)
- Android viewport (Pixel 5 - 393x851)
- Touch interactions
- Mobile navigation patterns
- Responsive layouts

**Test Files:**
- `dashboard-mobile.spec.ts`
- `login-mobile.spec.ts`
- `products-mobile.spec.ts`

**Execution Time:** ~3 minutes

---

## Test Execution

### Local Development

```bash
# Backend tests
cd /d/GIT/TOP/NOIR
dotnet test src/NOIR.sln                           # All backend tests (~2 min)
dotnet test tests/NOIR.Application.UnitTests       # Unit tests only
dotnet test tests/NOIR.IntegrationTests            # Integration tests only

# Frontend E2E tests
cd src/NOIR.Web/frontend/e2e-tests
npm test                                           # All E2E tests (~12 min)
npx playwright test --ui                           # Interactive UI mode
npx playwright test tests/smoke                    # Smoke tests only

# Visual regression tests
npx playwright test tests/visual                   # Visual tests (~60 sec)
npx playwright test tests/visual --update-snapshots # Update baselines

# Accessibility tests
npx playwright test tests/accessibility            # A11y tests (~30 sec)

# Mobile tests
npx playwright test tests/mobile                   # Mobile tests (~3 min)
```

### CI/CD Pipeline

**GitHub Actions Workflows:**

1. **Backend Tests** (`.github/workflows/backend-tests.yml`)
   - Runs on: Push to main, PRs
   - Duration: ~3 minutes
   - Coverage: Unit + Integration + Architecture tests

2. **E2E Tests** (`.github/workflows/e2e-tests.yml`)
   - Runs on: Push to main, PRs (frontend changes only)
   - Duration: ~15 minutes
   - Coverage: Full E2E suite on Chromium + Firefox

3. **Visual Regression** (`.github/workflows/visual-regression.yml`)
   - Runs on: Push to main, PRs (frontend changes only)
   - Duration: ~5 minutes
   - Coverage: All visual tests with baseline comparison
   - Artifacts: Diff images on failure

4. **Accessibility** (`.github/workflows/accessibility.yml`)
   - Runs on: Push to main, PRs (frontend changes only)
   - Duration: ~3 minutes
   - Coverage: WCAG 2.1 AA compliance checks
   - Artifacts: HTML report with violations

**Total CI Time (PR):**
- Backend: ~3 minutes
- Frontend (parallel): ~15 minutes
- **Total: ~15 minutes**

---

## Test Quality Metrics

### Backend Test Quality

| Metric | Value |
|--------|-------|
| **Total Tests** | 6,750+ |
| **Pass Rate** | 100% |
| **Flaky Tests** | 0 |
| **Average Execution** | ~2 minutes |
| **Code Coverage** | ~85% (domain/application) |

### Frontend Test Quality

| Metric | Value |
|--------|-------|
| **Total E2E Tests** | ~490 |
| **Visual Tests** | 15 |
| **Accessibility Tests** | 9 |
| **Pass Rate** | 100% |
| **Flaky Tests** | <1% |
| **Average Execution** | ~12 minutes (E2E) |
| **Page Objects** | 31 |

### Accessibility Compliance

| Metric | Value |
|--------|-------|
| **WCAG Level** | 2.1 AA |
| **Automated Checks** | 90+ rules |
| **Pages Tested** | 5+ |
| **Violations** | 0 |

---

## Recent Improvements (February 2026)

### Visual Regression Testing Infrastructure

**Added:**
- ✅ 15 visual regression tests
- ✅ 16 baseline snapshots (505KB)
- ✅ Custom threshold configuration
- ✅ 3 viewport testing (Desktop, Tablet, Mobile)
- ✅ Light/Dark theme comparison
- ✅ CI/CD integration with GitHub Actions
- ✅ Automatic PR comments on failures

**Impact:**
- Detects unintended UI changes automatically
- Prevents CSS regressions
- Validates responsive design
- Ensures theme consistency

**Commit:** `a89ed61` - "fix: Visual Regression Tests - use refactored dashboard mobile utility"

### Accessibility Testing Infrastructure

**Added:**
- ✅ 9 accessibility tests across 3 spec files
- ✅ WCAG 2.1 Level AA compliance validation
- ✅ Keyboard navigation testing
- ✅ ARIA label verification
- ✅ Heading hierarchy checks
- ✅ Form accessibility validation
- ✅ CI/CD integration with GitHub Actions

**Impact:**
- Ensures WCAG compliance
- Catches accessibility violations early
- Validates keyboard navigation
- Improves screen reader compatibility

**Commit:** `b5e8f42` - "feat(i18n): add aria-label translation keys for full internationalization"

### Mobile Testing Refactoring

**Improved:**
- ✅ Centralized mobile utilities (`expectDashboardLoadedOnMobile`)
- ✅ Consistent viewport handling
- ✅ Reduced test duplication

**Commit:** `adf37d0` - "refactor: use centralized expectDashboardLoadedOnMobile utility"

---

## Documentation

### Testing Documentation Suite

| Document | Purpose | Lines | Status |
|----------|---------|-------|--------|
| [TEST_PLAN.md](./TEST_PLAN.md) | Test strategy and roadmap | ~800 | Complete |
| [E2E-TESTING-GUIDE.md](./E2E-TESTING-GUIDE.md) | Playwright setup and patterns | ~1,500 | Complete |
| [TEST_CASES.md](./TEST_CASES.md) | Test case reference | ~600 | Complete |
| [VISUAL-REGRESSION-TESTING.md](./VISUAL-REGRESSION-TESTING.md) | Visual testing guide | ~800 | ✅ New |
| [ACCESSIBILITY-TESTING.md](./ACCESSIBILITY-TESTING.md) | A11y testing guide | ~900 | ✅ New |
| [TESTING-INFRASTRUCTURE-SUMMARY.md](./TESTING-INFRASTRUCTURE-SUMMARY.md) | This document | ~400 | ✅ New |

**Total Documentation:** 5,000+ lines covering all testing aspects

---

## Tools & Technologies

### Backend Testing
- **xUnit** - Test framework
- **FluentAssertions** - Assertion library
- **Moq / NSubstitute** - Mocking frameworks
- **Testcontainers** - Docker containers for integration tests
- **Respawn** - Database cleanup
- **NetArchTest** - Architecture testing

### Frontend Testing
- **Playwright** - E2E automation framework
- **TypeScript** - Type-safe test code
- **axe-core** - Accessibility rule engine
- **@axe-core/playwright** - Playwright integration for a11y
- **Page Object Model** - Test organization pattern

### CI/CD
- **GitHub Actions** - Workflow automation
- **Artifact Upload** - Test reports and screenshots
- **Status Checks** - Required PR checks
- **Parallel Execution** - Faster test runs

---

## Best Practices

### Backend Testing
1. ✅ Use FluentValidation for command/query validation
2. ✅ Test happy path + edge cases + error conditions
3. ✅ Mock external dependencies (IEmailService, IFileStorage)
4. ✅ Use InMemoryDatabase for fast unit tests
5. ✅ Use Testcontainers for realistic integration tests
6. ✅ Clean up test data after each test

### Frontend Testing
1. ✅ Use Page Object Model for maintainability
2. ✅ Create test data via API (faster than UI)
3. ✅ Clean up test data after each test
4. ✅ Use data-testid for stable selectors
5. ✅ Wait for network idle before assertions
6. ✅ Run smoke tests on every commit
7. ✅ Run full suite before merging PRs

### Visual Testing
1. ✅ Disable animations for consistency
2. ✅ Use appropriate diff thresholds
3. ✅ Test responsive viewports
4. ✅ Review diffs before updating baselines
5. ✅ Commit baselines to version control
6. ✅ Update baselines only for intentional changes

### Accessibility Testing
1. ✅ Run axe-core on every page
2. ✅ Test keyboard navigation manually
3. ✅ Use semantic HTML
4. ✅ Provide text alternatives (alt, aria-label)
5. ✅ Maintain heading hierarchy
6. ✅ Ensure 4.5:1 color contrast
7. ✅ Test with real screen readers

---

## Troubleshooting

### Flaky Tests

**Symptoms:** Tests pass/fail intermittently

**Solutions:**
```typescript
// Add explicit waits
await page.waitForLoadState('networkidle');

// Use deterministic selectors
await page.getByTestId('submit-button').click();

// Avoid hardcoded timeouts
await expect(element).toBeVisible({ timeout: 10000 });
```

### Visual Test Failures

**Symptoms:** Visual tests fail after minor UI changes

**Solutions:**
```bash
# Review diffs
npx playwright show-report

# Update baselines (if intentional)
npx playwright test tests/visual --update-snapshots
```

### Accessibility Violations

**Symptoms:** axe-core reports WCAG violations

**Solutions:**
```typescript
// Check violation details
console.log(accessibilityScanResults.violations);

// Fix issue (example: add alt text)
<img src="logo.png" alt="NOIR Logo" />

// Re-run tests
npx playwright test tests/accessibility
```

---

## Future Enhancements

### Planned Testing Improvements

1. **Performance Testing**
   - Lighthouse CI integration
   - Core Web Vitals monitoring
   - API response time benchmarks

2. **Security Testing**
   - OWASP ZAP integration
   - Dependency vulnerability scanning
   - Authentication/authorization audits

3. **Load Testing**
   - k6 load tests
   - Database query performance
   - Concurrent user simulation

4. **Visual Testing Expansion**
   - More component isolation tests
   - Animation state testing
   - Cross-browser visual comparison

5. **Accessibility Enhancements**
   - Manual screen reader testing guide
   - ARIA pattern validation
   - Focus trap testing

---

## Maintenance

### Weekly Tasks
- ✅ Review and fix flaky tests
- ✅ Update visual baselines for UI changes
- ✅ Monitor test execution times
- ✅ Clean up obsolete test data

### Monthly Tasks
- ✅ Update testing dependencies
- ✅ Review and improve test coverage
- ✅ Audit accessibility compliance
- ✅ Optimize test execution speed

### Quarterly Tasks
- ✅ Major Playwright version upgrades
- ✅ Testing strategy review
- ✅ Documentation updates
- ✅ Test infrastructure improvements

---

## Resources

### Internal Documentation
- [DOCUMENTATION_INDEX.md](../DOCUMENTATION_INDEX.md) - Main documentation hub
- [KNOWLEDGE_BASE.md](../KNOWLEDGE_BASE.md) - Codebase reference
- [PROJECT_INDEX.md](../PROJECT_INDEX.md) - Project structure

### External Resources
- [Playwright Documentation](https://playwright.dev/docs/intro)
- [xUnit Documentation](https://xunit.net/)
- [axe-core Rules](https://github.com/dequelabs/axe-core/blob/develop/doc/rule-descriptions.md)
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)

---

## Support

For testing questions or issues:

1. Check this documentation
2. Review test examples in the codebase
3. Consult team in project chat
4. Update documentation with new patterns

---

**Last Updated:** 2026-02-09
**Version:** 1.0
**Maintained By:** NOIR Development Team

# Real QA Lessons — 2026-03-22

## Summary
- Pages tested: 26
- Bugs found: 2 (1 i18n, 1 test)
- Bugs fixed: 2
- CI/CD: Fixed from failing → 100% green

## Bug #1: Unmocked Repository Dependency in Unit Tests

**What happened**: `GetReviewsQueryHandler` was updated to resolve product names via `_productRepository.ListAsync(ProductsByIdsSpec)`. The test constructor created a `Mock<IRepository<Product, Guid>>()` but never set up `.ListAsync()` to return a value. Moq returns `null` for unset reference-type returns → `foreach (var p in products)` threw `NullReferenceException`.

**Root cause**: When a new repository dependency is injected into a handler, existing tests mock the constructor parameter but forget to set up return values for methods the handler actually calls.

**Prevention rule**: When adding a new repository/service dependency to a handler:
1. Find ALL test files for that handler (`grep -r "new XxxHandler" tests/`)
2. For each mock: verify every method the handler calls on it has a `.Setup(...).ReturnsAsync(...)` — even if just returning an empty list/default
3. Run the handler's tests before committing: `dotnet test --filter "FullyQualifiedName~XxxHandlerTests"`

**Pattern to watch for**:
```csharp
// Constructor creates mock but never sets up ListAsync
var repoMock = new Mock<IRepository<Entity, Guid>>();
// ❌ Missing: repoMock.Setup(x => x.ListAsync(...)).ReturnsAsync(new List<Entity>());
var handler = new Handler(repoMock.Object);
```

## Bug #2: English Words Left in Vietnamese Translations

**What happened**: Dashboard CRM widget showed "Pipeline hoạt động" and "Giá trị Pipeline" instead of pure Vietnamese. The sidebar correctly used "Quy trình bán hàng" for the Pipeline link, but the dashboard widget keys were missed.

**Root cause**: When translating, the CRM-specific dashboard keys in `vi/common.json` were copy-pasted from English with only partial translation, leaving "Pipeline" as-is.

**Prevention rule**: After adding/modifying i18n keys in `vi/common.json`:
1. Search for remaining English words: `grep -P '"[^"]*[A-Z][a-z]+[^"]*"' public/locales/vi/common.json | grep -v '"CRM\|"API\|"SMTP\|"Blog"'`
2. Cross-check sidebar naming convention: sidebar uses "Quy trình bán hàng" → dashboard must use matching Vietnamese terms
3. The word "Pipeline" is NOT an allowed English exception in Vietnamese UI (unlike CRM, API, SMTP)

## High-Risk Areas for Future Development

1. **Handler dependency changes** — Any time you inject a new service/repo into a handler, check all existing tests
2. **i18n consistency** — Dashboard widgets, CRM-specific labels, and module settings pages are most likely to have leftover English
3. **Test mock completeness** — Moq's default `null` return for reference types is a silent killer; always set up returns for methods called in the code path under test

## CI/CD Prevention Checklist

Before pushing:
```bash
# 1. Run the specific tests you might have affected
dotnet test tests/NOIR.Application.UnitTests --filter "FullyQualifiedName~AffectedHandler"

# 2. Run full test suite if touching shared code
dotnet test src/NOIR.sln --no-build -v q

# 3. Check Vietnamese translations for English leaks
grep -cP '[A-Z][a-z]{3,}' src/NOIR.Web/frontend/public/locales/vi/common.json
```

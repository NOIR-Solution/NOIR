---
name: noir-seo-check
description: Verify SEO + social-sharing metadata on NOIR public-facing pages (blog posts, product pages, category pages, landing pages). Use when the user asks to audit SEO, add meta tags, improve search ranking, add Open Graph / Twitter Card / JSON-LD / Schema.org, check structured data, or ensure rich snippets work. Complements the `seo-audit` + `schema-markup` skills from `marketing-skills` plugin with NOIR-specific DTO knowledge and enforcement of `docs/backend/research/seo-meta-and-hint-text-best-practices.md`.
---

# noir-seo-check — SEO + structured data for NOIR public pages

NOIR has significant public-facing surface area: blog posts, product detail pages, product category pages, public tenant landing. Each needs proper metadata to rank and share well. This skill walks a full audit + remediation for a given page type.

## When to invoke

User says anything like:
- "audit SEO for the blog"
- "the product pages don't have Open Graph tags"
- "Google doesn't show rich snippets for our reviews"
- "generate structured data for X"
- "check meta descriptions"
- "fix sitemap"

## Prerequisites

- The `marketing-skills` plugin (from `coreyhaines31/marketingskills`) is installed — it provides generic `seo-audit` + `schema-markup` workflows. This skill layers NOIR-specific knowledge on top.
- Reference doc: `docs/backend/research/seo-meta-and-hint-text-best-practices.md` — the team's SEO principles. Read this before audit.

## Page types + what to check

| Page type | Required meta | Required structured data | NOIR DTO source |
|---|---|---|---|
| **Blog post** | title ≤60, meta description 120–160, canonical URL, og:title, og:description, og:image, og:type=article, twitter:card | `Article` with author, datePublished, dateModified, image, headline | `BlogPostDto` — title, excerpt, featuredImageUrl, publishedAt, author |
| **Product detail** | title, description, canonical, og:* | `Product` with offers, priceCurrency, price, availability, aggregateRating if reviews exist | `ProductDto` + `ProductVariantDto` + aggregated review data |
| **Product category** | title, description, canonical | `ItemList` of product URLs + `BreadcrumbList` | `ProductCategoryDto` + top N products |
| **Blog category / tag** | title, description, canonical | `Blog` + `BreadcrumbList` | `BlogCategoryDto` / `PostTagDto` |
| **Reviews (per product)** | n/a (embedded) | `Review` aggregated into parent `Product.aggregateRating` | `ReviewDto` |
| **Tenant landing / marketing** | title, description, canonical, og:*, twitter:card | `Organization` or `WebSite` with potentialAction (SearchAction) | Tenant settings |

## Workflow

### Phase 1 — Audit (read-only)

1. **Run the generic audit first.** If the `seo-audit` skill from `marketing-skills` is available, invoke it to get the baseline (broken links, missing meta, duplicate titles).
2. **Check the NOIR-specific dimensions:**
   - Every public page component has a `<Helmet>` or `react-helmet-async` block OR equivalent (meta tags rendered server-side for bot visibility — if client-rendered only, Google + social crawlers often can't read them)
   - **Check SSR strategy:** grep for `react-helmet`, `Head`, or `<title>` dynamic binding. If the frontend is pure CSR and there's no SSR/prerender layer, SEO is fundamentally broken — flag this as HIGH severity
   - Meta description in `BlogPost` / `Product` entity has been populated (seeder, admin editor, or auto-generated)
   - `featuredImageUrl` / `thumbnailUrl` is absolute (not relative) for og:image
   - `slug` column has a unique-per-tenant index (canonical URL correctness)
3. **Check structured data:**
   - Are JSON-LD `<script type="application/ld+json">` blocks rendered?
   - Do they validate against schema.org? Test with the [Rich Results Test](https://search.google.com/test/rich-results) tool
   - For products with reviews: is `aggregateRating` aggregated from `ReviewDto`?
4. **Check sitemap:**
   - `GET /sitemap.xml` returns valid XML (if NOIR has one)
   - Includes all public blog posts + products + categories
   - `<lastmod>` reflects entity `ModifiedAt`
   - Robots.txt points at the sitemap
5. **Check Core Web Vitals impact:**
   - Image dimensions set (prevents CLS)
   - Fonts preloaded
   - Critical CSS inlined (unless Vite's default + Tailwind coverage is sufficient)

### Phase 2 — Report

Output a findings table:

| Severity | Page type | Issue | Fix |
|---|---|---|---|
| HIGH | Blog post | No SSR — meta invisible to Googlebot | Add prerender or move blog to SSR route |
| HIGH | Product | Missing JSON-LD | Add `Product` structured data component |
| MEDIUM | Blog post | Meta description empty on 42 posts | Backfill via seeder or admin UI |
| LOW | Product | og:image uses relative URL | Prefix with absolute base URL |

### Phase 3 — Remediation (with user consent)

For each HIGH/MEDIUM issue:
1. Identify the affected files (grep for the page component)
2. Show the diff before applying
3. Update BOTH the page component AND any relevant DTO/entity (e.g. add `MetaTitle`, `MetaDescription`, `OgImageUrl` columns to `BlogPost` via migration if missing)
4. If backfilling DB data: write a one-time seeder or Hangfire job, don't update rows manually
5. Add/update i18n keys if the meta is multi-locale (EN + VI)

### Phase 4 — Verify

- View-source the page — all tags present
- Run the Rich Results Test on a representative URL
- Re-run `audit-website` / `seo-audit` — deltas should show only fixes
- Update `docs/backend/research/seo-meta-and-hint-text-best-practices.md` if a new pattern emerged

## Anti-patterns this skill prevents

- **Client-rendered meta tags** — React setting `<title>` only on mount; Googlebot sees the pre-render HTML which has the default title. If not SSR'd or prerendered, you must either migrate the public routes to SSR, add a prerender service (Prerender.io, Rendertron), or generate static meta at build time.
- **Relative og:image URLs** — Facebook/Twitter/LinkedIn unfurl uses the origin from their crawler, not the page. Always absolute.
- **Duplicate canonical URLs** — tenant-slug pages that share a slug across tenants (e.g. `acme/products/widget` and `globex/products/widget`) need tenant-scoped canonicals. Rule 18 (tenant-scoped unique indexes) supports this at the DB layer; the canonical URL must reflect the tenant subdomain or path.
- **Missing `aggregateRating`** — you have `ReviewDto` data; aggregate it into `Product` structured data, don't leave it as separate `Review` blocks
- **Over-stuffed keywords meta** — Google ignores `<meta name="keywords">` entirely; don't bother
- **Boilerplate meta descriptions** — "Welcome to our site" pulls zero click-through. Generate per-entity or require editors to write one
- **Empty `ModifiedAt`** — sitemap `<lastmod>` defaults to `CreatedAt`, signaling stale content. Use `ModifiedAt` from audit columns (Rule 11 + audit columns standard)

## Cross-references

- `marketing-skills` plugin → `seo-audit`, `schema-markup`, `site-architecture`, `programmatic-seo`, `analytics-tracking`
- `accessibility-compliance` plugin → alt-text and ARIA issues overlap with SEO (image alt, heading hierarchy)
- `ui-audit` project skill → catches accessibility issues that also hurt SEO
- `docs/backend/research/seo-meta-and-hint-text-best-practices.md` — team's SEO principles (living doc)

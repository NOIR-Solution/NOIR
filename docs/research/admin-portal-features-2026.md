# Research Report: Essential Admin Portal Features for ERP/CMS

> **Date:** January 2026
> **Purpose:** Identify essential features for enterprise admin portals
> **Scope:** Blog scheduling UX, platform settings, dashboard, media library

---

## Executive Summary

Based on research into leading CMS/admin frameworks (ABP Framework, Strapi, Directus, Craft CMS, CoreUI, AdminLTE), this report identifies **10 essential features** for NOIR's admin portal, prioritized by business value and implementation complexity.

**Key Finding:** NOIR already has strong foundations (users, roles, tenants, blog, audit logging). The gaps are primarily in:
1. **Dashboard analytics** (current dashboard is placeholder)
2. **Platform-level settings management**
3. **Blog scheduling UX improvements**
4. **Media library browsing UI**

---

## Research Findings

### 1. Blog Post Scheduling UX (Single Save Button Pattern)

**Industry Patterns Observed:**

| CMS | Pattern | UX Approach |
|-----|---------|-------------|
| **Strapi** | Draft → Scheduled → Published | Single "Save" button + optional schedule date picker |
| **Sanity** | Draft → Scheduled → Published | Moving toward "Content Releases" for scheduling |
| **Directus** | Content versioning + scheduling | Timeline-based scheduling with visual indicators |
| **Ghost** | Draft → Scheduled → Published | Date picker in sidebar, single publish action |

**Recommended UX Pattern for NOIR:**

```
┌─────────────────────────────────────────────────────────────┐
│  Post Editor                                                │
├─────────────────────────────────────────────────────────────┤
│  Title: [__________________________________]                │
│  Content: [...TinyMCE Editor...]                            │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Publishing Options                                   │   │
│  ├─────────────────────────────────────────────────────┤   │
│  │ ○ Save as Draft (not published)                     │   │
│  │ ○ Publish immediately                               │   │
│  │ ○ Schedule for: [📅 2026-01-25 09:00 ▼]            │   │
│  │                                                      │   │
│  │ ℹ️ Scheduled posts auto-publish at the set time    │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  [💾 Save]                                                  │
└─────────────────────────────────────────────────────────────┘
```

**Key UX Principles:**
- **Single "Save" button** - behavior determined by radio selection
- **Clear status indicators** - badge showing Draft/Scheduled/Published
- **Inline hints** - explain what each option does
- **Date picker** - only visible when "Schedule" is selected
- **Visual timeline** - show scheduled date prominently in post list

**NOIR Current State:**
- ✅ Has `ScheduledPublishAt` field in Post entity
- ✅ Has `Schedule()` method in Post entity
- ✅ Has `PublishPostCommand` with scheduling support
- ❌ **Missing**: Frontend UX to select schedule date in single save flow

---

### 2. Platform Settings Management

**ABP Framework Pattern:**
```
Settings Management
├── Application Settings (global)
├── Tenant Settings (per-tenant overrides)
└── User Settings (personal preferences)
```

**Directus Pattern:**
- "Manage global settings with singletons" - single-row tables for site config
- Policy-based access control for settings

**Recommended NOIR Structure:**

```
Platform Settings (Platform Admin only)
├── Branding
│   ├── Site Name
│   ├── Logo URL
│   └── Favicon URL
├── Email (SMTP Configuration)
│   ├── SMTP Host
│   ├── SMTP Port
│   ├── SMTP Username/Password
│   ├── Enable SSL
│   └── Default From Email/Name
├── Security
│   ├── Password Policy (min length, complexity)
│   ├── Lockout Policy (attempts, duration)
│   └── Session Timeout
└── Features (Feature Flags)
    ├── Enable Blog Module
    ├── Enable Public Registration
    └── Maintenance Mode
```

**Implementation Approach:**
- Store in database (PlatformSetting entity or extend existing Settings)
- Cache settings with FusionCache
- Provide typed settings service: `IPlatformSettingsService`
- UI: Tabbed settings page with grouped options

---

### 3. Dashboard Analytics

**CoreUI Features:**
- Chart.js integration for data visualization
- Statistics widgets (cards showing KPIs)
- Recent activity feeds
- Quick action buttons

**AdminLTE Features:**
- Small/Info boxes for statistics
- Timeline component
- Direct Chat integration
- Todo List widget

**Recommended NOIR Dashboard:**

```
┌─────────────────────────────────────────────────────────────────┐
│  Dashboard                                                       │
├──────────────────────┬──────────────────────┬───────────────────┤
│  📊 Users            │  📝 Blog Posts       │  🏢 Tenants       │
│  Total: 156          │  Published: 45       │  Active: 12       │
│  Active: 142         │  Drafts: 8           │  Trial: 3         │
│  New (7d): +12       │  Scheduled: 3        │  New (30d): +2    │
├──────────────────────┴──────────────────────┴───────────────────┤
│  📈 Activity This Week                                          │
│  [========Bar Chart showing daily activity=========]            │
├─────────────────────────────────────────────────────────────────┤
│  🕐 Recent Activity                                             │
│  • User john@example.com created - 2 min ago                    │
│  • Blog post "Getting Started" published - 1 hour ago           │
│  • Tenant "Acme Corp" created - 3 hours ago                     │
├─────────────────────────────────────────────────────────────────┤
│  ⚡ Quick Actions                                               │
│  [+ New User] [+ New Post] [📊 View Reports] [⚙️ Settings]      │
└─────────────────────────────────────────────────────────────────┘
```

**Required Backend Endpoints:**
- `GET /api/dashboard/stats` - aggregate counts
- `GET /api/dashboard/activity-chart` - time-series data for charts
- `GET /api/dashboard/recent-activity` - last N audit events

---

### 4. Media Library

**Strapi Media Library Features:**
- Upload images, videos, audio, documents
- Editing and optimization capabilities
- Search and filter by type
- Folder organization (Pro)

**Craft CMS Media Features:**
- Centralized asset management
- Custom fields on assets
- Cloud storage integration

**NOIR Current State:**
- ✅ Has `ImageProcessingService` - uploads and processes images
- ✅ Has `ImageMetadata` entity - stores processed images with variants
- ❌ **Missing**: Browse/search UI for uploaded media

**Recommended Media Library UI:**

```
┌─────────────────────────────────────────────────────────────────┐
│  Media Library                           [🔍 Search] [📤 Upload]│
├──────────┬──────────────────────────────────────────────────────┤
│ Filters  │  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐            │
│ ○ All    │  │ 📷  │ │ 📷  │ │ 📷  │ │ 📷  │ │ 📷  │            │
│ ○ Images │  │img1 │ │img2 │ │img3 │ │img4 │ │img5 │            │
│ ○ Docs   │  └─────┘ └─────┘ └─────┘ └─────┘ └─────┘            │
│          │  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐            │
│ Sort by: │  │ 📷  │ │ 📷  │ │ 📷  │ │ 📷  │ │ 📷  │            │
│ [Date ▼] │  │img6 │ │img7 │ │img8 │ │img9 │ │img10│            │
│          │  └─────┘ └─────┘ └─────┘ └─────┘ └─────┘            │
├──────────┴──────────────────────────────────────────────────────┤
│  Selected: img3.jpg | 1920x1080 | 245 KB | [Copy URL] [Delete] │
└─────────────────────────────────────────────────────────────────┘
```

---

### 5. Data Export

**Directus Features:**
- Import/export for CSV and JSON formats
- Bulk update capabilities

**ABP Framework:**
- Background job-based exports
- Excel/CSV generation

**Recommended NOIR Exports:**
- Export users to CSV/Excel
- Export audit logs (with date range filter)
- Export blog posts (JSON or HTML archive)

---

## Prioritized Feature List (5-10 Features)

Based on research and your requirements for essential Portal/Admin features:

### Tier 1: Must-Have (Phase 5)

| # | Feature | Complexity | Value | Description |
|---|---------|------------|-------|-------------|
| 1 | **Dashboard with Analytics** | Medium | High | Statistics widgets, activity chart, recent activity |
| 2 | **Platform SMTP Settings** | Low | High | UI for platform admin to configure email (already have backend) |
| 3 | **Blog Scheduling UX** | Low | Medium | Single save button with publish date picker |
| 4 | **Media Library Browse** | Medium | Medium | Browse/search uploaded images, copy URLs |

### Tier 2: Should-Have (Phase 6)

| # | Feature | Complexity | Value | Description |
|---|---------|------------|-------|-------------|
| 5 | **Platform Branding Settings** | Low | Medium | Site name, logo, favicon configuration |
| 6 | **Platform Security Settings** | Low | Medium | UI for password policy, lockout policy |
| 7 | **Data Export** | Medium | Medium | Export users/audit logs to CSV |

### Tier 3: Nice-to-Have (Future)

| # | Feature | Complexity | Value | Description |
|---|---------|------------|-------|-------------|
| 8 | **System Announcements** | Medium | Low | Banner management for admin portal |
| 9 | **Feature Flags UI** | Low | Low | Toggle features on/off |
| 10 | **Scheduled Reports** | High | Medium | Auto-generate and email reports |

---

## Implementation Recommendations

### Blog Scheduling UX (Simplified)

**Current Flow:**
1. Create/Edit post → Save button
2. Separate "Publish" button/endpoint

**Proposed Flow:**
1. Create/Edit post with publishing options inline
2. Single "Save" button that respects selected option:
   - Draft (no PublishedAt)
   - Publish Now (PublishedAt = now)
   - Schedule (PublishedAt = selected date)

**Backend:** Already supported via `PublishPostCommand`
**Frontend:** Add radio buttons + date picker to PostEditorPage

### Platform Settings

**Recommended Entity:**
```csharp
public class PlatformSetting : Entity<Guid>
{
    public string Key { get; set; }        // e.g., "Smtp:Host"
    public string Value { get; set; }      // encrypted for sensitive
    public string? Description { get; set; }
    public bool IsEncrypted { get; set; }
}
```

**Or:** Extend existing `appsettings.json` override pattern with database-backed settings.

---

## Conclusion

NOIR has a strong foundation. The recommended next phase focuses on:

1. **Dashboard** - Gives immediate visibility into system health
2. **Platform Settings (SMTP)** - Per your request, platform admin configurable
3. **Blog Scheduling UX** - Simplify to single save with date picker
4. **Media Library** - Essential for any CMS

These 4 features would make NOIR's admin portal complete for typical ERP/CMS use cases.

---

## Sources

- [ABP Framework Modules](https://abp.io/docs/en/abp/latest/Modules/Index)
- [Strapi Features](https://strapi.io/features)
- [Directus Features](https://directus.io/features)
- [Craft CMS Features](https://craftcms.com/features)
- [CoreUI React Template](https://coreui.io/product/free-react-admin-template/)
- [AdminLTE Documentation](https://adminlte.io/docs/3.2/)
- [Sanity Scheduling API](https://www.sanity.io/docs/scheduling-api)

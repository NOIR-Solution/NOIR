# Date/Time Formatting Standard

## Rule

ALL date/time display in authenticated pages MUST use `useRegionalSettings()` formatters. Never raw `toLocaleString()`, `toLocaleDateString()`, `toLocaleTimeString()`, or date-fns `format()`/`formatDistanceToNow()` for display.

## Required Pattern

```tsx
const { formatDate, formatDateTime, formatTime, formatRelativeTime } = useRegionalSettings()

// In JSX:
{formatDate(item.createdAt)}        // Date only (DD/MM/YYYY or tenant format)
{formatDateTime(item.updatedAt)}    // Date + time
{formatRelativeTime(item.timestamp)} // "2h ago", "Yesterday"
```

## UIKit Components (DatePicker, DateRangePicker)

Pass `formatDate` prop from parent — UIKit components do NOT import context directly:

```tsx
const { formatDate } = useRegionalSettings()
<DatePicker formatDate={formatDate} ... />
<DateRangePicker formatDate={formatDate} ... />
```

## Exceptions

| Context | What to do |
|---------|-----------|
| Developer logs `formatTimestamp`/`formatFullTimestamp` | en-US locale OK (technical format), MUST pass `timezone` param |
| Public pages (terms, privacy) | `useRegionalSettingsOptional()` with Intl fallback |
| date-fns for date MATH only | `subDays`, `startOfDay`, `parseISO` OK — never for display |
| Hooks (useAutoSave) | Explicit `Intl.DateTimeFormat` options with browser locale (no context access) |

## Bug This Prevents

Seeder used `dd/MM/yyyy` (lowercase) but frontend `getLocaleForFormat()` checked `DD/MM/YYYY` (uppercase) — all Vietnamese tenants silently got ISO format instead of DD/MM/YYYY.

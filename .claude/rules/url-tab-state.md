# URL Tab State Convention

All tabbed pages MUST use `useUrlTab()` hook from `@/hooks/useUrlTab` for URL-synced tab state.

```tsx
const { activeTab, handleTabChange, isPending } = useUrlTab({ defaultTab: 'overview' })
```

- Enables direct linking, bookmarking, and sharing of specific tab views
- Default tab is omitted from URL for cleaner URLs
- Uses `replace: true` to avoid browser history pollution
- Preserves existing search params (functional updater)
- Built-in `useTransition` for pending state (use `isPending` for opacity transition)

**Applies to:** Pages with `<Tabs>`. Does NOT apply to dialog/popup tabs (transient UI).

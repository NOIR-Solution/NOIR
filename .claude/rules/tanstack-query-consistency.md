# TanStack Query Consistency Rules

## MANDATORY: All Server Data Fetching Must Use TanStack Query

Every component that reads server data MUST use `useQuery` (or `useSuspenseQuery`/`useInfiniteQuery`). Never use `useState + useEffect + apiClient()` for data fetching.

## Prohibited Pattern

```tsx
// BAD — raw fetch in component
const [data, setData] = useState(null)
const [loading, setLoading] = useState(true)
useEffect(() => {
  fetchSomething().then(setData).finally(() => setLoading(false))
}, [])
```

## Required Pattern

```tsx
// GOOD — TanStack Query hook
const { data, isLoading } = useSomethingQuery()
```

## How to Add a New Query

1. **Query keys** in `queries/queryKeys.ts` (per domain):
   ```ts
   export const entityKeys = {
     all: ['entities'] as const,
     lists: () => [...entityKeys.all, 'list'] as const,
     list: (params: Params) => [...entityKeys.lists(), params] as const,
     detail: (id: string) => [...entityKeys.all, 'detail', id] as const,
   }
   ```

2. **Query hook** in `queries/use{Domain}Queries.ts`:
   ```ts
   export const useEntityQuery = (id: string | undefined) =>
     useQuery({
       queryKey: entityKeys.detail(id!),
       queryFn: () => getEntityById(id!),
       enabled: !!id,
     })
   ```

3. **Mutation hook** in `queries/use{Domain}Mutations.ts`:
   ```ts
   export const useUpdateEntity = () => {
     const queryClient = useQueryClient()
     return useMutation({
       mutationFn: (request: UpdateRequest) => updateEntity(request),
       onSuccess: () => {
         queryClient.invalidateQueries({ queryKey: entityKeys.all })
       },
     })
   }
   ```

## Dialog Data Loading

Dialogs that load data on open MUST use `enabled` flag:
```tsx
const { data, isLoading } = useEntityDetailQuery(id, open && !!id)
```

## Exceptions (NOT required to use TanStack Query)

- File uploads (`useImageUpload`, RichTextEditor `onImageUpload`)
- Real-time streams (SSE, WebSocket, SignalR — e.g., `useLogStream`)
- Auth login/logout flows (one-time redirecting operations)
- Service files in `src/services/` (these are `queryFn` implementations)
- Lazy-loading JS libraries (Shiki, Mermaid, d3) — not API calls
- On-demand tooltip data with shared caching (e.g., `RolePermissionInfo`)

## Quick Checklist

- [ ] No `useEffect` + `apiClient()`/`fetch()` in components
- [ ] No `setLoading(true/false)` around API calls — use `isLoading` from query
- [ ] No `setSaving(true/false)` around mutations — use `mutation.isPending`
- [ ] Query keys follow hierarchical factory pattern
- [ ] Mutations invalidate correct query keys via `queryClient.invalidateQueries()`

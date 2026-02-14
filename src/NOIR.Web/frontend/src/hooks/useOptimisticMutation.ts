import type { QueryClient, QueryKey } from '@tanstack/react-query'

/**
 * Shared optimistic mutation helpers for TanStack Query.
 *
 * Provides type-safe onMutate / onError / onSettled callbacks that can be
 * spread into useMutation options. Two data shapes are supported:
 *
 *  - **Paginated lists** (`{ items: T[], totalCount, … }`)
 *    → `optimisticListDelete`, `optimisticListPatch`
 *  - **Flat arrays** (`T[]`)
 *    → `optimisticArrayDelete`
 *
 * Usage:
 * ```ts
 * useMutation({
 *   mutationFn: (id: string) => deleteProduct(id),
 *   ...optimisticListDelete(queryClient, productKeys.lists(), productKeys.all),
 * })
 * ```
 */

// ---------------------------------------------------------------------------
// Internal types
// ---------------------------------------------------------------------------

/** Minimal structural type shared by all XxxPagedResult / PaginatedResponse types. */
interface PagedData {
  items: { id: string }[]
  totalCount: number
}

type Snapshot = [QueryKey, unknown][]

// ---------------------------------------------------------------------------
// Internal helpers
// ---------------------------------------------------------------------------

const snapshotAndCancel = async (
  qc: QueryClient,
  key: readonly unknown[],
): Promise<Snapshot> => {
  await qc.cancelQueries({ queryKey: key })
  return qc.getQueriesData({ queryKey: key })
}

const restore = (qc: QueryClient, entries: Snapshot) => {
  for (const [key, data] of entries) {
    qc.setQueryData(key, data)
  }
}

interface OptimisticContext {
  prev: Snapshot
}

const makeRollback = (qc: QueryClient) =>
  (_err: unknown, _v: string, ctx?: OptimisticContext) => {
    if (ctx?.prev) restore(qc, ctx.prev)
  }

// ---------------------------------------------------------------------------
// Public API
// ---------------------------------------------------------------------------

/**
 * Optimistic delete from a **paginated list** cache (`{ items[], totalCount }`).
 * Removes the item and decrements `totalCount` immediately.
 */
export const optimisticListDelete = (
  qc: QueryClient,
  listKey: readonly unknown[],
  allKey: readonly unknown[],
) => ({
  onMutate: async (id: string): Promise<OptimisticContext> => {
    const prev = await snapshotAndCancel(qc, listKey)
    qc.setQueriesData<PagedData>({ queryKey: listKey }, (old) => {
      if (!old?.items) return old
      return {
        ...old,
        items: old.items.filter((i) => i.id !== id),
        totalCount: old.totalCount - 1,
      }
    })
    return { prev }
  },
  onError: makeRollback(qc),
  onSettled: () => {
    qc.invalidateQueries({ queryKey: allKey })
  },
})

/**
 * Optimistic field patch on a **paginated list** cache.
 * Merges `patch` into the matching item (by id) immediately.
 *
 * Example: `optimisticListPatch(qc, keys.lists(), keys.all, { status: 'Active' })`
 */
export const optimisticListPatch = (
  qc: QueryClient,
  listKey: readonly unknown[],
  allKey: readonly unknown[],
  patch: Record<string, unknown>,
) => ({
  onMutate: async (id: string): Promise<OptimisticContext> => {
    const prev = await snapshotAndCancel(qc, listKey)
    qc.setQueriesData<PagedData>({ queryKey: listKey }, (old) => {
      if (!old?.items) return old
      return {
        ...old,
        items: old.items.map((i) => (i.id === id ? { ...i, ...patch } : i)),
      }
    })
    return { prev }
  },
  onError: makeRollback(qc),
  onSettled: () => {
    qc.invalidateQueries({ queryKey: allKey })
  },
})

/**
 * Optimistic delete from a **flat array** cache (`T[]`).
 * Filters out the item immediately.
 */
export const optimisticArrayDelete = (
  qc: QueryClient,
  listKey: readonly unknown[],
  allKey: readonly unknown[],
) => ({
  onMutate: async (id: string): Promise<OptimisticContext> => {
    const prev = await snapshotAndCancel(qc, listKey)
    qc.setQueriesData<{ id: string }[]>({ queryKey: listKey }, (old) => {
      if (!old) return old
      return old.filter((i) => i.id !== id)
    })
    return { prev }
  },
  onError: makeRollback(qc),
  onSettled: () => {
    qc.invalidateQueries({ queryKey: allKey })
  },
})

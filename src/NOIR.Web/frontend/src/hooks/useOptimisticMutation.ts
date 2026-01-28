import { useMutation, useQueryClient, type QueryKey } from '@tanstack/react-query'
import { toast } from 'sonner'

interface OptimisticMutationOptions<TData, TVariables, TContext = unknown> {
  /** The mutation function to execute */
  mutationFn: (variables: TVariables) => Promise<TData>
  /** Query key(s) to invalidate and update optimistically */
  queryKey: QueryKey
  /** Function to optimistically update the cache before the mutation completes */
  optimisticUpdate?: (
    currentData: TData | undefined,
    variables: TVariables
  ) => TData | undefined
  /** Success message to show (optional) */
  successMessage?: string
  /** Error message to show (optional, defaults to error message from API) */
  errorMessage?: string
  /** Callback on success */
  onSuccess?: (data: TData, variables: TVariables, context: TContext | undefined) => void
  /** Callback on error */
  onError?: (error: Error, variables: TVariables, context: TContext | undefined) => void
  /** Callback when mutation settles (success or error) */
  onSettled?: (
    data: TData | undefined,
    error: Error | null,
    variables: TVariables,
    context: TContext | undefined
  ) => void
}

/**
 * Hook for mutations with optimistic updates
 * Automatically handles:
 * - Optimistic cache updates
 * - Rollback on error
 * - Toast notifications
 * - Query invalidation
 */
export function useOptimisticMutation<TData, TVariables, TContext = { previousData: TData | undefined }>({
  mutationFn,
  queryKey,
  optimisticUpdate,
  successMessage,
  errorMessage,
  onSuccess,
  onError,
  onSettled,
}: OptimisticMutationOptions<TData, TVariables, TContext>) {
  const queryClient = useQueryClient()

  return useMutation<TData, Error, TVariables, TContext>({
    mutationFn,

    onMutate: async (variables) => {
      // Cancel any outgoing refetches to avoid overwriting optimistic update
      await queryClient.cancelQueries({ queryKey })

      // Snapshot the previous value
      const previousData = queryClient.getQueryData<TData>(queryKey)

      // Optimistically update the cache
      if (optimisticUpdate && previousData !== undefined) {
        const newData = optimisticUpdate(previousData, variables)
        if (newData !== undefined) {
          queryClient.setQueryData<TData>(queryKey, newData)
        }
      }

      // Return context with snapshot for rollback
      return { previousData } as TContext
    },

    onError: (error, variables, context) => {
      // Rollback to previous data on error
      if (context && 'previousData' in context) {
        queryClient.setQueryData(queryKey, context.previousData)
      }

      // Show error toast
      const message = errorMessage || error.message || 'An error occurred'
      toast.error(message)

      // Call custom error handler
      onError?.(error, variables, context)
    },

    onSuccess: (data, variables, context) => {
      // Show success toast
      if (successMessage) {
        toast.success(successMessage)
      }

      // Call custom success handler
      onSuccess?.(data, variables, context)
    },

    onSettled: (data, error, variables, context) => {
      // Always invalidate to ensure fresh data
      queryClient.invalidateQueries({ queryKey })

      // Call custom settled handler
      onSettled?.(data, error, variables, context)
    },
  })
}

/**
 * Simplified optimistic toggle mutation for boolean fields
 */
interface ToggleMutationOptions<TItem extends { id: string }> {
  /** Query key for the list of items */
  queryKey: QueryKey
  /** The mutation function */
  mutationFn: (id: string) => Promise<TItem>
  /** Field to toggle (must be boolean) */
  toggleField: keyof TItem
  /** Success message */
  successMessage?: string
}

export function useOptimisticToggle<TItem extends { id: string }>({
  queryKey,
  mutationFn,
  toggleField,
  successMessage,
}: ToggleMutationOptions<TItem>) {
  return useOptimisticMutation<TItem, string, { previousData: TItem[] | undefined }>({
    mutationFn,
    queryKey,
    successMessage,
    optimisticUpdate: (currentData, id) => {
      if (!Array.isArray(currentData)) return currentData

      return currentData.map((item) =>
        item.id === id
          ? { ...item, [toggleField]: !item[toggleField] }
          : item
      ) as TItem[] as unknown as TItem
    },
  })
}

/**
 * Simplified optimistic delete mutation
 */
interface DeleteMutationOptions<TItem extends { id: string }> {
  /** Query key for the list of items */
  queryKey: QueryKey
  /** The delete mutation function */
  mutationFn: (id: string) => Promise<void>
  /** Success message */
  successMessage?: string
}

export function useOptimisticDelete<TItem extends { id: string }>({
  queryKey,
  mutationFn,
  successMessage = 'Item deleted',
}: DeleteMutationOptions<TItem>) {
  const queryClient = useQueryClient()

  return useMutation<void, Error, string, { previousData: TItem[] | undefined }>({
    mutationFn,

    onMutate: async (id) => {
      await queryClient.cancelQueries({ queryKey })
      const previousData = queryClient.getQueryData<TItem[]>(queryKey)

      // Optimistically remove the item
      if (previousData && Array.isArray(previousData)) {
        queryClient.setQueryData<TItem[]>(
          queryKey,
          previousData.filter((item) => item.id !== id)
        )
      }

      return { previousData }
    },

    onError: (error, _id, context) => {
      // Rollback
      if (context?.previousData) {
        queryClient.setQueryData(queryKey, context.previousData)
      }
      toast.error(error.message || 'Failed to delete')
    },

    onSuccess: () => {
      toast.success(successMessage)
    },

    onSettled: () => {
      queryClient.invalidateQueries({ queryKey })
    },
  })
}

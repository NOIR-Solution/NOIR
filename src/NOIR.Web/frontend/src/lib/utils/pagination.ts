/**
 * Calculate pagination display range (from-to of total).
 * Returns { from, to } for "Showing X-Y of Z items".
 */
export const getPaginationRange = (
  currentPage: number,
  pageSize: number,
  totalItems: number
): { from: number; to: number } => {
  if (totalItems === 0) {
    return { from: 0, to: 0 }
  }

  const from = (currentPage - 1) * pageSize + 1
  const to = Math.min(currentPage * pageSize, totalItems)

  return { from, to }
}

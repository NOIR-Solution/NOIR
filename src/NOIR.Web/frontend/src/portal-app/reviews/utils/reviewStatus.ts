import { getStatusBadgeClasses } from '@/utils/statusBadge'

export const getReviewStatusColor = (status: string): string => {
  switch (status) {
    case 'Pending': return getStatusBadgeClasses('yellow')
    case 'Approved': return getStatusBadgeClasses('green')
    case 'Rejected': return getStatusBadgeClasses('red')
    default: return getStatusBadgeClasses('gray')
  }
}

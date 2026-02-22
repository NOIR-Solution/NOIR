import { getStatusBadgeClasses } from '@/utils/statusBadge'

export const getSegmentBadgeClass = (segment: string): string => {
  switch (segment) {
    case 'New': return getStatusBadgeClasses('blue')
    case 'Active': return getStatusBadgeClasses('green')
    case 'VIP': return getStatusBadgeClasses('purple')
    case 'AtRisk': return getStatusBadgeClasses('orange')
    case 'Dormant': return getStatusBadgeClasses('yellow')
    case 'Lost': return getStatusBadgeClasses('red')
    default: return getStatusBadgeClasses('gray')
  }
}

export const getTierBadgeClass = (tier: string): string => {
  switch (tier) {
    case 'Standard': return getStatusBadgeClasses('slate')
    case 'Silver': return getStatusBadgeClasses('gray')
    case 'Gold': return getStatusBadgeClasses('amber')
    case 'Platinum': return getStatusBadgeClasses('cyan')
    case 'Diamond': return getStatusBadgeClasses('violet')
    default: return getStatusBadgeClasses('gray')
  }
}

import { Badge } from '../badge/Badge'
import { cn } from '@/lib/utils'

export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE' | 'OPTIONS' | 'HEAD' | string

/**
 * HTTP Method Badge with industry-standard colors (Swagger/OpenAPI style)
 *
 * Color scheme:
 * - GET: Blue (safe, read-only, idempotent)
 * - POST: Green (creates resources)
 * - PUT: Orange (replaces/updates, idempotent)
 * - PATCH: Teal (partial updates)
 * - DELETE: Red (destructive action)
 * - OPTIONS/HEAD: Gray (metadata operations)
 */
export const getHttpMethodStyles = (method: HttpMethod): string => {
  switch (method.toUpperCase()) {
    case 'GET':
      return 'bg-blue-700 text-white border-transparent hover:bg-blue-800'
    case 'POST':
      return 'bg-emerald-700 text-white border-transparent hover:bg-emerald-800'
    case 'PUT':
      return 'bg-amber-700 text-white border-transparent hover:bg-amber-800'
    case 'PATCH':
      return 'bg-teal-700 text-white border-transparent hover:bg-teal-800'
    case 'DELETE':
      return 'bg-red-700 text-white border-transparent hover:bg-red-800'
    case 'OPTIONS':
    case 'HEAD':
      return 'bg-slate-700 text-white border-transparent hover:bg-slate-800'
    default:
      return 'bg-gray-700 text-white border-transparent hover:bg-gray-800'
  }
}

interface HttpMethodBadgeProps {
  method: HttpMethod
  className?: string
}

export const HttpMethodBadge = ({ method, className }: HttpMethodBadgeProps) => {
  return (
    <Badge className={cn('font-mono', getHttpMethodStyles(method), className)}>
      {method.toUpperCase()}
    </Badge>
  )
}

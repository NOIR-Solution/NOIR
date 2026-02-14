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
      return 'bg-blue-500 text-white border-transparent hover:bg-blue-600'
    case 'POST':
      return 'bg-emerald-500 text-white border-transparent hover:bg-emerald-600'
    case 'PUT':
      return 'bg-amber-500 text-white border-transparent hover:bg-amber-600'
    case 'PATCH':
      return 'bg-teal-500 text-white border-transparent hover:bg-teal-600'
    case 'DELETE':
      return 'bg-red-500 text-white border-transparent hover:bg-red-600'
    case 'OPTIONS':
    case 'HEAD':
      return 'bg-slate-500 text-white border-transparent hover:bg-slate-600'
    default:
      return 'bg-gray-500 text-white border-transparent hover:bg-gray-600'
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

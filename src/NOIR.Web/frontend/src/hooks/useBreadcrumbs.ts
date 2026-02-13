import { useMemo } from 'react'
import { useLocation } from 'react-router-dom'
import type { BreadcrumbItem } from '@uikit'
/**
 * Hook to generate breadcrumb items based on current route
 */
export function useBreadcrumbs(): BreadcrumbItem[] {
  const location = useLocation()

  return useMemo(() => {
    const path = location.pathname

    // Define breadcrumb configurations for routes
    const breadcrumbConfig: Record<string, BreadcrumbItem[]> = {
      // Dashboard
      '/portal': [{ label: 'Dashboard' }],

      // E-commerce
      '/portal/ecommerce/products': [
        { label: 'Portal', href: '/portal' },
        { label: 'E-commerce' },
        { label: 'Products' },
      ],
      '/portal/ecommerce/products/new': [
        { label: 'Portal', href: '/portal' },
        { label: 'E-commerce' },
        { label: 'Products', href: '/portal/ecommerce/products' },
        { label: 'New Product' },
      ],
      '/portal/ecommerce/categories': [
        { label: 'Portal', href: '/portal' },
        { label: 'E-commerce' },
        { label: 'Categories' },
      ],

      // Blog
      '/portal/blog/posts': [
        { label: 'Portal', href: '/portal' },
        { label: 'Blog' },
        { label: 'Posts' },
      ],
      '/portal/blog/posts/new': [
        { label: 'Portal', href: '/portal' },
        { label: 'Blog' },
        { label: 'Posts', href: '/portal/blog/posts' },
        { label: 'New Post' },
      ],
      '/portal/blog/categories': [
        { label: 'Portal', href: '/portal' },
        { label: 'Blog' },
        { label: 'Categories' },
      ],
      '/portal/blog/tags': [
        { label: 'Portal', href: '/portal' },
        { label: 'Blog' },
        { label: 'Tags' },
      ],

      // Admin - Users & Access
      '/portal/admin/users': [
        { label: 'Portal', href: '/portal' },
        { label: 'Users & Access' },
        { label: 'Users' },
      ],
      '/portal/admin/roles': [
        { label: 'Portal', href: '/portal' },
        { label: 'Users & Access' },
        { label: 'Roles' },
      ],
      '/portal/admin/tenants': [
        { label: 'Portal', href: '/portal' },
        { label: 'Users & Access' },
        { label: 'Tenants' },
      ],

      // Admin - Settings
      '/portal/admin/platform-settings': [
        { label: 'Portal', href: '/portal' },
        { label: 'Settings' },
        { label: 'Platform Settings' },
      ],
      '/portal/admin/tenant-settings': [
        { label: 'Portal', href: '/portal' },
        { label: 'Settings' },
        { label: 'Tenant Settings' },
      ],

      // Admin - System
      '/portal/activity-timeline': [
        { label: 'Portal', href: '/portal' },
        { label: 'System' },
        { label: 'Activity Timeline' },
      ],
      '/portal/developer-logs': [
        { label: 'Portal', href: '/portal' },
        { label: 'System' },
        { label: 'Developer Logs' },
      ],

      // User Settings
      '/portal/settings': [
        { label: 'Portal', href: '/portal' },
        { label: 'Settings' },
      ],
      '/portal/notifications': [
        { label: 'Portal', href: '/portal' },
        { label: 'Notifications' },
      ],
      '/portal/settings/notifications': [
        { label: 'Portal', href: '/portal' },
        { label: 'Settings', href: '/portal/settings' },
        { label: 'Notification Preferences' },
      ],
    }

    // Check for exact match first
    if (breadcrumbConfig[path]) {
      return breadcrumbConfig[path]
    }

    // Handle dynamic routes with params
    // Product detail/edit pages
    if (path.match(/^\/portal\/ecommerce\/products\/[^/]+$/)) {
      return [
        { label: 'Portal', href: '/portal' },
        { label: 'E-commerce' },
        { label: 'Products', href: '/portal/ecommerce/products' },
        { label: 'Product Details' },
      ]
    }
    if (path.match(/^\/portal\/ecommerce\/products\/[^/]+\/edit$/)) {
      return [
        { label: 'Portal', href: '/portal' },
        { label: 'E-commerce' },
        { label: 'Products', href: '/portal/ecommerce/products' },
        { label: 'Edit Product' },
      ]
    }

    // Blog post edit
    if (path.match(/^\/portal\/blog\/posts\/[^/]+\/edit$/)) {
      return [
        { label: 'Portal', href: '/portal' },
        { label: 'Blog' },
        { label: 'Posts', href: '/portal/blog/posts' },
        { label: 'Edit Post' },
      ]
    }

    // Tenant detail
    if (path.match(/^\/portal\/admin\/tenants\/[^/]+$/)) {
      return [
        { label: 'Portal', href: '/portal' },
        { label: 'Users & Access' },
        { label: 'Tenants', href: '/portal/admin/tenants' },
        { label: 'Tenant Details' },
      ]
    }

    // Email template edit
    if (path.match(/^\/portal\/email-templates\/[^/]+$/)) {
      return [
        { label: 'Portal', href: '/portal' },
        { label: 'Settings' },
        { label: 'Tenant Settings', href: '/portal/admin/tenant-settings' },
        { label: 'Edit Email Template' },
      ]
    }

    // Legal page edit
    if (path.match(/^\/portal\/legal-pages\/[^/]+$/)) {
      return [
        { label: 'Portal', href: '/portal' },
        { label: 'Settings' },
        { label: 'Tenant Settings', href: '/portal/admin/tenant-settings' },
        { label: 'Edit Legal Page' },
      ]
    }

    // Default fallback - just show Portal
    return [{ label: 'Portal', href: '/portal' }]
  }, [location.pathname])
}

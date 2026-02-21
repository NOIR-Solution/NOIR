import { useMemo } from 'react'
import { useLocation } from 'react-router-dom'
import type { BreadcrumbItem } from '@uikit'
/**
 * Hook to generate breadcrumb items based on current route
 */
export const useBreadcrumbs = (): BreadcrumbItem[] => {
  const location = useLocation()

  return useMemo(() => {
    const path = location.pathname
    const searchParams = new URLSearchParams(location.search)
    const fromContext = searchParams.get('from')

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
      '/portal/ecommerce/brands': [
        { label: 'Portal', href: '/portal' },
        { label: 'E-commerce' },
        { label: 'Brands' },
      ],
      '/portal/ecommerce/attributes': [
        { label: 'Portal', href: '/portal' },
        { label: 'E-commerce' },
        { label: 'Attributes' },
      ],
      '/portal/ecommerce/orders': [
        { label: 'Portal', href: '/portal' },
        { label: 'E-commerce' },
        { label: 'Orders' },
      ],
      '/portal/ecommerce/inventory': [
        { label: 'Portal', href: '/portal' },
        { label: 'E-commerce' },
        { label: 'Inventory' },
      ],
      '/portal/ecommerce/customers': [
        { label: 'Portal', href: '/portal' },
        { label: 'E-commerce' },
        { label: 'Customers' },
      ],
      '/portal/ecommerce/customer-groups': [
        { label: 'Portal', href: '/portal' },
        { label: 'E-commerce' },
        { label: 'Customer Groups' },
      ],
      '/portal/ecommerce/reviews': [
        { label: 'Portal', href: '/portal' },
        { label: 'E-commerce' },
        { label: 'Reviews' },
      ],
      '/portal/ecommerce/wishlists': [
        { label: 'Portal', href: '/portal' },
        { label: 'E-commerce' },
        { label: 'Wishlists' },
      ],
      '/portal/ecommerce/wishlists/manage': [
        { label: 'Portal', href: '/portal' },
        { label: 'E-commerce' },
        { label: 'Wishlists', href: '/portal/ecommerce/wishlists' },
        { label: 'Manage' },
      ],
      '/portal/ecommerce/shipping': [
        { label: 'Portal', href: '/portal' },
        { label: 'E-commerce' },
        { label: 'Shipping' },
      ],

      // Marketing
      '/portal/marketing/promotions': [
        { label: 'Portal', href: '/portal' },
        { label: 'Marketing' },
        { label: 'Promotions' },
      ],
      '/portal/marketing/reports': [
        { label: 'Portal', href: '/portal' },
        { label: 'Marketing' },
        { label: 'Reports' },
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

    // Order detail
    if (path.match(/^\/portal\/ecommerce\/orders\/[^/]+$/)) {
      return [
        { label: 'Portal', href: '/portal' },
        { label: 'E-commerce' },
        { label: 'Orders', href: '/portal/ecommerce/orders' },
        { label: 'Order Details' },
      ]
    }

    // Customer detail
    if (path.match(/^\/portal\/ecommerce\/customers\/[^/]+$/)) {
      return [
        { label: 'Portal', href: '/portal' },
        { label: 'E-commerce' },
        { label: 'Customers', href: '/portal/ecommerce/customers' },
        { label: 'Customer Details' },
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

    // Email template edit - context-aware breadcrumb based on ?from= param
    if (path.match(/^\/portal\/email-templates\/[^/]+$/)) {
      const isPlatformContext = fromContext === 'platform'
      return [
        { label: 'Portal', href: '/portal' },
        { label: 'Settings' },
        {
          label: isPlatformContext ? 'Platform Settings' : 'Tenant Settings',
          href: isPlatformContext
            ? '/portal/admin/platform-settings?tab=emailTemplates'
            : '/portal/admin/tenant-settings?tab=emailTemplates',
        },
        { label: 'Edit Email Template' },
      ]
    }

    // Legal page edit - context-aware breadcrumb based on ?from= param
    if (path.match(/^\/portal\/legal-pages\/[^/]+$/)) {
      const isPlatformContext = fromContext === 'platform'
      return [
        { label: 'Portal', href: '/portal' },
        { label: 'Settings' },
        {
          label: isPlatformContext ? 'Platform Settings' : 'Tenant Settings',
          href: isPlatformContext
            ? '/portal/admin/platform-settings?tab=legalPages'
            : '/portal/admin/tenant-settings?tab=legalPages',
        },
        { label: 'Edit Legal Page' },
      ]
    }

    // Default fallback - just show Portal
    return [{ label: 'Portal', href: '/portal' }]
  }, [location.pathname, location.search])
}

import { useMemo } from 'react'
import { useLocation } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import type { BreadcrumbItem } from '@uikit'

/**
 * Hook to generate breadcrumb items based on current route
 */
export const useBreadcrumbs = (): BreadcrumbItem[] => {
  const location = useLocation()
  const { t } = useTranslation('common')

  return useMemo(() => {
    const path = location.pathname
    const searchParams = new URLSearchParams(location.search)
    const fromContext = searchParams.get('from')

    // Reusable breadcrumb segments
    const portal = { label: t('nav.portal'), href: '/portal' }
    const ecommerce = { label: t('breadcrumbs.ecommerce') }
    const marketing = { label: t('nav.marketing') }
    const blog = { label: t('breadcrumbs.blog') }
    const usersAccess = { label: t('nav.usersAccess') }
    const settings = { label: t('nav.settings') }
    const system = { label: t('nav.system') }

    // Define breadcrumb configurations for routes
    const breadcrumbConfig: Record<string, BreadcrumbItem[]> = {
      // Dashboard
      '/portal': [{ label: t('breadcrumbs.dashboard') }],

      // E-commerce
      '/portal/ecommerce/products': [
        portal, ecommerce,
        { label: t('ecommerce.products') },
      ],
      '/portal/ecommerce/products/new': [
        portal, ecommerce,
        { label: t('ecommerce.products'), href: '/portal/ecommerce/products' },
        { label: t('ecommerce.newProduct') },
      ],
      '/portal/ecommerce/categories': [
        portal, ecommerce,
        { label: t('ecommerce.categories') },
      ],
      '/portal/ecommerce/brands': [
        portal, ecommerce,
        { label: t('ecommerce.brands') },
      ],
      '/portal/ecommerce/attributes': [
        portal, ecommerce,
        { label: t('ecommerce.attributes') },
      ],
      '/portal/ecommerce/orders': [
        portal, ecommerce,
        { label: t('ecommerce.orders') },
      ],
      '/portal/ecommerce/inventory': [
        portal, ecommerce,
        { label: t('ecommerce.inventory') },
      ],
      '/portal/ecommerce/customers': [
        portal, ecommerce,
        { label: t('ecommerce.customers') },
      ],
      '/portal/ecommerce/customer-groups': [
        portal, ecommerce,
        { label: t('ecommerce.customerGroups') },
      ],
      '/portal/ecommerce/reviews': [
        portal, ecommerce,
        { label: t('ecommerce.reviews') },
      ],
      '/portal/ecommerce/wishlists': [
        portal, ecommerce,
        { label: t('ecommerce.wishlists') },
      ],
      '/portal/ecommerce/wishlists/manage': [
        portal, ecommerce,
        { label: t('ecommerce.wishlists'), href: '/portal/ecommerce/wishlists' },
        { label: t('breadcrumbs.manage') },
      ],
      '/portal/ecommerce/shipping': [
        portal, ecommerce,
        { label: t('ecommerce.shipping') },
      ],

      // Marketing
      '/portal/marketing/promotions': [
        portal, marketing,
        { label: t('ecommerce.promotions') },
      ],
      '/portal/marketing/reports': [
        portal, marketing,
        { label: t('nav.reports') },
      ],

      // Blog
      '/portal/blog/posts': [
        portal, blog,
        { label: t('breadcrumbs.posts') },
      ],
      '/portal/blog/posts/new': [
        portal, blog,
        { label: t('breadcrumbs.posts'), href: '/portal/blog/posts' },
        { label: t('blog.newPost') },
      ],
      '/portal/blog/categories': [
        portal, blog,
        { label: t('ecommerce.categories') },
      ],
      '/portal/blog/tags': [
        portal, blog,
        { label: t('breadcrumbs.tags') },
      ],

      // Admin - Users & Access
      '/portal/admin/users': [
        portal, usersAccess,
        { label: t('nav.users') },
      ],
      '/portal/admin/roles': [
        portal, usersAccess,
        { label: t('breadcrumbs.roles') },
      ],
      '/portal/admin/tenants': [
        portal, usersAccess,
        { label: t('breadcrumbs.tenants') },
      ],

      // Admin - Settings
      '/portal/admin/platform-settings': [
        portal, settings,
        { label: t('breadcrumbs.platformSettings') },
      ],
      '/portal/admin/tenant-settings': [
        portal, settings,
        { label: t('breadcrumbs.tenantSettings') },
      ],

      // Admin - System
      '/portal/activity-timeline': [
        portal, system,
        { label: t('breadcrumbs.activityTimeline') },
      ],
      '/portal/developer-logs': [
        portal, system,
        { label: t('breadcrumbs.developerLogs') },
      ],

      // User Settings
      '/portal/settings': [
        portal,
        settings,
      ],
      '/portal/notifications': [
        portal,
        { label: t('nav.notifications') },
      ],
      '/portal/settings/notifications': [
        portal,
        { label: t('nav.settings'), href: '/portal/settings' },
        { label: t('breadcrumbs.notificationPreferences') },
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
        portal, ecommerce,
        { label: t('ecommerce.products'), href: '/portal/ecommerce/products' },
        { label: t('breadcrumbs.productDetails') },
      ]
    }
    if (path.match(/^\/portal\/ecommerce\/products\/[^/]+\/edit$/)) {
      return [
        portal, ecommerce,
        { label: t('ecommerce.products'), href: '/portal/ecommerce/products' },
        { label: t('breadcrumbs.editProduct') },
      ]
    }

    // Order detail
    if (path.match(/^\/portal\/ecommerce\/orders\/[^/]+$/)) {
      return [
        portal, ecommerce,
        { label: t('ecommerce.orders'), href: '/portal/ecommerce/orders' },
        { label: t('breadcrumbs.orderDetails') },
      ]
    }

    // Customer detail
    if (path.match(/^\/portal\/ecommerce\/customers\/[^/]+$/)) {
      return [
        portal, ecommerce,
        { label: t('ecommerce.customers'), href: '/portal/ecommerce/customers' },
        { label: t('breadcrumbs.customerDetails') },
      ]
    }

    // Blog post edit
    if (path.match(/^\/portal\/blog\/posts\/[^/]+\/edit$/)) {
      return [
        portal, blog,
        { label: t('breadcrumbs.posts'), href: '/portal/blog/posts' },
        { label: t('breadcrumbs.editPost') },
      ]
    }

    // Tenant detail
    if (path.match(/^\/portal\/admin\/tenants\/[^/]+$/)) {
      return [
        portal, usersAccess,
        { label: t('breadcrumbs.tenants'), href: '/portal/admin/tenants' },
        { label: t('breadcrumbs.tenantDetails') },
      ]
    }

    // Email template edit - context-aware breadcrumb based on ?from= param
    if (path.match(/^\/portal\/email-templates\/[^/]+$/)) {
      const isPlatformContext = fromContext === 'platform'
      return [
        portal, settings,
        {
          label: isPlatformContext ? t('breadcrumbs.platformSettings') : t('breadcrumbs.tenantSettings'),
          href: isPlatformContext
            ? '/portal/admin/platform-settings?tab=emailTemplates'
            : '/portal/admin/tenant-settings?tab=emailTemplates',
        },
        { label: t('breadcrumbs.editEmailTemplate') },
      ]
    }

    // Legal page edit - context-aware breadcrumb based on ?from= param
    if (path.match(/^\/portal\/legal-pages\/[^/]+$/)) {
      const isPlatformContext = fromContext === 'platform'
      return [
        portal, settings,
        {
          label: isPlatformContext ? t('breadcrumbs.platformSettings') : t('breadcrumbs.tenantSettings'),
          href: isPlatformContext
            ? '/portal/admin/platform-settings?tab=legalPages'
            : '/portal/admin/tenant-settings?tab=legalPages',
        },
        { label: t('breadcrumbs.editLegalPage') },
      ]
    }

    // Default fallback - just show Portal
    return [{ label: t('nav.portal'), href: '/portal' }]
  }, [location.pathname, location.search, t])
}

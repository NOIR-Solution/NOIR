import { useState, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams, useNavigate } from 'react-router-dom'
import {
  ArrowLeft,
  Crown,
  Eye,
  Gift,
  Mail,
  MapPin,
  Minus,
  Pencil,
  Phone,
  Plus,
  ShoppingCart,
  Star,
  Trash2,
  User,
  Users,
} from 'lucide-react'
import { toast } from 'sonner'
import { usePageContext } from '@/hooks/usePageContext'
import { useUrlTab } from '@/hooks/useUrlTab'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  EmptyState,
  PageHeader,
  Pagination,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Separator,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@uikit'
import {
  useCustomerQuery,
  useCustomerOrdersQuery,
  useUpdateCustomerSegmentMutation,
} from '@/portal-app/customers/queries'
import type { CustomerSegment, CustomerAddressDto } from '@/types/customer'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { formatCurrency } from '@/lib/utils/currency'
import { CustomerFormDialog } from '../../components/CustomerFormDialog'
import { AddressFormDialog } from '../../components/AddressFormDialog'
import { LoyaltyPointsDialog } from '../../components/LoyaltyPointsDialog'
import { DeleteCustomerDialog } from '../../components/DeleteCustomerDialog'
import { getSegmentBadgeClass, getTierBadgeClass } from '@/portal-app/customers/utils/customerStatus'

const CUSTOMER_SEGMENTS: CustomerSegment[] = ['New', 'Active', 'AtRisk', 'Dormant', 'Lost', 'VIP']

export const CustomerDetailPage = () => {
  const { t } = useTranslation('common')
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  const { formatDateTime } = useRegionalSettings()
  usePageContext('Customers')

  const canUpdate = hasPermission(Permissions.CustomersUpdate)
  const canDelete = hasPermission(Permissions.CustomersDelete)
  const canManage = hasPermission(Permissions.CustomersManage)

  const { data: customer, isLoading, error: queryError } = useCustomerQuery(id)
  const updateSegmentMutation = useUpdateCustomerSegmentMutation()
  const { activeTab, handleTabChange, isPending: isTabPending } = useUrlTab({ defaultTab: 'orders' })

  // Orders pagination
  const [orderParams, setOrderParams] = useState({ page: 1, pageSize: 10 })
  const [isOrderPending, startOrderTransition] = useTransition()
  const { data: ordersResponse, isLoading: ordersLoading } = useCustomerOrdersQuery(id, orderParams)
  const orders = ordersResponse?.items ?? []
  const ordersTotalPages = ordersResponse?.totalPages ?? 1
  const ordersCurrentPage = orderParams.page

  // Dialog states
  const [showEditDialog, setShowEditDialog] = useState(false)
  const [showDeleteDialog, setShowDeleteDialog] = useState(false)
  const [showAddressDialog, setShowAddressDialog] = useState(false)
  const [addressToEdit, setAddressToEdit] = useState<CustomerAddressDto | null>(null)
  const [loyaltyMode, setLoyaltyMode] = useState<'add' | 'redeem' | null>(null)

  const handleSegmentChange = async (segment: string) => {
    if (!customer) return
    try {
      await updateSegmentMutation.mutateAsync({
        id: customer.id,
        request: { segment: segment as CustomerSegment },
      })
      toast.success(t('customers.segmentUpdateSuccess', 'Customer segment updated'))
    } catch (err) {
      const message = err instanceof Error ? err.message : t('customers.segmentUpdateError', 'Failed to update segment')
      toast.error(message)
    }
  }

  const handleOrderPageChange = (page: number) => {
    startOrderTransition(() => {
      setOrderParams(prev => ({ ...prev, page }))
    })
  }

  if (isLoading) {
    return (
      <div className="container max-w-6xl py-6 space-y-6">
        <div className="flex items-center gap-4">
          <Skeleton className="h-10 w-10 rounded" />
          <div className="space-y-2">
            <Skeleton className="h-6 w-48" />
            <Skeleton className="h-4 w-72" />
          </div>
        </div>
        {/* Stats row */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
          {[...Array(4)].map((_, i) => (
            <Card key={i} className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardContent className="p-4 text-center">
                <Skeleton className="h-3 w-16 mx-auto mb-2" />
                <Skeleton className="h-6 w-12 mx-auto" />
              </CardContent>
            </Card>
          ))}
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2 space-y-6">
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardContent className="pt-6">
                <Skeleton className="h-4 w-32 mb-4" />
                <div className="space-y-3">
                  <Skeleton className="h-3 w-full" />
                  <Skeleton className="h-3 w-full" />
                  <Skeleton className="h-3 w-full" />
                  <Skeleton className="h-3 w-full" />
                  <Skeleton className="h-3 w-full" />
                  <Skeleton className="h-3 w-2/3" />
                </div>
              </CardContent>
            </Card>
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardContent className="pt-6">
                <Skeleton className="h-4 w-32 mb-4" />
                <div className="space-y-3">
                  <Skeleton className="h-3 w-full" />
                  <Skeleton className="h-3 w-full" />
                  <Skeleton className="h-3 w-full" />
                  <Skeleton className="h-3 w-full" />
                  <Skeleton className="h-3 w-full" />
                  <Skeleton className="h-3 w-2/3" />
                </div>
              </CardContent>
            </Card>
          </div>
          <div className="space-y-6">
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardContent className="pt-6">
                <Skeleton className="h-4 w-24 mb-4" />
                <div className="space-y-3">
                  <Skeleton className="h-3 w-full" />
                  <Skeleton className="h-3 w-full" />
                  <Skeleton className="h-3 w-2/3" />
                </div>
              </CardContent>
            </Card>
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardContent className="pt-6">
                <Skeleton className="h-4 w-24 mb-4" />
                <div className="space-y-3">
                  <Skeleton className="h-3 w-full" />
                  <Skeleton className="h-3 w-full" />
                  <Skeleton className="h-3 w-2/3" />
                </div>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    )
  }

  if (queryError || !customer) {
    return (
      <div className="container max-w-6xl py-6 space-y-6">
        <Button variant="ghost" onClick={() => navigate('/portal/ecommerce/customers')} className="cursor-pointer">
          <ArrowLeft className="h-4 w-4 mr-2" />
          {t('customers.backToCustomers', 'Back to Customers')}
        </Button>
        <div className="p-8 text-center">
          <p className="text-destructive">{queryError?.message || t('customers.customerNotFound', 'Customer not found')}</p>
        </div>
      </div>
    )
  }

  const fullName = `${customer.firstName} ${customer.lastName}`

  return (
    <div className="container max-w-6xl py-6 space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" onClick={() => navigate('/portal/ecommerce/customers')} className="cursor-pointer" aria-label={t('customers.backToCustomers', 'Back to Customers')}>
          <ArrowLeft className="h-5 w-5" />
        </Button>
        <PageHeader
          icon={Users}
          title={fullName}
          description={customer.email}
          responsive
          action={
            <div className="flex items-center gap-2">
              <Badge variant="outline" className={`text-sm px-3 py-1 ${getSegmentBadgeClass(customer.segment)}`}>
                {t(`customers.segment.${customer.segment.toLowerCase()}`, customer.segment)}
              </Badge>
              <Badge variant="outline" className={`text-sm px-3 py-1 ${getTierBadgeClass(customer.tier)}`}>
                {t(`customers.tier.${customer.tier.toLowerCase()}`, customer.tier)}
              </Badge>
            </div>
          }
        />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left Column - Main Content */}
        <div className="lg:col-span-2 space-y-6">
          {/* Stats Cards */}
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardContent className="p-4 text-center">
                <p className="text-sm text-muted-foreground">{t('customers.totalOrders', 'Total Orders')}</p>
                <p className="text-2xl font-bold">{customer.totalOrders}</p>
              </CardContent>
            </Card>
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardContent className="p-4 text-center">
                <p className="text-sm text-muted-foreground">{t('customers.totalSpent', 'Total Spent')}</p>
                <p className="text-2xl font-bold">{formatCurrency(customer.totalSpent, 'VND')}</p>
              </CardContent>
            </Card>
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardContent className="p-4 text-center">
                <p className="text-sm text-muted-foreground">{t('customers.avgOrderValue', 'Avg Order')}</p>
                <p className="text-2xl font-bold">{formatCurrency(customer.averageOrderValue, 'VND')}</p>
              </CardContent>
            </Card>
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardContent className="p-4 text-center">
                <p className="text-sm text-muted-foreground">{t('customers.loyaltyPoints', 'Points')}</p>
                <p className="text-2xl font-bold">{customer.loyaltyPoints.toLocaleString()}</p>
              </CardContent>
            </Card>
          </div>

          {/* Tabs */}
          <Tabs value={activeTab} onValueChange={handleTabChange} className={`w-full${isTabPending ? ' opacity-70 transition-opacity duration-200' : ' transition-opacity duration-200'}`}>
            <TabsList>
              <TabsTrigger value="orders" className="cursor-pointer">
                <ShoppingCart className="h-4 w-4 mr-2" />
                {t('customers.ordersTab', 'Orders')}
              </TabsTrigger>
              <TabsTrigger value="addresses" className="cursor-pointer">
                <MapPin className="h-4 w-4 mr-2" />
                {t('customers.addressesTab', 'Addresses')}
              </TabsTrigger>
            </TabsList>

            {/* Orders Tab */}
            <TabsContent value="orders">
              <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-4 py-5">
                <CardHeader>
                  <CardTitle className="text-sm flex items-center gap-2">
                    <ShoppingCart className="h-4 w-4" />
                    {t('customers.orderHistory', 'Order History')}
                  </CardTitle>
                  <CardDescription>
                    {t('customers.orderCount', { count: ordersResponse?.totalCount ?? 0, defaultValue: `${ordersResponse?.totalCount ?? 0} orders` })}
                  </CardDescription>
                </CardHeader>
                <CardContent className={isOrderPending ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
                  <div className="rounded-lg border overflow-hidden">
                    <Table>
                      <TableHeader>
                        <TableRow>
                          <TableHead className="w-10 sticky left-0 z-10 bg-background"></TableHead>
                          <TableHead>{t('orders.orderNumber', 'Order #')}</TableHead>
                          <TableHead>{t('labels.status', 'Status')}</TableHead>
                          <TableHead className="text-center">{t('orders.items', 'Items')}</TableHead>
                          <TableHead className="text-right">{t('orders.total', 'Total')}</TableHead>
                          <TableHead>{t('labels.date', 'Date')}</TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {ordersLoading ? (
                          [...Array(3)].map((_, i) => (
                            <TableRow key={i} className="animate-pulse">
                              <TableCell className="sticky left-0 z-10 bg-background"><Skeleton className="h-8 w-8 rounded" /></TableCell>
                              <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                              <TableCell><Skeleton className="h-5 w-20 rounded-full" /></TableCell>
                              <TableCell className="text-center"><Skeleton className="h-5 w-8 mx-auto" /></TableCell>
                              <TableCell className="text-right"><Skeleton className="h-4 w-24 ml-auto" /></TableCell>
                              <TableCell><Skeleton className="h-4 w-28" /></TableCell>
                            </TableRow>
                          ))
                        ) : orders.length === 0 ? (
                          <TableRow>
                            <TableCell colSpan={6} className="p-0">
                              <EmptyState
                                icon={ShoppingCart}
                                title={t('customers.noOrders', 'No orders yet')}
                                description={t('customers.noOrdersDescription', 'Orders will appear here when this customer places them.')}
                                className="border-0 rounded-none px-4 py-12"
                              />
                            </TableCell>
                          </TableRow>
                        ) : (
                          orders.map((order) => (
                            <TableRow key={order.id} className="group transition-colors hover:bg-muted/50">
                              <TableCell className="sticky left-0 z-10 bg-background">
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary"
                                  aria-label={t('orders.viewOrder', { orderNumber: order.orderNumber, defaultValue: `View order ${order.orderNumber}` })}
                                  onClick={() => navigate(`/portal/ecommerce/orders/${order.id}`)}
                                >
                                  <Eye className="h-4 w-4" />
                                </Button>
                              </TableCell>
                              <TableCell>
                                <span className="font-mono font-medium text-sm">{order.orderNumber}</span>
                              </TableCell>
                              <TableCell>
                                <Badge variant="outline">{order.status}</Badge>
                              </TableCell>
                              <TableCell className="text-center">
                                <Badge variant="secondary">{order.itemCount}</Badge>
                              </TableCell>
                              <TableCell className="text-right">
                                <span className="font-medium">{formatCurrency(order.grandTotal, order.currency)}</span>
                              </TableCell>
                              <TableCell>
                                <span className="text-sm text-muted-foreground">{formatDateTime(order.createdAt)}</span>
                              </TableCell>
                            </TableRow>
                          ))
                        )}
                      </TableBody>
                    </Table>
                  </div>

                  {ordersTotalPages > 1 && (
                    <Pagination
                      currentPage={ordersCurrentPage}
                      totalPages={ordersTotalPages}
                      totalItems={ordersResponse?.totalCount ?? 0}
                      pageSize={orderParams.pageSize || 10}
                      onPageChange={handleOrderPageChange}
                      showPageSizeSelector={false}
                      className="mt-4"
                    />
                  )}
                </CardContent>
              </Card>
            </TabsContent>

            {/* Addresses Tab */}
            <TabsContent value="addresses">
              <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-4 py-5">
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <div>
                      <CardTitle className="text-sm flex items-center gap-2">
                        <MapPin className="h-4 w-4" />
                        {t('customers.addresses', 'Addresses')}
                      </CardTitle>
                      <CardDescription>
                        {t('customers.addressCount', { count: customer.addresses.length, defaultValue: `${customer.addresses.length} addresses` })}
                      </CardDescription>
                    </div>
                    {canUpdate && (
                      <Button
                        size="sm"
                        className="cursor-pointer"
                        onClick={() => {
                          setAddressToEdit(null)
                          setShowAddressDialog(true)
                        }}
                      >
                        <Plus className="h-4 w-4 mr-1" />
                        {t('customers.addAddress', 'Add Address')}
                      </Button>
                    )}
                  </div>
                </CardHeader>
                <CardContent>
                  {customer.addresses.length === 0 ? (
                    <EmptyState
                      icon={MapPin}
                      title={t('customers.noAddresses', 'No addresses')}
                      description={t('customers.noAddressesDescription', 'Add a shipping or billing address for this customer.')}
                      action={canUpdate ? {
                        label: t('customers.addAddress', 'Add Address'),
                        onClick: () => {
                          setAddressToEdit(null)
                          setShowAddressDialog(true)
                        },
                      } : undefined}
                      className="border-0 rounded-none px-4 py-12"
                    />
                  ) : (
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      {customer.addresses.map((address) => (
                        <Card key={address.id} className="relative shadow-sm hover:shadow-lg transition-all duration-300">
                          <CardHeader className="pb-2">
                            <div className="flex items-center justify-between">
                              <div className="flex items-center gap-2">
                                <Badge variant="outline">
                                  {t(`customers.addressType.${address.addressType.toLowerCase()}`, address.addressType)}
                                </Badge>
                                {address.isDefault && (
                                  <Badge variant="secondary" className="text-xs">
                                    {t('customers.defaultAddress', 'Default')}
                                  </Badge>
                                )}
                              </div>
                              {canUpdate && (
                                <div className="flex items-center gap-1">
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    className="cursor-pointer h-8 w-8 p-0"
                                    aria-label={t('customers.editAddress', { name: address.fullName, defaultValue: `Edit address for ${address.fullName}` })}
                                    onClick={() => {
                                      setAddressToEdit(address)
                                      setShowAddressDialog(true)
                                    }}
                                  >
                                    <Pencil className="h-3.5 w-3.5" />
                                  </Button>
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    className="cursor-pointer h-8 w-8 p-0 text-destructive hover:text-destructive"
                                    aria-label={t('customers.deleteAddress', { name: address.fullName, defaultValue: `Delete address for ${address.fullName}` })}
                                    onClick={() => {
                                      // Address deletion is handled inline
                                      setAddressToEdit(address)
                                    }}
                                  >
                                    <Trash2 className="h-3.5 w-3.5" />
                                  </Button>
                                </div>
                              )}
                            </div>
                          </CardHeader>
                          <CardContent className="space-y-1 text-sm">
                            <p className="font-medium">{address.fullName}</p>
                            <p className="text-muted-foreground">{address.phone}</p>
                            <p className="text-muted-foreground">{address.addressLine1}</p>
                            {address.addressLine2 && <p className="text-muted-foreground">{address.addressLine2}</p>}
                            <p className="text-muted-foreground">
                              {[address.ward, address.district, address.province].filter(Boolean).join(', ')}
                            </p>
                            {address.postalCode && <p className="text-muted-foreground">{address.postalCode}</p>}
                          </CardContent>
                        </Card>
                      ))}
                    </div>
                  )}
                </CardContent>
              </Card>
            </TabsContent>
          </Tabs>
        </div>

        {/* Right Column - Customer Info & Actions */}
        <div className="space-y-6">
          {/* Customer Info */}
          <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-4 py-5">
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle className="text-sm">{t('customers.customerInfo', 'Customer Information')}</CardTitle>
                <div className="flex items-center gap-1">
                  {canUpdate && (
                    <Button
                      variant="ghost"
                      size="sm"
                      className="cursor-pointer h-8 w-8 p-0"
                      aria-label={t('customers.editCustomer', { name: fullName, defaultValue: `Edit ${fullName}` })}
                      onClick={() => setShowEditDialog(true)}
                    >
                      <Pencil className="h-3.5 w-3.5" />
                    </Button>
                  )}
                  {canDelete && (
                    <Button
                      variant="ghost"
                      size="sm"
                      className="cursor-pointer h-8 w-8 p-0 text-destructive hover:text-destructive"
                      aria-label={t('customers.deleteCustomer', { name: fullName, defaultValue: `Delete ${fullName}` })}
                      onClick={() => setShowDeleteDialog(true)}
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </Button>
                  )}
                </div>
              </div>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              <div className="flex items-center gap-2">
                <User className="h-4 w-4 text-muted-foreground" />
                <span className="font-medium">{fullName}</span>
              </div>
              <div className="flex items-center gap-2">
                <Mail className="h-4 w-4 text-muted-foreground" />
                <span>{customer.email}</span>
              </div>
              {customer.phone && (
                <div className="flex items-center gap-2">
                  <Phone className="h-4 w-4 text-muted-foreground" />
                  <span>{customer.phone}</span>
                </div>
              )}
              <Separator />
              <div>
                <p className="text-muted-foreground text-xs mb-1">{t('labels.status', 'Status')}</p>
                <Badge variant={customer.isActive ? 'default' : 'secondary'}>
                  {customer.isActive ? t('labels.active', 'Active') : t('labels.inactive', 'Inactive')}
                </Badge>
              </div>
              <div>
                <p className="text-muted-foreground text-xs mb-1">{t('labels.createdAt', 'Created At')}</p>
                <p className="font-medium">{formatDateTime(customer.createdAt)}</p>
              </div>
              {customer.lastOrderDate && (
                <div>
                  <p className="text-muted-foreground text-xs mb-1">{t('customers.lastOrderDate', 'Last Order Date')}</p>
                  <p className="font-medium">{formatDateTime(customer.lastOrderDate)}</p>
                </div>
              )}
              {customer.tags && (
                <div>
                  <p className="text-muted-foreground text-xs mb-1">{t('labels.tags', 'Tags')}</p>
                  <div className="flex flex-wrap gap-1">
                    {customer.tags.split(',').map((tag) => (
                      <Badge key={tag.trim()} variant="outline" className="text-xs">
                        {tag.trim()}
                      </Badge>
                    ))}
                  </div>
                </div>
              )}
              {customer.notes && (
                <div>
                  <p className="text-muted-foreground text-xs mb-1">{t('labels.notes', 'Notes')}</p>
                  <p className="text-sm whitespace-pre-wrap">{customer.notes}</p>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Segment Management */}
          {canManage && (
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-4 py-5">
              <CardHeader>
                <CardTitle className="text-sm flex items-center gap-2">
                  <Crown className="h-4 w-4" />
                  {t('customers.segmentManagement', 'Segment Management')}
                </CardTitle>
                <CardDescription className="text-xs">
                  {t('customers.segmentManagementDescription', 'Manually override the customer segment')}
                </CardDescription>
              </CardHeader>
              <CardContent>
                <Select
                  value={customer.segment}
                  onValueChange={handleSegmentChange}
                  disabled={updateSegmentMutation.isPending}
                >
                  <SelectTrigger className="cursor-pointer">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {CUSTOMER_SEGMENTS.map((segment) => (
                      <SelectItem key={segment} value={segment} className="cursor-pointer">
                        {t(`customers.segment.${segment.toLowerCase()}`, segment)}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </CardContent>
            </Card>
          )}

          {/* Loyalty Points */}
          <Card className="shadow-sm hover:shadow-lg transition-all duration-300 gap-4 py-5">
            <CardHeader>
              <CardTitle className="text-sm flex items-center gap-2">
                <Gift className="h-4 w-4" />
                {t('customers.loyalty', 'Loyalty Points')}
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex justify-between items-center">
                <span className="text-sm text-muted-foreground">{t('customers.currentPoints', 'Current Points')}</span>
                <span className="text-lg font-bold">{customer.loyaltyPoints.toLocaleString()}</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-sm text-muted-foreground">{t('customers.lifetimePoints', 'Lifetime Points')}</span>
                <span className="text-sm font-medium">{customer.lifetimeLoyaltyPoints.toLocaleString()}</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-sm text-muted-foreground">{t('customers.tierLabel', 'Tier')}</span>
                <Badge variant="outline" className={getTierBadgeClass(customer.tier)}>
                  <Star className="h-3 w-3 mr-1" />
                  {t(`customers.tier.${customer.tier.toLowerCase()}`, customer.tier)}
                </Badge>
              </div>
              {canManage && (
                <>
                  <Separator />
                  <div className="flex gap-2">
                    <Button
                      size="sm"
                      className="flex-1 cursor-pointer"
                      onClick={() => setLoyaltyMode('add')}
                    >
                      <Plus className="h-4 w-4 mr-1" />
                      {t('customers.addPoints', 'Add')}
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      className="flex-1 cursor-pointer"
                      onClick={() => setLoyaltyMode('redeem')}
                      disabled={customer.loyaltyPoints <= 0}
                    >
                      <Minus className="h-4 w-4 mr-1" />
                      {t('customers.redeemPoints', 'Redeem')}
                    </Button>
                  </div>
                </>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Edit Customer Dialog */}
      <CustomerFormDialog
        open={showEditDialog}
        onOpenChange={setShowEditDialog}
        customer={customer}
      />

      {/* Delete Customer Dialog */}
      <DeleteCustomerDialog
        open={showDeleteDialog}
        onOpenChange={setShowDeleteDialog}
        customer={customer}
        onSuccess={() => navigate('/portal/ecommerce/customers')}
      />

      {/* Address Form Dialog */}
      <AddressFormDialog
        open={showAddressDialog}
        onOpenChange={(open) => {
          setShowAddressDialog(open)
          if (!open) setAddressToEdit(null)
        }}
        customerId={customer.id}
        address={addressToEdit}
      />

      {/* Loyalty Points Dialog */}
      <LoyaltyPointsDialog
        open={loyaltyMode !== null}
        onOpenChange={(open) => !open && setLoyaltyMode(null)}
        customerId={customer.id}
        customerName={fullName}
        mode={loyaltyMode ?? 'add'}
        currentPoints={customer.loyaltyPoints}
      />
    </div>
  )
}

export default CustomerDetailPage

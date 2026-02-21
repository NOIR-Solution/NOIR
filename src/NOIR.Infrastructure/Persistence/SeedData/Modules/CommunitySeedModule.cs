namespace NOIR.Infrastructure.Persistence.SeedData;

/// <summary>
/// Seeds community data: customer groups, product reviews, wishlists, and shipping.
/// Order: 350 (depends on CatalogSeedModule and CommerceSeedModule).
/// </summary>
public class CommunitySeedModule : ISeedDataModule
{
    public int Order => 350;
    public string ModuleName => "Community";

    public async Task SeedAsync(SeedDataContext context, CancellationToken ct = default)
    {
        var tenantId = context.CurrentTenant.Id;

        // Idempotency: skip if customer groups already exist for this tenant
        var hasData = await context.DbContext.Set<CustomerGroup>()
            .IgnoreQueryFilters()
            .TagWith("SeedData:CheckCommunity")
            .AnyAsync(g => g.TenantId == tenantId, ct);

        if (hasData)
        {
            context.Logger.LogInformation("[SeedData] Community already seeded for {Tenant}", tenantId);
            return;
        }

        // Load existing data for foreign keys
        var productLookup = await BuildProductLookupAsync(context, tenantId, ct);
        var customers = await LoadCustomersAsync(context, tenantId, ct);
        var orders = await LoadOrdersAsync(context, tenantId, ct);

        // 1. Seed Customer Groups + Memberships
        var groupCount = SeedCustomerGroups(context, customers, tenantId);
        await context.DbContext.SaveChangesAsync(ct);

        // 2. Seed Product Reviews
        var reviewCount = SeedReviews(context, productLookup, orders, tenantId);
        await context.DbContext.SaveChangesAsync(ct);

        // 3. Seed Wishlists
        var wishlistCount = SeedWishlists(context, productLookup, tenantId);
        await context.DbContext.SaveChangesAsync(ct);

        // 4. Seed Shipping Providers + Shipping Orders
        var (providerCount, shippingCount) = SeedShipping(context, orders, tenantId);
        await context.DbContext.SaveChangesAsync(ct);

        context.Logger.LogInformation(
            "[SeedData] Community: {Groups} groups, {Reviews} reviews, {Wishlists} wishlists, {Providers} providers, {Shipments} shipments",
            groupCount, reviewCount, wishlistCount, providerCount, shippingCount);
    }

    private static async Task<Dictionary<string, (Product Product, List<ProductVariant> Variants)>> BuildProductLookupAsync(
        SeedDataContext context,
        string tenantId,
        CancellationToken ct)
    {
        var products = await context.DbContext.Set<Product>()
            .IgnoreQueryFilters()
            .TagWith("SeedData:GetProductsForCommunity")
            .Where(p => p.TenantId == tenantId)
            .Include(p => p.Variants)
            .AsSplitQuery()
            .ToListAsync(ct);

        return products.ToDictionary(
            p => p.Slug,
            p => (p, p.Variants.ToList()),
            StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<List<(Guid Id, string? UserId, string Email)>> LoadCustomersAsync(
        SeedDataContext context,
        string tenantId,
        CancellationToken ct)
    {
        return await context.DbContext.Set<Customer>()
            .IgnoreQueryFilters()
            .TagWith("SeedData:GetCustomersForCommunity")
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new ValueTuple<Guid, string?, string>(c.Id, c.UserId, c.Email))
            .ToListAsync(ct);
    }

    private static async Task<List<(Guid Id, string OrderNumber, OrderStatus Status)>> LoadOrdersAsync(
        SeedDataContext context,
        string tenantId,
        CancellationToken ct)
    {
        return await context.DbContext.Set<NOIR.Domain.Entities.Order.Order>()
            .IgnoreQueryFilters()
            .TagWith("SeedData:GetOrdersForCommunity")
            .Where(o => o.TenantId == tenantId)
            .OrderBy(o => o.CreatedAt)
            .Select(o => new ValueTuple<Guid, string, OrderStatus>(o.Id, o.OrderNumber, o.Status))
            .ToListAsync(ct);
    }

    private static int SeedCustomerGroups(
        SeedDataContext context,
        List<(Guid Id, string? UserId, string Email)> customers,
        string tenantId)
    {
        var groupDefs = CommunityData.GetCustomerGroups();
        var membershipDefs = CommunityData.GetMemberships();
        var groupEntities = new List<CustomerGroup>();

        foreach (var def in groupDefs)
        {
            var groupId = SeedDataConstants.TenantGuid(tenantId, $"custgroup:{def.Name.ToLowerInvariant().Replace(' ', '-')}");
            var group = CustomerGroup.Create(def.Name, def.Description, tenantId);
            SeedDataConstants.SetEntityId(group, groupId);

            if (!def.IsActive)
            {
                group.Deactivate();
            }

            groupEntities.Add(group);
            context.DbContext.Set<CustomerGroup>().Add(group);
        }

        // Add memberships
        foreach (var (groupIndex, customerIndices) in membershipDefs)
        {
            if (groupIndex >= groupEntities.Count) continue;

            var group = groupEntities[groupIndex];
            var memberCount = 0;

            foreach (var custIndex in customerIndices)
            {
                if (custIndex >= customers.Count) continue;

                var customer = customers[custIndex];
                var membership = CustomerGroupMembership.Create(group.Id, customer.Id, tenantId);
                context.DbContext.Set<CustomerGroupMembership>().Add(membership);
                memberCount++;
            }

            group.UpdateMemberCount(memberCount);
        }

        return groupEntities.Count;
    }

    private static int SeedReviews(
        SeedDataContext context,
        Dictionary<string, (Product Product, List<ProductVariant> Variants)> productLookup,
        List<(Guid Id, string OrderNumber, OrderStatus Status)> orders,
        string tenantId)
    {
        var reviewDefs = CommunityData.GetReviews();
        var count = 0;

        // Find completed orders for verified purchase linking
        var completedOrders = orders
            .Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered)
            .ToList();

        foreach (var def in reviewDefs)
        {
            if (!productLookup.TryGetValue(def.ProductSlug, out var entry))
            {
                context.Logger.LogWarning(
                    "[SeedData] Product slug '{Slug}' not found for review. Skipping.",
                    def.ProductSlug);
                continue;
            }

            var (product, _) = entry;

            // Use tenant admin userId for all reviews (seed customers don't have user accounts)
            var userId = context.TenantAdminUserId;

            // Link verified purchases to a completed order if available
            Guid? orderId = null;
            if (def.IsVerifiedPurchase && completedOrders.Count > 0)
            {
                orderId = completedOrders[count % completedOrders.Count].Id;
            }

            var reviewId = SeedDataConstants.TenantGuid(tenantId, $"review:{def.ProductSlug}:{count}");
            var review = ProductReview.Create(
                productId: product.Id,
                userId: userId,
                rating: def.Rating,
                title: def.Title,
                content: def.Content,
                orderId: orderId,
                isVerifiedPurchase: def.IsVerifiedPurchase,
                tenantId: tenantId);
            SeedDataConstants.SetEntityId(review, reviewId);

            // Apply status transitions
            switch (def.TargetStatus)
            {
                case ReviewStatus.Approved:
                    review.Approve();
                    break;
                case ReviewStatus.Rejected:
                    review.Reject();
                    break;
                case ReviewStatus.Pending:
                    // Default status, no action needed
                    break;
            }

            // Add admin response if defined
            if (!string.IsNullOrEmpty(def.AdminResponse))
            {
                review.AddAdminResponse(def.AdminResponse);
            }

            context.DbContext.Set<ProductReview>().Add(review);
            count++;
        }

        return count;
    }

    private static int SeedWishlists(
        SeedDataContext context,
        Dictionary<string, (Product Product, List<ProductVariant> Variants)> productLookup,
        string tenantId)
    {
        var wishlistDefs = CommunityData.GetWishlists();
        var userId = context.TenantAdminUserId;
        var count = 0;

        foreach (var def in wishlistDefs)
        {
            var wishlistId = SeedDataConstants.TenantGuid(tenantId, $"wishlist:{def.Name.ToLowerInvariant().Replace(' ', '-')}");
            var wishlist = Domain.Entities.Wishlist.Wishlist.Create(
                userId: userId,
                name: def.Name,
                isDefault: def.IsDefault,
                tenantId: tenantId);
            SeedDataConstants.SetEntityId(wishlist, wishlistId);

            // Add items
            foreach (var slug in def.ProductSlugs)
            {
                if (!productLookup.TryGetValue(slug, out var entry))
                {
                    context.Logger.LogWarning(
                        "[SeedData] Product slug '{Slug}' not found for wishlist '{Name}'. Skipping item.",
                        slug, def.Name);
                    continue;
                }

                var (product, variants) = entry;
                var defaultVariant = variants.FirstOrDefault();
                wishlist.AddItem(product.Id, defaultVariant?.Id);
            }

            // Set public visibility and generate share token
            if (def.IsPublic)
            {
                wishlist.SetPublic(true);
                wishlist.GenerateShareToken();
            }

            context.DbContext.Set<Domain.Entities.Wishlist.Wishlist>().Add(wishlist);
            count++;
        }

        return count;
    }

    private static (int ProviderCount, int ShippingCount) SeedShipping(
        SeedDataContext context,
        List<(Guid Id, string OrderNumber, OrderStatus Status)> orders,
        string tenantId)
    {
        var providerDefs = CommunityData.GetShippingProviders();
        var providerEntities = new Dictionary<ShippingProviderCode, ShippingProvider>();

        // 1. Create shipping providers
        foreach (var def in providerDefs)
        {
            var providerId = SeedDataConstants.TenantGuid(tenantId, $"shipprovider:{def.Code}");
            var provider = ShippingProvider.Create(
                providerCode: def.Code,
                displayName: def.DisplayName,
                providerName: def.ProviderName,
                environment: GatewayEnvironment.Sandbox,
                tenantId: tenantId);
            SeedDataConstants.SetEntityId(provider, providerId);

            provider.Activate();
            provider.SetCodSupport(true);

            // Set service types
            var services = def.Code == ShippingProviderCode.GHTK
                ? "[\"Standard\",\"Express\"]"
                : "[\"Standard\",\"Express\",\"SameDay\"]";
            provider.SetSupportedServices(services);

            // Set tracking URL templates
            var trackingTemplate = def.Code == ShippingProviderCode.GHTK
                ? "https://i.ghtk.vn/{trackingNumber}"
                : "https://tracking.ghn.vn/?order_code={trackingNumber}";
            provider.SetTrackingUrlTemplate(trackingTemplate);

            providerEntities[def.Code] = provider;
            context.DbContext.Set<ShippingProvider>().Add(provider);
        }

        // 2. Create shipping orders for Shipped/Delivered/Completed orders
        var shippableOrders = orders
            .Where(o => o.Status == OrderStatus.Shipped
                     || o.Status == OrderStatus.Delivered
                     || o.Status == OrderStatus.Completed)
            .ToList();

        var shippingCount = 0;
        var addresses = VietnameseAddresses.GetAddresses();

        foreach (var order in shippableOrders)
        {
            // Alternate between providers
            var providerCode = shippingCount % 2 == 0 ? ShippingProviderCode.GHTK : ShippingProviderCode.GHN;
            var provider = providerEntities[providerCode];

            var pickupAddr = addresses[0]; // Store address (first address)
            var deliveryAddr = addresses[(shippingCount + 1) % addresses.Length];

            var pickupJson = JsonSerializer.Serialize(new
            {
                fullName = pickupAddr.FullName,
                phone = pickupAddr.Phone,
                address = pickupAddr.AddressLine1,
                ward = pickupAddr.Ward,
                district = pickupAddr.District,
                province = pickupAddr.Province
            });

            var deliveryJson = JsonSerializer.Serialize(new
            {
                fullName = deliveryAddr.FullName,
                phone = deliveryAddr.Phone,
                address = deliveryAddr.AddressLine1,
                ward = deliveryAddr.Ward,
                district = deliveryAddr.District,
                province = deliveryAddr.Province
            });

            var senderJson = JsonSerializer.Serialize(new
            {
                fullName = pickupAddr.FullName,
                phone = pickupAddr.Phone
            });

            var recipientJson = JsonSerializer.Serialize(new
            {
                fullName = deliveryAddr.FullName,
                phone = deliveryAddr.Phone
            });

            var itemsJson = JsonSerializer.Serialize(new[]
            {
                new { name = $"Order {order.OrderNumber}", quantity = 1, weight = 500 }
            });

            var shippingOrderId = SeedDataConstants.TenantGuid(tenantId, $"shiporder:{order.OrderNumber}");
            var trackingNumber = $"TN{tenantId[..8]}-{order.OrderNumber[4..]}";

            var shippingOrder = ShippingOrder.Create(
                orderId: order.Id,
                providerId: provider.Id,
                providerCode: providerCode,
                serviceTypeCode: "Standard",
                serviceTypeName: "Giao h\u00e0ng ti\u00eau chu\u1ea9n",
                pickupAddressJson: pickupJson,
                deliveryAddressJson: deliveryJson,
                senderJson: senderJson,
                recipientJson: recipientJson,
                itemsJson: itemsJson,
                weightGrams: 500m,
                declaredValue: 1_000_000m,
                codAmount: 500_000m,
                isFreeship: false,
                notes: null,
                tenantId: tenantId);
            SeedDataConstants.SetEntityId(shippingOrder, shippingOrderId);

            // Set provider response (moves to AwaitingPickup)
            var estimatedDelivery = SeedDataConstants.BaseTimestamp.AddDays(5);
            shippingOrder.SetProviderResponse(
                trackingNumber: trackingNumber,
                providerOrderId: $"{providerCode}-{order.OrderNumber}",
                labelUrl: null,
                trackingUrl: provider.GetTrackingUrl(trackingNumber),
                baseRate: 30_000m,
                codFee: 10_000m,
                insuranceFee: 0m,
                estimatedDeliveryDate: estimatedDelivery,
                rawResponse: null);

            // Add tracking events based on order status
            var baseDate = SeedDataConstants.BaseTimestamp;

            // All shippable orders: picked up
            var pickedUpEvent = ShippingTrackingEvent.Create(
                shippingOrderId: shippingOrder.Id,
                eventType: "PICKED_UP",
                status: ShippingStatus.PickedUp,
                description: "\u0110\u00e3 l\u1ea5y h\u00e0ng t\u1eeb ng\u01b0\u1eddi g\u1eedi",
                location: pickupAddr.District + ", " + pickupAddr.Province,
                eventDate: baseDate.AddHours(2),
                tenantId: tenantId);
            shippingOrder.AddTrackingEvent(pickedUpEvent);

            // InTransit event
            var inTransitEvent = ShippingTrackingEvent.Create(
                shippingOrderId: shippingOrder.Id,
                eventType: "IN_TRANSIT",
                status: ShippingStatus.InTransit,
                description: "H\u00e0ng \u0111ang v\u1eadn chuy\u1ec3n",
                location: "Trung t\u00e2m ph\u00e2n lo\u1ea1i",
                eventDate: baseDate.AddHours(8),
                tenantId: tenantId);
            shippingOrder.AddTrackingEvent(inTransitEvent);

            if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Completed)
            {
                // Out for delivery
                var outForDeliveryEvent = ShippingTrackingEvent.Create(
                    shippingOrderId: shippingOrder.Id,
                    eventType: "OUT_FOR_DELIVERY",
                    status: ShippingStatus.OutForDelivery,
                    description: "\u0110ang giao h\u00e0ng",
                    location: deliveryAddr.District + ", " + deliveryAddr.Province,
                    eventDate: baseDate.AddDays(1),
                    tenantId: tenantId);
                shippingOrder.AddTrackingEvent(outForDeliveryEvent);

                // Delivered
                var deliveredEvent = ShippingTrackingEvent.Create(
                    shippingOrderId: shippingOrder.Id,
                    eventType: "DELIVERED",
                    status: ShippingStatus.Delivered,
                    description: "Giao h\u00e0ng th\u00e0nh c\u00f4ng",
                    location: deliveryAddr.District + ", " + deliveryAddr.Province,
                    eventDate: baseDate.AddDays(1).AddHours(4),
                    tenantId: tenantId);
                shippingOrder.AddTrackingEvent(deliveredEvent);
            }

            context.DbContext.Set<ShippingOrder>().Add(shippingOrder);
            shippingCount++;
        }

        return (providerEntities.Count, shippingCount);
    }
}

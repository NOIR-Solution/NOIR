namespace NOIR.Infrastructure.Persistence.SeedData.Data;

/// <summary>
/// Customer group definition for seed data.
/// </summary>
public record CustomerGroupDef(string Name, string? Description, bool IsActive);

/// <summary>
/// Product review definition for seed data.
/// CustomerIndex references CommerceData.GetCustomers().
/// ProductSlug references CatalogData.GetProducts().
/// </summary>
public record ReviewDef(
    string ProductSlug,
    int Rating,
    string? Title,
    string Content,
    ReviewStatus TargetStatus,
    bool IsVerifiedPurchase,
    string? AdminResponse,
    int CustomerIndex);

/// <summary>
/// Wishlist definition for seed data (uses tenant admin user).
/// </summary>
public record WishlistDef(string Name, bool IsDefault, bool IsPublic, string[] ProductSlugs);

/// <summary>
/// Shipping provider definition for seed data.
/// </summary>
public record ShippingProviderDef(ShippingProviderCode Code, string DisplayName, string ProviderName);

/// <summary>
/// Vietnamese community seed data: customer groups, product reviews, wishlists, and shipping providers.
/// </summary>
public static class CommunityData
{
    /// <summary>
    /// 5 customer groups - mix of active/inactive for realistic segmentation.
    /// </summary>
    public static CustomerGroupDef[] GetCustomerGroups() =>
    [
        new("Khách hàng VIP", "Khách hàng có tổng đơn hàng trên 5 triệu đồng", true),
        new("Đăng ký bản tin", "Khách hàng đăng ký nhận email khuyến mãi", true),
        new("Khách hàng mới", "Khách hàng đăng ký trong 30 ngày gần đây", true),
        new("Thành viên thân thiết", "Khách hàng có trên 3 đơn hàng hoàn thành", true),
        new("Mua sỉ", "Khách hàng mua sỉ với số lượng lớn", false)
    ];

    /// <summary>
    /// Membership assignments: (groupIndex, customerIndices from CommerceData.GetCustomers()).
    /// </summary>
    public static (int GroupIndex, int[] CustomerIndices)[] GetMemberships() =>
    [
        (0, [0, 1, 4, 8]),         // VIP: 4 members
        (1, [0, 1, 2, 3, 5, 9]),   // Newsletter: 6 members
        (2, [9, 10, 11]),          // New: 3 members
        (3, [0, 1]),               // Loyal: 2 members
        (4, [])                    // Wholesale: 0 members (inactive)
    ];

    /// <summary>
    /// 14 reviews across different products with varied ratings and statuses.
    /// CustomerIndex references CommerceData.GetCustomers().
    /// ProductSlug references CatalogData.GetProducts().
    /// </summary>
    public static ReviewDef[] GetReviews() =>
    [
        // 5-star reviews (verified purchases)
        new("ao-thun-tron-co-tron", 5, "Rất thoải mái",
            "Áo thun cotton 100% rất mềm và thoáng mát. Mặc cả ngày không bị nóng. Rất hài lòng với chất lượng so với giá tiền.",
            ReviewStatus.Approved, true, null, 0),
        new("ao-polo-classic", 5, "Đáng đồng tiền",
            "Áo polo này thiết kế rất đẹp, chất vải dày dặn mà vẫn thoáng. Mặc đi làm rất phù hợp. Sẽ mua thêm màu khác.",
            ReviewStatus.Approved, true, "Cảm ơn bạn đã ủng hộ NOIR! Chúc bạn luôn hài lòng.", 1),
        new("quan-jeans-slim-fit", 5, "Form chuẩn",
            "Quần jeans slim fit vừa vặn, co giãn tốt, thoải mái khi ngồi. Màu xanh đậm rất đẹp, giặt không bị phai.",
            ReviewStatus.Approved, true, null, 0),

        // 4-star reviews
        new("ao-khoac-gio-nhe", 4, "Nhẹ và tiện",
            "Áo khoác gió nhẹ lắm, gấp gọn bỏ túi rất tiện. Chống nước tốt với mưa nhỏ. Trừ 1 sao vì khóa kéo hơi khó kéo ban đầu.",
            ReviewStatus.Approved, true, "Cảm ơn bạn đã phản hồi! Khóa kéo sẽ mượt hơn sau vài lần sử dụng.", 4),
        new("quan-kaki-chat", 4, "Chất lượng tốt",
            "Quần kaki chất vải đẹp, form regular fit thoải mái. Màu be dễ phối đồ. Chỉ tiếc là không có thêm size 33.",
            ReviewStatus.Approved, false, null, 3),
        new("dong-ho-nam-thoi-trang", 4, null,
            "Đồng hồ đẹp, mặt số rõ ràng. Dây da thật mềm mại. Đóng gói cẩn thận. Giao hàng nhanh.",
            ReviewStatus.Approved, true, null, 8),
        new("tui-xach-da-thoi-trang", 4, "Sang trọng",
            "Túi xách da PU nhưng trông rất giống da thật. Nhiều ngăn tiện lợi. Phù hợp đi làm.",
            ReviewStatus.Approved, false, null, 1),

        // 3-star reviews
        new("ao-thun-nu-crop-top", 3, "Bình thường",
            "Áo crop top cơ bản, chất lượng tạm ổn cho giá tiền. Form hơi rộng so với size chart. Nên chọn nhỏ hơn 1 size.",
            ReviewStatus.Approved, false, "Cảm ơn bạn đã góp ý. Chúng tôi đang cập nhật bảng size cho chính xác hơn.", 5),
        new("quan-short-the-thao", 3, null,
            "Quần short mặc tập gym được. Chất vải hơi mỏng nhưng thoáng mát. Giá hợp lý.",
            ReviewStatus.Approved, true, null, 6),

        // 2-star review
        new("mu-luoi-trai-unisex", 2, "Không như mong đợi",
            "Mũ hơi cứng, đội không thoải mái lắm. Chất cotton nhưng hơi nóng. Phần điều chỉnh size khó dùng.",
            ReviewStatus.Approved, false, "Xin lỗi bạn về trải nghiệm này. Vui lòng liên hệ để đổi trả nếu cần.", 7),

        // 1-star review
        new("kinh-mat-phan-cuc", 1, "Thất vọng",
            "Kính bị xước nhẹ khi nhận hàng. Gọng hơi lỏng. Phải tự chỉnh lại. Đóng gói không tốt.",
            ReviewStatus.Approved, false, "Chúng tôi rất xin lỗi. Đã chuyển phản hồi đến bộ phận đóng gói. Bạn có thể liên hệ để đổi sản phẩm mới.", 2),

        // Pending reviews (waiting for approval)
        new("ao-so-mi-oxford", 5, "Tuyệt vời",
            "Sơ mi Oxford rất đẹp, chất vải dày dặn. Mặc đi làm cực kỳ lịch sự. Recommend cho anh em.",
            ReviewStatus.Pending, false, null, 10),
        new("ao-tank-top-the-thao", 4, null,
            "Áo tập gym thoáng mát, nhanh khô. Chất lượng ổn cho tầm giá.",
            ReviewStatus.Pending, false, null, 11),

        // Rejected review (spam/inappropriate)
        new("that-lung-da-nam", 1, "Quảng cáo",
            "Mua thắt lưng ở shop XYZ rẻ hơn nhiều, chất lượng tốt hơn...",
            ReviewStatus.Rejected, false, null, 9)
    ];

    /// <summary>
    /// 3 wishlists using tenant admin user.
    /// Product slugs reference CatalogData.GetProducts().
    /// </summary>
    public static WishlistDef[] GetWishlists() =>
    [
        new("Yêu thích", true, false,
            ["ao-polo-classic", "quan-jeans-slim-fit", "dong-ho-nam-thoi-trang", "ao-khoac-gio-nhe"]),
        new("Mua sau", false, false,
            ["tui-xach-da-thoi-trang", "ao-so-mi-oxford"]),
        new("Quà tặng", false, true,
            ["dong-ho-nam-thoi-trang", "that-lung-da-nam", "kinh-mat-phan-cuc"])
    ];

    /// <summary>
    /// 2 shipping providers (Vietnamese carriers).
    /// </summary>
    public static ShippingProviderDef[] GetShippingProviders() =>
    [
        new(ShippingProviderCode.GHTK, "Giao Hàng Tiết Kiệm", "GHTK"),
        new(ShippingProviderCode.GHN, "Giao Hàng Nhanh", "GHN")
    ];
}

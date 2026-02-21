namespace NOIR.Infrastructure.Persistence.SeedData.Data;

// Record types for structured blog seed data
public record PostCategoryDef(string Name, string Slug, string? Description, string? ParentSlug);

public record PostTagDef(string Name, string Slug, string? Color);

public record PostDef(
    string Title,
    string Slug,
    string CategorySlug,
    string[] TagSlugs,
    PostStatus Status,
    string Excerpt,
    string ContentHtml,
    string ImageColor,
    int DayOffset);

/// <summary>
/// Static blog data for seed: post categories, tags, and posts.
/// All text uses proper Vietnamese Unicode. Content is realistic blog-style HTML.
/// </summary>
public static class BlogData
{
    public static PostCategoryDef[] GetCategories() =>
    [
        new("C\u00f4ng ngh\u1ec7", "cong-nghe", "\u0110\u00e1nh gi\u00e1 v\u00e0 tin t\u1ee9c c\u00f4ng ngh\u1ec7 m\u1edbi nh\u1ea5t", null),
        new("Phong c\u00e1ch s\u1ed1ng", "phong-cach-song", "Xu h\u01b0\u1edbng th\u1eddi trang v\u00e0 phong c\u00e1ch s\u1ed1ng hi\u1ec7n \u0111\u1ea1i", null),
        new("M\u1eb9o mua s\u1eafm", "meo-mua-sam", "H\u01b0\u1edbng d\u1eabn v\u00e0 m\u1eb9o mua s\u1eafm th\u00f4ng minh", null)
    ];

    public static PostTagDef[] GetTags() =>
    [
        new("Khuy\u1ebfn m\u00e3i", "khuyen-mai", "#EF4444"),
        new("M\u1edbi nh\u1ea5t", "moi-nhat", "#3B82F6"),
        new("Review", "review", "#10B981"),
        new("H\u01b0\u1edbng d\u1eabn", "huong-dan", "#F59E0B"),
        new("Xu h\u01b0\u1edbng", "xu-huong", "#8B5CF6"),
        new("So s\u00e1nh", "so-sanh", "#EC4899"),
        new("M\u1eb9o hay", "meo-hay", "#14B8A6"),
        new("Tin t\u1ee9c", "tin-tuc", "#6366F1")
    ];

    public static PostDef[] GetPosts() =>
    [
        // === 6 Published posts ===
        new(
            "Top 10 xu h\u01b0\u1edbng th\u1eddi trang nam 2026",
            "top-10-xu-huong-thoi-trang-nam-2026",
            "phong-cach-song",
            ["xu-huong", "moi-nhat"],
            PostStatus.Published,
            "Kh\u00e1m ph\u00e1 nh\u1eefng xu h\u01b0\u1edbng th\u1eddi trang nam n\u1ed5i b\u1eadt nh\u1ea5t n\u0103m 2026, t\u1eeb phong c\u00e1ch minimalist \u0111\u1ebfn streetwear \u0111\u01b0\u1eddng ph\u1ed1.",
            """
            <h2>Xu hướng thời trang nam 2026: Những điều bạn cần biết</h2>
            <p>Năm 2026 đánh dấu sự trở lại của nhiều phong cách thời trang cổ điển, kết hợp với những yếu tố hiện đại để tạo nên xu hướng mới độc đáo. Hãy cùng NOIR khám phá 10 xu hướng nổi bật nhất trong bài viết này.</p>
            <h2>1. Minimalist - Đơn giản là đỉnh cao</h2>
            <p>Phong cách tối giản tiếp tục lên ngôi với những thiết kế gọn gàng, màu sắc trung tính như đen, trắng, xám và be. Các item cơ bản như áo thun trơn, quần kaki và áo khoác nhẹ là những món đồ không thể thiếu.</p>
            <h2>2. Streetwear phá cách</h2>
            <p>Đường phố vẫn là nguồn cảm hứng vô tận cho thời trang nam. Những chiếc áo oversized, quần jogger và giày sneaker độc đáo sẽ là lựa chọn hàng đầu cho những chàng trai đã chán phong cách truyền thống.</p>
            <h2>3. Smart Casual linh hoạt</h2>
            <p>Sự kết hợp giữa trang phục công sở và đời thường ngày càng được ưa chuộng. Một chiếc áo polo kết hợp với quần chinos và giày lười sẽ giúp bạn vừa lịch sự vừa thoải mái trong mọi hoàn cảnh.</p>
            <h2>4. Sustainable Fashion</h2>
            <p>Thời trang bền vững không còn là xu hướng mà đã trở thành tiêu chuẩn. Các thương hiệu như NOIR cam kết sử dụng nguyên liệu thân thiện với môi trường, mang đến cho bạn sự lựa chọn có trách nhiệm hơn.</p>
            <ul>
                <li>Chất liệu cotton hữu cơ</li>
                <li>Quy trình sản xuất tiết kiệm nước</li>
                <li>Bao bì tái chế 100%</li>
            </ul>
            <h2>5. Color Blocking</h2>
            <p>Sự kết hợp màu sắc tương phản trên cùng một trang phục đang là xu hướng hot. Hãy thử phối áo xanh với quần cam, hoặc áo đỏ với quần trắng để tạo điểm nhấn cho outfit của mình.</p>
            """,
            "#6A1B9A",
            -90),

        new(
            "Review \u00e1o kho\u00e1c gi\u00f3 NOIR Sport - C\u00f3 \u0111\u00e1ng mua?",
            "review-ao-khoac-gio-noir-sport",
            "cong-nghe",
            ["review", "moi-nhat"],
            PostStatus.Published,
            "\u0110\u00e1nh gi\u00e1 chi ti\u1ebft \u00e1o kho\u00e1c gi\u00f3 NOIR Sport sau 2 th\u00e1ng s\u1eed d\u1ee5ng th\u1ef1c t\u1ebf.",
            """
            <h2>Đánh giá áo khoác gió NOIR Sport sau 2 tháng sử dụng</h2>
            <p>Sau 2 tháng sử dụng thực tế trong nhiều điều kiện thời tiết khác nhau, mình xin chia sẻ những đánh giá khách quan nhất về chiếc áo khoác gió NOIR Sport.</p>
            <h2>Thiết kế và chất liệu</h2>
            <p>Chiếc áo được làm từ vải nylon ripstop, cảm giác rất nhẹ và mỏng nhưng vẫn đủ chắn gió tốt. Phần khóa kéo chất lượng YKK chạy rất mượt. Túi đựng được thiết kế thông minh để gập gọn áo khi không sử dụng.</p>
            <h2>Khả năng chống nước</h2>
            <p>Áo được quảng cáo là chống nước cấp độ 3, và thực tế khi gặp mưa nhỏ đến vừa, nước lăn trên bề mặt áo mà không thấm vào. Tuy nhiên với mưa lớn kéo dài thì vẫn sẽ thấm ở phần đường may.</p>
            <h2>Đánh giá tổng thể</h2>
            <ul>
                <li><strong>Thiết kế:</strong> 9/10 - Gọn gàng, hiện đại</li>
                <li><strong>Chất lượng:</strong> 8/10 - Tốt trong tầm giá</li>
                <li><strong>Tiện dụng:</strong> 9/10 - Nhẹ, gập gọn dễ dàng</li>
                <li><strong>Giá:</strong> 8/10 - Hợp lý cho chất lượng nhận được</li>
            </ul>
            <p><strong>Kết luận:</strong> Với mức giá 750.000đ, áo khoác gió NOIR Sport là một lựa chọn xứng đáng cho những ai cần một chiếc áo khoác nhẹ, đa năng cho cuộc sống hàng ngày.</p>
            """,
            "#2D5F2D",
            -60),

        new(
            "H\u01b0\u1edbng d\u1eabn ch\u1ecdn size \u00e1o thun chu\u1ea9n nh\u1ea5t",
            "huong-dan-chon-size-ao-thun-chuan-nhat",
            "meo-mua-sam",
            ["huong-dan", "meo-hay"],
            PostStatus.Published,
            "B\u00ed quy\u1ebft ch\u1ecdn size \u00e1o thun v\u1eeba v\u1eb7n, kh\u00f4ng c\u1ea7n th\u1eed \u0111\u1ed3 tr\u1ef1c ti\u1ebfp khi mua online.",
            """
            <h2>Làm sao để chọn đúng size áo thun khi mua online?</h2>
            <p>Mua sắm online tiện lợi nhưng nhiều người vẫn lo lắng về việc chọn sai size. Bài viết này sẽ giúp bạn biết cách đo và chọn size chuẩn nhất.</p>
            <h2>Bước 1: Đo số đo cơ thể</h2>
            <p>Bạn cần chuẩn bị một thước dây và đo các số đo sau:</p>
            <ul>
                <li><strong>Vong ngực:</strong> Đo vòng quanh ngực tại điểm rộng nhất</li>
                <li><strong>Chiều dài áo:</strong> Từ vai đến gấu áo</li>
                <li><strong>Vai:</strong> Từ mối vai trái sang mối vai phải</li>
            </ul>
            <h2>Bước 2: So sánh với bảng size</h2>
            <p>Mỗi thương hiệu có bảng size riêng, nhưng thông thường với size Việt Nam:</p>
            <ul>
                <li><strong>Size S:</strong> Ngực 84-88cm, dài 66cm, vai 40cm</li>
                <li><strong>Size M:</strong> Ngực 88-92cm, dài 68cm, vai 42cm</li>
                <li><strong>Size L:</strong> Ngực 92-96cm, dài 70cm, vai 44cm</li>
                <li><strong>Size XL:</strong> Ngực 96-100cm, dài 72cm, vai 46cm</li>
            </ul>
            <h2>Mẹo nhỏ khi chọn size</h2>
            <p>Nếu bạn ở giữa 2 size, hãy chọn size lớn hơn để thoải mái hơn. Vải cotton sau vài lần giặt sẽ hơi co lại nên việc chọn rộng hơn một chút là hợp lý. Tại NOIR, chúng tôi có chính sách đổi size miễn phí trong 30 ngày.</p>
            """,
            "#F59E0B",
            -45),

        new(
            "5 c\u00e1ch ph\u1ed1i \u0111\u1ed3 v\u1edbi qu\u1ea7n jeans cho nam",
            "5-cach-phoi-do-voi-quan-jeans-cho-nam",
            "phong-cach-song",
            ["huong-dan", "xu-huong"],
            PostStatus.Published,
            "Qu\u1ea7n jeans l\u00e0 item \u0111a n\u0103ng nh\u1ea5t - h\u00e3y h\u1ecdc c\u00e1ch ph\u1ed1i \u0111\u1ed3 chu\u1ea9n men v\u1edbi jeans.",
            """
            <h2>Quần jeans - Item đa năng nhất trong tủ đồ</h2>
            <p>Quần jeans là một trong những item không bao giờ lỗi mốt. Dưới đây là 5 cách phối đồ với quần jeans mà bạn có thể áp dụng ngay.</p>
            <h2>1. Jeans + Áo thun trơn</h2>
            <p>Sự kết hợp kinh điển và đơn giản nhất. Chọn áo thun màu trơn như trắng, đen hoặc xám để tạo phong cách casual gọn gàng.</p>
            <h2>2. Jeans + Áo sơ mi</h2>
            <p>Muốn lịch sự hơn? Hãy kết hợp jeans với áo sơ mi. Xắn tay áo lên và để tà áo ngoài quần để trông thoải mái nhưng vẫn thanh lịch.</p>
            <h2>3. Jeans + Blazer</h2>
            <p>Đây là công thức smart casual hoàn hảo. Jeans slim fit kết hợp với blazer và giày Chelsea boot sẽ làm bạn nổi bật trong các buổi gặp mặt bán trang trọng.</p>
            <h2>4. Jeans + Áo khoác bomber</h2>
            <p>Phong cách streetwear cực chất. Chọn áo khoác bomber với jeans rách nhẹ và sneaker trắng để thể hiện cá tính riêng.</p>
            <h2>5. Jeans + Áo polo</h2>
            <p>Đơn giản nhưng không hề đơn điệu. Áo polo mang lại vẻ lịch lãm mà vẫn thoải mái. Rất phù hợp cho các buổi hẹn hò hoặc đi cà phê cuối tuần.</p>
            """,
            "#1E3A5F",
            -30),

        new(
            "So s\u00e1nh ch\u1ea5t li\u1ec7u v\u1ea3i: Cotton vs Polyester vs Blend",
            "so-sanh-chat-lieu-vai-cotton-polyester-blend",
            "cong-nghe",
            ["so-sanh", "huong-dan"],
            PostStatus.Published,
            "Ph\u00e2n t\u00edch \u01b0u nh\u01b0\u1ee3c \u0111i\u1ec3m c\u1ee7a c\u00e1c lo\u1ea1i v\u1ea3i ph\u1ed5 bi\u1ebfn \u0111\u1ec3 b\u1ea1n ch\u1ecdn mua \u0111\u00fang.",
            """
            <h2>Chất liệu vải: Biết để chọn đúng</h2>
            <p>Khi mua quần áo, nhiều người chỉ quan tâm đến mẫu mã mà quên mất chất liệu vải - yếu tố quyết định độ bền và thoải mái của sản phẩm.</p>
            <h2>Cotton - Vải tự nhiên số 1</h2>
            <p>Cotton là chất liệu tự nhiên được yêu thích nhất. Ưu điểm lớn nhất là thấm hút mồ hôi tốt, mềm mịn và thân thiện với da. Tuy nhiên, cotton dễ nhăn và co rút sau khi giặt.</p>
            <h2>Polyester - Bền bỉ vượt trội</h2>
            <p>Polyester là sợi tổng hợp có độ bền cao, không nhăn, nhanh khô và giữ màu tốt. Đây là lựa chọn tuyệt vời cho đồ thể thao và trang phục outdoor. Nhược điểm là không thấm hút mồ hôi tốt và có thể gây nóng bức.</p>
            <h2>Cotton Blend - Sự kết hợp hoàn hảo</h2>
            <p>Vải pha cotton và polyester (thường là tỉ lệ 60/40 hoặc 65/35) kết hợp ưu điểm của cả hai: mềm mại của cotton và độ bền của polyester. Đây là chất liệu được NOIR sử dụng nhiều trong các dòng áo polo và áo sơ mi.</p>
            <ul>
                <li><strong>Cotton 100%:</strong> Thích hợp cho áo thun, đồ mặc nhà</li>
                <li><strong>Polyester:</strong> Thích hợp cho đồ thể thao, áo khoác</li>
                <li><strong>Blend:</strong> Thích hợp cho áo polo, sơ mi, trang phục hàng ngày</li>
            </ul>
            """,
            "#0D47A1",
            -15),

        new(
            "Ch\u01b0\u01a1ng tr\u00ecnh khuy\u1ebfn m\u00e3i m\u00f9a h\u00e8 2026 - Gi\u1ea3m \u0111\u1ebfn 50%",
            "chuong-trinh-khuyen-mai-mua-he-2026",
            "meo-mua-sam",
            ["khuyen-mai", "tin-tuc", "moi-nhat"],
            PostStatus.Published,
            "NOIR gi\u1ea3m gi\u00e1 l\u1edbn nh\u00e2n d\u1ecbp m\u00f9a h\u00e8 - c\u01a1 h\u1ed9i s\u1eafm \u0111\u1ed3 ch\u1ea5t l\u01b0\u1ee3ng v\u1edbi gi\u00e1 t\u1ed1t nh\u1ea5t.",
            """
            <h2>NOIR Summer Sale 2026 - Giảm sốc đến 50%</h2>
            <p>Nhân dịp mùa hè 2026, NOIR tri ân khách hàng với chương trình giảm giá lớn nhất trong năm. Đây là cơ hội tuyệt vời để bạn làm mới tủ quần áo với những món đồ chất lượng.</p>
            <h2>Các mức giảm giá</h2>
            <ul>
                <li><strong>Giảm 30%:</strong> Tất cả áo thun và áo polo</li>
                <li><strong>Giảm 40%:</strong> Quần jeans và quần kaki</li>
                <li><strong>Giảm 50%:</strong> Phụ kiện: túi xách, thắt lưng, mũ</li>
            </ul>
            <h2>Điều kiện áp dụng</h2>
            <p>Chương trình áp dụng từ 01/06 đến 31/07/2026 cho tất cả đơn hàng online và tại cửa hàng. Giảm giá được áp dụng trực tiếp, không cần mã. Không cộng dồn với các chương trình khuyến mãi khác.</p>
            <p><strong>Lưu ý:</strong> Số lượng có hạn, hãy mua sắm sớm để không bỏ lỡ những món đồ yêu thích nhé!</p>
            """,
            "#EF4444",
            -5),

        // === 2 Draft posts ===
        new(
            "B\u1ed9 s\u01b0u t\u1eadp Thu \u0110\u00f4ng 2026 s\u1eafp ra m\u1eaft",
            "bo-suu-tap-thu-dong-2026-sap-ra-mat",
            "phong-cach-song",
            ["moi-nhat", "xu-huong"],
            PostStatus.Draft,
            "Nh\u00e1 h\u00e0ng b\u1ed9 s\u01b0u t\u1eadp Thu \u0110\u00f4ng 2026 v\u1edbi nh\u1eefng thi\u1ebft k\u1ebf \u0111\u1ed9t ph\u00e1.",
            """
            <h2>NOIR Thu Đông 2026 - Sắp ra mắt</h2>
            <p>Chúng tôi rất vui được thông báo rằng bộ sưu tập Thu Đông 2026 của NOIR sẽ chính thức ra mắt vào tháng 9. Bộ sưu tập lần này lấy cảm hứng từ vẻ đẹp của miền Bắc Việt Nam vào mùa thu, với tông màu ấm áp và chất liệu giữ nhiệt cao cấp.</p>
            <h2>Điểm nhấn của bộ sưu tập</h2>
            <ul>
                <li>Áo khoác lông cừ</li>
                <li>Áo len cashmere blend</li>
                <li>Khăn choang cổ handmade</li>
                <li>Giày boot da lộn</li>
            </ul>
            <p><em>Bài viết đang được hoàn thiện, vui lòng quay lại sau...</em></p>
            """,
            "#8B4513",
            -2),

        new(
            "B\u00ed quy\u1ebft b\u1ea3o qu\u1ea3n qu\u1ea7n \u00e1o \u0111\u00fang c\u00e1ch",
            "bi-quyet-bao-quan-quan-ao-dung-cach",
            "meo-mua-sam",
            ["meo-hay", "huong-dan"],
            PostStatus.Draft,
            "H\u01b0\u1edbng d\u1eabn chi ti\u1ebft c\u00e1ch b\u1ea3o qu\u1ea3n qu\u1ea7n \u00e1o \u0111\u1ec3 lu\u00f4n m\u1edbi v\u00e0 b\u1ec1n l\u00e2u.",
            """
            <h2>Bảo quản quần áo đúng cách để luôn như mới</h2>
            <p>Nhiều người đầu tư vào quần áo chất lượng nhưng lại không biết cách bảo quản đúng, khiến quần áo nhanh chóng xuống cấp. Dưới đây là một số mẹo hữu ích.</p>
            <h2>1. Giặt đúng cách</h2>
            <ul>
                <li>Lộn trái áo trước khi giặt</li>
                <li>Giặt nước lạnh với màu đậm</li>
                <li>Tách riêng quần áo trắng và màu</li>
                <li>Dùng túi giặt cho đồ nhạy cảm</li>
            </ul>
            <h2>2. Phơi và cất giữ</h2>
            <p>Không phơi trực tiếp dưới nắng gắt, đặc biệt với quần áo màu. Dùng móc áo phù hợp để giữ form. Cất áo len gấp thay vì treo để tránh giãn.</p>
            <p><em>Bài viết đang được hoàn thiện...</em></p>
            """,
            "#14B8A6",
            -1),

        // === 1 Scheduled post ===
        new(
            "Black Friday 2026 - L\u1ecbch gi\u1ea3m gi\u00e1 \u0111\u1ed9c quy\u1ec1n",
            "black-friday-2026-lich-giam-gia-doc-quyen",
            "meo-mua-sam",
            ["khuyen-mai", "tin-tuc"],
            PostStatus.Scheduled,
            "Chu\u1ea9n b\u1ecb cho Black Friday 2026 v\u1edbi l\u1ecbch gi\u1ea3m gi\u00e1 \u0111\u1ed9c quy\u1ec1n ch\u1ec9 c\u00f3 t\u1ea1i NOIR.",
            """
            <h2>Black Friday 2026 tại NOIR</h2>
            <p>Năm nay, NOIR mang đến chương trình Black Friday lớn nhất từ trước đến nay với nhiều ưu đãi hấp dẫn.</p>
            <h2>Lịch giảm giá</h2>
            <ul>
                <li><strong>25/11:</strong> Early Access - Giảm 20% cho thành viên VIP</li>
                <li><strong>26/11:</strong> Flash Sale - Giảm 50% các sản phẩm chọn lọc (số lượng giới hạn)</li>
                <li><strong>27/11 - Black Friday:</strong> Giảm 30-60% toàn bộ cửa hàng</li>
                <li><strong>28-29/11:</strong> Weekend Deals - Giảm thêm 10% cho đơn từ 1.000.000đ</li>
            </ul>
            <h2>Ưu đãi đặc biệt</h2>
            <p>Free ship toàn quốc cho mọi đơn hàng trong suốt chương trình. Tặng voucher 100.000đ cho lần mua tiếp theo khi đơn hàng từ 2.000.000đ.</p>
            <p>Hãy theo dõi NOIR để nhận thông báo sớm nhất về các ưu đãi!</p>
            """,
            "#1F2937",
            7),

        // === 1 Archived post ===
        new(
            "T\u1ed5ng k\u1ebft n\u0103m 2025 - C\u1ea3m \u01a1n kh\u00e1ch h\u00e0ng",
            "tong-ket-nam-2025-cam-on-khach-hang",
            "phong-cach-song",
            ["tin-tuc"],
            PostStatus.Archived,
            "Nh\u00ecn l\u1ea1i m\u1ed9t n\u0103m \u0111\u1ea7y th\u00e0nh c\u00f4ng c\u00f9ng NOIR v\u00e0 g\u1eedi l\u1eddi c\u1ea3m \u01a1n ch\u00e2n th\u00e0nh.",
            """
            <h2>Cảm ơn một năm tuyệt vời cùng NOIR</h2>
            <p>Năm 2025 đã khép lại với nhiều thành công vượt mong đợi. Chúng tôi xin gửi lời cảm ơn sâu sắc đến tất cả khách hàng đã đồng hành cùng NOIR trong suốt một năm qua.</p>
            <h2>Những con số nổi bật</h2>
            <ul>
                <li>Hơn 50.000 khách hàng tin tưởng</li>
                <li>3 cửa hàng mới khai trương</li>
                <li>200+ sản phẩm mới ra mắt</li>
                <li>Tỉ lệ hài lòng 98%</li>
            </ul>
            <h2>Hướng tới năm 2026</h2>
            <p>Trong năm mới, chúng tôi cam kết tiếp tục mang đến những sản phẩm chất lượng với giá cả hợp lý. Nhiều bộ sưu tập mới sẽ được ra mắt, cùng với các chương trình ưu đãi hấp dẫn dành riêng cho thành viên thân thiết.</p>
            <p>Chúc bạn một năm mới thật nhiều niềm vui và thành công!</p>
            """,
            "#6A1B9A",
            -120)
    ];
}

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
            <h2>Xu h\u01b0\u1edbng th\u1eddi trang nam 2026: Nh\u1eefng \u0111i\u1ec1u b\u1ea1n c\u1ea7n bi\u1ebft</h2>
            <p>N\u0103m 2026 \u0111\u00e1nh d\u1ea5u s\u1ef1 tr\u1edf l\u1ea1i c\u1ee7a nhi\u1ec1u phong c\u00e1ch th\u1eddi trang c\u1ed5 \u0111i\u1ec3n, k\u1ebft h\u1ee3p v\u1edbi nh\u1eefng y\u1ebfu t\u1ed1 hi\u1ec7n \u0111\u1ea1i \u0111\u1ec3 t\u1ea1o n\u00ean xu h\u01b0\u1edbng m\u1edbi \u0111\u1ed9c \u0111\u00e1o. H\u00e3y c\u00f9ng NOIR kh\u00e1m ph\u00e1 10 xu h\u01b0\u1edbng n\u1ed5i b\u1eadt nh\u1ea5t trong b\u00e0i vi\u1ebft n\u00e0y.</p>
            <h2>1. Minimalist - \u0110\u01a1n gi\u1ea3n l\u00e0 \u0111\u1ec9nh cao</h2>
            <p>Phong c\u00e1ch t\u1ed1i gi\u1ea3n ti\u1ebfp t\u1ee5c l\u00ean ng\u00f4i v\u1edbi nh\u1eefng thi\u1ebft k\u1ebf g\u1ecdn g\u00e0ng, m\u00e0u s\u1eafc trung t\u00ednh nh\u01b0 \u0111en, tr\u1eafng, x\u00e1m v\u00e0 be. C\u00e1c item c\u01a1 b\u1ea3n nh\u01b0 \u00e1o thun tr\u01a1n, qu\u1ea7n kaki v\u00e0 \u00e1o kho\u00e1c nh\u1eb9 l\u00e0 nh\u1eefng m\u00f3n \u0111\u1ed3 kh\u00f4ng th\u1ec3 thi\u1ebfu.</p>
            <h2>2. Streetwear ph\u00e1 c\u00e1ch</h2>
            <p>\u0110\u01b0\u1eddng ph\u1ed1 v\u1eabn l\u00e0 ngu\u1ed3n c\u1ea3m h\u1ee9ng v\u00f4 t\u1eadn cho th\u1eddi trang nam. Nh\u1eefng chi\u1ebfc \u00e1o oversized, qu\u1ea7n jogger v\u00e0 gi\u00e0y sneaker \u0111\u1ed9c \u0111\u00e1o s\u1ebd l\u00e0 l\u1ef1a ch\u1ecdn h\u00e0ng \u0111\u1ea7u cho nh\u1eefng ch\u00e0ng trai \u0111\u00e3 ch\u00e1n phong c\u00e1ch truy\u1ec1n th\u1ed1ng.</p>
            <h2>3. Smart Casual linh ho\u1ea1t</h2>
            <p>S\u1ef1 k\u1ebft h\u1ee3p gi\u1eefa trang ph\u1ee5c c\u00f4ng s\u1edf v\u00e0 \u0111\u1eddi th\u01b0\u1eddng ng\u00e0y c\u00e0ng \u0111\u01b0\u1ee3c \u01b0a chu\u1ed9ng. M\u1ed9t chi\u1ebfc \u00e1o polo k\u1ebft h\u1ee3p v\u1edbi qu\u1ea7n chinos v\u00e0 gi\u00e0y l\u01b0\u1eddi s\u1ebd gi\u00fap b\u1ea1n v\u1eeba l\u1ecbch s\u1ef1 v\u1eeba tho\u1ea3i m\u00e1i trong m\u1ecdi ho\u00e0n c\u1ea3nh.</p>
            <h2>4. Sustainable Fashion</h2>
            <p>Th\u1eddi trang b\u1ec1n v\u1eefng kh\u00f4ng c\u00f2n l\u00e0 xu h\u01b0\u1edbng m\u00e0 \u0111\u00e3 tr\u1edf th\u00e0nh ti\u00eau chu\u1ea9n. C\u00e1c th\u01b0\u01a1ng hi\u1ec7u nh\u01b0 NOIR cam k\u1ebft s\u1eed d\u1ee5ng nguy\u00ean li\u1ec7u th\u00e2n thi\u1ec7n v\u1edbi m\u00f4i tr\u01b0\u1eddng, mang \u0111\u1ebfn cho b\u1ea1n s\u1ef1 l\u1ef1a ch\u1ecdn c\u00f3 tr\u00e1ch nhi\u1ec7m h\u01a1n.</p>
            <ul>
                <li>Ch\u1ea5t li\u1ec7u cotton h\u1eefu c\u01a1</li>
                <li>Quy tr\u00ecnh s\u1ea3n xu\u1ea5t ti\u1ebft ki\u1ec7m n\u01b0\u1edbc</li>
                <li>Bao b\u00ec t\u00e1i ch\u1ebf 100%</li>
            </ul>
            <h2>5. Color Blocking</h2>
            <p>S\u1ef1 k\u1ebft h\u1ee3p m\u00e0u s\u1eafc t\u01b0\u01a1ng ph\u1ea3n tr\u00ean c\u00f9ng m\u1ed9t trang ph\u1ee5c \u0111ang l\u00e0 xu h\u01b0\u1edbng hot. H\u00e3y th\u1eed ph\u1ed1i \u00e1o xanh v\u1edbi qu\u1ea7n cam, ho\u1eb7c \u00e1o \u0111\u1ecf v\u1edbi qu\u1ea7n tr\u1eafng \u0111\u1ec3 t\u1ea1o \u0111i\u1ec3m nh\u1ea5n cho outfit c\u1ee7a m\u00ecnh.</p>
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
            <h2>\u0110\u00e1nh gi\u00e1 \u00e1o kho\u00e1c gi\u00f3 NOIR Sport sau 2 th\u00e1ng s\u1eed d\u1ee5ng</h2>
            <p>Sau 2 th\u00e1ng s\u1eed d\u1ee5ng th\u1ef1c t\u1ebf trong nhi\u1ec1u \u0111i\u1ec1u ki\u1ec7n th\u1eddi ti\u1ebft kh\u00e1c nhau, m\u00ecnh xin chia s\u1ebb nh\u1eefng \u0111\u00e1nh gi\u00e1 kh\u00e1ch quan nh\u1ea5t v\u1ec1 chi\u1ebfc \u00e1o kho\u00e1c gi\u00f3 NOIR Sport.</p>
            <h2>Thi\u1ebft k\u1ebf v\u00e0 ch\u1ea5t li\u1ec7u</h2>
            <p>Chi\u1ebfc \u00e1o \u0111\u01b0\u1ee3c l\u00e0m t\u1eeb v\u1ea3i nylon ripstop, c\u1ea3m gi\u00e1c r\u1ea5t nh\u1eb9 v\u00e0 m\u1ecfng nh\u01b0ng v\u1eabn \u0111\u1ee7 ch\u1eafn gi\u00f3 t\u1ed1t. Ph\u1ea7n kh\u00f3a k\u00e9o ch\u1ea5t l\u01b0\u1ee3ng YKK ch\u1ea1y r\u1ea5t m\u01b0\u1ee3t. T\u00fai \u0111\u1ef1ng \u0111\u01b0\u1ee3c thi\u1ebft k\u1ebf th\u00f4ng minh \u0111\u1ec3 g\u1eadp g\u1ecdn \u00e1o khi kh\u00f4ng s\u1eed d\u1ee5ng.</p>
            <h2>Kh\u1ea3 n\u0103ng ch\u1ed1ng n\u01b0\u1edbc</h2>
            <p>\u00c1o \u0111\u01b0\u1ee3c qu\u1ea3ng c\u00e1o l\u00e0 ch\u1ed1ng n\u01b0\u1edbc c\u1ea5p \u0111\u1ed9 3, v\u00e0 th\u1ef1c t\u1ebf khi g\u1eb7p m\u01b0a nh\u1ecf \u0111\u1ebfn v\u1eeba, n\u01b0\u1edbc l\u0103n tr\u00ean b\u1ec1 m\u1eb7t \u00e1o m\u00e0 kh\u00f4ng th\u1ea5m v\u00e0o. Tuy nhi\u00ean v\u1edbi m\u01b0a l\u1edbn k\u00e9o d\u00e0i th\u00ec v\u1eabn s\u1ebd th\u1ea5m \u1edf ph\u1ea7n \u0111\u01b0\u1eddng may.</p>
            <h2>\u0110\u00e1nh gi\u00e1 t\u1ed5ng th\u1ec3</h2>
            <ul>
                <li><strong>Thi\u1ebft k\u1ebf:</strong> 9/10 - G\u1ecdn g\u00e0ng, hi\u1ec7n \u0111\u1ea1i</li>
                <li><strong>Ch\u1ea5t l\u01b0\u1ee3ng:</strong> 8/10 - T\u1ed1t trong t\u1ea7m gi\u00e1</li>
                <li><strong>Ti\u1ec7n d\u1ee5ng:</strong> 9/10 - Nh\u1eb9, g\u1eadp g\u1ecdn d\u1ec5 d\u00e0ng</li>
                <li><strong>Gi\u00e1:</strong> 8/10 - H\u1ee3p l\u00fd cho ch\u1ea5t l\u01b0\u1ee3ng nh\u1eadn \u0111\u01b0\u1ee3c</li>
            </ul>
            <p><strong>K\u1ebft lu\u1eadn:</strong> V\u1edbi m\u1ee9c gi\u00e1 750.000\u0111, \u00e1o kho\u00e1c gi\u00f3 NOIR Sport l\u00e0 m\u1ed9t l\u1ef1a ch\u1ecdn x\u1ee9ng \u0111\u00e1ng cho nh\u1eefng ai c\u1ea7n m\u1ed9t chi\u1ebfc \u00e1o kho\u00e1c nh\u1eb9, \u0111a n\u0103ng cho cu\u1ed9c s\u1ed1ng h\u00e0ng ng\u00e0y.</p>
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
            <h2>L\u00e0m sao \u0111\u1ec3 ch\u1ecdn \u0111\u00fang size \u00e1o thun khi mua online?</h2>
            <p>Mua s\u1eafm online ti\u1ec7n l\u1ee3i nh\u01b0ng nhi\u1ec1u ng\u01b0\u1eddi v\u1eabn lo l\u1eafng v\u1ec1 vi\u1ec7c ch\u1ecdn sai size. B\u00e0i vi\u1ebft n\u00e0y s\u1ebd gi\u00fap b\u1ea1n bi\u1ebft c\u00e1ch \u0111o v\u00e0 ch\u1ecdn size chu\u1ea9n nh\u1ea5t.</p>
            <h2>B\u01b0\u1edbc 1: \u0110o s\u1ed1 \u0111o c\u01a1 th\u1ec3</h2>
            <p>B\u1ea1n c\u1ea7n chu\u1ea9n b\u1ecb m\u1ed9t th\u01b0\u1edbc d\u00e2y v\u00e0 \u0111o c\u00e1c s\u1ed1 \u0111o sau:</p>
            <ul>
                <li><strong>Vong ng\u1ef1c:</strong> \u0110o v\u00f2ng quanh ng\u1ef1c t\u1ea1i \u0111i\u1ec3m r\u1ed9ng nh\u1ea5t</li>
                <li><strong>Chi\u1ec1u d\u00e0i \u00e1o:</strong> T\u1eeb vai \u0111\u1ebfn g\u1ea5u \u00e1o</li>
                <li><strong>Vai:</strong> T\u1eeb m\u1ed1i vai tr\u00e1i sang m\u1ed1i vai ph\u1ea3i</li>
            </ul>
            <h2>B\u01b0\u1edbc 2: So s\u00e1nh v\u1edbi b\u1ea3ng size</h2>
            <p>M\u1ed7i th\u01b0\u01a1ng hi\u1ec7u c\u00f3 b\u1ea3ng size ri\u00eang, nh\u01b0ng th\u00f4ng th\u01b0\u1eddng v\u1edbi size Vi\u1ec7t Nam:</p>
            <ul>
                <li><strong>Size S:</strong> Ng\u1ef1c 84-88cm, d\u00e0i 66cm, vai 40cm</li>
                <li><strong>Size M:</strong> Ng\u1ef1c 88-92cm, d\u00e0i 68cm, vai 42cm</li>
                <li><strong>Size L:</strong> Ng\u1ef1c 92-96cm, d\u00e0i 70cm, vai 44cm</li>
                <li><strong>Size XL:</strong> Ng\u1ef1c 96-100cm, d\u00e0i 72cm, vai 46cm</li>
            </ul>
            <h2>M\u1eb9o nh\u1ecf khi ch\u1ecdn size</h2>
            <p>N\u1ebfu b\u1ea1n \u1edf gi\u1eefa 2 size, h\u00e3y ch\u1ecdn size l\u1edbn h\u01a1n \u0111\u1ec3 tho\u1ea3i m\u00e1i h\u01a1n. V\u1ea3i cotton sau v\u00e0i l\u1ea7n gi\u1eb7t s\u1ebd h\u01a1i co l\u1ea1i n\u00ean vi\u1ec7c ch\u1ecdn r\u1ed9ng h\u01a1n m\u1ed9t ch\u00fat l\u00e0 h\u1ee3p l\u00fd. T\u1ea1i NOIR, ch\u00fang t\u00f4i c\u00f3 ch\u00ednh s\u00e1ch \u0111\u1ed5i size mi\u1ec5n ph\u00ed trong 30 ng\u00e0y.</p>
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
            <h2>Qu\u1ea7n jeans - Item \u0111a n\u0103ng nh\u1ea5t trong t\u1ee7 \u0111\u1ed3</h2>
            <p>Qu\u1ea7n jeans l\u00e0 m\u1ed9t trong nh\u1eefng item kh\u00f4ng bao gi\u1edd l\u1ed7i m\u1ed1t. D\u01b0\u1edbi \u0111\u00e2y l\u00e0 5 c\u00e1ch ph\u1ed1i \u0111\u1ed3 v\u1edbi qu\u1ea7n jeans m\u00e0 b\u1ea1n c\u00f3 th\u1ec3 \u00e1p d\u1ee5ng ngay.</p>
            <h2>1. Jeans + \u00c1o thun tr\u01a1n</h2>
            <p>S\u1ef1 k\u1ebft h\u1ee3p kinh \u0111i\u1ec3n v\u00e0 \u0111\u01a1n gi\u1ea3n nh\u1ea5t. Ch\u1ecdn \u00e1o thun m\u00e0u tr\u01a1n nh\u01b0 tr\u1eafng, \u0111en ho\u1eb7c x\u00e1m \u0111\u1ec3 t\u1ea1o phong c\u00e1ch casual g\u1ecdn g\u00e0ng.</p>
            <h2>2. Jeans + \u00c1o s\u01a1 mi</h2>
            <p>Mu\u1ed1n l\u1ecbch s\u1ef1 h\u01a1n? H\u00e3y k\u1ebft h\u1ee3p jeans v\u1edbi \u00e1o s\u01a1 mi. X\u1eafn tay \u00e1o l\u00ean v\u00e0 \u0111\u1ec3 t\u00e0 \u00e1o ngo\u00e0i qu\u1ea7n \u0111\u1ec3 tr\u00f4ng tho\u1ea3i m\u00e1i nh\u01b0ng v\u1eabn thanh l\u1ecbch.</p>
            <h2>3. Jeans + Blazer</h2>
            <p>\u0110\u00e2y l\u00e0 c\u00f4ng th\u1ee9c smart casual ho\u00e0n h\u1ea3o. Jeans slim fit k\u1ebft h\u1ee3p v\u1edbi blazer v\u00e0 gi\u00e0y Chelsea boot s\u1ebd l\u00e0m b\u1ea1n n\u1ed5i b\u1eadt trong c\u00e1c bu\u1ed5i g\u1eb7p m\u1eb7t b\u00e1n trang tr\u1ecdng.</p>
            <h2>4. Jeans + \u00c1o kho\u00e1c bomber</h2>
            <p>Phong c\u00e1ch streetwear c\u1ef1c ch\u1ea5t. Ch\u1ecdn \u00e1o kho\u00e1c bomber v\u1edbi jeans r\u00e1ch nh\u1eb9 v\u00e0 sneaker tr\u1eafng \u0111\u1ec3 th\u1ec3 hi\u1ec7n c\u00e1 t\u00ednh ri\u00eang.</p>
            <h2>5. Jeans + \u00c1o polo</h2>
            <p>\u0110\u01a1n gi\u1ea3n nh\u01b0ng kh\u00f4ng h\u1ec1 \u0111\u01a1n \u0111i\u1ec7u. \u00c1o polo mang l\u1ea1i v\u1ebb l\u1ecbch l\u00e3m m\u00e0 v\u1eabn tho\u1ea3i m\u00e1i. R\u1ea5t ph\u00f9 h\u1ee3p cho c\u00e1c bu\u1ed5i h\u1eb9n h\u00f2 ho\u1eb7c \u0111i c\u00e0 ph\u00ea cu\u1ed1i tu\u1ea7n.</p>
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
            <h2>Ch\u1ea5t li\u1ec7u v\u1ea3i: Bi\u1ebft \u0111\u1ec3 ch\u1ecdn \u0111\u00fang</h2>
            <p>Khi mua qu\u1ea7n \u00e1o, nhi\u1ec1u ng\u01b0\u1eddi ch\u1ec9 quan t\u00e2m \u0111\u1ebfn m\u1eabu m\u00e3 m\u00e0 qu\u00ean m\u1ea5t ch\u1ea5t li\u1ec7u v\u1ea3i - y\u1ebfu t\u1ed1 quy\u1ebft \u0111\u1ecbnh \u0111\u1ed9 b\u1ec1n v\u00e0 tho\u1ea3i m\u00e1i c\u1ee7a s\u1ea3n ph\u1ea9m.</p>
            <h2>Cotton - V\u1ea3i t\u1ef1 nhi\u00ean s\u1ed1 1</h2>
            <p>Cotton l\u00e0 ch\u1ea5t li\u1ec7u t\u1ef1 nhi\u00ean \u0111\u01b0\u1ee3c y\u00eau th\u00edch nh\u1ea5t. \u01afu \u0111i\u1ec3m l\u1edbn nh\u1ea5t l\u00e0 th\u1ea5m h\u00fat m\u1ed3 h\u00f4i t\u1ed1t, m\u1ec1m m\u1ecbn v\u00e0 th\u00e2n thi\u1ec7n v\u1edbi da. Tuy nhi\u00ean, cotton d\u1ec5 nh\u0103n v\u00e0 co r\u00fat sau khi gi\u1eb7t.</p>
            <h2>Polyester - B\u1ec1n b\u1ec9 v\u01b0\u1ee3t tr\u1ed9i</h2>
            <p>Polyester l\u00e0 s\u1ee3i t\u1ed5ng h\u1ee3p c\u00f3 \u0111\u1ed9 b\u1ec1n cao, kh\u00f4ng nh\u0103n, nhanh kh\u00f4 v\u00e0 gi\u1eef m\u00e0u t\u1ed1t. \u0110\u00e2y l\u00e0 l\u1ef1a ch\u1ecdn tuy\u1ec7t v\u1eddi cho \u0111\u1ed3 th\u1ec3 thao v\u00e0 trang ph\u1ee5c outdoor. Nh\u01b0\u1ee3c \u0111i\u1ec3m l\u00e0 kh\u00f4ng th\u1ea5m h\u00fat m\u1ed3 h\u00f4i t\u1ed1t v\u00e0 c\u00f3 th\u1ec3 g\u00e2y n\u00f3ng b\u1ee9c.</p>
            <h2>Cotton Blend - S\u1ef1 k\u1ebft h\u1ee3p ho\u00e0n h\u1ea3o</h2>
            <p>V\u1ea3i pha cotton v\u00e0 polyester (th\u01b0\u1eddng l\u00e0 t\u1ec9 l\u1ec7 60/40 ho\u1eb7c 65/35) k\u1ebft h\u1ee3p \u01b0u \u0111i\u1ec3m c\u1ee7a c\u1ea3 hai: m\u1ec1m m\u1ea1i c\u1ee7a cotton v\u00e0 \u0111\u1ed9 b\u1ec1n c\u1ee7a polyester. \u0110\u00e2y l\u00e0 ch\u1ea5t li\u1ec7u \u0111\u01b0\u1ee3c NOIR s\u1eed d\u1ee5ng nhi\u1ec1u trong c\u00e1c d\u00f2ng \u00e1o polo v\u00e0 \u00e1o s\u01a1 mi.</p>
            <ul>
                <li><strong>Cotton 100%:</strong> Th\u00edch h\u1ee3p cho \u00e1o thun, \u0111\u1ed3 m\u1eb7c nh\u00e0</li>
                <li><strong>Polyester:</strong> Th\u00edch h\u1ee3p cho \u0111\u1ed3 th\u1ec3 thao, \u00e1o kho\u00e1c</li>
                <li><strong>Blend:</strong> Th\u00edch h\u1ee3p cho \u00e1o polo, s\u01a1 mi, trang ph\u1ee5c h\u00e0ng ng\u00e0y</li>
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
            <h2>NOIR Summer Sale 2026 - Gi\u1ea3m s\u1ed1c \u0111\u1ebfn 50%</h2>
            <p>Nh\u00e2n d\u1ecbp m\u00f9a h\u00e8 2026, NOIR tri \u00e2n kh\u00e1ch h\u00e0ng v\u1edbi ch\u01b0\u01a1ng tr\u00ecnh gi\u1ea3m gi\u00e1 l\u1edbn nh\u1ea5t trong n\u0103m. \u0110\u00e2y l\u00e0 c\u01a1 h\u1ed9i tuy\u1ec7t v\u1eddi \u0111\u1ec3 b\u1ea1n l\u00e0m m\u1edbi t\u1ee7 qu\u1ea7n \u00e1o v\u1edbi nh\u1eefng m\u00f3n \u0111\u1ed3 ch\u1ea5t l\u01b0\u1ee3ng.</p>
            <h2>C\u00e1c m\u1ee9c gi\u1ea3m gi\u00e1</h2>
            <ul>
                <li><strong>Gi\u1ea3m 30%:</strong> T\u1ea5t c\u1ea3 \u00e1o thun v\u00e0 \u00e1o polo</li>
                <li><strong>Gi\u1ea3m 40%:</strong> Qu\u1ea7n jeans v\u00e0 qu\u1ea7n kaki</li>
                <li><strong>Gi\u1ea3m 50%:</strong> Ph\u1ee5 ki\u1ec7n: t\u00fai x\u00e1ch, th\u1eaft l\u01b0ng, m\u0169</li>
            </ul>
            <h2>\u0110i\u1ec1u ki\u1ec7n \u00e1p d\u1ee5ng</h2>
            <p>Ch\u01b0\u01a1ng tr\u00ecnh \u00e1p d\u1ee5ng t\u1eeb 01/06 \u0111\u1ebfn 31/07/2026 cho t\u1ea5t c\u1ea3 \u0111\u01a1n h\u00e0ng online v\u00e0 t\u1ea1i c\u1eeda h\u00e0ng. Gi\u1ea3m gi\u00e1 \u0111\u01b0\u1ee3c \u00e1p d\u1ee5ng tr\u1ef1c ti\u1ebfp, kh\u00f4ng c\u1ea7n m\u00e3. Kh\u00f4ng c\u1ed9ng d\u1ed3n v\u1edbi c\u00e1c ch\u01b0\u01a1ng tr\u00ecnh khuy\u1ebfn m\u00e3i kh\u00e1c.</p>
            <p><strong>L\u01b0u \u00fd:</strong> S\u1ed1 l\u01b0\u1ee3ng c\u00f3 h\u1ea1n, h\u00e3y mua s\u1eafm s\u1edbm \u0111\u1ec3 kh\u00f4ng b\u1ecf l\u1ee1 nh\u1eefng m\u00f3n \u0111\u1ed3 y\u00eau th\u00edch nh\u00e9!</p>
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
            <h2>NOIR Thu \u0110\u00f4ng 2026 - S\u1eafp ra m\u1eaft</h2>
            <p>Ch\u00fang t\u00f4i r\u1ea5t vui \u0111\u01b0\u1ee3c th\u00f4ng b\u00e1o r\u1eb1ng b\u1ed9 s\u01b0u t\u1eadp Thu \u0110\u00f4ng 2026 c\u1ee7a NOIR s\u1ebd ch\u00ednh th\u1ee9c ra m\u1eaft v\u00e0o th\u00e1ng 9. B\u1ed9 s\u01b0u t\u1eadp l\u1ea7n n\u00e0y l\u1ea5y c\u1ea3m h\u1ee9ng t\u1eeb v\u1ebb \u0111\u1eb9p c\u1ee7a mi\u1ec1n B\u1eafc Vi\u1ec7t Nam v\u00e0o m\u00f9a thu, v\u1edbi t\u00f4ng m\u00e0u \u1ea5m \u00e1p v\u00e0 ch\u1ea5t li\u1ec7u gi\u1eef nhi\u1ec7t cao c\u1ea5p.</p>
            <h2>\u0110i\u1ec3m nh\u1ea5n c\u1ee7a b\u1ed9 s\u01b0u t\u1eadp</h2>
            <ul>
                <li>\u00c1o kho\u00e1c l\u00f4ng c\u1eeb</li>
                <li>\u00c1o len cashmere blend</li>
                <li>Kh\u0103n choang c\u1ed5 handmade</li>
                <li>Gi\u00e0y boot da l\u1ed9n</li>
            </ul>
            <p><em>B\u00e0i vi\u1ebft \u0111ang \u0111\u01b0\u1ee3c ho\u00e0n thi\u1ec7n, vui l\u00f2ng quay l\u1ea1i sau...</em></p>
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
            <h2>B\u1ea3o qu\u1ea3n qu\u1ea7n \u00e1o \u0111\u00fang c\u00e1ch \u0111\u1ec3 lu\u00f4n nh\u01b0 m\u1edbi</h2>
            <p>Nhi\u1ec1u ng\u01b0\u1eddi \u0111\u1ea7u t\u01b0 v\u00e0o qu\u1ea7n \u00e1o ch\u1ea5t l\u01b0\u1ee3ng nh\u01b0ng l\u1ea1i kh\u00f4ng bi\u1ebft c\u00e1ch b\u1ea3o qu\u1ea3n \u0111\u00fang, khi\u1ebfn qu\u1ea7n \u00e1o nhanh ch\u00f3ng xu\u1ed1ng c\u1ea5p. D\u01b0\u1edbi \u0111\u00e2y l\u00e0 m\u1ed9t s\u1ed1 m\u1eb9o h\u1eefu \u00edch.</p>
            <h2>1. Gi\u1eb7t \u0111\u00fang c\u00e1ch</h2>
            <ul>
                <li>L\u1ed9n tr\u00e1i \u00e1o tr\u01b0\u1edbc khi gi\u1eb7t</li>
                <li>Gi\u1eb7t n\u01b0\u1edbc l\u1ea1nh v\u1edbi m\u00e0u \u0111\u1eadm</li>
                <li>T\u00e1ch ri\u00eang qu\u1ea7n \u00e1o tr\u1eafng v\u00e0 m\u00e0u</li>
                <li>D\u00f9ng t\u00fai gi\u1eb7t cho \u0111\u1ed3 nh\u1ea1y c\u1ea3m</li>
            </ul>
            <h2>2. Ph\u01a1i v\u00e0 c\u1ea5t gi\u1eef</h2>
            <p>Kh\u00f4ng ph\u01a1i tr\u1ef1c ti\u1ebfp d\u01b0\u1edbi n\u1eafng g\u1eaft, \u0111\u1eb7c bi\u1ec7t v\u1edbi qu\u1ea7n \u00e1o m\u00e0u. D\u00f9ng m\u00f3c \u00e1o ph\u00f9 h\u1ee3p \u0111\u1ec3 gi\u1eef form. C\u1ea5t \u00e1o len g\u1ea5p thay v\u00ec treo \u0111\u1ec3 tr\u00e1nh gi\u00e3n.</p>
            <p><em>B\u00e0i vi\u1ebft \u0111ang \u0111\u01b0\u1ee3c ho\u00e0n thi\u1ec7n...</em></p>
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
            <h2>Black Friday 2026 t\u1ea1i NOIR</h2>
            <p>N\u0103m nay, NOIR mang \u0111\u1ebfn ch\u01b0\u01a1ng tr\u00ecnh Black Friday l\u1edbn nh\u1ea5t t\u1eeb tr\u01b0\u1edbc \u0111\u1ebfn nay v\u1edbi nhi\u1ec1u \u01b0u \u0111\u00e3i h\u1ea5p d\u1eabn.</p>
            <h2>L\u1ecbch gi\u1ea3m gi\u00e1</h2>
            <ul>
                <li><strong>25/11:</strong> Early Access - Gi\u1ea3m 20% cho th\u00e0nh vi\u00ean VIP</li>
                <li><strong>26/11:</strong> Flash Sale - Gi\u1ea3m 50% c\u00e1c s\u1ea3n ph\u1ea9m ch\u1ecdn l\u1ecdc (s\u1ed1 l\u01b0\u1ee3ng gi\u1edbi h\u1ea1n)</li>
                <li><strong>27/11 - Black Friday:</strong> Gi\u1ea3m 30-60% to\u00e0n b\u1ed9 c\u1eeda h\u00e0ng</li>
                <li><strong>28-29/11:</strong> Weekend Deals - Gi\u1ea3m th\u00eam 10% cho \u0111\u01a1n t\u1eeb 1.000.000\u0111</li>
            </ul>
            <h2>\u01afu \u0111\u00e3i \u0111\u1eb7c bi\u1ec7t</h2>
            <p>Free ship to\u00e0n qu\u1ed1c cho m\u1ecdi \u0111\u01a1n h\u00e0ng trong su\u1ed1t ch\u01b0\u01a1ng tr\u00ecnh. T\u1eb7ng voucher 100.000\u0111 cho l\u1ea7n mua ti\u1ebfp theo khi \u0111\u01a1n h\u00e0ng t\u1eeb 2.000.000\u0111.</p>
            <p>H\u00e3y theo d\u00f5i NOIR \u0111\u1ec3 nh\u1eadn th\u00f4ng b\u00e1o s\u1edbm nh\u1ea5t v\u1ec1 c\u00e1c \u01b0u \u0111\u00e3i!</p>
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
            <h2>C\u1ea3m \u01a1n m\u1ed9t n\u0103m tuy\u1ec7t v\u1eddi c\u00f9ng NOIR</h2>
            <p>N\u0103m 2025 \u0111\u00e3 kh\u00e9p l\u1ea1i v\u1edbi nhi\u1ec1u th\u00e0nh c\u00f4ng v\u01b0\u1ee3t mong \u0111\u1ee3i. Ch\u00fang t\u00f4i xin g\u1eedi l\u1eddi c\u1ea3m \u01a1n s\u00e2u s\u1eafc \u0111\u1ebfn t\u1ea5t c\u1ea3 kh\u00e1ch h\u00e0ng \u0111\u00e3 \u0111\u1ed3ng h\u00e0nh c\u00f9ng NOIR trong su\u1ed1t m\u1ed9t n\u0103m qua.</p>
            <h2>Nh\u1eefng con s\u1ed1 n\u1ed5i b\u1eadt</h2>
            <ul>
                <li>H\u01a1n 50.000 kh\u00e1ch h\u00e0ng tin t\u01b0\u1edfng</li>
                <li>3 c\u1eeda h\u00e0ng m\u1edbi khai tr\u01b0\u01a1ng</li>
                <li>200+ s\u1ea3n ph\u1ea9m m\u1edbi ra m\u1eaft</li>
                <li>T\u1ec9 l\u1ec7 h\u00e0i l\u00f2ng 98%</li>
            </ul>
            <h2>H\u01b0\u1edbng t\u1edbi n\u0103m 2026</h2>
            <p>Trong n\u0103m m\u1edbi, ch\u00fang t\u00f4i cam k\u1ebft ti\u1ebfp t\u1ee5c mang \u0111\u1ebfn nh\u1eefng s\u1ea3n ph\u1ea9m ch\u1ea5t l\u01b0\u1ee3ng v\u1edbi gi\u00e1 c\u1ea3 h\u1ee3p l\u00fd. Nhi\u1ec1u b\u1ed9 s\u01b0u t\u1eadp m\u1edbi s\u1ebd \u0111\u01b0\u1ee3c ra m\u1eaft, c\u00f9ng v\u1edbi c\u00e1c ch\u01b0\u01a1ng tr\u00ecnh \u01b0u \u0111\u00e3i h\u1ea5p d\u1eabn d\u00e0nh ri\u00eang cho th\u00e0nh vi\u00ean th\u00e2n thi\u1ebft.</p>
            <p>Ch\u00fac b\u1ea1n m\u1ed9t n\u0103m m\u1edbi th\u1eadt nhi\u1ec1u ni\u1ec1m vui v\u00e0 th\u00e0nh c\u00f4ng!</p>
            """,
            "#6A1B9A",
            -120)
    ];
}

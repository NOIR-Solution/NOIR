namespace NOIR.Infrastructure.Persistence.SeedData.Data;

// Record types for structured catalog seed data
public record CategoryDef(string Name, string Slug, string? ParentSlug, string? Description);

public record BrandDef(string Name, string Slug, string? Description);

public record AttributeDef(
    string Code,
    string Name,
    AttributeType Type,
    bool IsFilterable,
    AttributeValueDef[]? Values);

public record AttributeValueDef(string Value, string DisplayValue, string? ColorCode);

public record ProductDef(
    string Name,
    string Slug,
    string CategorySlug,
    string BrandSlug,
    decimal BasePrice,
    string ShortDescription,
    string? DescriptionHtml,
    VariantDef[] Variants,
    string ImageColor);

public record VariantDef(
    string Name,
    decimal Price,
    string? Sku,
    Dictionary<string, string>? Options,
    int Stock);

/// <summary>
/// Static catalog data for seed: categories, brands, attributes, and products.
/// All text uses proper Vietnamese Unicode. Prices are in VND.
/// </summary>
public static class CatalogData
{
    public static CategoryDef[] GetCategories() =>
    [
        // Parent categories
        new("Th\u1eddi trang nam", "thoi-trang-nam", null, "Qu\u1ea7n \u00e1o v\u00e0 ph\u1ee5 ki\u1ec7n d\u00e0nh cho nam gi\u1edbi"),
        new("Th\u1eddi trang n\u1eef", "thoi-trang-nu", null, "Qu\u1ea7n \u00e1o v\u00e0 ph\u1ee5 ki\u1ec7n d\u00e0nh cho n\u1eef gi\u1edbi"),
        new("Ph\u1ee5 ki\u1ec7n", "phu-kien", null, "\u0110\u1ed3ng h\u1ed3, k\u00ednh m\u1eaft, t\u00fai x\u00e1ch v\u00e0 nhi\u1ec1u h\u01a1n"),

        // Children of "Th\u1eddi trang nam"
        new("\u00c1o nam", "ao-nam", "thoi-trang-nam", "\u00c1o thun, \u00e1o polo, \u00e1o s\u01a1 mi nam"),
        new("Qu\u1ea7n nam", "quan-nam", "thoi-trang-nam", "Qu\u1ea7n jeans, qu\u1ea7n kaki, qu\u1ea7n short nam"),

        // Children of "Th\u1eddi trang n\u1eef"
        new("\u00c1o n\u1eef", "ao-nu", "thoi-trang-nu", "\u00c1o thun, \u00e1o ki\u1ec3u, \u00e1o s\u01a1 mi n\u1eef")
    ];

    public static BrandDef[] GetBrands() =>
    [
        new("NOIR Basic", "noir-basic", "D\u00f2ng s\u1ea3n ph\u1ea9m c\u01a1 b\u1ea3n, gi\u00e1 t\u1ed1t cho m\u1ecdi ng\u01b0\u1eddi"),
        new("NOIR Premium", "noir-premium", "D\u00f2ng s\u1ea3n ph\u1ea9m cao c\u1ea5p, ch\u1ea5t li\u1ec7u th\u01b0\u1ee3ng h\u1ea1ng"),
        new("NOIR Sport", "noir-sport", "D\u00f2ng th\u1ec3 thao n\u0103ng \u0111\u1ed9ng, tho\u00e1ng m\u00e1t")
    ];

    public static AttributeDef[] GetAttributes() =>
    [
        new("color", "M\u00e0u s\u1eafc", AttributeType.Select, true,
        [
            new("red", "\u0110\u1ecf", "#EF4444"),
            new("blue", "Xanh d\u01b0\u01a1ng", "#3B82F6"),
            new("black", "\u0110en", "#1F2937"),
            new("white", "Tr\u1eafng", "#F9FAFB")
        ]),
        new("size", "K\u00edch c\u1ee1", AttributeType.Select, true,
        [
            new("s", "S", null),
            new("m", "M", null),
            new("l", "L", null),
            new("xl", "XL", null)
        ]),
        new("material", "Ch\u1ea5t li\u1ec7u", AttributeType.Text, false, null),
        new("weight", "Tr\u1ecdng l\u01b0\u1ee3ng (g)", AttributeType.Decimal, false, null),
        new("origin", "Xu\u1ea5t x\u1ee9", AttributeType.Text, true, null),
        new("warranty", "B\u1ea3o h\u00e0nh (th\u00e1ng)", AttributeType.Number, false, null),
        new("waterproof", "Ch\u1ed1ng n\u01b0\u1edbc", AttributeType.Boolean, true, null),
        new("style", "Phong c\u00e1ch", AttributeType.Select, true,
        [
            new("casual", "Casual", null),
            new("formal", "L\u1ecbch s\u1ef1", null),
            new("sporty", "Th\u1ec3 thao", null),
            new("streetwear", "Streetwear", null)
        ])
    ];

    public static ProductDef[] GetProducts() =>
    [
        // === \u00c1o nam (ao-nam) ===
        new("\u00c1o thun tr\u01a1n c\u1ed5 tr\u00f2n", "ao-thun-tron-co-tron", "ao-nam", "noir-basic",
            199000m,
            "\u00c1o thun cotton 100% tho\u00e1ng m\u00e1t, ph\u00f9 h\u1ee3p m\u1eb7c h\u00e0ng ng\u00e0y",
            "<h2>M\u00f4 t\u1ea3 s\u1ea3n ph\u1ea9m</h2><p>\u00c1o thun tr\u01a1n c\u1ed5 tr\u00f2n l\u00e0 item c\u01a1 b\u1ea3n kh\u00f4ng th\u1ec3 thi\u1ebfu trong t\u1ee7 \u0111\u1ed3 c\u1ee7a b\u1ea1n. Ch\u1ea5t li\u1ec7u cotton 100% m\u1ec1m m\u1ecbn, tho\u00e1ng kh\u00ed, ph\u00f9 h\u1ee3p cho th\u1eddi ti\u1ebft Vi\u1ec7t Nam.</p><ul><li>Cotton 100% cao c\u1ea5p</li><li>Form regular fit tho\u1ea3i m\u00e1i</li><li>Nhi\u1ec1u m\u00e0u s\u1eafc l\u1ef1a ch\u1ecdn</li></ul>",
            [
                new("S - Tr\u1eafng", 199000m, "NB-ATT-S-W", new() { ["size"] = "S", ["color"] = "Tr\u1eafng" }, 50),
                new("M - Tr\u1eafng", 199000m, "NB-ATT-M-W", new() { ["size"] = "M", ["color"] = "Tr\u1eafng" }, 80),
                new("L - \u0110en", 199000m, "NB-ATT-L-B", new() { ["size"] = "L", ["color"] = "\u0110en" }, 60),
                new("XL - \u0110en", 199000m, "NB-ATT-XL-B", new() { ["size"] = "XL", ["color"] = "\u0110en" }, 30)
            ],
            "#F9FAFB"),

        new("\u00c1o polo classic", "ao-polo-classic", "ao-nam", "noir-premium",
            450000m,
            "\u00c1o polo thi\u1ebft k\u1ebf tinh t\u1ebf, ph\u00f9 h\u1ee3p \u0111i l\u00e0m v\u00e0 d\u1ea1o ph\u1ed1",
            "<h2>M\u00f4 t\u1ea3 s\u1ea3n ph\u1ea9m</h2><p>\u00c1o polo classic v\u1edbi thi\u1ebft k\u1ebf c\u1ed5 b\u1ebb, ch\u1ea5t li\u1ec7u cotton pha polyester gi\u1eef form t\u1ed1t. Ph\u00f9 h\u1ee3p cho nhi\u1ec1u d\u1ecbp t\u1eeb \u0111i l\u00e0m \u0111\u1ebfn d\u1ea1o ph\u1ed1.</p>",
            [
                new("M - Xanh navy", 450000m, "NP-APC-M-NV", new() { ["size"] = "M", ["color"] = "Xanh navy" }, 40),
                new("L - Xanh navy", 450000m, "NP-APC-L-NV", new() { ["size"] = "L", ["color"] = "Xanh navy" }, 35),
                new("L - Tr\u1eafng", 450000m, "NP-APC-L-W", new() { ["size"] = "L", ["color"] = "Tr\u1eafng" }, 25)
            ],
            "#1E3A5F"),

        new("\u00c1o s\u01a1 mi Oxford", "ao-so-mi-oxford", "ao-nam", "noir-premium",
            599000m,
            "\u00c1o s\u01a1 mi Oxford ch\u1ea5t li\u1ec7u v\u1ea3i d\u00e0y d\u1eb7n, l\u1ecbch s\u1ef1",
            "<h2>M\u00f4 t\u1ea3 s\u1ea3n ph\u1ea9m</h2><p>S\u01a1 mi Oxford truy\u1ec1n th\u1ed1ng v\u1edbi ch\u1ea5t v\u1ea3i d\u00e0y d\u1eb7n \u0111\u1eb7c tr\u01b0ng. Thi\u1ebft k\u1ebf button-down c\u1ed5 \u0111i\u1ec3n, ph\u00f9 h\u1ee3p m\u1eb7c \u0111i l\u00e0m ho\u1eb7c d\u1ef1 ti\u1ec7c.</p>",
            [
                new("M - Xanh nh\u1ea1t", 599000m, "NP-SMO-M-XN", new() { ["size"] = "M", ["color"] = "Xanh nh\u1ea1t" }, 30),
                new("L - Xanh nh\u1ea1t", 599000m, "NP-SMO-L-XN", new() { ["size"] = "L", ["color"] = "Xanh nh\u1ea1t" }, 25),
                new("L - Tr\u1eafng", 599000m, "NP-SMO-L-W", new() { ["size"] = "L", ["color"] = "Tr\u1eafng" }, 20),
                new("XL - Tr\u1eafng", 599000m, "NP-SMO-XL-W", new() { ["size"] = "XL", ["color"] = "Tr\u1eafng" }, 15)
            ],
            "#B0C4DE"),

        new("\u00c1o kho\u00e1c gi\u00f3 nh\u1eb9", "ao-khoac-gio-nhe", "ao-nam", "noir-sport",
            750000m,
            "\u00c1o kho\u00e1c gi\u00f3 si\u00eau nh\u1eb9, ch\u1ed1ng n\u01b0\u1edbc, g\u1eadp g\u1ecdn",
            "<h2>M\u00f4 t\u1ea3 s\u1ea3n ph\u1ea9m</h2><p>\u00c1o kho\u00e1c gi\u00f3 v\u1edbi c\u00f4ng ngh\u1ec7 ch\u1ed1ng n\u01b0\u1edbc cao c\u1ea5p, si\u00eau nh\u1eb9 ch\u1ec9 200g. G\u1eadp g\u1ecdn b\u1ecf t\u00fai d\u1ec5 d\u00e0ng mang theo.</p><ul><li>Ch\u1ea5t li\u1ec7u nylon ripstop</li><li>Ch\u1ed1ng n\u01b0\u1edbc c\u1ea5p \u0111\u1ed9 3</li><li>G\u1eadp g\u1ecdn v\u00e0o t\u00fai \u0111\u1ef1ng ri\u00eang</li></ul>",
            [
                new("M - \u0110en", 750000m, "NS-AKG-M-B", new() { ["size"] = "M", ["color"] = "\u0110en" }, 20),
                new("L - \u0110en", 750000m, "NS-AKG-L-B", new() { ["size"] = "L", ["color"] = "\u0110en" }, 25),
                new("L - Xanh r\u00eau", 750000m, "NS-AKG-L-XR", new() { ["size"] = "L", ["color"] = "Xanh r\u00eau" }, 15)
            ],
            "#2D5F2D"),

        new("\u00c1o tank top th\u1ec3 thao", "ao-tank-top-the-thao", "ao-nam", "noir-sport",
            179000m,
            "\u00c1o ba l\u1ed7 th\u1ec3 thao tho\u00e1ng kh\u00ed, nhanh kh\u00f4",
            null,
            [
                new("M - \u0110en", 179000m, "NS-ATT-M-B", new() { ["size"] = "M", ["color"] = "\u0110en" }, 45),
                new("L - X\u00e1m", 179000m, "NS-ATT-L-G", new() { ["size"] = "L", ["color"] = "X\u00e1m" }, 40)
            ],
            "#374151"),

        // === Qu\u1ea7n nam (quan-nam) ===
        new("Qu\u1ea7n jeans slim fit", "quan-jeans-slim-fit", "quan-nam", "noir-basic",
            499000m,
            "Qu\u1ea7n jeans co gi\u00e3n nh\u1eb9, form slim fit tr\u1ebb trung",
            "<h2>M\u00f4 t\u1ea3 s\u1ea3n ph\u1ea9m</h2><p>Qu\u1ea7n jeans slim fit v\u1edbi ch\u1ea5t li\u1ec7u denim co gi\u00e3n, mang l\u1ea1i c\u1ea3m gi\u00e1c tho\u1ea3i m\u00e1i su\u1ed1t c\u1ea3 ng\u00e0y. M\u00e0u wash c\u1ed5 \u0111i\u1ec3n, d\u1ec5 ph\u1ed1i \u0111\u1ed3.</p>",
            [
                new("30 - Xanh \u0111\u1eadm", 499000m, "NB-QJS-30-XD", new() { ["size"] = "30", ["color"] = "Xanh \u0111\u1eadm" }, 30),
                new("31 - Xanh \u0111\u1eadm", 499000m, "NB-QJS-31-XD", new() { ["size"] = "31", ["color"] = "Xanh \u0111\u1eadm" }, 40),
                new("32 - Xanh nh\u1ea1t", 499000m, "NB-QJS-32-XN", new() { ["size"] = "32", ["color"] = "Xanh nh\u1ea1t" }, 35),
                new("33 - \u0110en", 499000m, "NB-QJS-33-B", new() { ["size"] = "33", ["color"] = "\u0110en" }, 25)
            ],
            "#1E3A5F"),

        new("Qu\u1ea7n kaki ch\u1ea5t", "quan-kaki-chat", "quan-nam", "noir-premium",
            550000m,
            "Qu\u1ea7n kaki cao c\u1ea5p, form regular fit thanh l\u1ecbch",
            "<h2>M\u00f4 t\u1ea3 s\u1ea3n ph\u1ea9m</h2><p>Qu\u1ea7n kaki ch\u1ea5t li\u1ec7u cotton pha spandex, m\u1ec1m m\u1ecbn v\u00e0 \u0111\u00e0n h\u1ed3i t\u1ed1t. Thi\u1ebft k\u1ebf regular fit v\u1eeba v\u1eb7n, ph\u00f9 h\u1ee3p \u0111i l\u00e0m.</p>",
            [
                new("31 - Be", 550000m, "NP-QKC-31-BE", new() { ["size"] = "31", ["color"] = "Be" }, 20),
                new("32 - Be", 550000m, "NP-QKC-32-BE", new() { ["size"] = "32", ["color"] = "Be" }, 30),
                new("32 - \u0110en", 550000m, "NP-QKC-32-B", new() { ["size"] = "32", ["color"] = "\u0110en" }, 25)
            ],
            "#C4A35A"),

        new("Qu\u1ea7n short th\u1ec3 thao", "quan-short-the-thao", "quan-nam", "noir-sport",
            299000m,
            "Qu\u1ea7n short th\u1ec3 thao tho\u00e1ng m\u00e1t, co gi\u00e3n 4 chi\u1ec1u",
            null,
            [
                new("M - \u0110en", 299000m, "NS-QST-M-B", new() { ["size"] = "M", ["color"] = "\u0110en" }, 50),
                new("L - X\u00e1m", 299000m, "NS-QST-L-G", new() { ["size"] = "L", ["color"] = "X\u00e1m" }, 45),
                new("XL - Xanh navy", 299000m, "NS-QST-XL-NV", new() { ["size"] = "XL", ["color"] = "Xanh navy" }, 30)
            ],
            "#374151"),

        new("Qu\u1ea7n jogger \u0111\u00e1y th\u1ee5n", "quan-jogger-day-thun", "quan-nam", "noir-basic",
            350000m,
            "Qu\u1ea7n jogger tho\u1ea3i m\u00e1i, \u0111\u00e1y th\u1ee5n \u0111\u00e0n h\u1ed3i, th\u00edch h\u1ee3p m\u1eb7c nh\u00e0 v\u00e0 d\u1ea1o ph\u1ed1",
            null,
            [
                new("M - X\u00e1m", 350000m, "NB-QJD-M-G", new() { ["size"] = "M", ["color"] = "X\u00e1m" }, 35),
                new("L - \u0110en", 350000m, "NB-QJD-L-B", new() { ["size"] = "L", ["color"] = "\u0110en" }, 40)
            ],
            "#6B7280"),

        // === \u00c1o n\u1eef (ao-nu) ===
        new("\u00c1o thun n\u1eef crop top", "ao-thun-nu-crop-top", "ao-nu", "noir-basic",
            179000m,
            "\u00c1o thun crop top tr\u1ebb trung, n\u0103ng \u0111\u1ed9ng",
            "<h2>M\u00f4 t\u1ea3 s\u1ea3n ph\u1ea9m</h2><p>\u00c1o crop top thi\u1ebft k\u1ebf tr\u1ebb trung, ph\u00f9 h\u1ee3p m\u1eb7c v\u1edbi qu\u1ea7n c\u1ea1p cao ho\u1eb7c ch\u00e2n v\u00e1y. Ch\u1ea5t li\u1ec7u cotton m\u1ec1m, m\u00e1t.</p>",
            [
                new("S - H\u1ed3ng", 179000m, "NB-ANCT-S-H", new() { ["size"] = "S", ["color"] = "H\u1ed3ng" }, 35),
                new("M - Tr\u1eafng", 179000m, "NB-ANCT-M-W", new() { ["size"] = "M", ["color"] = "Tr\u1eafng" }, 45),
                new("M - \u0110en", 179000m, "NB-ANCT-M-B", new() { ["size"] = "M", ["color"] = "\u0110en" }, 40)
            ],
            "#F472B6"),

        new("\u00c1o ki\u1ec3u tay ph\u1ed3ng", "ao-kieu-tay-phong", "ao-nu", "noir-premium",
            499000m,
            "\u00c1o ki\u1ec3u tay ph\u1ed3ng n\u1eef t\u00ednh, thanh l\u1ecbch",
            "<h2>M\u00f4 t\u1ea3 s\u1ea3n ph\u1ea9m</h2><p>\u00c1o ki\u1ec3u v\u1edbi tay ph\u1ed3ng \u0111\u1ed9c \u0111\u00e1o, t\u00f4n d\u00e1ng v\u00e0 n\u1eef t\u00ednh. Ch\u1ea5t li\u1ec7u voan cao c\u1ea5p, nh\u1eb9 nh\u00e0ng.</p>",
            [
                new("S - Tr\u1eafng", 499000m, "NP-AKTP-S-W", new() { ["size"] = "S", ["color"] = "Tr\u1eafng" }, 20),
                new("M - Tr\u1eafng", 499000m, "NP-AKTP-M-W", new() { ["size"] = "M", ["color"] = "Tr\u1eafng" }, 25),
                new("M - H\u1ed3ng ph\u1ea5n", 499000m, "NP-AKTP-M-HP", new() { ["size"] = "M", ["color"] = "H\u1ed3ng ph\u1ea5n" }, 15)
            ],
            "#FBB6CE"),

        new("\u00c1o s\u01a1 mi n\u1eef oversize", "ao-so-mi-nu-oversize", "ao-nu", "noir-basic",
            399000m,
            "\u00c1o s\u01a1 mi n\u1eef form oversize tho\u1ea3i m\u00e1i, phong c\u00e1ch",
            null,
            [
                new("Free size - Tr\u1eafng", 399000m, "NB-ASMNO-FS-W", new() { ["size"] = "Free size", ["color"] = "Tr\u1eafng" }, 30),
                new("Free size - Xanh nh\u1ea1t", 399000m, "NB-ASMNO-FS-XN", new() { ["size"] = "Free size", ["color"] = "Xanh nh\u1ea1t" }, 25)
            ],
            "#BFDBFE"),

        // === Ph\u1ee5 ki\u1ec7n (phu-kien) ===
        new("T\u00fai x\u00e1ch da th\u1eddi trang", "tui-xach-da-thoi-trang", "phu-kien", "noir-premium",
            1290000m,
            "T\u00fai x\u00e1ch da PU cao c\u1ea5p, thi\u1ebft k\u1ebf sang tr\u1ecdng",
            "<h2>M\u00f4 t\u1ea3 s\u1ea3n ph\u1ea9m</h2><p>T\u00fai x\u00e1ch l\u00e0m t\u1eeb da PU cao c\u1ea5p, m\u1ec1m m\u1ecbn v\u00e0 b\u1ec1n \u0111\u1eb9p. Nhi\u1ec1u ng\u0103n ti\u1ec7n l\u1ee3i, ph\u00f9 h\u1ee3p \u0111i l\u00e0m v\u00e0 d\u1ea1o ph\u1ed1.</p>",
            [
                new("\u0110en", 1290000m, "NP-TXDT-B", new() { ["color"] = "\u0110en" }, 15),
                new("N\u00e2u b\u00f2", 1290000m, "NP-TXDT-NB", new() { ["color"] = "N\u00e2u b\u00f2" }, 12)
            ],
            "#8B4513"),

        new("\u0110\u1ed3ng h\u1ed3 nam th\u1eddi trang", "dong-ho-nam-thoi-trang", "phu-kien", "noir-premium",
            1890000m,
            "\u0110\u1ed3ng h\u1ed3 nam m\u1eb7t tr\u00f2n c\u1ed5 \u0111i\u1ec3n, d\u00e2y da th\u1eadt",
            "<h2>M\u00f4 t\u1ea3 s\u1ea3n ph\u1ea9m</h2><p>\u0110\u1ed3ng h\u1ed3 nam thi\u1ebft k\u1ebf c\u1ed5 \u0111i\u1ec3n v\u1edbi m\u1eb7t s\u1ed1 r\u00f5 r\u00e0ng, d\u00e2y da th\u1eadt m\u1ec1m m\u1ea1i. Ch\u1ed1ng n\u01b0\u1edbc 3ATM, ph\u00f9 h\u1ee3p s\u1eed d\u1ee5ng h\u00e0ng ng\u00e0y.</p><ul><li>M\u00e1y Miyota Nh\u1eadt B\u1ea3n</li><li>K\u00ednh Sapphire ch\u1ed1ng tr\u1ea7y</li><li>Ch\u1ed1ng n\u01b0\u1edbc 3ATM</li></ul>",
            [
                new("D\u00e2y da \u0111en", 1890000m, "NP-DHNT-DD", new() { ["color"] = "\u0110en" }, 10),
                new("D\u00e2y da n\u00e2u", 1890000m, "NP-DHNT-DN", new() { ["color"] = "N\u00e2u" }, 8)
            ],
            "#3E2723"),

        new("K\u00ednh m\u00e1t ph\u00e2n c\u1ef1c", "kinh-mat-phan-cuc", "phu-kien", "noir-sport",
            699000m,
            "K\u00ednh m\u00e1t ph\u00e2n c\u1ef1c ch\u1ed1ng UV400, g\u1ecdng nh\u1eb9",
            null,
            [
                new("G\u1ecdng \u0111en - Tr\u00f2ng x\u00e1m", 699000m, "NS-KMPC-BG", new() { ["color"] = "\u0110en" }, 20),
                new("G\u1ecdng v\u00e0ng - Tr\u00f2ng n\u00e2u", 699000m, "NS-KMPC-GB", new() { ["color"] = "V\u00e0ng" }, 15)
            ],
            "#1F2937"),

        new("M\u0169 l\u01b0\u1ee1i trai unisex", "mu-luoi-trai-unisex", "phu-kien", "noir-basic",
            199000m,
            "M\u0169 l\u01b0\u1ee1i trai cotton, \u0111i\u1ec1u ch\u1ec9nh \u0111\u01b0\u1ee3c, unisex",
            null,
            [
                new("\u0110en", 199000m, "NB-MLT-B", new() { ["color"] = "\u0110en" }, 60),
                new("Tr\u1eafng", 199000m, "NB-MLT-W", new() { ["color"] = "Tr\u1eafng" }, 50),
                new("Xanh navy", 199000m, "NB-MLT-NV", new() { ["color"] = "Xanh navy" }, 40)
            ],
            "#1E3A5F"),

        new("Th\u1eaft l\u01b0ng da nam", "that-lung-da-nam", "phu-kien", "noir-premium",
            450000m,
            "Th\u1eaft l\u01b0ng da b\u00f2 th\u1eadt, kh\u00f3a t\u1ef1 \u0111\u1ed9ng sang tr\u1ecdng",
            "<h2>M\u00f4 t\u1ea3 s\u1ea3n ph\u1ea9m</h2><p>Th\u1eaft l\u01b0ng l\u00e0m t\u1eeb da b\u00f2 th\u1eadt 100%, kh\u00f3a t\u1ef1 \u0111\u1ed9ng b\u1eb1ng h\u1ee3p kim kh\u00f4ng g\u1ec9. B\u1ec1n \u0111\u1eb9p, l\u1ecbch l\u00e3m cho qu\u00fd \u00f4ng.</p>",
            [
                new("\u0110en - 110cm", 450000m, "NP-TLD-B-110", new() { ["color"] = "\u0110en", ["size"] = "110cm" }, 18),
                new("N\u00e2u - 110cm", 450000m, "NP-TLD-N-110", new() { ["color"] = "N\u00e2u", ["size"] = "110cm" }, 15),
                new("\u0110en - 120cm", 450000m, "NP-TLD-B-120", new() { ["color"] = "\u0110en", ["size"] = "120cm" }, 12)
            ],
            "#5D4037")
    ];
}

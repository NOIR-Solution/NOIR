namespace NOIR.Infrastructure.Media;

/// <summary>
/// Generates SEO-friendly slugs from filenames.
/// Removes special characters, normalizes unicode, and ensures URL-safe output.
/// </summary>
public static class SlugGenerator
{
    private static readonly Regex InvalidChars = new(@"[^a-z0-9\-]", RegexOptions.Compiled);
    private static readonly Regex MultipleHyphens = new(@"-+", RegexOptions.Compiled);
    private static readonly Regex LeadingTrailingHyphens = new(@"^-|-$", RegexOptions.Compiled);

    /// <summary>
    /// Unicode character replacements for common special characters.
    /// </summary>
    private static readonly Dictionary<char, string> CharReplacements = new()
    {
        ['à'] = "a", ['á'] = "a", ['â'] = "a", ['ã'] = "a", ['ä'] = "a", ['å'] = "a", ['æ'] = "ae",
        ['ç'] = "c", ['è'] = "e", ['é'] = "e", ['ê'] = "e", ['ë'] = "e",
        ['ì'] = "i", ['í'] = "i", ['î'] = "i", ['ï'] = "i",
        ['ñ'] = "n", ['ò'] = "o", ['ó'] = "o", ['ô'] = "o", ['õ'] = "o", ['ö'] = "o", ['ø'] = "o",
        ['ù'] = "u", ['ú'] = "u", ['û'] = "u", ['ü'] = "u",
        ['ý'] = "y", ['ÿ'] = "y", ['ß'] = "ss", ['œ'] = "oe",
        ['đ'] = "d", ['ð'] = "d", ['þ'] = "th",
        // Vietnamese
        ['ạ'] = "a", ['ả'] = "a", ['ấ'] = "a", ['ầ'] = "a", ['ẩ'] = "a", ['ẫ'] = "a", ['ậ'] = "a",
        ['ắ'] = "a", ['ằ'] = "a", ['ẳ'] = "a", ['ẵ'] = "a", ['ặ'] = "a",
        ['ẹ'] = "e", ['ẻ'] = "e", ['ẽ'] = "e", ['ế'] = "e", ['ề'] = "e", ['ể'] = "e", ['ễ'] = "e", ['ệ'] = "e",
        ['ỉ'] = "i", ['ị'] = "i",
        ['ọ'] = "o", ['ỏ'] = "o", ['ố'] = "o", ['ồ'] = "o", ['ổ'] = "o", ['ỗ'] = "o", ['ộ'] = "o",
        ['ớ'] = "o", ['ờ'] = "o", ['ở'] = "o", ['ỡ'] = "o", ['ợ'] = "o", ['ơ'] = "o",
        ['ụ'] = "u", ['ủ'] = "u", ['ứ'] = "u", ['ừ'] = "u", ['ử'] = "u", ['ữ'] = "u", ['ự'] = "u", ['ư'] = "u",
        ['ỳ'] = "y", ['ỵ'] = "y", ['ỷ'] = "y", ['ỹ'] = "y"
    };

    /// <summary>
    /// Generate an SEO-friendly slug from a filename.
    /// </summary>
    /// <param name="fileName">The original filename (with or without extension).</param>
    /// <param name="maxLength">Maximum slug length. Default: 100.</param>
    /// <returns>URL-safe slug.</returns>
    public static string Generate(string fileName, int maxLength = 100)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return GenerateRandomSlug();

        // Remove file extension
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

        if (string.IsNullOrWhiteSpace(nameWithoutExtension))
            return GenerateRandomSlug();

        // Convert to lowercase
        var slug = nameWithoutExtension.ToLowerInvariant();

        // Replace special characters
        var sb = new StringBuilder(slug.Length);
        foreach (var c in slug)
        {
            if (CharReplacements.TryGetValue(c, out var replacement))
                sb.Append(replacement);
            else
                sb.Append(c);
        }
        slug = sb.ToString();

        // Replace spaces and underscores with hyphens
        slug = slug.Replace(' ', '-').Replace('_', '-');

        // Remove invalid characters
        slug = InvalidChars.Replace(slug, "");

        // Replace multiple hyphens with single hyphen
        slug = MultipleHyphens.Replace(slug, "-");

        // Remove leading and trailing hyphens
        slug = LeadingTrailingHyphens.Replace(slug, "");

        // Truncate to max length
        if (slug.Length > maxLength)
        {
            slug = slug[..maxLength];
            // Don't cut in the middle of a word
            var lastHyphen = slug.LastIndexOf('-');
            if (lastHyphen > maxLength / 2)
                slug = slug[..lastHyphen];
        }

        // If empty after processing, generate random
        if (string.IsNullOrEmpty(slug))
            return GenerateRandomSlug();

        return slug;
    }

    /// <summary>
    /// Generate a slug with a unique suffix.
    /// Format: {readable-name}_{shortId} (e.g., "hero-banner_a1b2c3d4")
    /// The underscore separator makes the short ID easy to extract.
    /// </summary>
    /// <param name="fileName">The original filename.</param>
    /// <param name="uniqueSuffix">Optional unique suffix (e.g., timestamp or GUID).</param>
    /// <returns>URL-safe slug with unique suffix.</returns>
    public static string GenerateUnique(string fileName, string? uniqueSuffix = null)
    {
        var baseSlug = Generate(fileName, 80); // Leave room for suffix
        var suffix = uniqueSuffix ?? GenerateShortId();
        // Use underscore to clearly separate the short ID from the readable name
        return $"{baseSlug}_{suffix}";
    }

    /// <summary>
    /// Extract the short ID from a slug.
    /// Returns the part after the last underscore.
    /// </summary>
    /// <param name="slug">The full slug (e.g., "hero-banner_a1b2c3d4").</param>
    /// <returns>The short ID (e.g., "a1b2c3d4"), or null if no underscore found.</returns>
    public static string? ExtractShortId(string slug)
    {
        if (string.IsNullOrEmpty(slug))
            return null;

        var lastUnderscore = slug.LastIndexOf('_');
        if (lastUnderscore < 0 || lastUnderscore == slug.Length - 1)
            return null;

        return slug[(lastUnderscore + 1)..];
    }

    /// <summary>
    /// Generate a random slug.
    /// </summary>
    private static string GenerateRandomSlug()
    {
        return $"image_{GenerateShortId()}";
    }

    /// <summary>
    /// Generate a short unique ID (8 characters, alphanumeric).
    /// </summary>
    public static string GenerateShortId()
    {
        // Use first 8 chars of a GUID (enough for uniqueness in most cases)
        return Guid.NewGuid().ToString("N")[..8];
    }
}

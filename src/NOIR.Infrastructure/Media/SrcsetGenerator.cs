namespace NOIR.Infrastructure.Media;

/// <summary>
/// Generates responsive srcset and picture element markup for images.
/// </summary>
public static class SrcsetGenerator
{
    /// <summary>
    /// Generate srcset attribute value for responsive images.
    /// </summary>
    /// <param name="variants">Available image variants.</param>
    /// <param name="format">Output format to filter by (null for all).</param>
    /// <returns>Srcset attribute value.</returns>
    public static string GenerateSrcset(IEnumerable<ImageVariantInfo> variants, OutputFormat? format = null)
    {
        var filteredVariants = format.HasValue
            ? variants.Where(v => v.Format == format.Value)
            : variants;

        return string.Join(", ",
            filteredVariants
                .Where(v => !string.IsNullOrEmpty(v.Url))
                .OrderBy(v => v.Width)
                .Select(v => $"{v.Url} {v.Width}w"));
    }

    /// <summary>
    /// Generate sizes attribute value for responsive images.
    /// </summary>
    /// <param name="defaultSize">Default size in vw or px. Default: "100vw".</param>
    /// <param name="breakpoints">Custom breakpoints as (maxWidth, size) tuples.</param>
    /// <returns>Sizes attribute value.</returns>
    public static string GenerateSizes(
        string defaultSize = "100vw",
        IEnumerable<(int maxWidth, string size)>? breakpoints = null)
    {
        if (breakpoints == null || !breakpoints.Any())
            return defaultSize;

        var parts = breakpoints
            .OrderBy(b => b.maxWidth)
            .Select(b => $"(max-width: {b.maxWidth}px) {b.size}")
            .ToList();

        parts.Add(defaultSize);
        return string.Join(", ", parts);
    }

    /// <summary>
    /// Generate a complete picture element with source sets for modern formats.
    /// </summary>
    /// <param name="variants">Available image variants.</param>
    /// <param name="alt">Alt text for the image.</param>
    /// <param name="sizes">Sizes attribute value.</param>
    /// <param name="className">CSS class name.</param>
    /// <param name="loading">Loading attribute (lazy, eager). Default: lazy.</param>
    /// <param name="decoding">Decoding attribute (async, sync, auto). Default: async.</param>
    /// <returns>HTML picture element markup.</returns>
    public static string GeneratePictureElement(
        IEnumerable<ImageVariantInfo> variants,
        string alt,
        string? sizes = null,
        string? className = null,
        string loading = "lazy",
        string decoding = "async")
    {
        var variantList = variants.ToList();
        sizes ??= "100vw";

        var sb = new StringBuilder();
        sb.AppendLine("<picture>");

        // AVIF source (best compression)
        var avifSrcset = GenerateSrcset(variantList, OutputFormat.Avif);
        if (!string.IsNullOrEmpty(avifSrcset))
        {
            sb.AppendLine($"  <source type=\"image/avif\" srcset=\"{avifSrcset}\" sizes=\"{sizes}\">");
        }

        // WebP source (good compression, wide support)
        var webpSrcset = GenerateSrcset(variantList, OutputFormat.WebP);
        if (!string.IsNullOrEmpty(webpSrcset))
        {
            sb.AppendLine($"  <source type=\"image/webp\" srcset=\"{webpSrcset}\" sizes=\"{sizes}\">");
        }

        // JPEG fallback
        var jpegSrcset = GenerateSrcset(variantList, OutputFormat.Jpeg);
        var fallbackUrl = variantList
            .Where(v => v.Format == OutputFormat.Jpeg)
            .OrderByDescending(v => v.Width)
            .Select(v => v.Url)
            .FirstOrDefault();

        var classAttr = string.IsNullOrEmpty(className) ? "" : $" class=\"{className}\"";

        sb.AppendLine($"  <img{classAttr} src=\"{fallbackUrl}\" srcset=\"{jpegSrcset}\" sizes=\"{sizes}\" alt=\"{EscapeHtml(alt)}\" loading=\"{loading}\" decoding=\"{decoding}\">");
        sb.Append("</picture>");

        return sb.ToString();
    }

    /// <summary>
    /// Generate a simple img tag with srcset.
    /// </summary>
    /// <param name="variants">Available image variants.</param>
    /// <param name="alt">Alt text for the image.</param>
    /// <param name="format">Format to use (null for JPEG fallback).</param>
    /// <param name="sizes">Sizes attribute value.</param>
    /// <param name="className">CSS class name.</param>
    /// <param name="loading">Loading attribute. Default: lazy.</param>
    /// <returns>HTML img element markup.</returns>
    public static string GenerateImgTag(
        IEnumerable<ImageVariantInfo> variants,
        string alt,
        OutputFormat? format = null,
        string? sizes = null,
        string? className = null,
        string loading = "lazy")
    {
        var variantList = variants.ToList();
        var targetFormat = format ?? OutputFormat.Jpeg;
        sizes ??= "100vw";

        var srcset = GenerateSrcset(variantList, targetFormat);
        var src = variantList
            .Where(v => v.Format == targetFormat)
            .OrderByDescending(v => v.Width)
            .Select(v => v.Url)
            .FirstOrDefault();

        var classAttr = string.IsNullOrEmpty(className) ? "" : $" class=\"{className}\"";

        return $"<img{classAttr} src=\"{src}\" srcset=\"{srcset}\" sizes=\"{sizes}\" alt=\"{EscapeHtml(alt)}\" loading=\"{loading}\" decoding=\"async\">";
    }

    /// <summary>
    /// Generate CSS background-image with image-set for multiple formats.
    /// </summary>
    /// <param name="variants">Available image variants.</param>
    /// <param name="targetSize">Target variant size.</param>
    /// <returns>CSS background-image value.</returns>
    public static string GenerateBackgroundImageCss(IEnumerable<ImageVariantInfo> variants, ImageVariant targetSize)
    {
        var variantList = variants
            .Where(v => v.Variant == targetSize)
            .ToList();

        if (variantList.Count == 0)
            return "";

        // Modern browsers: image-set()
        var imageSetParts = new List<string>();

        var avif = variantList.FirstOrDefault(v => v.Format == OutputFormat.Avif);
        if (avif != null)
            imageSetParts.Add($"url(\"{avif.Url}\") type(\"image/avif\")");

        var webp = variantList.FirstOrDefault(v => v.Format == OutputFormat.WebP);
        if (webp != null)
            imageSetParts.Add($"url(\"{webp.Url}\") type(\"image/webp\")");

        var jpeg = variantList.FirstOrDefault(v => v.Format == OutputFormat.Jpeg);
        if (jpeg != null)
            imageSetParts.Add($"url(\"{jpeg.Url}\") type(\"image/jpeg\")");

        if (imageSetParts.Count == 0)
            return "";

        // Fallback for older browsers
        var fallbackUrl = jpeg?.Url ?? webp?.Url ?? avif?.Url;

        return $"background-image: url(\"{fallbackUrl}\"); background-image: image-set({string.Join(", ", imageSetParts)});";
    }

    /// <summary>
    /// Escape HTML special characters.
    /// </summary>
    private static string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}

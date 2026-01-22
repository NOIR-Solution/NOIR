using System.Text.RegularExpressions;

namespace NOIR.Application.Features.Blog.Helpers;

/// <summary>
/// Helper methods for generating effective SEO meta values.
/// Based on research: docs/backend/research/seo-meta-and-hint-text-best-practices.md
/// </summary>
public static partial class SeoHelpers
{
    /// <summary>
    /// Maximum characters for meta title before truncation in search results.
    /// </summary>
    public const int MetaTitleMaxLength = 60;

    /// <summary>
    /// Maximum characters for meta description before truncation in search results.
    /// </summary>
    public const int MetaDescriptionMaxLength = 160;

    /// <summary>
    /// Gets the effective meta title for a post.
    /// Returns the custom metaTitle if provided, otherwise generates from post title.
    /// </summary>
    /// <param name="metaTitle">Custom meta title (nullable)</param>
    /// <param name="postTitle">Post title</param>
    /// <param name="siteName">Site name for branding</param>
    /// <returns>Effective meta title</returns>
    public static string GetEffectiveMetaTitle(
        string? metaTitle,
        string postTitle,
        string siteName = "NOIR")
    {
        // Use custom meta title if provided
        if (!string.IsNullOrWhiteSpace(metaTitle))
        {
            return metaTitle.Length > MetaTitleMaxLength
                ? metaTitle[..(MetaTitleMaxLength - 1)].TrimEnd() + "…"
                : metaTitle;
        }

        // Generate from post title
        if (string.IsNullOrWhiteSpace(postTitle))
        {
            return siteName;
        }

        const string separator = " | ";
        var maxTitleLength = MetaTitleMaxLength - siteName.Length - separator.Length;

        var truncatedTitle = postTitle.Length > maxTitleLength
            ? postTitle[..(maxTitleLength - 1)].TrimEnd() + "…"
            : postTitle;

        return $"{truncatedTitle}{separator}{siteName}";
    }

    /// <summary>
    /// Gets the effective meta description for a post.
    /// Returns the custom metaDescription if provided, otherwise generates from excerpt or content.
    /// </summary>
    /// <param name="metaDescription">Custom meta description (nullable)</param>
    /// <param name="excerpt">Post excerpt (nullable)</param>
    /// <param name="contentHtml">Post HTML content (nullable)</param>
    /// <returns>Effective meta description</returns>
    public static string GetEffectiveMetaDescription(
        string? metaDescription,
        string? excerpt,
        string? contentHtml)
    {
        // Use custom meta description if provided
        if (!string.IsNullOrWhiteSpace(metaDescription))
        {
            return TruncateToWordBoundary(metaDescription, MetaDescriptionMaxLength);
        }

        // Priority 1: Use excerpt
        if (!string.IsNullOrWhiteSpace(excerpt))
        {
            return TruncateToWordBoundary(excerpt.Trim(), MetaDescriptionMaxLength);
        }

        // Priority 2: Use first part of content (strip HTML)
        if (!string.IsNullOrWhiteSpace(contentHtml))
        {
            var plainText = StripHtml(contentHtml);
            if (!string.IsNullOrWhiteSpace(plainText))
            {
                return TruncateToWordBoundary(plainText, MetaDescriptionMaxLength);
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Strips HTML tags from content and normalizes whitespace.
    /// </summary>
    private static string StripHtml(string html)
    {
        // Remove HTML tags
        var text = HtmlTagRegex().Replace(html, " ");

        // Decode common HTML entities
        text = text
            .Replace("&nbsp;", " ")
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&quot;", "\"")
            .Replace("&#39;", "'")
            .Replace("&apos;", "'");

        // Normalize whitespace (collapse multiple spaces/newlines into single space)
        text = WhitespaceRegex().Replace(text, " ");

        return text.Trim();
    }

    /// <summary>
    /// Truncates text to a maximum length, ending at a word boundary when possible.
    /// </summary>
    private static string TruncateToWordBoundary(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
        {
            return text;
        }

        // Find the last space before maxLength
        var truncated = text[..maxLength];
        var lastSpace = truncated.LastIndexOf(' ');

        // If we found a space reasonably close to the limit (within 30% of max length)
        if (lastSpace > maxLength * 0.7)
        {
            return truncated[..lastSpace].TrimEnd() + "…";
        }

        // Otherwise just truncate at the limit
        return truncated.TrimEnd() + "…";
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}

namespace NOIR.Infrastructure.Persistence.SeedData;

/// <summary>
/// Seeds blog data: post categories, tags, posts with content, and tag assignments.
/// Order 200 - runs after Catalog.
/// </summary>
public class BlogSeedModule : ISeedDataModule
{
    public int Order => 200;
    public string ModuleName => "Blog";

    public async Task SeedAsync(SeedDataContext context, CancellationToken ct = default)
    {
        var tenantId = context.CurrentTenant.Id;

        // Idempotency: skip if posts already exist for this tenant
        var hasData = await context.DbContext.Set<Post>()
            .IgnoreQueryFilters()
            .TagWith("SeedData:CheckBlog")
            .AnyAsync(p => p.TenantId == tenantId, ct);

        if (hasData)
        {
            context.Logger.LogInformation("[SeedData] Blog already seeded for {Tenant}, skipping", tenantId);
            return;
        }

        // Parse author ID from TenantAdminUserId
        if (!Guid.TryParse(context.TenantAdminUserId, out var authorId))
        {
            context.Logger.LogWarning("[SeedData] Invalid TenantAdminUserId '{UserId}', skipping Blog module",
                context.TenantAdminUserId);
            return;
        }

        // 1. Seed post categories
        var categoryLookup = SeedPostCategories(context, tenantId);

        // 2. Seed post tags
        var tagLookup = SeedPostTags(context, tenantId);

        // Flush categories and tags
        await context.DbContext.SaveChangesAsync(ct);

        // 3. Seed posts with content and featured images
        var (postLookup, postCount, imageCount) = await SeedPostsAsync(
            context, tenantId, authorId, categoryLookup, ct);

        // Flush posts
        await context.DbContext.SaveChangesAsync(ct);

        // 4. Seed tag assignments
        var assignmentCount = SeedTagAssignments(context, tenantId, postLookup, tagLookup);

        await context.DbContext.SaveChangesAsync(ct);

        context.Logger.LogInformation(
            "[SeedData] Blog: {Posts} posts, {Images} images, {Assignments} tag assignments",
            postCount, imageCount, assignmentCount);
    }

    private static Dictionary<string, Guid> SeedPostCategories(
        SeedDataContext context, string tenantId)
    {
        var categoryLookup = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var categories = BlogData.GetCategories();

        // Parents first (ParentSlug == null)
        foreach (var def in categories.Where(c => c.ParentSlug == null))
        {
            var id = SeedDataConstants.TenantGuid(tenantId, $"postcat:{def.Slug}");
            var cat = PostCategory.Create(def.Name, def.Slug, def.Description, null, tenantId);
            SeedDataConstants.SetEntityId(cat, id);
            context.DbContext.Set<PostCategory>().Add(cat);
            categoryLookup[def.Slug] = id;
        }

        // Then children (ParentSlug != null)
        foreach (var def in categories.Where(c => c.ParentSlug != null))
        {
            var id = SeedDataConstants.TenantGuid(tenantId, $"postcat:{def.Slug}");

            if (!categoryLookup.TryGetValue(def.ParentSlug!, out var parentId))
            {
                context.Logger.LogWarning(
                    "[SeedData] Parent post category '{Parent}' not found for '{Child}', skipping",
                    def.ParentSlug, def.Slug);
                continue;
            }

            var cat = PostCategory.Create(def.Name, def.Slug, def.Description, parentId, tenantId);
            SeedDataConstants.SetEntityId(cat, id);
            context.DbContext.Set<PostCategory>().Add(cat);
            categoryLookup[def.Slug] = id;
        }

        return categoryLookup;
    }

    private static Dictionary<string, Guid> SeedPostTags(
        SeedDataContext context, string tenantId)
    {
        var tagLookup = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var def in BlogData.GetTags())
        {
            var id = SeedDataConstants.TenantGuid(tenantId, $"posttag:{def.Slug}");
            var tag = PostTag.Create(def.Name, def.Slug, null, def.Color, tenantId);
            SeedDataConstants.SetEntityId(tag, id);
            context.DbContext.Set<PostTag>().Add(tag);
            tagLookup[def.Slug] = id;
        }

        return tagLookup;
    }

    private static async Task<(Dictionary<string, Guid> PostLookup, int PostCount, int ImageCount)> SeedPostsAsync(
        SeedDataContext context,
        string tenantId,
        Guid authorId,
        Dictionary<string, Guid> categoryLookup,
        CancellationToken ct)
    {
        var imageProcessor = context.ServiceProvider.GetService<IImageProcessor>();
        var postLookup = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var postCount = 0;
        var imageCount = 0;

        foreach (var def in BlogData.GetPosts())
        {
            var postId = SeedDataConstants.TenantGuid(tenantId, $"post:{def.Slug}");
            var post = Post.Create(def.Title, def.Slug, authorId, tenantId);
            SeedDataConstants.SetEntityId(post, postId);

            // Update content
            post.UpdateContent(def.Title, def.Slug, def.Excerpt, null, def.ContentHtml);

            // Set category
            if (categoryLookup.TryGetValue(def.CategorySlug, out var catId))
            {
                post.SetCategory(catId);
            }

            // Generate and process featured image
            var imgResult = await SeedImageHelper.GenerateAndProcessAsync(
                imageProcessor, 1200, 630, def.ImageColor, def.Title,
                def.Slug, $"images/blog/{postId}", context.Logger, ct);

            if (imgResult != null)
            {
                var primaryUrl = SeedImageHelper.GetPrimaryUrl(imgResult);
                if (primaryUrl != null)
                {
                    var shortId = postId.ToString("N")[..8];
                    var mediaFile = MediaFile.Create(
                        shortId: shortId,
                        slug: $"{def.Slug}_{shortId}",
                        originalFileName: $"{def.Slug}.webp",
                        folder: $"images/blog/{postId}",
                        defaultUrl: primaryUrl,
                        thumbHash: imgResult.ThumbHash,
                        dominantColor: imgResult.DominantColor,
                        width: imgResult.Metadata?.Width ?? 1200,
                        height: imgResult.Metadata?.Height ?? 630,
                        format: "webp",
                        mimeType: "image/webp",
                        sizeBytes: imgResult.Variants.Sum(v => v.SizeBytes),
                        hasTransparency: false,
                        variantsJson: JsonSerializer.Serialize(imgResult.Variants),
                        srcsetsJson: "{}",
                        uploadedBy: context.TenantAdminUserId,
                        tenantId: tenantId);

                    context.DbContext.Set<MediaFile>().Add(mediaFile);
                    post.SetFeaturedImage(mediaFile.Id, def.Title);
                    imageCount++;
                }
            }

            // Apply status based on definition
            var publishDate = SeedDataConstants.SpreadDate(def.DayOffset);
            switch (def.Status)
            {
                case PostStatus.Published:
                    post.Publish();
                    break;

                case PostStatus.Scheduled:
                    // Schedule for future date relative to base timestamp
                    post.Schedule(publishDate);
                    break;

                case PostStatus.Archived:
                    // Must publish first, then archive
                    post.Publish();
                    post.Archive();
                    break;

                case PostStatus.Draft:
                default:
                    // Draft is the default status, no action needed
                    break;
            }

            context.DbContext.Set<Post>().Add(post);
            postLookup[def.Slug] = postId;
            postCount++;
        }

        return (postLookup, postCount, imageCount);
    }

    private static int SeedTagAssignments(
        SeedDataContext context,
        string tenantId,
        Dictionary<string, Guid> postLookup,
        Dictionary<string, Guid> tagLookup)
    {
        var assignmentCount = 0;

        foreach (var def in BlogData.GetPosts())
        {
            if (!postLookup.TryGetValue(def.Slug, out var postId))
                continue;

            foreach (var tagSlug in def.TagSlugs)
            {
                if (!tagLookup.TryGetValue(tagSlug, out var tagId))
                {
                    context.Logger.LogWarning(
                        "[SeedData] Tag '{Tag}' not found for post '{Post}', skipping assignment",
                        tagSlug, def.Slug);
                    continue;
                }

                var assignment = PostTagAssignment.Create(postId, tagId, tenantId);
                context.DbContext.Set<PostTagAssignment>().Add(assignment);
                assignmentCount++;

                // Update denormalized PostCount on the tag entity
                var tag = context.DbContext.Set<PostTag>().Local.FirstOrDefault(t => t.Id == tagId);
                tag?.IncrementPostCount();
            }
        }

        return assignmentCount;
    }

}

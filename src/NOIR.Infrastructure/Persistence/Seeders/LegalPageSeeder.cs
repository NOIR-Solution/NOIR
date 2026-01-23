namespace NOIR.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds platform-level legal pages (TenantId = null).
/// Tenants can customize these pages using the copy-on-edit pattern.
/// </summary>
public class LegalPageSeeder : ISeeder
{
    /// <summary>
    /// Legal pages can be seeded after basic system setup.
    /// </summary>
    public int Order => 45;

    public async Task SeedAsync(SeederContext context, CancellationToken ct = default)
    {
        // Platform legal pages (TenantId = null) are shared defaults across all tenants.
        // Tenants inherit these pages and can create their own copies via copy-on-edit.
        //
        // Smart upsert logic for platform pages:
        // 1. If page doesn't exist at platform level -> Add it
        // 2. If page exists AND Version = 1 -> Update it (never customized)
        // 3. If page exists AND Version > 1 -> Skip it (platform admin customized it)

        // Get existing platform-level pages (TenantId = null)
        var existingPages = await context.DbContext.Set<LegalPage>()
            .IgnoreQueryFilters()
            .TagWith("Seeder:GetExistingLegalPages")
            .Where(p => p.TenantId == null && !p.IsDeleted)
            .ToListAsync(ct);

        var pageDefinitions = GetLegalPageDefinitions();
        var addedCount = 0;
        var updatedCount = 0;
        var skippedCount = 0;

        foreach (var definition in pageDefinitions)
        {
            var existing = existingPages.FirstOrDefault(p => p.Slug == definition.Slug);

            if (existing == null)
            {
                // Page doesn't exist at platform level - add it
                await context.DbContext.Set<LegalPage>().AddAsync(definition, ct);
                addedCount++;
            }
            else if (existing.Version == 1)
            {
                // Page exists but was never modified by user - update it
                existing.Update(
                    definition.Title,
                    definition.HtmlContent,
                    metaTitle: definition.MetaTitle,
                    metaDescription: definition.MetaDescription,
                    canonicalUrl: definition.CanonicalUrl,
                    allowIndexing: definition.AllowIndexing);

                // Reset version back to 1 since this is a seed update, not a user update
                existing.ResetVersionForSeeding();
                updatedCount++;
            }
            else
            {
                // Page was customized by user (Version > 1) - skip it
                skippedCount++;
                context.Logger.LogDebug(
                    "Skipping legal page '{PageSlug}' - user customized (Version={Version})",
                    existing.Slug, existing.Version);
            }
        }

        if (addedCount > 0 || updatedCount > 0)
        {
            await context.DbContext.SaveChangesAsync(ct);
            context.Logger.LogInformation(
                "Platform legal pages: {Added} added, {Updated} updated, {Skipped} skipped (customized)",
                addedCount, updatedCount, skippedCount);
        }
    }

    /// <summary>
    /// Gets the legal page definitions for seeding.
    /// </summary>
    private static List<LegalPage> GetLegalPageDefinitions()
    {
        return
        [
            LegalPage.CreatePlatformDefault(
                "terms-of-service",
                "Terms of Service",
                GetTermsOfServiceHtmlContent(),
                metaTitle: "Terms of Service",
                metaDescription: "Our terms and conditions for using this platform."),
            LegalPage.CreatePlatformDefault(
                "privacy-policy",
                "Privacy Policy",
                GetPrivacyPolicyHtmlContent(),
                metaTitle: "Privacy Policy",
                metaDescription: "How we collect, use, and protect your personal information."),
        ];
    }

    /// <summary>
    /// Gets the default Terms of Service HTML content.
    /// </summary>
    private static string GetTermsOfServiceHtmlContent()
    {
        return """
            <h1>Terms of Service</h1>

            <p><strong>Last Updated:</strong> {{CurrentDate}}</p>

            <h2>1. Acceptance of Terms</h2>
            <p>By accessing and using this platform, you accept and agree to be bound by the terms and provision of this agreement. If you do not agree to abide by the above, please do not use this service.</p>

            <h2>2. Use License</h2>
            <p>Permission is granted to temporarily access the materials (information or software) on this platform for personal, non-commercial transitory viewing only. This is the grant of a license, not a transfer of title.</p>

            <h2>3. User Responsibilities</h2>
            <p>Users are responsible for:</p>
            <ul>
                <li>Maintaining the confidentiality of their account credentials</li>
                <li>All activities that occur under their account</li>
                <li>Complying with all applicable laws and regulations</li>
                <li>Respecting the rights and privacy of other users</li>
            </ul>

            <h2>4. Prohibited Activities</h2>
            <p>You may not:</p>
            <ul>
                <li>Use the service for any unlawful purpose</li>
                <li>Attempt to gain unauthorized access to any portion of the platform</li>
                <li>Interfere with or disrupt the service</li>
                <li>Upload or transmit viruses or malicious code</li>
            </ul>

            <h2>5. Disclaimer</h2>
            <p>The materials on this platform are provided on an 'as is' basis. We make no warranties, expressed or implied, and hereby disclaim and negate all other warranties including, without limitation, implied warranties or conditions of merchantability, fitness for a particular purpose, or non-infringement of intellectual property or other violation of rights.</p>

            <h2>6. Limitations</h2>
            <p>In no event shall we or our suppliers be liable for any damages (including, without limitation, damages for loss of data or profit, or due to business interruption) arising out of the use or inability to use the materials on this platform.</p>

            <h2>7. Changes to Terms</h2>
            <p>We reserve the right to modify these terms at any time. Continued use of the platform after any such changes shall constitute your consent to such changes.</p>

            <h2>8. Contact</h2>
            <p>If you have any questions about these Terms of Service, please contact us.</p>
            """;
    }

    /// <summary>
    /// Gets the default Privacy Policy HTML content.
    /// </summary>
    private static string GetPrivacyPolicyHtmlContent()
    {
        return """
            <h1>Privacy Policy</h1>

            <p><strong>Last Updated:</strong> {{CurrentDate}}</p>

            <h2>1. Introduction</h2>
            <p>We respect your privacy and are committed to protecting your personal data. This privacy policy will inform you about how we look after your personal data and tell you about your privacy rights.</p>

            <h2>2. Information We Collect</h2>
            <p>We may collect and process the following data:</p>
            <ul>
                <li><strong>Identity Data:</strong> Name, username, and similar identifiers</li>
                <li><strong>Contact Data:</strong> Email address and telephone numbers</li>
                <li><strong>Technical Data:</strong> IP address, browser type, time zone settings, and device information</li>
                <li><strong>Usage Data:</strong> Information about how you use our platform</li>
            </ul>

            <h2>3. How We Use Your Information</h2>
            <p>We use your personal data for:</p>
            <ul>
                <li>Providing and managing your account</li>
                <li>Delivering our services to you</li>
                <li>Communicating with you about our services</li>
                <li>Improving our platform and user experience</li>
                <li>Complying with legal obligations</li>
            </ul>

            <h2>4. Data Security</h2>
            <p>We have implemented appropriate security measures to prevent your personal data from being accidentally lost, used, or accessed in an unauthorized way. We limit access to your personal data to those employees and partners who have a business need to know.</p>

            <h2>5. Data Retention</h2>
            <p>We will only retain your personal data for as long as necessary to fulfil the purposes we collected it for, including for the purposes of satisfying any legal, accounting, or reporting requirements.</p>

            <h2>6. Your Rights</h2>
            <p>Under certain circumstances, you have rights under data protection laws in relation to your personal data, including:</p>
            <ul>
                <li>The right to access your personal data</li>
                <li>The right to request correction of your personal data</li>
                <li>The right to request erasure of your personal data</li>
                <li>The right to object to processing of your personal data</li>
                <li>The right to request transfer of your personal data</li>
            </ul>

            <h2>7. Cookies</h2>
            <p>Our platform uses cookies to distinguish you from other users. This helps us provide you with a good experience when you browse our platform and allows us to improve our site.</p>

            <h2>8. Third-Party Links</h2>
            <p>This platform may include links to third-party websites. Clicking on those links may allow third parties to collect or share data about you. We do not control these third-party websites and are not responsible for their privacy statements.</p>

            <h2>9. Changes to This Policy</h2>
            <p>We may update this privacy policy from time to time. We will notify you of any changes by posting the new privacy policy on this page and updating the "Last Updated" date.</p>

            <h2>10. Contact Us</h2>
            <p>If you have any questions about this privacy policy or our privacy practices, please contact us.</p>
            """;
    }
}

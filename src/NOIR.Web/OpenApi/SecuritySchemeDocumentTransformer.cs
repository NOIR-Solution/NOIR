using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace NOIR.Web.OpenApi;

/// <summary>
/// Wraps a <see cref="JsonNode"/> as an <see cref="IOpenApiExtension"/> for use in OpenAPI v2 Extensions dictionaries.
/// </summary>
internal sealed class JsonNodeExtension(JsonNode node) : IOpenApiExtension
{
    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
    {
        // Serialize the JsonNode and let the writer handle the raw JSON
        var json = node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        writer.WriteRaw(json);
    }
}

/// <summary>
/// OpenAPI document transformer that adds:
/// - JWT Bearer security scheme (enables the "Authorize" button in Scalar UI)
/// - API contact and license metadata
/// - Tag descriptions and ordering for logical grouping in Scalar UI
/// </summary>
public sealed class SecuritySchemeDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        // --- Contact & License ---
        document.Info.Contact = new OpenApiContact
        {
            Name = "NOIR Support",
            Email = "support@noir.local"
        };
        document.Info.License = new OpenApiLicense
        {
            Name = "Proprietary"
        };

        // --- JWT Bearer Security Scheme ---
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter your JWT access token obtained from `POST /api/auth/login`. " +
                          "Example: `eyJhbGciOiJIUzI1NiIs...`"
        };

        // --- API Key Security Scheme ---
        document.Components.SecuritySchemes["ApiKey"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = "X-API-Key",
            Description = "API Key identifier (e.g., `noir_key_...`). Must be paired with `X-API-Secret` header."
        };
        document.Components.SecuritySchemes["ApiSecret"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = "X-API-Secret",
            Description = "API Secret (e.g., `noir_secret_...`). Obtained on key creation or rotation — shown only once."
        };

        // Apply Bearer security globally — all endpoints inherit this unless AllowAnonymous
        // API Key auth is an alternative — external systems use X-API-Key + X-API-Secret headers instead of JWT
        document.Security ??= new List<OpenApiSecurityRequirement>();
        var bearerRequirement = new OpenApiSecurityRequirement();
        bearerRequirement[new OpenApiSecuritySchemeReference("Bearer", document)] = [];
        document.Security.Add(bearerRequirement);

        var apiKeyRequirement = new OpenApiSecurityRequirement();
        apiKeyRequirement[new OpenApiSecuritySchemeReference("ApiKey", document)] = [];
        apiKeyRequirement[new OpenApiSecuritySchemeReference("ApiSecret", document)] = [];
        document.Security.Add(apiKeyRequirement);

        // --- Tag Descriptions & Ordering ---
        // Organized to match the NOIR portal sidebar menu groups for consistency.
        document.Tags = new HashSet<OpenApiTag>
        {
            // Dashboard
            new() { Name = "Dashboard", Description = "Business KPI widgets: revenue, orders, customers, inventory." },

            // Marketing
            new() { Name = "Reports", Description = "Revenue, order, inventory, and product performance analytics." },
            new() { Name = "Promotions", Description = "Discount codes with percentage/fixed amounts, usage limits, and date ranges." },

            // Orders & Fulfillment
            new() { Name = "Cart", Description = "Guest and authenticated cart operations with merge-on-login support." },
            new() { Name = "Checkout", Description = "Multi-step checkout: address → shipping → payment → confirmation." },
            new() { Name = "Orders", Description = "Order lifecycle: pending → confirmed → processing → shipped → delivered → completed." },
            new() { Name = "Payments", Description = "Payment transaction tracking and status timeline." },
            new() { Name = "Payment Gateways", Description = "Payment gateway configuration and management." },
            new() { Name = "Payment Refunds", Description = "Refund initiation and tracking." },
            new() { Name = "Payment Webhooks", Description = "Inbound payment gateway webhook handlers." },
            new() { Name = "Shipping", Description = "Shipment tracking and carrier management." },
            new() { Name = "Shipping Providers", Description = "Shipping provider configuration." },
            new() { Name = "Shipping Webhooks", Description = "Inbound shipping carrier webhook handlers." },
            new() { Name = "Inventory", Description = "Stock receipts (phieu nhap/xuat): draft → confirmed/cancelled." },

            // Customers
            new() { Name = "Customers", Description = "Customer profiles, addresses, and order history." },
            new() { Name = "Customer Groups", Description = "Customer segmentation with rule-based membership." },
            new() { Name = "Reviews", Description = "Product review submission and moderation workflow." },
            new() { Name = "Wishlists", Description = "User wishlist management." },

            // Catalog
            new() { Name = "Products", Description = "Product CRUD, variants, options, images, and lifecycle (draft → active → archived)." },
            new() { Name = "Product Categories", Description = "Hierarchical product category management." },
            new() { Name = "Product Attributes", Description = "Attribute types, values, and product assignments." },
            new() { Name = "Product Filters", Description = "Faceted filter index for storefront search." },
            new() { Name = "Filter Analytics", Description = "Filter interaction event tracking." },
            new() { Name = "Brands", Description = "Brand management for product catalog." },
            new() { Name = "Media", Description = "Media library CRUD with folder organisation." },
            new() { Name = "Media Files", Description = "File upload and management." },

            // Human Resources
            new() { Name = "HR - Employees", Description = "Employee management with org hierarchy, bulk operations, and import/export." },
            new() { Name = "HR - Departments", Description = "Department tree management." },
            new() { Name = "HR - Tags", Description = "Employee tag categories for segmentation." },

            // Project Management
            new() { Name = "PM - Projects", Description = "Project management with kanban boards and task tracking." },
            new() { Name = "PM - Tasks", Description = "Task lifecycle with subtasks, comments, and attachments." },

            // CRM
            new() { Name = "CRM - Contacts", Description = "Contact management with lead pipeline linking." },
            new() { Name = "CRM - Companies", Description = "Company management with contact associations." },
            new() { Name = "CRM - Leads", Description = "Lead lifecycle: new → contacted → qualified → proposal → won/lost." },
            new() { Name = "CRM - Pipelines", Description = "Sales pipeline and stage configuration." },
            new() { Name = "CRM - Activities", Description = "CRM activity logging (calls, emails, meetings)." },
            new() { Name = "CRM - Dashboard", Description = "CRM performance metrics and pipeline overview." },

            // Content
            new() { Name = "Blog Posts", Description = "Blog post CRUD with rich content, SEO metadata, and publishing workflow." },
            new() { Name = "Blog Categories", Description = "Blog category management." },
            new() { Name = "Blog Tags", Description = "Blog tag management." },
            new() { Name = "Blog Feeds", Description = "Public RSS/Atom feed endpoints." },
            new() { Name = "Legal Pages", Description = "Legal page management (terms, privacy policy)." },
            new() { Name = "Public Legal Pages", Description = "Public-facing legal page access." },

            // Users & Access
            new() { Name = "Authentication", Description = "Login, logout, token refresh, and session management." },
            new() { Name = "Users", Description = "User account CRUD, role assignment, and profile management." },
            new() { Name = "Roles", Description = "Role definitions and assignment." },
            new() { Name = "Permissions", Description = "Permission listing and role-permission mapping." },
            new() { Name = "API Keys", Description = "API key lifecycle: create, rotate, revoke. Self-service and admin management." },
            new() { Name = "Tenants", Description = "Multi-tenant management (Platform Admin only)." },

            // Settings & Configuration
            new() { Name = "Platform Settings", Description = "Platform-wide configuration (Platform Admin only)." },
            new() { Name = "Tenant Settings", Description = "Per-tenant configuration (Tenant Admin only)." },
            new() { Name = "Feature Management", Description = "Module enable/disable control at platform and tenant level." },
            new() { Name = "Email Templates", Description = "Database-driven email template management." },
            new() { Name = "Webhooks", Description = "Outbound webhook subscription and delivery management." },

            // System
            new() { Name = "Search", Description = "Global cross-entity search powered by Cmd+K." },
            new() { Name = "Audit", Description = "Activity timeline and audit log access." },
            new() { Name = "Notifications", Description = "In-app notification management." },
            new() { Name = "SSE", Description = "Server-Sent Events for real-time job progress." },
            new() { Name = "Developer Logs", Description = "Application log viewer (Development/Staging only)." },
            new() { Name = "Development", Description = "Development-only utility endpoints (not available in Production)." },
        };

        // --- Tag Groups (x-tagGroups) ---
        // Scalar and Redoc use this OpenAPI extension to create sidebar folder groups.
        // Matches the NOIR portal sidebar navigation for consistency.
        document.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        document.Extensions["x-tagGroups"] = new JsonNodeExtension(JsonNode.Parse("""
            [
                { "name": "Dashboard", "tags": ["Dashboard"] },
                { "name": "Marketing", "tags": ["Reports", "Promotions"] },
                { "name": "Orders & Fulfillment", "tags": ["Cart", "Checkout", "Orders", "Payments", "Payment Gateways", "Payment Refunds", "Payment Webhooks", "Shipping", "Shipping Providers", "Shipping Webhooks", "Inventory"] },
                { "name": "Customers", "tags": ["Customers", "Customer Groups", "Reviews", "Wishlists"] },
                { "name": "Catalog", "tags": ["Products", "Product Categories", "Product Attributes", "Product Filters", "Filter Analytics", "Brands", "Media", "Media Files"] },
                { "name": "Human Resources", "tags": ["HR - Employees", "HR - Departments", "HR - Tags"] },
                { "name": "Project Management", "tags": ["PM - Projects", "PM - Tasks"] },
                { "name": "CRM", "tags": ["CRM - Contacts", "CRM - Companies", "CRM - Leads", "CRM - Pipelines", "CRM - Activities", "CRM - Dashboard"] },
                { "name": "Content", "tags": ["Blog Posts", "Blog Categories", "Blog Tags", "Blog Feeds", "Legal Pages", "Public Legal Pages"] },
                { "name": "Users & Access", "tags": ["Authentication", "Users", "Roles", "Permissions", "API Keys", "Tenants"] },
                { "name": "Settings", "tags": ["Platform Settings", "Tenant Settings", "Feature Management", "Email Templates", "Webhooks"] },
                { "name": "System", "tags": ["Search", "Audit", "Notifications", "SSE", "Developer Logs", "Development"] }
            ]
            """)!);

        return Task.CompletedTask;
    }
}

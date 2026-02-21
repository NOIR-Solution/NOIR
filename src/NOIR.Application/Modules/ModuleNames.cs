namespace NOIR.Application.Modules;

/// <summary>
/// Centralized module/feature name constants. Prevents magic strings.
/// </summary>
public static class ModuleNames
{
    public static class Core
    {
        public const string Auth = "Core.Auth";
        public const string Users = "Core.Users";
        public const string Roles = "Core.Roles";
        public const string Permissions = "Core.Permissions";
        public const string Dashboard = "Core.Dashboard";
        public const string Settings = "Core.Settings";
        public const string Audit = "Core.Audit";
        public const string Notifications = "Core.Notifications";
    }

    public static class Ecommerce
    {
        public const string Products = "Ecommerce.Products";
        public const string Categories = "Ecommerce.Categories";
        public const string Brands = "Ecommerce.Brands";
        public const string Attributes = "Ecommerce.Attributes";
        public const string Cart = "Ecommerce.Cart";
        public const string Checkout = "Ecommerce.Checkout";
        public const string Orders = "Ecommerce.Orders";
        public const string Payments = "Ecommerce.Payments";
        public const string Inventory = "Ecommerce.Inventory";
        public const string Promotions = "Ecommerce.Promotions";
        public const string Reviews = "Ecommerce.Reviews";
        public const string Customers = "Ecommerce.Customers";
        public const string CustomerGroups = "Ecommerce.CustomerGroups";
        public const string Wishlist = "Ecommerce.Wishlist";
    }

    public static class Content
    {
        public const string Blog = "Content.Blog";
        public const string BlogCategories = "Content.BlogCategories";
        public const string BlogTags = "Content.BlogTags";
    }

    public static class Platform
    {
        public const string Tenants = "Platform.Tenants";
        public const string EmailTemplates = "Platform.EmailTemplates";
        public const string LegalPages = "Platform.LegalPages";
    }

    public static class Analytics
    {
        public const string Reports = "Analytics.Reports";
        public const string DeveloperLogs = "Analytics.DeveloperLogs";
    }
}

namespace NOIR.Domain.Common;

/// <summary>
/// Centralized error codes for the NOIR application.
/// Format: NOIR-{CATEGORY}-{NUMBER}
///
/// Categories:
/// - AUTH (1xxx): Authentication and authorization errors
/// - VAL (2xxx): Validation errors
/// - BIZ (3xxx): Business logic errors
/// - EXT (4xxx): External service errors
/// - SYS (9xxx): System/internal errors
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// Authentication and authorization errors (1xxx)
    /// </summary>
    public static class Auth
    {
        /// <summary>Invalid email or password</summary>
        public const string InvalidCredentials = "NOIR-AUTH-1001";

        /// <summary>User account is disabled</summary>
        public const string AccountDisabled = "NOIR-AUTH-1002";

        /// <summary>Account locked due to failed login attempts</summary>
        public const string AccountLockedOut = "NOIR-AUTH-1003";

        /// <summary>Access token has expired</summary>
        public const string TokenExpired = "NOIR-AUTH-1004";

        /// <summary>Refresh token is required</summary>
        public const string RefreshTokenRequired = "NOIR-AUTH-1005";

        /// <summary>Refresh token is invalid or expired</summary>
        public const string RefreshTokenInvalid = "NOIR-AUTH-1006";

        /// <summary>Email not confirmed</summary>
        public const string EmailNotConfirmed = "NOIR-AUTH-1007";

        /// <summary>Password reset required</summary>
        public const string PasswordResetRequired = "NOIR-AUTH-1008";

        /// <summary>Invalid password</summary>
        public const string InvalidPassword = "NOIR-AUTH-1009";

        /// <summary>General unauthorized access (401)</summary>
        public const string Unauthorized = "NOIR-AUTH-1401";

        /// <summary>Access forbidden (403)</summary>
        public const string Forbidden = "NOIR-AUTH-1403";

        /// <summary>User not found</summary>
        public const string UserNotFound = "NOIR-AUTH-1404";

        /// <summary>Role not found</summary>
        public const string RoleNotFound = "NOIR-AUTH-1405";

        /// <summary>Permission not found</summary>
        public const string PermissionNotFound = "NOIR-AUTH-1406";

        /// <summary>Update operation failed</summary>
        public const string UpdateFailed = "NOIR-AUTH-1010";

        /// <summary>Duplicate email address</summary>
        public const string DuplicateEmail = "NOIR-AUTH-1011";

        /// <summary>Too many requests (rate limited)</summary>
        public const string TooManyRequests = "NOIR-AUTH-1429";

        /// <summary>Invalid session token</summary>
        public const string InvalidSession = "NOIR-AUTH-1012";

        /// <summary>OTP has expired</summary>
        public const string OtpExpired = "NOIR-AUTH-1013";

        /// <summary>Invalid OTP code</summary>
        public const string InvalidOtp = "NOIR-AUTH-1014";

        /// <summary>OTP has already been used</summary>
        public const string OtpAlreadyUsed = "NOIR-AUTH-1015";

        /// <summary>Cooldown is active</summary>
        public const string CooldownActive = "NOIR-AUTH-1016";

        /// <summary>Maximum resends reached</summary>
        public const string MaxResendsReached = "NOIR-AUTH-1017";

        /// <summary>Tenant not found</summary>
        public const string TenantNotFound = "NOIR-AUTH-1407";

        /// <summary>Tenant is inactive</summary>
        public const string TenantInactive = "NOIR-AUTH-1408";

        /// <summary>User creation failed</summary>
        public const string UserCreationFailed = "NOIR-AUTH-1018";

        /// <summary>Role assignment failed</summary>
        public const string RoleAssignmentFailed = "NOIR-AUTH-1019";
    }

    /// <summary>
    /// Validation errors (2xxx)
    /// </summary>
    public static class Validation
    {
        /// <summary>Required field is missing</summary>
        public const string Required = "NOIR-VAL-2001";

        /// <summary>Invalid format (email, phone, etc.)</summary>
        public const string InvalidFormat = "NOIR-VAL-2002";

        /// <summary>Value is out of allowed range</summary>
        public const string OutOfRange = "NOIR-VAL-2003";

        /// <summary>Maximum length exceeded</summary>
        public const string MaxLengthExceeded = "NOIR-VAL-2004";

        /// <summary>Minimum length not met</summary>
        public const string MinLengthNotMet = "NOIR-VAL-2005";

        /// <summary>Unique constraint violation</summary>
        public const string UniqueConstraint = "NOIR-VAL-2006";

        /// <summary>Invalid reference to another entity</summary>
        public const string InvalidReference = "NOIR-VAL-2007";

        /// <summary>Invalid date or date range</summary>
        public const string InvalidDate = "NOIR-VAL-2008";

        /// <summary>General validation error (400)</summary>
        public const string General = "NOIR-VAL-2400";

        /// <summary>Resource not found (404)</summary>
        public const string NotFound = "NOIR-VAL-2404";

        /// <summary>Invalid input value</summary>
        public const string InvalidInput = "NOIR-VAL-2009";
    }

    /// <summary>
    /// Tenant-related errors (15xx)
    /// </summary>
    public static class Tenant
    {
        /// <summary>Tenant not found</summary>
        public const string NotFound = "NOIR-AUTH-1500";

        /// <summary>Tenant is inactive</summary>
        public const string Inactive = "NOIR-AUTH-1501";

        /// <summary>User tenant membership not found</summary>
        public const string MembershipNotFound = "NOIR-AUTH-1502";

        /// <summary>User is already a member of the tenant</summary>
        public const string AlreadyMember = "NOIR-AUTH-1503";

        /// <summary>Cannot remove owner from tenant</summary>
        public const string CannotRemoveOwner = "NOIR-AUTH-1504";

        /// <summary>Tenant domain not found</summary>
        public const string DomainNotFound = "NOIR-AUTH-1505";

        /// <summary>Tenant domain already exists</summary>
        public const string DomainAlreadyExists = "NOIR-AUTH-1506";

        /// <summary>Tenant setting not found</summary>
        public const string SettingNotFound = "NOIR-AUTH-1507";

        /// <summary>Invalid tenant role</summary>
        public const string InvalidRole = "NOIR-AUTH-1508";
    }

    /// <summary>
    /// Business logic errors (3xxx)
    /// </summary>
    public static class Business
    {
        /// <summary>Entity not found</summary>
        public const string NotFound = "NOIR-BIZ-3001";

        /// <summary>Entity already exists</summary>
        public const string AlreadyExists = "NOIR-BIZ-3002";

        /// <summary>Cannot delete entity (has dependencies)</summary>
        public const string CannotDelete = "NOIR-BIZ-3003";

        /// <summary>Cannot modify entity (locked or finalized)</summary>
        public const string CannotModify = "NOIR-BIZ-3004";

        /// <summary>Operation not allowed in current state</summary>
        public const string InvalidState = "NOIR-BIZ-3005";

        /// <summary>Insufficient permissions for operation</summary>
        public const string InsufficientPermissions = "NOIR-BIZ-3006";

        /// <summary>Resource conflict (409)</summary>
        public const string Conflict = "NOIR-BIZ-3409";
    }


    /// <summary>
    /// Payment-related errors (PAY-xxx)
    /// </summary>
    public static class Payment
    {
        /// <summary>Payment transaction not found</summary>
        public const string TransactionNotFound = "NOIR-PAY-001";

        /// <summary>Payment gateway not found</summary>
        public const string GatewayNotFound = "NOIR-PAY-002";

        /// <summary>Payment gateway already exists</summary>
        public const string GatewayAlreadyExists = "NOIR-PAY-003";

        /// <summary>Payment gateway is not active</summary>
        public const string GatewayNotActive = "NOIR-PAY-004";

        /// <summary>Payment provider is not configured</summary>
        public const string ProviderNotConfigured = "NOIR-PAY-005";

        /// <summary>Payment initiation failed</summary>
        public const string InitiationFailed = "NOIR-PAY-006";

        /// <summary>Invalid payment status transition</summary>
        public const string InvalidStatusTransition = "NOIR-PAY-007";

        /// <summary>Refund not found</summary>
        public const string RefundNotFound = "NOIR-PAY-008";

        /// <summary>Invalid refund status</summary>
        public const string InvalidRefundStatus = "NOIR-PAY-009";

        /// <summary>Refund window has expired</summary>
        public const string RefundWindowExpired = "NOIR-PAY-010";

        /// <summary>Refund amount exceeds available balance</summary>
        public const string RefundAmountExceedsBalance = "NOIR-PAY-011";

        /// <summary>Invalid requester ID</summary>
        public const string InvalidRequesterId = "NOIR-PAY-012";

        /// <summary>Payment is not a COD payment</summary>
        public const string NotCodPayment = "NOIR-PAY-013";

        /// <summary>Invalid webhook signature</summary>
        public const string InvalidWebhookSignature = "NOIR-PAY-014";

        /// <summary>Payment gateway communication error</summary>
        public const string GatewayError = "NOIR-PAY-015";

        /// <summary>Refund processing failed</summary>
        public const string RefundFailed = "NOIR-PAY-016";
    }

    /// <summary>
    /// Brand-related errors (BRAND-xxx)
    /// </summary>
    public static class Brand
    {
        /// <summary>Brand not found</summary>
        public const string NotFound = "NOIR-BRAND-001";

        /// <summary>Brand slug already exists</summary>
        public const string DuplicateSlug = "NOIR-BRAND-002";

        /// <summary>Cannot delete brand with products</summary>
        public const string HasProducts = "NOIR-BRAND-003";
    }

    /// <summary>
    /// Product-related errors (PRODUCT-xxx)
    /// </summary>
    public static class Product
    {
        /// <summary>Product not found</summary>
        public const string NotFound = "NOIR-PRODUCT-001";

        /// <summary>Category not found</summary>
        public const string CategoryNotFound = "NOIR-PRODUCT-002";

        /// <summary>Product category not found</summary>
        public const string ProductCategoryNotFound = "NOIR-PRODUCT-003";

        /// <summary>Product variant not found</summary>
        public const string VariantNotFound = "NOIR-PRODUCT-004";

        /// <summary>Cannot delete category with children</summary>
        public const string CategoryHasChildren = "NOIR-PRODUCT-005";

        /// <summary>Cannot delete category with products</summary>
        public const string CategoryHasProducts = "NOIR-PRODUCT-006";

        /// <summary>Product slug already exists</summary>
        public const string DuplicateSlug = "NOIR-PRODUCT-010";

        /// <summary>Product SKU already exists</summary>
        public const string DuplicateSku = "NOIR-PRODUCT-011";

        /// <summary>Product not found (general query)</summary>
        public const string NotFoundById = "NOIR-PRODUCT-012";

        /// <summary>Invalid status for operation</summary>
        public const string InvalidStatus = "NOIR-PRODUCT-013";

        /// <summary>Either ID or Slug must be provided</summary>
        public const string IdOrSlugRequired = "NOIR-PRODUCT-014";

        /// <summary>Product not found for deletion</summary>
        public const string NotFoundForDelete = "NOIR-PRODUCT-020";

        /// <summary>Product not found for option operations</summary>
        public const string NotFoundForOptions = "NOIR-PRODUCT-021";

        /// <summary>Option not found</summary>
        public const string OptionNotFound = "NOIR-PRODUCT-051";

        /// <summary>Option value not found or already exists</summary>
        public const string OptionValueNotFound = "NOIR-PRODUCT-052";

        /// <summary>Option value not found for deletion</summary>
        public const string OptionValueNotFoundForDelete = "NOIR-PRODUCT-053";
    }

    /// <summary>
    /// Attribute-related errors (ATTR-xxx)
    /// </summary>
    public static class Attribute
    {
        /// <summary>Attribute not found</summary>
        public const string NotFound = "NOIR-ATTR-001";

        /// <summary>Attribute code already exists</summary>
        public const string DuplicateCode = "NOIR-ATTR-002";

        /// <summary>Values can only be added to Select or MultiSelect types</summary>
        public const string ValuesOnlyForSelectTypes = "NOIR-ATTR-003";

        /// <summary>Attribute value already exists</summary>
        public const string ValueAlreadyExists = "NOIR-ATTR-004";

        /// <summary>Attribute value not found</summary>
        public const string ValueNotFound = "NOIR-ATTR-005";

        /// <summary>Cannot delete attribute with product associations</summary>
        public const string HasProducts = "NOIR-ATTR-006";

        /// <summary>Cannot change attribute type when products are assigned</summary>
        public const string CannotChangeTypeWithProducts = "NOIR-ATTR-007";

        /// <summary>Attribute is required for this category</summary>
        public const string RequiredForCategory = "NOIR-ATTR-008";

        /// <summary>Invalid attribute value for type</summary>
        public const string InvalidValueForType = "NOIR-ATTR-009";

        /// <summary>Category attribute link not found</summary>
        public const string CategoryLinkNotFound = "NOIR-ATTR-010";

        /// <summary>Attribute is already linked to category</summary>
        public const string AlreadyLinkedToCategory = "NOIR-ATTR-011";
    }

    /// <summary>
    /// External service errors (4xxx)
    /// </summary>
    public static class External
    {
        /// <summary>External service is unavailable</summary>
        public const string ServiceUnavailable = "NOIR-EXT-4001";

        /// <summary>Request to external service timed out</summary>
        public const string Timeout = "NOIR-EXT-4002";

        /// <summary>Failed to connect to external service</summary>
        public const string ConnectionFailed = "NOIR-EXT-4003";

        /// <summary>External service returned an error</summary>
        public const string ServiceError = "NOIR-EXT-4004";

        /// <summary>Invalid response from external service</summary>
        public const string InvalidResponse = "NOIR-EXT-4005";
    }

    /// <summary>
    /// System/internal errors (9xxx)
    /// </summary>
    public static class System
    {
        /// <summary>Internal server error</summary>
        public const string InternalError = "NOIR-SYS-9001";

        /// <summary>Database error</summary>
        public const string DatabaseError = "NOIR-SYS-9002";

        /// <summary>Configuration error</summary>
        public const string ConfigurationError = "NOIR-SYS-9003";

        /// <summary>File system error</summary>
        public const string FileSystemError = "NOIR-SYS-9004";

        /// <summary>Memory or resource error</summary>
        public const string ResourceError = "NOIR-SYS-9005";

        /// <summary>Unknown/unexpected error</summary>
        public const string UnknownError = "NOIR-SYS-9500";
    }

    /// <summary>
    /// Live configuration management errors (CFG-xxx)
    /// </summary>
    public static class Configuration
    {
        /// <summary>Runtime configuration changes are disabled</summary>
        public const string RuntimeChangesDisabled = "NOIR-CFG-001";

        /// <summary>Configuration section is not allowed for editing</summary>
        public const string SectionNotAllowed = "NOIR-CFG-002";

        /// <summary>Invalid JSON format in configuration value</summary>
        public const string InvalidJson = "NOIR-CFG-003";

        /// <summary>Configuration section not found</summary>
        public const string SectionNotFound = "NOIR-CFG-004";

        /// <summary>Restart cooldown active (rate limiting)</summary>
        public const string RestartTooSoon = "NOIR-CFG-005";

        /// <summary>Failed to load configuration backups</summary>
        public const string BackupLoadFailed = "NOIR-CFG-006";

        /// <summary>Backup file not found</summary>
        public const string BackupNotFound = "NOIR-CFG-007";

        /// <summary>Backup file is corrupted</summary>
        public const string BackupCorrupted = "NOIR-CFG-008";

        /// <summary>Failed to rollback configuration</summary>
        public const string RollbackFailed = "NOIR-CFG-009";
    }
}

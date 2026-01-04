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
}

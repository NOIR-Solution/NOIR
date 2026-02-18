// Global using directives for NOIR.Application.UnitTests

// Testing Framework
global using Xunit;
global using FluentAssertions;
global using Moq;
global using Bogus;
global using MockQueryable.Moq;

// System
global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Linq.Expressions;
global using System.Net;
global using System.Security.Claims;
global using System.Security.Cryptography;
global using System.Security.Principal;
global using System.Text;
global using System.Text.Json;
global using System.Threading;
global using System.Threading.Tasks;

// Microsoft - ASP.NET Core
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Authorization.Infrastructure;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Identity;

// Microsoft - Entity Framework Core
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.ChangeTracking;
global using Microsoft.EntityFrameworkCore.Diagnostics;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;
global using Microsoft.EntityFrameworkCore.Metadata.Conventions;
global using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
global using Microsoft.EntityFrameworkCore.Query;

// Microsoft - Extensions
global using Microsoft.Extensions.Caching.Memory;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using Microsoft.Extensions.Primitives;

// FluentValidation
global using FluentValidation;
global using FluentValidation.Results;
global using FluentValidation.TestHelper;

// FluentEmail
global using FluentEmail.Core;
global using FluentEmail.Core.Models;

// FluentStorage
global using FluentStorage.Blobs;

// Finbuckle MultiTenant
global using Finbuckle.MultiTenant;
global using Finbuckle.MultiTenant.Abstractions;

// Hangfire
global using Hangfire;
global using Hangfire.Common;
global using Hangfire.Dashboard;
global using Hangfire.States;
global using Hangfire.Storage;

// Wolverine
global using Wolverine;

// NOIR Application
global using NOIR.Application;
global using NOIR.Application.Behaviors;
global using NOIR.Application.Common.Exceptions;
global using NOIR.Application.Common.Interfaces;
global using NOIR.Application.Common.Models;
global using NOIR.Application.Common.Settings;
global using NOIR.Application.Common.Utilities;
global using NOIR.Application.Features.Auth.Commands.Login;
global using NOIR.Application.Features.Auth.Commands.Logout;
global using NOIR.Application.Features.Auth.Commands.RefreshToken;
global using NOIR.Application.Features.Auth.Commands.ChangePassword;
global using NOIR.Application.Features.Auth.Commands.RevokeSession;
global using NOIR.Application.Features.Auth.Commands.UploadAvatar;
global using NOIR.Application.Features.Auth.Commands.DeleteAvatar;
global using NOIR.Application.Features.Auth.Commands.ChangeEmail;
global using NOIR.Application.Features.Auth.Commands.UpdateUserProfile;
global using NOIR.Application.Features.Auth.Commands.PasswordReset.RequestPasswordReset;
global using NOIR.Application.Features.Auth.Commands.PasswordReset.VerifyPasswordResetOtp;
global using NOIR.Application.Features.Auth.Commands.PasswordReset.ResendPasswordResetOtp;
global using NOIR.Application.Features.Auth.Commands.PasswordReset.ResetPassword;
global using NOIR.Application.Features.Auth.DTOs;
global using NOIR.Application.Features.Auth.Queries.GetCurrentUser;
global using NOIR.Application.Specifications;
global using NOIR.Domain.Specifications;

// NOIR Domain
global using NOIR.Domain.Common;
global using NOIR.Domain.Entities;
global using NOIR.Domain.Entities.Cart;
global using NOIR.Domain.Entities.Checkout;
global using NOIR.Domain.Entities.Order;
global using NOIR.Domain.Entities.Payment;
global using NOIR.Domain.Entities.Product;
global using NOIR.Domain.Enums;
global using NOIR.Domain.Interfaces;
// Note: NOIR.Domain.ValueObjects excluded due to Address conflict with FluentEmail.Core.Models.Address

// NOIR Infrastructure
global using NOIR.Infrastructure;
global using NOIR.Infrastructure.BackgroundJobs;
global using NOIR.Infrastructure.Email;
global using NOIR.Infrastructure.Identity;
global using NOIR.Infrastructure.Identity.Authorization;
global using NOIR.Infrastructure.Localization;
global using NOIR.Infrastructure.Media;
global using NOIR.Infrastructure.Persistence;
global using NOIR.Infrastructure.Persistence.Conventions;
global using NOIR.Infrastructure.Persistence.Interceptors;
global using NOIR.Infrastructure.Persistence.Seeders;
global using NOIR.Infrastructure.Services;
global using NOIR.Infrastructure.Storage;
global using NOIR.Infrastructure.Caching;

// FusionCache
global using ZiggyCreatures.Caching.Fusion;

// NOIR Web
global using NOIR.Web.Filters;
global using NOIR.Web.Middleware;

// NOIR Application - Roles
global using NOIR.Application.Features.Roles.Commands.CreateRole;
global using NOIR.Application.Features.Roles.Commands.UpdateRole;
global using NOIR.Application.Features.Roles.Commands.DeleteRole;
global using NOIR.Application.Features.Roles.Queries.GetRoles;
global using NOIR.Application.Features.Roles.Queries.GetRoleById;
global using NOIR.Application.Features.Roles.DTOs;

// NOIR Application - Users
global using NOIR.Application.Features.Users.Commands.CreateUser;
global using NOIR.Application.Features.Users.Commands.UpdateUser;
global using NOIR.Application.Features.Users.Commands.DeleteUser;
global using NOIR.Application.Features.Users.Commands.LockUser;
global using NOIR.Application.Features.Users.Commands.AssignRoles;
global using NOIR.Application.Features.Users.Queries.GetUsers;
global using NOIR.Application.Features.Users.Queries.GetUserRoles;
global using NOIR.Application.Features.Users.DTOs;

// NOIR Application - Permissions
global using NOIR.Application.Features.Permissions.Commands.AssignToRole;
global using NOIR.Application.Features.Permissions.Commands.RemoveFromRole;
global using NOIR.Application.Features.Permissions.Queries.GetAllPermissions;
global using NOIR.Application.Features.Permissions.Queries.GetPermissionTemplates;
global using NOIR.Application.Features.Permissions.Queries.GetRolePermissions;
global using NOIR.Application.Features.Permissions.Queries.GetUserPermissions;

// NOIR Application - Notifications
global using NOIR.Application.Features.Notifications.Commands.DeleteNotification;
global using NOIR.Application.Features.Notifications.Commands.MarkAsRead;
global using NOIR.Application.Features.Notifications.Commands.MarkAllAsRead;
global using NOIR.Application.Features.Notifications.Commands.UpdatePreferences;
global using NOIR.Application.Features.Notifications.DTOs;
global using NOIR.Application.Features.Notifications.Queries.GetNotifications;
global using NOIR.Application.Features.Notifications.Queries.GetPreferences;
global using NOIR.Application.Features.Notifications.Queries.GetUnreadCount;

// NOIR Application - Blog
global using NOIR.Application.Features.Blog.DTOs;
global using NOIR.Application.Features.Blog.Commands.UnpublishPost;

// NOIR Application - Legal Pages
global using NOIR.Application.Features.LegalPages.Commands.UpdateLegalPage;
global using NOIR.Application.Features.LegalPages.Commands.RevertLegalPageToDefault;
global using NOIR.Application.Features.LegalPages.Queries.GetLegalPage;
global using NOIR.Application.Features.LegalPages.Queries.GetLegalPages;
global using NOIR.Application.Features.LegalPages.Queries.GetPublicLegalPage;
global using NOIR.Application.Features.LegalPages.DTOs;

// NOIR Application - Platform Settings
global using NOIR.Application.Features.PlatformSettings.Commands.UpdateSmtpSettings;
global using NOIR.Application.Features.PlatformSettings.Commands.TestSmtpConnection;
global using NOIR.Application.Features.PlatformSettings.Queries.GetSmtpSettings;
global using NOIR.Application.Features.PlatformSettings.DTOs;

// NOIR Application - Tenant Settings
global using NOIR.Application.Features.TenantSettings.Commands.UpdateBrandingSettings;
global using NOIR.Application.Features.TenantSettings.Commands.UpdateContactSettings;
global using NOIR.Application.Features.TenantSettings.Commands.UpdateRegionalSettings;
global using NOIR.Application.Features.TenantSettings.Commands.UpdateTenantSmtpSettings;
global using NOIR.Application.Features.TenantSettings.Commands.RevertTenantSmtpSettings;
global using NOIR.Application.Features.TenantSettings.Commands.TestTenantSmtpConnection;
global using NOIR.Application.Features.TenantSettings.Queries.GetBrandingSettings;
global using NOIR.Application.Features.TenantSettings.Queries.GetContactSettings;
global using NOIR.Application.Features.TenantSettings.Queries.GetRegionalSettings;
global using NOIR.Application.Features.TenantSettings.Queries.GetTenantSmtpSettings;
global using NOIR.Application.Features.TenantSettings.DTOs;

// NOIR Application - Developer Logs
global using NOIR.Application.Features.DeveloperLogs.DTOs;

// NOIR Application - Brands
global using NOIR.Application.Features.Brands.Commands.CreateBrand;
global using NOIR.Application.Features.Brands.Commands.UpdateBrand;
global using NOIR.Application.Features.Brands.Commands.DeleteBrand;
global using NOIR.Application.Features.Brands.Queries.GetBrandById;
global using NOIR.Application.Features.Brands.Queries.GetBrands;
global using NOIR.Application.Features.Brands.DTOs;
global using NOIR.Application.Features.Brands.Specifications;

// NOIR Infrastructure - Logging
global using NOIR.Infrastructure.Logging;

// NOIR Application - Shipping
global using NOIR.Application.Features.Shipping.Commands.CreateShippingOrder;
global using NOIR.Application.Features.Shipping.Commands.CancelShippingOrder;
global using NOIR.Application.Features.Shipping.Queries.CalculateShippingRates;
global using NOIR.Application.Features.Shipping.Queries.GetShippingTracking;
global using NOIR.Application.Features.Shipping.Queries.GetShippingOrder;
global using NOIR.Application.Features.Shipping.DTOs;
global using NOIR.Application.Features.Shipping.Specifications;
global using NOIR.Domain.Entities.Shipping;

// NOIR Application - Promotions
global using NOIR.Application.Features.Promotions.Commands.CreatePromotion;
global using NOIR.Application.Features.Promotions.Commands.UpdatePromotion;
global using NOIR.Application.Features.Promotions.Commands.DeletePromotion;
global using NOIR.Application.Features.Promotions.Commands.ActivatePromotion;
global using NOIR.Application.Features.Promotions.Commands.DeactivatePromotion;
global using NOIR.Application.Features.Promotions.Commands.ApplyPromotion;
global using NOIR.Application.Features.Promotions.Queries.GetPromotions;
global using NOIR.Application.Features.Promotions.Queries.GetPromotionById;
global using NOIR.Application.Features.Promotions.Queries.ValidatePromoCode;
global using NOIR.Application.Features.Promotions.DTOs;
global using NOIR.Application.Features.Promotions.Specifications;
global using NOIR.Domain.Entities.Promotion;

// NOIR Application - Reviews
global using NOIR.Application.Features.Reviews.Commands.CreateReview;
global using NOIR.Application.Features.Reviews.Commands.ApproveReview;
global using NOIR.Application.Features.Reviews.Commands.RejectReview;
global using NOIR.Application.Features.Reviews.Commands.AddAdminResponse;
global using NOIR.Application.Features.Reviews.Commands.BulkApproveReviews;
global using NOIR.Application.Features.Reviews.Commands.BulkRejectReviews;
global using NOIR.Application.Features.Reviews.Commands.VoteReview;
global using NOIR.Application.Features.Reviews.Queries.GetReviews;
global using NOIR.Application.Features.Reviews.Queries.GetProductReviews;
global using NOIR.Application.Features.Reviews.Queries.GetReviewStats;
global using NOIR.Application.Features.Reviews.Queries.GetReviewById;
global using NOIR.Application.Features.Reviews.DTOs;
global using NOIR.Application.Features.Reviews.Specifications;
global using NOIR.Application.Features.Orders.Specifications;
global using NOIR.Domain.Entities.Review;

// NOIR Application - Wishlists
global using NOIR.Application.Features.Wishlists.Commands.CreateWishlist;
global using NOIR.Application.Features.Wishlists.Commands.UpdateWishlist;
global using NOIR.Application.Features.Wishlists.Commands.DeleteWishlist;
global using NOIR.Application.Features.Wishlists.Commands.AddToWishlist;
global using NOIR.Application.Features.Wishlists.Commands.RemoveFromWishlist;
global using NOIR.Application.Features.Wishlists.Commands.MoveToCart;
global using NOIR.Application.Features.Wishlists.Commands.ShareWishlist;
global using NOIR.Application.Features.Wishlists.Commands.UpdateWishlistItemPriority;
global using NOIR.Application.Features.Wishlists.Queries.GetWishlists;
global using NOIR.Application.Features.Wishlists.Queries.GetWishlistById;
global using NOIR.Application.Features.Wishlists.Queries.GetSharedWishlist;
global using NOIR.Application.Features.Wishlists.Queries.GetWishlistAnalytics;
global using NOIR.Application.Features.Wishlists.DTOs;
global using NOIR.Application.Features.Wishlists.Common;
global using NOIR.Application.Features.Wishlists.Specifications;
global using NOIR.Domain.Entities.Wishlist;

// NOIR Application - Customers
global using NOIR.Application.Features.Customers.Commands.CreateCustomer;
global using NOIR.Application.Features.Customers.Commands.UpdateCustomer;
global using NOIR.Application.Features.Customers.Commands.DeleteCustomer;
global using NOIR.Application.Features.Customers.Commands.AddCustomerAddress;
global using NOIR.Application.Features.Customers.Commands.UpdateCustomerAddress;
global using NOIR.Application.Features.Customers.Commands.DeleteCustomerAddress;
global using NOIR.Application.Features.Customers.Commands.AddLoyaltyPoints;
global using NOIR.Application.Features.Customers.Commands.RedeemLoyaltyPoints;
global using NOIR.Application.Features.Customers.Commands.UpdateCustomerSegment;
global using NOIR.Application.Features.Customers.Queries.GetCustomerById;
global using NOIR.Application.Features.Customers.Queries.GetCustomers;
global using NOIR.Application.Features.Customers.Queries.GetCustomerOrders;
global using NOIR.Application.Features.Customers.Queries.GetCustomerStats;
global using NOIR.Application.Features.Customers.DTOs;
global using NOIR.Application.Features.Customers.Specifications;
global using NOIR.Domain.Entities.Customer;

// NOIR Test Helpers
global using NOIR.Application.UnitTests.Common;

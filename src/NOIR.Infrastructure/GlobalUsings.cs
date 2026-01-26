// Global using directives for NOIR.Infrastructure

// System
global using System;
global using System.Collections.Concurrent;
global using System.Collections.Generic;
global using System.ComponentModel.DataAnnotations;
global using System.Diagnostics;
global using System.IdentityModel.Tokens.Jwt;
global using System.IO.Compression;
global using System.Linq;
global using System.Linq.Expressions;
global using System.Reflection;
global using System.Security.Claims;
global using System.Security.Cryptography;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Nodes;
global using System.Text.RegularExpressions;
global using System.Threading;
global using System.Threading.Tasks;

// Microsoft - ASP.NET Core
global using Microsoft.AspNetCore.Authentication;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Identity;
global using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

// Microsoft - Entity Framework Core
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.ChangeTracking;
global using Microsoft.EntityFrameworkCore.Design;
global using Microsoft.EntityFrameworkCore.Diagnostics;
global using Microsoft.EntityFrameworkCore.Storage;

// EFCore.BulkExtensions (High Performance Bulk Operations)
global using EFCore.BulkExtensions;

// Microsoft - Extensions
global using Microsoft.Extensions.Caching.Memory;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using Microsoft.IdentityModel.Tokens;

// Finbuckle MultiTenant
global using Finbuckle.MultiTenant;
global using Finbuckle.MultiTenant.Abstractions;
global using Finbuckle.MultiTenant.AspNetCore.Extensions;
global using Finbuckle.MultiTenant.EntityFrameworkCore;
global using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
global using Finbuckle.MultiTenant.Extensions;

// FluentEmail
global using FluentEmail.Core;
global using FluentEmail.MailKitSmtp;

// FluentValidation
global using FluentValidation;

// FluentStorage
global using FluentStorage;
global using FluentStorage.Blobs;

// Hangfire
global using Hangfire;
global using Hangfire.Common;
global using Hangfire.SqlServer;
global using Hangfire.States;

// Wolverine
global using Wolverine;

// Scrutor
global using Scrutor;

// FusionCache (Hybrid L1/L2 Caching)
global using ZiggyCreatures.Caching.Fusion;

// NOIR Application
global using NOIR.Application.Common.Interfaces;
global using NOIR.Application.Common.Models;
global using NOIR.Application.Common.Settings;
global using NOIR.Application.Common.Utilities;
global using NOIR.Application.Features.Auth.Commands.Login;
global using NOIR.Application.Features.Auth.Commands.Logout;
global using NOIR.Application.Features.Auth.Commands.RefreshToken;
global using NOIR.Application.Features.Auth.Commands.UpdateUserProfile;
global using NOIR.Application.Features.Auth.DTOs;
global using NOIR.Application.Features.Auth.Queries.GetCurrentUser;
global using NOIR.Application.Features.Auth.Queries.GetUserById;
global using NOIR.Application.Features.Roles.Commands.CreateRole;
global using NOIR.Application.Features.Roles.Commands.UpdateRole;
global using NOIR.Application.Features.Roles.Commands.DeleteRole;
global using NOIR.Application.Features.Roles.Queries.GetRoles;
global using NOIR.Application.Features.Roles.Queries.GetRoleById;
global using NOIR.Application.Features.Roles.DTOs;
global using NOIR.Application.Features.Users.Commands.CreateUser;
global using NOIR.Application.Features.Users.Commands.UpdateUser;
global using NOIR.Application.Features.Users.Commands.DeleteUser;
global using NOIR.Application.Features.Users.Commands.AssignRoles;
global using NOIR.Application.Features.Users.Queries.GetUsers;
global using NOIR.Application.Features.Users.Queries.GetUserRoles;
global using NOIR.Application.Features.Users.DTOs;
global using NOIR.Application.Features.Tenants.DTOs;
global using NOIR.Application.Features.Tenants.Queries.GetTenantById;
global using NOIR.Application.Features.Permissions.Commands.AssignToRole;
global using NOIR.Application.Features.Permissions.Commands.RemoveFromRole;
global using NOIR.Application.Features.Permissions.Queries.GetRolePermissions;
global using NOIR.Application.Features.Permissions.Queries.GetUserPermissions;
global using NOIR.Application.Specifications;
global using NOIR.Application.Specifications.EmailChangeOtps;
global using NOIR.Application.Specifications.Notifications;
global using NOIR.Application.Specifications.RefreshTokens;
global using NOIR.Application.Specifications.ResourceShares;
global using NOIR.Application.Specifications.PasswordResetOtps;
global using NOIR.Application.Specifications.TenantSettings;
global using NOIR.Application.Features.EmailTemplates.Specifications;
global using NOIR.Application.Features.DeveloperLogs.DTOs;
global using NOIR.Application.Features.Blog.DTOs;
global using NOIR.Application.Features.Blog.Queries.GetPost;
global using NOIR.Application.Features.Blog.Queries.GetCategoryById;
global using NOIR.Application.Features.Blog.Queries.GetTagById;
global using NOIR.Application.Features.EmailTemplates.DTOs;
global using NOIR.Application.Features.EmailTemplates.Queries.GetEmailTemplate;
global using NOIR.Application.Features.LegalPages.DTOs;
global using NOIR.Application.Features.LegalPages.Queries.GetLegalPage;
global using NOIR.Application.Features.Products.DTOs;
global using NOIR.Application.Features.Products.Queries.GetProductById;
global using NOIR.Application.Features.Products.Queries.GetProductCategoryById;
global using NOIR.Application.Features.Orders.DTOs;
global using NOIR.Application.Features.Orders.Queries.GetOrderById;
global using NOIR.Application.Features.PlatformSettings.DTOs;
global using NOIR.Application.Features.PlatformSettings.Queries.GetSmtpSettings;
global using NOIR.Application.Features.TenantSettings.DTOs;
global using NOIR.Application.Features.TenantSettings.Queries.GetBrandingSettings;
global using NOIR.Application.Features.TenantSettings.Queries.GetContactSettings;
global using NOIR.Application.Features.TenantSettings.Queries.GetRegionalSettings;
global using NOIR.Application.Features.TenantSettings.Queries.GetTenantSmtpSettings;

// NOIR Domain
global using NOIR.Domain.Common;
global using NOIR.Domain.Enums;
global using NOIR.Domain.Interfaces;
global using NOIR.Domain.Specifications;
global using NOIR.Domain.ValueObjects;

// Microsoft - Authorization
global using Microsoft.AspNetCore.Authorization;

// NOIR Infrastructure
global using NOIR.Infrastructure.Audit;
global using NOIR.Infrastructure.BackgroundJobs;
global using NOIR.Infrastructure.Email;
global using NOIR.Infrastructure.Hubs;
global using NOIR.Infrastructure.Identity;
global using NOIR.Infrastructure.Identity.Authorization;
global using NOIR.Infrastructure.Logging;
global using NOIR.Infrastructure.Persistence;
global using NOIR.Infrastructure.Persistence.Configurations;
global using NOIR.Infrastructure.Persistence.Interceptors;
global using NOIR.Infrastructure.Persistence.Seeders;
global using NOIR.Infrastructure.Services;
global using NOIR.Infrastructure.Services.Payment;
global using NOIR.Infrastructure.Storage;
global using NOIR.Infrastructure.Localization;
global using NOIR.Infrastructure.Caching;
global using NOIR.Infrastructure.Media;

// NOIR Domain Entities
global using NOIR.Domain.Entities;
global using NOIR.Domain.Entities.Cart;
global using NOIR.Domain.Entities.Checkout;
global using NOIR.Domain.Entities.Order;
global using NOIR.Domain.Entities.Payment;
global using NOIR.Domain.Entities.Product;
global using NOIR.Domain.Events.Cart;
global using NOIR.Domain.Events.Checkout;
global using NOIR.Domain.Events.Order;
global using NOIR.Domain.Events.Payment;
global using NOIR.Domain.Events.Product;
global using NOIR.Application.Features.Payments.Specifications;

// Microsoft - Entity Framework Core Metadata
global using Microsoft.EntityFrameworkCore.Metadata;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;
global using Microsoft.EntityFrameworkCore.Metadata.Conventions;
global using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

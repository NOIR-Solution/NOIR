// Global using directives for NOIR.Web

// System
global using System;
global using System.IO.Compression;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Threading.RateLimiting;

// Microsoft - ASP.NET Core
global using Microsoft.AspNetCore.Authentication.JwtBearer;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Diagnostics.HealthChecks;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.OutputCaching;
global using Microsoft.AspNetCore.RateLimiting;
global using Microsoft.AspNetCore.ResponseCompression;
global using Microsoft.AspNetCore.Server.Kestrel.Core;

// Microsoft - Extensions
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using Microsoft.IdentityModel.Tokens;

// Finbuckle MultiTenant
global using Finbuckle.MultiTenant;
global using Finbuckle.MultiTenant.Abstractions;
global using Finbuckle.MultiTenant.AspNetCore.Extensions;

// FluentValidation
global using FluentValidation;

// Hangfire
global using Hangfire;
global using Hangfire.Dashboard;

// HealthChecks
global using HealthChecks.UI.Client;

// JasperFx (Wolverine)
global using JasperFx.CodeGeneration;

// Scalar
global using Scalar.AspNetCore;

// Serilog
global using Serilog;

// Wolverine
global using Wolverine;
global using Wolverine.FluentValidation;

// NOIR Application
global using NOIR.Application;
global using NOIR.Application.Common.Exceptions;
global using NOIR.Application.Common.Interfaces;
global using NOIR.Application.Common.Models;
global using NOIR.Application.Common.Settings;
global using NOIR.Application.Features.Auth.Commands.ChangePassword;
global using NOIR.Application.Features.Auth.Commands.DeleteAvatar;
global using NOIR.Application.Features.Auth.Commands.Login;
global using NOIR.Application.Features.Auth.Commands.Logout;
global using NOIR.Application.Features.Auth.Commands.RefreshToken;
global using NOIR.Application.Features.Auth.Commands.RevokeSession;
global using NOIR.Application.Features.Auth.Commands.UpdateUserProfile;
global using NOIR.Application.Features.Auth.Commands.UploadAvatar;
global using NOIR.Application.Features.Auth.Queries.GetActiveSessions;
global using NOIR.Application.Features.Auth.Queries.GetUserById;
global using NOIR.Application.Features.Auth.DTOs;
global using NOIR.Application.Features.Auth.Queries.GetCurrentUser;

// NOIR Domain
global using NOIR.Domain.Common;
global using NOIR.Domain.Entities;
global using NOIR.Domain.Enums;
global using NOIR.Domain.Interfaces;

// NOIR Infrastructure
global using NOIR.Infrastructure;
global using NOIR.Infrastructure.Audit;
global using NOIR.Infrastructure.Identity;
global using NOIR.Infrastructure.Localization;
global using NOIR.Infrastructure.Logging;
global using NOIR.Infrastructure.Persistence;
global using NOIR.Infrastructure.Storage;

// NOIR Web
global using NOIR.Web.Endpoints;
global using NOIR.Web.Extensions;
global using NOIR.Web.Filters;
global using NOIR.Web.Internal;
global using NOIR.Web.Json;
global using NOIR.Web.Middleware;

// NOIR Application Behaviors
global using NOIR.Application.Behaviors;

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
global using NOIR.Application.Features.Permissions.Queries.GetRolePermissions;
global using NOIR.Application.Features.Permissions.Queries.GetUserPermissions;

// NOIR Application - Email Templates
global using NOIR.Application.Features.EmailTemplates.DTOs;
global using NOIR.Application.Features.EmailTemplates.Specifications;
global using NOIR.Application.Features.EmailTemplates.Queries.GetEmailTemplates;
global using NOIR.Application.Features.EmailTemplates.Queries.GetEmailTemplate;
global using NOIR.Application.Features.EmailTemplates.Commands.UpdateEmailTemplate;
global using NOIR.Application.Features.EmailTemplates.Commands.SendTestEmail;

// NOIR Application - Legal Pages
global using NOIR.Application.Features.LegalPages.DTOs;

// NOIR Application - Tenant Settings
global using NOIR.Application.Features.TenantSettings.DTOs;

// NOIR Application - Tenants
global using NOIR.Application.Features.Tenants.DTOs;
global using NOIR.Application.Features.Tenants.Commands.CreateTenant;
global using NOIR.Application.Features.Tenants.Commands.UpdateTenant;
global using NOIR.Application.Features.Tenants.Commands.DeleteTenant;
global using NOIR.Application.Features.Tenants.Queries.GetTenants;
global using NOIR.Application.Features.Tenants.Queries.GetTenantById;

// NOIR Application - Developer Logs
global using NOIR.Application.Features.DeveloperLogs.DTOs;

// NOIR Application - Media
global using NOIR.Application.Features.Media.Dtos;

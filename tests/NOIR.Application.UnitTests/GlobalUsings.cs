// Global using directives for NOIR.Application.UnitTests

// Testing Framework
global using Xunit;
global using FluentAssertions;
global using Moq;
global using Bogus;

// System
global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Linq.Expressions;
global using System.Net;
global using System.Security.Claims;
global using System.Security.Principal;
global using System.Text;
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
global using NOIR.Application.Features.Auth.DTOs;
global using NOIR.Application.Features.Auth.Queries.GetCurrentUser;
global using NOIR.Application.Specifications;
global using NOIR.Domain.Specifications;

// NOIR Domain
global using NOIR.Domain.Common;
global using NOIR.Domain.Entities;
global using NOIR.Domain.Enums;
global using NOIR.Domain.Interfaces;

// NOIR Infrastructure
global using NOIR.Infrastructure;
global using NOIR.Infrastructure.BackgroundJobs;
global using NOIR.Infrastructure.Email;
global using NOIR.Infrastructure.Identity;
global using NOIR.Infrastructure.Identity.Authorization;
global using NOIR.Infrastructure.Localization;
global using NOIR.Infrastructure.Persistence;
global using NOIR.Infrastructure.Persistence.Conventions;
global using NOIR.Infrastructure.Persistence.Interceptors;
global using NOIR.Infrastructure.Services;
global using NOIR.Infrastructure.Storage;

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
global using NOIR.Application.Features.Users.Commands.UpdateUser;
global using NOIR.Application.Features.Users.Commands.DeleteUser;
global using NOIR.Application.Features.Users.Commands.AssignRoles;
global using NOIR.Application.Features.Users.Queries.GetUsers;
global using NOIR.Application.Features.Users.Queries.GetUserRoles;
global using NOIR.Application.Features.Users.DTOs;

// NOIR Application - Permissions
global using NOIR.Application.Features.Permissions.Commands.AssignToRole;
global using NOIR.Application.Features.Permissions.Commands.RemoveFromRole;
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

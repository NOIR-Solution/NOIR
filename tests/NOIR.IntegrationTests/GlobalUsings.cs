// Global using directives for NOIR.IntegrationTests

// Testing Framework
global using Xunit;
global using FluentAssertions;
global using Moq;

// System
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Net;
global using System.Net.Http.Json;
global using System.Text;
global using System.Text.Json;
global using System.Threading;
global using System.Threading.Tasks;

// Microsoft - ASP.NET Core
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Http.HttpResults;
global using Microsoft.AspNetCore.Identity;
global using Microsoft.AspNetCore.Mvc.Testing;

// Microsoft - Data
global using Microsoft.Data.SqlClient;

// Microsoft - Entity Framework Core
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Diagnostics;

// EFCore.BulkExtensions
global using EFCore.BulkExtensions;

// Microsoft - Extensions
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;

// Finbuckle MultiTenant
global using Finbuckle.MultiTenant;
global using Finbuckle.MultiTenant.Abstractions;

// Respawn
global using Respawn;

// Wolverine
global using Wolverine;

// NOIR Application
global using NOIR.Application.Common.Interfaces;
global using NOIR.Application.Common.Models;
global using NOIR.Application.Common.Settings;
global using NOIR.Application.Features.Auth.Commands.Login;
global using NOIR.Application.Features.Auth.Commands.Logout;
global using NOIR.Application.Features.Auth.Commands.RefreshToken;
global using NOIR.Application.Features.Auth.DTOs;
global using NOIR.Application.Features.Auth.Queries.GetCurrentUser;
global using NOIR.Application.Features.Auth.Queries.GetUserById;
global using NOIR.Application.Features.Auth.Commands.UpdateUserProfile;
global using NOIR.Application.Specifications;
global using NOIR.Application.Specifications.RefreshTokens;
global using NOIR.Application.Specifications.ResourceShares;
global using NOIR.Domain.Specifications;

// NOIR Domain
global using NOIR.Domain.Common;
global using NOIR.Domain.Entities;
global using NOIR.Domain.Interfaces;

// NOIR Infrastructure
global using NOIR.Infrastructure.Identity;
global using NOIR.Infrastructure.Persistence;
global using NOIR.Infrastructure.Persistence.Interceptors;

// NOIR Web
global using NOIR.Web.Extensions;

// NOIR Integration Tests Infrastructure
global using NOIR.IntegrationTests.Infrastructure;

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
global using NOIR.Application.Features.Users.Commands.AssignRoles;
global using NOIR.Application.Features.Users.Queries.GetUsers;
global using NOIR.Application.Features.Users.Queries.GetUserRoles;
global using NOIR.Application.Features.Users.DTOs;

// NOIR Application - Permissions
global using NOIR.Application.Features.Permissions.Commands.AssignToRole;
global using NOIR.Application.Features.Permissions.Commands.RemoveFromRole;
global using NOIR.Application.Features.Permissions.Queries.GetRolePermissions;
global using NOIR.Application.Features.Permissions.Queries.GetUserPermissions;

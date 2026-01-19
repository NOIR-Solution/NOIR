// Global using directives for NOIR.Application

// System
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Linq.Expressions;
global using System.Text;
global using System.Threading;
global using System.Threading.Tasks;

// Microsoft - Extensions
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;

// System - ComponentModel
global using System.ComponentModel.DataAnnotations;

// FluentValidation
global using FluentValidation;
global using FluentValidation.Results;

// System - Security
global using System.Security.Claims;

// NOIR Application
global using NOIR.Application.Common.Interfaces;
global using NOIR.Application.Common.Models;
global using NOIR.Application.Common.Settings;
global using NOIR.Application.Features.Auth.DTOs;
global using NOIR.Application.Features.Auth.Queries.GetUserById;
global using NOIR.Application.Features.Users.DTOs;
global using NOIR.Application.Features.Roles.DTOs;
global using NOIR.Application.Features.EmailTemplates.DTOs;
global using NOIR.Application.Features.EmailTemplates.Specifications;
global using NOIR.Application.Features.Tenants.DTOs;
global using NOIR.Application.Specifications;
global using NOIR.Application.Specifications.Notifications;
global using NOIR.Application.Specifications.PasswordResetOtps;

// Finbuckle MultiTenant Abstractions
// Note: For Tenant CRUD, use IMultiTenantStore<Tenant> (registered by Finbuckle)
// instead of IRepository<Tenant, Guid> (Tenant doesn't inherit from AggregateRoot)
global using Finbuckle.MultiTenant.Abstractions;

// NOIR Domain
global using NOIR.Domain.Common;
global using NOIR.Domain.Entities;
global using NOIR.Domain.Enums;
global using NOIR.Domain.Interfaces;
global using NOIR.Domain.Specifications;

// System.Text.Json
global using System.Text.Json;

// Wolverine
global using Wolverine;

// Diagnostics
global using System.Diagnostics;

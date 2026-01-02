// Global using directives for NOIR.Application

// System
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Linq.Expressions;
global using System.Threading;
global using System.Threading.Tasks;

// Microsoft - Entity Framework Core
global using Microsoft.EntityFrameworkCore;

// Microsoft - Extensions
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

// FluentValidation
global using FluentValidation;
global using FluentValidation.Results;

// System - Security
global using System.Security.Claims;

// NOIR Application
global using NOIR.Application.Common.Interfaces;
global using NOIR.Application.Common.Models;
global using NOIR.Application.Features.Auth.DTOs;
global using NOIR.Application.Features.Auth.Queries.GetUserById;
global using NOIR.Application.Features.Audit.DTOs;
global using NOIR.Application.Features.Audit.Queries;

// NOIR Domain
global using NOIR.Domain.Common;
global using NOIR.Domain.Entities;
global using NOIR.Domain.Specifications;

// Wolverine
global using Wolverine;

// Diagnostics
global using System.Diagnostics;

// Global using directives for NOIR.Domain

// System
global using System;
global using System.Collections.Generic;
global using System.ComponentModel.DataAnnotations;
global using System.Globalization;
global using System.Linq;
global using System.Linq.Expressions;
global using System.Reflection;
global using System.Text.Json;
global using System.Threading;
global using System.Threading.Tasks;

// Finbuckle MultiTenant Abstractions
global using Finbuckle.MultiTenant.Abstractions;

// NOIR Domain
global using NOIR.Domain.Common;
global using NOIR.Domain.Entities;
global using NOIR.Domain.Entities.Cart;
global using NOIR.Domain.Entities.Customer;
global using NOIR.Domain.Entities.Payment;
global using NOIR.Domain.Entities.Checkout;
global using NOIR.Domain.Entities.Order;
global using NOIR.Domain.Entities.Product;
global using NOIR.Domain.Entities.Analytics;
global using NOIR.Domain.Entities.Inventory;
global using NOIR.Domain.Entities.Review;
global using NOIR.Domain.Entities.Promotion;
global using NOIR.Domain.Entities.Shipping;
global using NOIR.Domain.Entities.Wishlist;
global using NOIR.Domain.Enums;
global using NOIR.Domain.Events.Cart;
global using NOIR.Domain.Events.Checkout;
global using NOIR.Domain.Events.Order;
global using NOIR.Domain.Events.Payment;
global using NOIR.Domain.Events.Product;
global using NOIR.Domain.Events.Shipping;
global using NOIR.Domain.Specifications;
global using NOIR.Domain.ValueObjects;

// System.Security
global using System.Security.Cryptography;

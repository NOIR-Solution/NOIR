using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NOIR.Application.Common.Interfaces;
using NOIR.Infrastructure.Services.Payment.Providers.COD;
using NOIR.Infrastructure.Services.Payment.Providers.MoMo;
using NOIR.Infrastructure.Services.Payment.Providers.PayOS;
using NOIR.Infrastructure.Services.Payment.Providers.SePay;
using NOIR.Infrastructure.Services.Payment.Providers.Stripe;
using NOIR.Infrastructure.Services.Payment.Providers.VnPay;
using NOIR.Infrastructure.Services.Payment.Providers.ZaloPay;
using Polly;
using Polly.Extensions.Http;

namespace NOIR.Infrastructure.Services.Payment;

/// <summary>
/// Extension methods for registering payment gateway services.
/// </summary>
public static class PaymentGatewayExtensions
{
    /// <summary>
    /// Adds payment gateway services including VNPay, MoMo, ZaloPay, SePay, Stripe, PayOS, and COD providers.
    /// </summary>
    public static IServiceCollection AddPaymentGatewayServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure gateway settings (Vietnam domestic)
        services.Configure<VnPaySettings>(configuration.GetSection(VnPaySettings.SectionName));
        services.Configure<MoMoSettings>(configuration.GetSection(MoMoSettings.SectionName));
        services.Configure<ZaloPaySettings>(configuration.GetSection(ZaloPaySettings.SectionName));
        services.Configure<SePaySettings>(configuration.GetSection(SePaySettings.SectionName));

        // Configure gateway settings (International + Advanced)
        services.Configure<StripeSettings>(configuration.GetSection(StripeSettings.SectionName));
        services.Configure<PayOSSettings>(configuration.GetSection(PayOSSettings.SectionName));
        services.Configure<CurrencySettings>(configuration.GetSection(CurrencySettings.SectionName));

        // Configure VNPay HTTP client with resilience policies
        services.AddHttpClient<IVnPayClient, VnPayClient>((sp, client) =>
            {
                var settings = configuration.GetSection(VnPaySettings.SectionName).Get<VnPaySettings>()
                    ?? new VnPaySettings();
                client.BaseAddress = new Uri(settings.ApiUrl.TrimEnd('/') + '/');
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Configure MoMo HTTP client with resilience policies
        services.AddHttpClient<IMoMoClient, MoMoClient>((sp, client) =>
            {
                var settings = configuration.GetSection(MoMoSettings.SectionName).Get<MoMoSettings>()
                    ?? new MoMoSettings();
                client.BaseAddress = new Uri(settings.ApiEndpoint.TrimEnd('/') + '/');
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Configure ZaloPay HTTP client with resilience policies
        services.AddHttpClient<IZaloPayClient, ZaloPayClient>((sp, client) =>
            {
                var settings = configuration.GetSection(ZaloPaySettings.SectionName).Get<ZaloPaySettings>()
                    ?? new ZaloPaySettings();
                client.BaseAddress = new Uri(settings.Endpoint.TrimEnd('/') + '/');
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Configure SePay HTTP client with resilience policies
        services.AddHttpClient<ISePayClient, SePayClient>((sp, client) =>
            {
                var settings = configuration.GetSection(SePaySettings.SectionName).Get<SePaySettings>()
                    ?? new SePaySettings();
                client.BaseAddress = new Uri(settings.ApiBaseUrl.TrimEnd('/') + '/');
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Configure PayOS HTTP client with resilience policies
        services.AddHttpClient<IPayOSClient, PayOSClient>((sp, client) =>
            {
                var settings = configuration.GetSection(PayOSSettings.SectionName).Get<PayOSSettings>()
                    ?? new PayOSSettings();
                client.BaseAddress = new Uri(settings.ApiUrl.TrimEnd('/') + '/');
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Configure Currency service HTTP client
        services.AddHttpClient<CurrencyService>((sp, client) =>
            {
                var settings = configuration.GetSection(CurrencySettings.SectionName).Get<CurrencySettings>()
                    ?? new CurrencySettings();
                client.BaseAddress = new Uri(settings.ExchangeRateApiUrl.TrimEnd('/') + '/');
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Register Stripe client (uses Stripe.net SDK)
        services.AddScoped<IStripeClient, StripeClient>();

        // Register payment gateway providers (Vietnam domestic)
        services.AddScoped<IPaymentGatewayProvider, VnPayProvider>();
        services.AddScoped<IPaymentGatewayProvider, MoMoProvider>();
        services.AddScoped<IPaymentGatewayProvider, ZaloPayProvider>();
        services.AddScoped<IPaymentGatewayProvider, SePayProvider>();
        services.AddScoped<IPaymentGatewayProvider, CodProvider>();

        // Register payment gateway providers (International + Advanced)
        services.AddScoped<IPaymentGatewayProvider, StripeProvider>();
        services.AddScoped<IPaymentGatewayProvider, PayOSProvider>();

        // Register currency service
        services.AddScoped<ICurrencyService, CurrencyService>();

        return services;
    }

    /// <summary>
    /// Creates a retry policy for transient HTTP errors.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    // Retry logging handled by caller
                });
    }

    /// <summary>
    /// Creates a circuit breaker policy for HTTP calls.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
    }
}

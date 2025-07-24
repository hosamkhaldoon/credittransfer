using CreditTransfer.Core.Application.Interfaces;
using CreditTransfer.Core.Application.Services;
using CreditTransfer.Infrastructure.Data;
using CreditTransfer.Infrastructure.Repositories;
using IntegrationProxies.Nobill.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CreditTransfer.Infrastructure.Configuration
{
    /// <summary>
    /// Dependency injection configuration for the Credit Transfer system
    /// Registers all services, repositories, and configurations
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds all Credit Transfer services to the dependency injection container
        /// </summary>
        public static IServiceCollection AddCreditTransferServices(this IServiceCollection services, IConfiguration _configuration)
        {
            // Register ActivitySource for OpenTelemetry instrumentation
            services.AddSingleton<ActivitySource>(sp =>
                new ActivitySource("CreditTransfer.Core", "1.0.0"));

            // Register Entity Framework DbContext
            services.AddDbContext<CreditTransferDbContext>(options =>
                options.UseSqlServer(_configuration.GetConnectionString("DefaultConnection")));

            // Register Redis distributed cache
            services.AddStackExchangeRedisCache(options =>
            {
                var redisConnectionString = _configuration.GetValue<string>("Redis:ConnectionString");
                if (!string.IsNullOrEmpty(redisConnectionString))
                {
                    options.Configuration = redisConnectionString;
                    options.InstanceName = _configuration.GetValue<string>("Redis:InstanceName", "CreditTransfer");
                }
            });

            // Fallback to in-memory cache if Redis is not available
            services.AddMemoryCache();

            // Register NobillCalls integration services (self-contained with database configuration)
            services.AddCompleteNobillIntegration(_configuration);

            // Register repositories - all require ActivitySource for OpenTelemetry instrumentation
            services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();
            services.AddScoped<IApplicationConfigRepository, ApplicationConfigRepository>();
            services.AddScoped<ITransferConfigRepository, TransferConfigRepository>();
            services.AddScoped<IMessageRepository, MessageRepository>();
            services.AddScoped<ITransferRulesService, TransferRulesRepository>();

            // Register application services - requires ActivitySource for OpenTelemetry instrumentation
            services.AddScoped<ICreditTransferService, CreditTransferService>();

            // Register database-driven error configuration service
            services.AddScoped<IErrorConfigurationService, ErrorConfigurationService>();

            // Register configuration sections
            services.Configure<CreditTransferOptions>(_configuration.GetSection("CreditTransfer"));
            services.Configure<NobillCallsOptions>(_configuration.GetSection("NobillCalls"));

            return services;
        }

        /// <summary>
        /// Adds logging configuration for Credit Transfer services
        /// </summary>
        public static IServiceCollection AddCreditTransferLogging(this IServiceCollection services)
        {
            // Logging is typically configured at the host level
            // This method can be extended for specific logging requirements
            return services;
        }
    }

    /// <summary>
    /// Configuration options for Credit Transfer system
    /// Maps to appsettings.json CreditTransfer section
    /// </summary>
    public class CreditTransferOptions
    {
        public const string SectionName = "CreditTransfer";

        public int MsisdnLength { get; set; } = 12;
        public int RefillPinLength { get; set; } = 4;
        public bool EnableExtendedDays { get; set; } = false;
        public string DefaultPIN { get; set; } = "0000";
        public decimal MaximumPercentageAmount { get; set; } = 1.0m;
        public string CustomerToCustomerTransferMoneyReason { get; set; } = "C2C";
        public List<decimal> Denominations { get; set; } = new() { 1, 2, 5, 10, 20, 50, 100 };

        // Business rule configurations
        public decimal MinTransferAmount { get; set; } = 1.0m;
        public decimal MaxTransferAmount { get; set; } = 1000.0m;
        public int MaxDailyTransferCount { get; set; } = 10;
        public decimal MinPostTransferBalance { get; set; } = 0.5m;

        // Service configurations
        public string CreditTransferCustomerService { get; set; } = "CreditTransfer";
        public int TransactionTimeoutSeconds { get; set; } = 30;
        public bool EnableTransactionLogging { get; set; } = true;
        public bool EnableAuditTrail { get; set; } = true;
    }

    /// <summary>
    /// Configuration options for NobillCalls web service
    /// Maps to appsettings.json NobillCalls section
    /// </summary>
    public class NobillCallsOptions
    {
        public const string SectionName = "NobillCalls";

        public string ServiceUrl { get; set; } = "http://10.1.132.98/NobillProxy/NobillCalls.asmx";
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryCount { get; set; } = 3;
        public bool EnableLogging { get; set; } = true;
    }
}
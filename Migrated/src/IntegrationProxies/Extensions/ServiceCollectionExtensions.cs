using IntegrationProxies.Nobill.Interfaces;
using IntegrationProxies.Nobill.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace IntegrationProxies.Nobill.Extensions
{
    /// <summary>
    /// Service collection extensions for IntegrationProxies.Nobill
    /// Provides self-contained dependency injection configuration
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds NobillCalls integration services with database configuration support
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddNobillServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register ActivitySource for OpenTelemetry instrumentation
            services.AddSingleton<ActivitySource>(sp =>
                new ActivitySource("IntegrationProxies.Nobill", "1.0.0"));

            // Register lightweight configuration database context
            services.AddDbContextFactory<NobillConfigDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Register configuration repository
            services.AddScoped<IConfigurationRepository, ConfigurationRepository>();

            // Register NobillCalls service
            services.AddScoped<INobillCallsService, NobillCallsService>();

            return services;
        }

        /// <summary>
        /// Adds caching services for configuration (Redis or in-memory)
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddNobillCaching(this IServiceCollection services, IConfiguration configuration)
        {
            // Add Redis distributed cache if available
            var redisConnectionString = configuration.GetValue<string>("Redis:ConnectionString");
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnectionString;
                    options.InstanceName = configuration.GetValue<string>("Redis:InstanceName", "NobillIntegration");
                });
            }
            else
            {
                // Fallback to in-memory cache
                services.AddMemoryCache();
                services.AddSingleton<Microsoft.Extensions.Caching.Distributed.IDistributedCache, 
                    Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache>();
            }

            return services;
        }

        /// <summary>
        /// Adds complete Nobill integration with configuration and caching
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddCompleteNobillIntegration(this IServiceCollection services, IConfiguration configuration)
        {
            return services
                .AddNobillCaching(configuration)
                .AddNobillServices(configuration);
        }
    }
} 
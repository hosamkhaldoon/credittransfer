using System.ComponentModel;
using System.Text.Json;
using IntegrationProxies.Nobill.Interfaces;
using IntegrationProxies.Nobill.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace IntegrationProxies.Nobill.Services
{
    /// <summary>
    /// Lightweight configuration repository implementation for integration proxy services
    /// Uses Entity Framework for database operations and Redis for caching
    /// </summary>
    public class ConfigurationRepository : IConfigurationRepository
    {
        private readonly IDbContextFactory<NobillConfigDbContext> _contextFactory;
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _fallbackConfiguration;
        private readonly ILogger<ConfigurationRepository> _logger;
        private const string CACHE_KEY_PREFIX = "NobillConfig:";
        private const int CACHE_EXPIRY_MINUTES = 43200; // 30 days - configuration changes infrequently
        private static bool _redisWarningLogged = false; // Prevent log spam

        /// <summary>
        /// ConfigurationRepository
        /// </summary>
        /// <param name="contextFactory"></param>
        /// <param name="cache"></param>
        /// <param name="fallbackConfiguration"></param>
        /// <param name="logger"></param>
        public ConfigurationRepository(
            IDbContextFactory<NobillConfigDbContext> contextFactory,
            IDistributedCache cache,
            IConfiguration fallbackConfiguration,
            ILogger<ConfigurationRepository> logger)
        {
            _contextFactory = contextFactory;
            _cache = cache;
            _fallbackConfiguration = fallbackConfiguration;
            _logger = logger;
        }

        /// <summary>
        /// Safely attempts to get value from Redis cache with proper exception handling
        /// </summary>
        private async Task<string?> TryGetFromCacheAsync(string cacheKey)
        {
            try
            {
                // Set a shorter timeout for cache operations to fail fast
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                return await _cache.GetStringAsync(cacheKey, cts.Token);
            }
            catch (RedisConnectionException ex)
            {
                if (!_redisWarningLogged)
                {
                    _logger.LogWarning(ex, "Redis connection unavailable, falling back to database and appsettings. Redis error: {Error}", ex.Message);
                    _redisWarningLogged = true;
                }
                return null;
            }
            catch (OperationCanceledException)
            {
                if (!_redisWarningLogged)
                {
                    _logger.LogWarning("Redis operation timed out after 2 seconds, falling back to database and appsettings");
                    _redisWarningLogged = true;
                }
                return null;
            }
            catch (Exception ex)
            {
                if (!_redisWarningLogged)
                {
                    _logger.LogWarning(ex, "Redis cache operation failed, falling back to database and appsettings. Error: {Error}", ex.Message);
                    _redisWarningLogged = true;
                }
                return null;
            }
        }

        /// <summary>
        /// Safely attempts to set value in Redis cache with proper exception handling
        /// </summary>
        private async Task TrySetCacheAsync(string cacheKey, string value)
        {
            try
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES)
                };
                
                // Set a shorter timeout for cache operations to fail fast
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await _cache.SetStringAsync(cacheKey, value, cacheOptions, cts.Token);
                _logger.LogDebug("Cached configuration '{Key}'", cacheKey);
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogDebug("Redis connection unavailable, skipping cache set: {Error}", ex.Message);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Redis cache set operation timed out, continuing without caching");
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Redis cache set operation failed, continuing without caching: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Gets a configuration value by key with caching support
        /// </summary>
        public async Task<string?> GetConfigValueAsync(string key, string? defaultValue = null)
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}{key}";
            string? value = null;

            // Try to get from cache first (with isolated exception handling)
            var cachedValue = await TryGetFromCacheAsync(cacheKey);
            if (cachedValue != null)
            {
                _logger.LogDebug("Retrieved configuration '{Key}' from cache", key);
                return cachedValue;
            }

            try
            {
                // Get from database
                using var context = await _contextFactory.CreateDbContextAsync();
                var config = await context.ApplicationConfigs
                    .Where(c => c.Key == key && c.IsActive)
                    .FirstOrDefaultAsync();

                value = config?.Value;
                
                if (value != null)
                {
                    _logger.LogDebug("Retrieved configuration '{Key}' from database", key);
                    
                    // Try to cache the result (non-blocking if Redis fails)
                    _ = Task.Run(async () => await TrySetCacheAsync(cacheKey, value));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving configuration '{Key}' from database", key);
            }
            
            // Fallback to appsettings.json if not found in database
            if (value == null)
            {
                // First try the key as-is
                value = _fallbackConfiguration.GetValue<string>(key);
                
                // If not found and key contains underscore, try converting to colon notation
                if (value == null && key.Contains('_'))
                {
                    var colonKey = key.Replace('_', ':');
                    value = _fallbackConfiguration.GetValue<string>(colonKey);
                    _logger.LogDebug("Configuration '{Key}' not found, tried '{ColonKey}' format", key, colonKey);
                }
                
                // Final fallback to default value
                value ??= defaultValue;
                
                _logger.LogDebug("Configuration '{Key}' not found in database, using fallback value", key);
                
                // Cache the fallback value too (if we have one and it's not the default)
                if (value != null && value != defaultValue)
                {
                    _ = Task.Run(async () => await TrySetCacheAsync(cacheKey, value));
                }
            }

            return value;
        }

        /// <summary>
        /// Gets a configuration value parsed to specific type
        /// </summary>
        public async Task<T> GetConfigValueAsync<T>(string key, T defaultValue = default!)
        {
            try
            {
                var stringValue = await GetConfigValueAsync(key);
                if (string.IsNullOrEmpty(stringValue))
                    return defaultValue;

                // Handle common type conversions
                var targetType = typeof(T);
                var nullableType = Nullable.GetUnderlyingType(targetType);
                if (nullableType != null)
                    targetType = nullableType;

                if (targetType == typeof(string))
                    return (T)(object)stringValue;
                
                if (targetType == typeof(int))
                    return int.TryParse(stringValue, out var intValue) ? (T)(object)intValue : defaultValue;
                
                if (targetType == typeof(decimal))
                    return decimal.TryParse(stringValue, out var decimalValue) ? (T)(object)decimalValue : defaultValue;
                
                if (targetType == typeof(bool))
                    return bool.TryParse(stringValue, out var boolValue) ? (T)(object)boolValue : defaultValue;
                
                if (targetType == typeof(DateTime))
                    return DateTime.TryParse(stringValue, out var dateValue) ? (T)(object)dateValue : defaultValue;

                // Use TypeConverter for other types
                var converter = TypeDescriptor.GetConverter(targetType);
                if (converter.CanConvertFrom(typeof(string)))
                {
                    return (T)converter.ConvertFromString(stringValue)!;
                }

                // Try JSON deserialization for complex types
                try
                {
                    return JsonSerializer.Deserialize<T>(stringValue) ?? defaultValue;
                }
                catch
                {
                    _logger.LogWarning("Could not parse configuration '{Key}' value '{Value}' to type '{Type}'", 
                        key, stringValue, typeof(T).Name);
                    return defaultValue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing configuration '{Key}' to type '{Type}'", key, typeof(T).Name);
                return defaultValue;
            }
        }

        /// <summary>
        /// Gets all configuration values by category
        /// </summary>
        public async Task<Dictionary<string, string>> GetConfigurationsByCategoryAsync(string category)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var configs = await context.ApplicationConfigs
                    .Where(c => c.Category == category && c.IsActive)
                    .ToDictionaryAsync(c => c.Key, c => c.Value);

                _logger.LogDebug("Retrieved {Count} configurations for category '{Category}'", configs.Count, category);
                return configs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving configurations for category '{Category}'", category);
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Clears the cache for a specific key
        /// </summary>
        public async Task ClearCacheAsync(string key)
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}{key}";
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await _cache.RemoveAsync(cacheKey, cts.Token);
                _logger.LogDebug("Cleared cache for configuration '{Key}'", key);
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogDebug("Redis connection unavailable, cannot clear cache for '{Key}': {Error}", key, ex.Message);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Redis cache clear operation timed out for '{Key}'", key);
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Error clearing cache for configuration '{Key}': {Error}", key, ex.Message);
            }
        }
    }

    /// <summary>
    /// Lightweight DbContext for configuration data access
    /// Used by integration proxy services for runtime configuration management
    /// </summary>
    public class NobillConfigDbContext : DbContext
    {
        /// <summary>
        /// NobillConfigDbContext
        /// </summary>
        /// <param name="options"></param>
        public NobillConfigDbContext(DbContextOptions<NobillConfigDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Application configurations stored in database
        /// </summary>
        public DbSet<ApplicationConfig> ApplicationConfigs { get; set; }

        /// <summary>
        /// OnModelCreating
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure ApplicationConfig entity
            modelBuilder.Entity<ApplicationConfig>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Key).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Value).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.CreatedBy).HasMaxLength(100);
                entity.Property(e => e.ModifiedBy).HasMaxLength(100);
                
                // Create unique index on Key
                entity.HasIndex(e => e.Key).IsUnique().HasDatabaseName("IX_ApplicationConfigs_Key");
                
                // Create index on Category
                entity.HasIndex(e => e.Category).HasDatabaseName("IX_ApplicationConfigs_Category");
                
                // Create index on IsActive
                entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_ApplicationConfigs_IsActive");
            });
        }
    }
} 
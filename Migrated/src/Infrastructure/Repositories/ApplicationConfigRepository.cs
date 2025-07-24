using System.ComponentModel;
using System.Text.Json;
using CreditTransfer.Core.Application.Interfaces;
using CreditTransfer.Core.Domain.Entities;
using CreditTransfer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CreditTransfer.Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for application configuration management
    /// Uses Entity Framework for database operations and Redis for caching
    /// </summary>
    public class ApplicationConfigRepository : IApplicationConfigRepository
    {
        private readonly CreditTransferDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<ApplicationConfigRepository> _logger;
        private const string CACHE_KEY_PREFIX = "AppConfig:";
        private const string CACHE_KEY_ALL = "AppConfig:All";
        private const int CACHE_EXPIRY_MINUTES = 43200; // Application configs change infrequently

        public ApplicationConfigRepository(
            CreditTransferDbContext context,
            IDistributedCache cache,
            ILogger<ApplicationConfigRepository> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Gets a configuration value by key with caching support
        /// </summary>
        public async Task<string?> GetConfigValueAsync(string key, string? defaultValue = null)
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}{key}";
                
                // Try to get from cache first
                string? cachedValue = null;
                try
                {
                    cachedValue = await _cache.GetStringAsync(cacheKey);
                    if (cachedValue != null)
                    {
                        _logger.LogDebug("Retrieved configuration '{Key}' from cache", key);
                        return cachedValue;
                    }
                }
                catch (Exception cacheEx)
                {
                    // Add Redis failure info to OpenTelemetry trace
                    System.Diagnostics.Activity.Current?.SetTag("redis.connection_failed", true);
                    System.Diagnostics.Activity.Current?.SetTag("redis.error_type", cacheEx.GetType().Name);
                    System.Diagnostics.Activity.Current?.SetTag($"config.{key}.redis_fallback", true);
                    
                    _logger.LogWarning(cacheEx, "⚠️ REDIS CONNECTION FAILED for key '{Key}' - falling back to database", key);
                }

                // Get from database
                var config = await _context.ApplicationConfigs
                    .Where(c => c.Key == key && c.IsActive)
                    .FirstOrDefaultAsync();

                var value = config?.Value ?? defaultValue;
                
                // Cache the result
                if (value != null)
                {
                    try
                    {
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES)
                        };
                        await _cache.SetStringAsync(cacheKey, value, cacheOptions);
                        _logger.LogDebug("Cached configuration '{Key}' with value", key);
                    }
                    catch (Exception cacheEx)
                    {
                        // Add Redis failure info to current activity
                        System.Diagnostics.Activity.Current?.SetTag("redis.caching_failed", true);
                        System.Diagnostics.Activity.Current?.SetTag("redis.cache_error_type", cacheEx.GetType().Name);
                        
                        _logger.LogWarning(cacheEx, "⚠️ REDIS CACHING FAILED for key '{Key}' - configuration retrieved from database successfully", key);
                    }
                }

                // DIAGNOSTIC: Add to OpenTelemetry for Jaeger visibility  
                System.Diagnostics.Activity.Current?.SetTag($"config.{key}.source", config != null ? "Database" : "Default");
                System.Diagnostics.Activity.Current?.SetTag($"config.{key}.has_value", value != null);
                System.Diagnostics.Activity.Current?.SetTag($"config.{key}.cache_used", cachedValue != null);
                
                _logger.LogInformation("🔍 CONFIG RETRIEVAL - Key: '{Key}', Source: {Source}, HasValue: {HasValue}", 
                    key, config != null ? "Database" : "Default", value != null);

                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving configuration '{Key}'", key);
                return defaultValue;
            }
        }
        /// <summary>
        /// Gets a configuration value by key without caching support (direct database access)
        /// Used for health checks and diagnostics to bypass cache issues
        /// </summary>
        public async Task<string?> GetConfigValueWithoutCachingAsync(string key, string? defaultValue = null, bool throwOnError = true)
        {
            try
            {
                // DIAGNOSTIC: Add OpenTelemetry activity tags for health check diagnostics
                System.Diagnostics.Activity.Current?.SetTag($"config.{key}.method", "GetConfigValueWithoutCaching");
                System.Diagnostics.Activity.Current?.SetTag($"config.{key}.cache_bypassed", true);
                System.Diagnostics.Activity.Current?.SetTag($"config.{key}.database_direct_access", true);
                System.Diagnostics.Activity.Current?.SetTag($"config.{key}.throw_on_error", throwOnError);

                var config = await _context.ApplicationConfigs
                    .Where(c => c.Key == key && c.IsActive)
                    .FirstOrDefaultAsync();

                var value = config?.Value ?? defaultValue;

                // DIAGNOSTIC: Add database result to OpenTelemetry for Jaeger visibility  
                System.Diagnostics.Activity.Current?.SetTag($"config.{key}.source", config != null ? "Database" : "Default");
                System.Diagnostics.Activity.Current?.SetTag($"config.{key}.has_value", value != null);
                System.Diagnostics.Activity.Current?.SetTag($"config.{key}.database_record_found", config != null);
                System.Diagnostics.Activity.Current?.SetTag($"config.{key}.is_active", config?.IsActive ?? false);
                
                _logger.LogInformation("🔍 CONFIG RETRIEVAL (NoCache) - Key: '{Key}', Source: {Source}, HasValue: {HasValue}, DatabaseRecord: {HasRecord}, ThrowOnError: {ThrowOnError}",
                    key, config != null ? "Database" : "Default", value != null, config != null, throwOnError);

                return value;
            }
            catch (Exception ex)
            {
                // DIAGNOSTIC: Add error information to OpenTelemetry trace
                System.Diagnostics.Activity.Current?.SetTag($"config.{key}.error", true);
                System.Diagnostics.Activity.Current?.SetTag($"config.{key}.error_type", ex.GetType().Name);
                System.Diagnostics.Activity.Current?.SetTag($"config.{key}.error_message", ex.Message);
                
                _logger.LogError(ex, "Error retrieving configuration '{Key}' without caching - ThrowOnError: {ThrowOnError}", key, throwOnError);
                
                if (throwOnError)
                {
                    throw; // Re-throw the exception to caller
                }
                
                return defaultValue;
            }
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

                // Handle List types
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var elementType = targetType.GetGenericArguments()[0];
                    var listResult = ParseListValue(stringValue, elementType, key);
                    if (listResult != null)
                        return (T)listResult;
                }

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
        /// Parses a string value into a List of the specified element type
        /// Supports JSON, comma-separated, and semicolon-separated formats
        /// </summary>
        private object? ParseListValue(string stringValue, Type elementType, string key)
        {
            try
            {
                // Try JSON deserialization first
                if (stringValue.TrimStart().StartsWith('['))
                {
                    var listType = typeof(List<>).MakeGenericType(elementType);
                    var jsonResult = JsonSerializer.Deserialize(stringValue, listType);
                    if (jsonResult != null)
                    {
                        _logger.LogDebug("Parsed configuration '{Key}' as JSON list", key);
                        return jsonResult;
                    }
                }

                // Try delimiter-separated values (semicolon first, then comma)
                var delimiters = new[] { ';', ',' };
                foreach (var delimiter in delimiters)
                {
                    if (stringValue.Contains(delimiter))
                    {
                        var parts = stringValue.Split(delimiter, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim())
                            .Where(p => !string.IsNullOrEmpty(p))
                            .ToArray();

                        if (parts.Length > 0)
                        {
                            var listResult = CreateTypedList(parts, elementType, key);
                            if (listResult != null)
                            {
                                _logger.LogDebug("Parsed configuration '{Key}' as {Delimiter}-separated list with {Count} items", 
                                    key, delimiter, parts.Length);
                                return listResult;
                            }
                        }
                    }
                }

                // Single value - treat as single-item list
                var singleItemList = CreateTypedList(new[] { stringValue }, elementType, key);
                if (singleItemList != null)
                {
                    _logger.LogDebug("Parsed configuration '{Key}' as single-item list", key);
                    return singleItemList;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse configuration '{Key}' as list of {ElementType}", key, elementType.Name);
            }

            return null;
        }

        /// <summary>
        /// Creates a typed list from string array
        /// </summary>
        private object? CreateTypedList(string[] values, Type elementType, string key)
        {
            try
            {
                var listType = typeof(List<>).MakeGenericType(elementType);
                var list = Activator.CreateInstance(listType);
                var addMethod = listType.GetMethod("Add");

                if (addMethod == null)
                    return null;

                foreach (var value in values)
                {
                    var convertedValue = ConvertToElementType(value, elementType);
                    if (convertedValue != null)
                    {
                        addMethod.Invoke(list, new[] { convertedValue });
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create typed list for configuration '{Key}' with element type {ElementType}", key, elementType.Name);
                return null;
            }
        }

        /// <summary>
        /// Converts a string value to the specified element type
        /// </summary>
        private object? ConvertToElementType(string value, Type elementType)
        {
            try
            {
                if (elementType == typeof(string))
                    return value;

                if (elementType == typeof(int))
                    return int.TryParse(value, out var intValue) ? intValue : null;

                if (elementType == typeof(decimal))
                    return decimal.TryParse(value, out var decimalValue) ? decimalValue : null;

                if (elementType == typeof(double))
                    return double.TryParse(value, out var doubleValue) ? doubleValue : null;

                if (elementType == typeof(float))
                    return float.TryParse(value, out var floatValue) ? floatValue : null;

                if (elementType == typeof(long))
                    return long.TryParse(value, out var longValue) ? longValue : null;

                if (elementType == typeof(bool))
                    return bool.TryParse(value, out var boolValue) ? boolValue : null;

                if (elementType == typeof(DateTime))
                    return DateTime.TryParse(value, out var dateValue) ? dateValue : null;

                if (elementType == typeof(Guid))
                    return Guid.TryParse(value, out var guidValue) ? guidValue : null;

                // Handle nullable types
                var nullableType = Nullable.GetUnderlyingType(elementType);
                if (nullableType != null)
                {
                    return ConvertToElementType(value, nullableType);
                }

                // Use TypeConverter for other types
                var converter = TypeDescriptor.GetConverter(elementType);
                if (converter.CanConvertFrom(typeof(string)))
                {
                    return converter.ConvertFromString(value);
                }

                // Try JSON deserialization for complex types
                try
                {
                    return JsonSerializer.Deserialize(value, elementType);
                }
                catch
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets all configuration values by category
        /// </summary>
        public async Task<Dictionary<string, string>> GetConfigurationsByCategoryAsync(string category)
        {
            try
            {
                var configs = await _context.ApplicationConfigs
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
        /// Gets all active configurations
        /// </summary>
        public async Task<List<ApplicationConfig>> GetAllActiveConfigurationsAsync()
        {
            try
            {
                var configs = await _context.ApplicationConfigs
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Category)
                    .ThenBy(c => c.Key)
                    .ToListAsync();

                _logger.LogDebug("Retrieved {Count} active configurations", configs.Count);
                return configs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all active configurations");
                return new List<ApplicationConfig>();
            }
        }

        /// <summary>
        /// Gets a specific configuration by key
        /// </summary>
        public async Task<ApplicationConfig?> GetConfigurationByKeyAsync(string key)
        {
            try
            {
                return await _context.ApplicationConfigs
                    .FirstOrDefaultAsync(c => c.Key == key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving configuration by key '{Key}'", key);
                return null;
            }
        }

        /// <summary>
        /// Adds or updates a configuration
        /// </summary>
        public async Task<ApplicationConfig> UpsertConfigurationAsync(ApplicationConfig config)
        {
            try
            {
                var existing = await _context.ApplicationConfigs
                    .FirstOrDefaultAsync(c => c.Key == config.Key);

                if (existing != null)
                {
                    // Update existing
                    existing.Value = config.Value;
                    existing.Description = config.Description;
                    existing.Note = config.Note;
                    existing.Category = config.Category;
                    existing.IsActive = config.IsActive;
                    existing.LastModified = DateTime.UtcNow;
                    existing.ModifiedBy = config.ModifiedBy;
                    
                    _context.ApplicationConfigs.Update(existing);
                    config = existing;
                }
                else
                {
                    // Add new
                    config.CreatedDate = DateTime.UtcNow;
                    _context.ApplicationConfigs.Add(config);
                }

                await _context.SaveChangesAsync();
                
                // Clear cache for this key
                await ClearCacheAsync(config.Key);
                
                _logger.LogInformation("Upserted configuration '{Key}'", config.Key);
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting configuration '{Key}'", config.Key);
                throw;
            }
        }

        /// <summary>
        /// Deletes a configuration by key
        /// </summary>
        public async Task<bool> DeleteConfigurationAsync(string key)
        {
            try
            {
                var config = await _context.ApplicationConfigs
                    .FirstOrDefaultAsync(c => c.Key == key);

                if (config == null)
                    return false;

                _context.ApplicationConfigs.Remove(config);
                await _context.SaveChangesAsync();
                
                // Clear cache for this key
                await ClearCacheAsync(key);
                
                _logger.LogInformation("Deleted configuration '{Key}'", key);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting configuration '{Key}'", key);
                throw;
            }
        }

        /// <summary>
        /// Refreshes the cache for all configurations
        /// </summary>
        public async Task RefreshCacheAsync()
        {
            try
            {
                // Remove all cached configurations
                var allConfigs = await _context.ApplicationConfigs
                    .Where(c => c.IsActive)
                    .ToListAsync();

                foreach (var config in allConfigs)
                {
                    await ClearCacheAsync(config.Key);
                }

                _logger.LogInformation("Refreshed cache for all configurations");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing configuration cache");
                throw;
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
                await _cache.RemoveAsync(cacheKey);
                _logger.LogDebug("Cleared cache for configuration '{Key}'", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache for configuration '{Key}'", key);
            }
        }

        /// <summary>
        /// Bulk insert or update configurations
        /// </summary>
        public async Task BulkUpsertConfigurationsAsync(List<ApplicationConfig> configurations)
        {
            try
            {
                foreach (var config in configurations)
                {
                    var existing = await _context.ApplicationConfigs
                        .FirstOrDefaultAsync(c => c.Key == config.Key);

                    if (existing != null)
                    {
                        // Update existing
                        existing.Value = config.Value;
                        existing.Description = config.Description;
                        existing.Note = config.Note;
                        existing.Category = config.Category;
                        existing.IsActive = config.IsActive;
                        existing.LastModified = DateTime.UtcNow;
                        existing.ModifiedBy = config.ModifiedBy;
                        
                        _context.ApplicationConfigs.Update(existing);
                    }
                    else
                    {
                        // Add new
                        config.CreatedDate = DateTime.UtcNow;
                        _context.ApplicationConfigs.Add(config);
                    }
                }

                await _context.SaveChangesAsync();
                
                // Clear cache for all updated keys
                foreach (var config in configurations)
                {
                    await ClearCacheAsync(config.Key);
                }
                
                _logger.LogInformation("Bulk upserted {Count} configurations", configurations.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk upserting configurations");
                throw;
            }
        }

        /// <summary>
        /// Performs a comprehensive database health check for monitoring purposes
        /// Tests database connectivity, table existence, and data integrity
        /// Used by health check endpoints to verify database system health
        /// </summary>
        public async Task<(bool isHealthy, string statusMessage, Dictionary<string, object> details)> PerformHealthCheckAsync()
        {
            var details = new Dictionary<string, object>();
            var startTime = DateTime.UtcNow;

            try
            {
                // DIAGNOSTIC: Add OpenTelemetry activity tags for health check diagnostics
                System.Diagnostics.Activity.Current?.SetTag("database.health_check.method", "PerformHealthCheck");
                System.Diagnostics.Activity.Current?.SetTag("database.health_check.start_time", startTime);

                // Test 1: Basic database connectivity (SELECT 1)
                var connectionTestResult = await TestDatabaseConnectionAsync();
                details["connection_test"] = connectionTestResult.success ? "SUCCESS" : "FAILED";
                details["connection_response_time_ms"] = connectionTestResult.responseTimeMs;

                if (!connectionTestResult.success)
                {
                    details["error"] = connectionTestResult.error;
                    return (false, "Database connection failed", details);
                }

                // Test 2: ApplicationConfigs table accessibility
                var tableTestResult = await TestTableAccessibilityAsync();
                details["table_accessibility"] = tableTestResult.success ? "SUCCESS" : "FAILED";
                details["total_configs"] = tableTestResult.totalConfigs;
                details["active_configs"] = tableTestResult.activeConfigs;

                if (!tableTestResult.success)
                {
                    details["error"] = tableTestResult.error;
                    return (false, "ApplicationConfigs table inaccessible", details);
                }

                // Test 3: Sample configuration retrieval (use a common key or any existing key)
                var configTestResult = await TestConfigurationRetrievalAsync();
                details["config_retrieval_test"] = configTestResult.success ? "SUCCESS" : "FAILED";
                details["sample_config_found"] = configTestResult.configFound;
                details["sample_config_key"] = configTestResult.testKey;

                var totalResponseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                details["total_response_time_ms"] = totalResponseTime;
                details["database_server"] = "10.1.133.31 (External SQL Server)";
                details["test_timestamp"] = DateTime.UtcNow;

                // DIAGNOSTIC: Add comprehensive health check results to OpenTelemetry
                System.Diagnostics.Activity.Current?.SetTag("database.health_check.overall_result", "SUCCESS");
                System.Diagnostics.Activity.Current?.SetTag("database.health_check.total_response_time_ms", totalResponseTime);
                System.Diagnostics.Activity.Current?.SetTag("database.health_check.total_configs", tableTestResult.totalConfigs);

                var statusMessage = configTestResult.configFound 
                    ? $"Database fully operational - {tableTestResult.activeConfigs} active configurations available"
                    : $"Database operational but configuration data may be limited - {tableTestResult.activeConfigs} active configurations";

                _logger.LogInformation("✅ DATABASE HEALTH CHECK SUCCESS - Total configs: {TotalConfigs}, Active: {ActiveConfigs}, Response time: {ResponseTime}ms",
                    tableTestResult.totalConfigs, tableTestResult.activeConfigs, totalResponseTime);

                return (true, statusMessage, details);
            }
            catch (Exception ex)
            {
                var totalResponseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                details["total_response_time_ms"] = totalResponseTime;
                details["error"] = ex.Message;
                details["error_type"] = ex.GetType().Name;
                details["database_server"] = "10.1.133.31 (External SQL Server)";

                // DIAGNOSTIC: Add error information to OpenTelemetry trace
                System.Diagnostics.Activity.Current?.SetTag("database.health_check.overall_result", "ERROR");
                System.Diagnostics.Activity.Current?.SetTag("database.health_check.error_type", ex.GetType().Name);
                System.Diagnostics.Activity.Current?.SetTag("database.health_check.error_message", ex.Message);

                _logger.LogError(ex, "❌ DATABASE HEALTH CHECK FAILED - Response time: {ResponseTime}ms", totalResponseTime);

                return (false, $"Database health check failed: {ex.Message}", details);
            }
        }

        /// <summary>
        /// Tests basic database connectivity using a simple SELECT query
        /// </summary>
        private async Task<(bool success, double responseTimeMs, string? error)> TestDatabaseConnectionAsync()
        {
            var startTime = DateTime.UtcNow;
            try
            {
                // Execute a simple query to test database connectivity
                var result = await _context.Database.ExecuteSqlRawAsync("SELECT 1");
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                _logger.LogDebug("Database connection test successful in {ResponseTime}ms", responseTime);
                return (true, responseTime, null);
            }
            catch (Exception ex)
            {
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning(ex, "Database connection test failed in {ResponseTime}ms", responseTime);
                return (false, responseTime, ex.Message);
            }
        }

        /// <summary>
        /// Tests ApplicationConfigs table accessibility and counts records
        /// </summary>
        private async Task<(bool success, int totalConfigs, int activeConfigs, string? error)> TestTableAccessibilityAsync()
        {
            try
            {
                var totalConfigs = await _context.ApplicationConfigs.CountAsync();
                var activeConfigs = await _context.ApplicationConfigs.CountAsync(c => c.IsActive);
                
                _logger.LogDebug("Table accessibility test successful - Total: {Total}, Active: {Active}", totalConfigs, activeConfigs);
                return (true, totalConfigs, activeConfigs, null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Table accessibility test failed");
                return (false, 0, 0, ex.Message);
            }
        }

        /// <summary>
        /// Tests configuration retrieval by attempting to get a sample configuration
        /// </summary>
        private async Task<(bool success, bool configFound, string testKey, string? error)> TestConfigurationRetrievalAsync()
        {
            try
            {
                // Try to get any existing configuration for testing
                var firstConfig = await _context.ApplicationConfigs
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Key)
                    .FirstOrDefaultAsync();

                if (firstConfig != null)
                {
                    // Test retrieval of an existing configuration
                    var testValue = await GetConfigValueWithoutCachingAsync(firstConfig.Key, null, false);
                    var configFound = !string.IsNullOrEmpty(testValue);
                    
                    _logger.LogDebug("Configuration retrieval test - Key: '{Key}', Found: {Found}", firstConfig.Key, configFound);
                    return (true, configFound, firstConfig.Key, null);
                }
                else
                {
                    // No configurations exist - this is unusual but not necessarily an error
                    _logger.LogWarning("No configurations found in database for retrieval test");
                    return (true, false, "No configurations available", null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Configuration retrieval test failed");
                return (false, false, "Test failed", ex.Message);
            }
        }
    }
} 
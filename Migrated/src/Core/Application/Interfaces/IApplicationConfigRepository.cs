using CreditTransfer.Core.Domain.Entities;

namespace CreditTransfer.Core.Application.Interfaces
{
    /// <summary>
    /// Repository interface for application configuration management
    /// Supports both database and Redis caching for optimal performance
    /// </summary>
    public interface IApplicationConfigRepository
    {
        /// <summary>
        /// Gets a configuration value by key with caching support
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="defaultValue">Default value if key not found</param>
        /// <returns>Configuration value or default</returns>
        Task<string?> GetConfigValueAsync(string key, string? defaultValue = null);
        /// <summary>
        /// Gets a configuration value by key without caching support (direct database access)
        /// Used for health checks and diagnostics to bypass cache issues
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="defaultValue">Default value if key not found</param>
        /// <param name="throwOnError">Whether to throw exception on error (default: true)</param>
        /// <returns>Configuration value or default</returns>
        Task<string?> GetConfigValueWithoutCachingAsync(string key, string? defaultValue = null, bool throwOnError = true);

        /// <summary>
        /// Gets a configuration value parsed to specific type
        /// </summary>
        /// <typeparam name="T">Type to parse to</typeparam>
        /// <param name="key">Configuration key</param>
        /// <param name="defaultValue">Default value if key not found or parsing fails</param>
        /// <returns>Parsed configuration value or default</returns>
        Task<T> GetConfigValueAsync<T>(string key, T defaultValue = default!);

        /// <summary>
        /// Gets all configuration values by category
        /// </summary>
        /// <param name="category">Configuration category</param>
        /// <returns>Dictionary of key-value pairs for the category</returns>
        Task<Dictionary<string, string>> GetConfigurationsByCategoryAsync(string category);

        /// <summary>
        /// Gets all active configurations
        /// </summary>
        /// <returns>List of all active application configurations</returns>
        Task<List<ApplicationConfig>> GetAllActiveConfigurationsAsync();

        /// <summary>
        /// Gets a specific configuration by key
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <returns>ApplicationConfig entity or null if not found</returns>
        Task<ApplicationConfig?> GetConfigurationByKeyAsync(string key);

        /// <summary>
        /// Adds or updates a configuration
        /// </summary>
        /// <param name="config">Configuration to add or update</param>
        /// <returns>Updated configuration with ID</returns>
        Task<ApplicationConfig> UpsertConfigurationAsync(ApplicationConfig config);

        /// <summary>
        /// Deletes a configuration by key
        /// </summary>
        /// <param name="key">Configuration key to delete</param>
        /// <returns>True if deleted, false if not found</returns>
        Task<bool> DeleteConfigurationAsync(string key);

        /// <summary>
        /// Refreshes the cache for all configurations
        /// </summary>
        Task RefreshCacheAsync();

        /// <summary>
        /// Clears the cache for a specific key
        /// </summary>
        /// <param name="key">Configuration key to clear from cache</param>
        Task ClearCacheAsync(string key);

        /// <summary>
        /// Bulk insert or update configurations
        /// </summary>
        /// <param name="configurations">List of configurations to upsert</param>
        Task BulkUpsertConfigurationsAsync(List<ApplicationConfig> configurations);

        /// <summary>
        /// Performs a comprehensive database health check for monitoring purposes
        /// Tests database connectivity, table existence, and data integrity
        /// Used by health check endpoints to verify database system health
        /// </summary>
        /// <returns>Health status, message, and detailed diagnostics</returns>
        Task<(bool isHealthy, string statusMessage, Dictionary<string, object> details)> PerformHealthCheckAsync();
    }
} 
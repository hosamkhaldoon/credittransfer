namespace IntegrationProxies.Nobill.Interfaces
{
    /// <summary>
    /// Lightweight configuration repository interface for database-driven configuration
    /// Used by integration proxy services for runtime configuration management
    /// </summary>
    public interface IConfigurationRepository
    {
        /// <summary>
        /// Gets a configuration value by key with fallback support
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="defaultValue">Default value if key not found</param>
        /// <returns>Configuration value or default</returns>
        Task<string?> GetConfigValueAsync(string key, string? defaultValue = null);

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
        /// Clears the cache for a specific key
        /// </summary>
        /// <param name="key">Configuration key to clear from cache</param>
        Task ClearCacheAsync(string key);
    }
} 
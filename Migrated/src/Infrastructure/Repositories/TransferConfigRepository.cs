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
    /// Repository implementation for transfer configuration management with Redis caching
    /// Handles subscription type configurations and transfer limits
    /// </summary>
    public class TransferConfigRepository : ITransferConfigRepository
    {
        private readonly CreditTransferDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<TransferConfigRepository> _logger;
        private const string CACHE_KEY_PREFIX = "TransferConfig:";
        private const string CACHE_KEY_NOBILL_PREFIX = "TransferConfig:Nobill:";
        private const string CACHE_KEY_ALL = "TransferConfig:All";
        private const int CACHE_EXPIRY_MINUTES = 43200; // Transfer configs change infrequently

        public TransferConfigRepository(
            CreditTransferDbContext context,
            IDistributedCache cache,
            ILogger<TransferConfigRepository> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Gets transfer configuration by subscription type with caching
        /// </summary>
        public async Task<TransferConfig?> GetBySubscriptionTypeAsync(string subscriptionType)
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}{subscriptionType}";

                // Try to get from cache first
                var cachedValue = await _cache.GetStringAsync(cacheKey);
                if (cachedValue != null)
                {
                    var cachedConfig = JsonSerializer.Deserialize<TransferConfig>(cachedValue);
                    if (cachedConfig != null)
                    {
                        _logger.LogDebug("Retrieved transfer config for subscription type '{SubscriptionType}' from cache", subscriptionType);
                        return cachedConfig;
                    }
                }

                // Get from database
                var config = await _context.TransferConfig
                    .Where(tc => tc.SubscriptionType == subscriptionType)
                    .FirstOrDefaultAsync();

                // Cache the result
                if (config != null)
                {
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES)
                    };
                    var serializedConfig = JsonSerializer.Serialize(config);
                    await _cache.SetStringAsync(cacheKey, serializedConfig, cacheOptions);
                    _logger.LogDebug("Cached transfer config for subscription type '{SubscriptionType}'", subscriptionType);
                }

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transfer config for subscription type '{SubscriptionType}'", subscriptionType);
                return null;
            }
        }

        /// <summary>
        /// Gets transfer configuration by Nobill subscription type with caching
        /// </summary>
        public async Task<TransferConfig?> GetByNobillSubscriptionTypeAsync(string nobillSubscriptionType)
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_NOBILL_PREFIX}{nobillSubscriptionType}";

                // Try to get from cache first
                var cachedValue = await _cache.GetStringAsync(cacheKey);
                if (cachedValue != null)
                {
                    var cachedConfig = JsonSerializer.Deserialize<TransferConfig>(cachedValue);
                    if (cachedConfig != null)
                    {
                        _logger.LogDebug("Retrieved transfer config for Nobill subscription type '{NobillSubscriptionType}' from cache", nobillSubscriptionType);
                        return cachedConfig;
                    }
                }

                // Get from database
                var config = await _context.TransferConfig
                    .Where(tc => tc.NobillSubscritpionType == nobillSubscriptionType)
                    .FirstOrDefaultAsync();

                // Cache the result
                if (config != null)
                {
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES)
                    };
                    var serializedConfig = JsonSerializer.Serialize(config);
                    await _cache.SetStringAsync(cacheKey, serializedConfig, cacheOptions);
                    _logger.LogDebug("Cached transfer config for Nobill subscription type '{NobillSubscriptionType}'", nobillSubscriptionType);
                }

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transfer config for Nobill subscription type '{NobillSubscriptionType}'", nobillSubscriptionType);
                return null;
            }
        }

        /// <summary>
        /// Gets minimum transfer amount for a subscription type with caching
        /// </summary>
        public async Task<decimal> GetMinTransferAmountAsync(string subscriptionType, decimal defaultValue = 0)
        {
            var config = await GetBySubscriptionTypeAsync(subscriptionType);
            return config?.MinTransferAmount ?? defaultValue;
        }

        /// <summary>
        /// Gets maximum transfer amount for a subscription type with caching
        /// </summary>
        public async Task<decimal?> GetMaxTransferAmountAsync(string subscriptionType, decimal? defaultValue = null)
        {
            var config = await GetBySubscriptionTypeAsync(subscriptionType);
            return config?.MaxTransferAmount ?? defaultValue;
        }

        /// <summary>
        /// Gets daily transfer count limit for a subscription type with caching
        /// </summary>
        public async Task<int?> GetDailyTransferCountLimitAsync(string subscriptionType, int? defaultValue = null)
        {
            var config = await GetBySubscriptionTypeAsync(subscriptionType);
            return config?.DailyTransferCountLimit ?? defaultValue;
        }

        /// <summary>
        /// Gets daily transfer amount cap limit for a subscription type with caching
        /// </summary>
        public async Task<decimal?> GetDailyTransferCapLimitAsync(string subscriptionType, decimal? defaultValue = null)
        {
            var config = await GetBySubscriptionTypeAsync(subscriptionType);
            return config?.DailyTransferCapLimit ?? defaultValue;
        }

        /// <summary>
        /// Gets minimum post-transfer balance for a subscription type with caching
        /// </summary>
        public async Task<decimal?> GetMinPostTransferBalanceAsync(string subscriptionType, decimal? defaultValue = null)
        {
            var config = await GetBySubscriptionTypeAsync(subscriptionType);
            return config?.MinPostTransferBalance ?? defaultValue;
        }

        /// <summary>
        /// Gets credit transfer customer service name for a subscription type with caching
        /// </summary>
        public async Task<string> GetCreditTransferCustomServiceAsync(string subscriptionType, string defaultValue = "CS.Credit_transfer")
        {
            var config = await GetBySubscriptionTypeAsync(subscriptionType);
            return config?.CreditTransferCustomerService ?? defaultValue;
        }

        /// <summary>
        /// Gets all active transfer configurations with caching
        /// </summary>
        public async Task<List<TransferConfig>> GetConfigurationsAsync()
        {
            try
            {
                // Try to get from cache first
                var cachedValue = await _cache.GetStringAsync(CACHE_KEY_ALL);
                if (cachedValue != null)
                {
                    var cachedConfigs = JsonSerializer.Deserialize<List<TransferConfig>>(cachedValue);
                    if (cachedConfigs != null)
                    {
                        _logger.LogDebug("Retrieved {Count} transfer configs from cache", cachedConfigs.Count);
                        return cachedConfigs;
                    }
                }

                // Get from database
                var configs = await _context.TransferConfig
                    .OrderBy(tc => tc.SubscriptionType)
                    .ToListAsync();

                // Cache the result
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES)
                };
                var serializedConfigs = JsonSerializer.Serialize(configs);
                await _cache.SetStringAsync(CACHE_KEY_ALL, serializedConfigs, cacheOptions);

                _logger.LogDebug("Retrieved and cached {Count} active transfer configs", configs.Count);
                return configs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all active transfer configs");
                return new List<TransferConfig>();
            }
        }

        /// <summary>
        /// Adds or updates a transfer configuration
        /// </summary>
        public async Task<TransferConfig> UpsertConfigurationAsync(TransferConfig config)
        {
            try
            {
                var existing = await _context.TransferConfig
                    .FirstOrDefaultAsync(tc => tc.ID == config.ID || tc.SubscriptionType == config.SubscriptionType);

                if (existing != null)
                {
                    // Update existing
                    existing.NobillSubscritpionType = config.NobillSubscritpionType;
                    existing.TransferCounterName = config.TransferCounterName;
                    existing.CreditTransferCustomerService = config.CreditTransferCustomerService;
                    existing.DailyTransferCountLimit = config.DailyTransferCountLimit;
                    existing.DailyTransferCapLimit = config.DailyTransferCapLimit;
                    existing.MinTransferAmount = config.MinTransferAmount;
                    existing.TransferFeesEventId = config.TransferFeesEventId;
                    existing.MinPostTransferBalance = config.MinPostTransferBalance;
                    existing.MaxTransferAmount = config.MaxTransferAmount;
                    existing.SubscriptionType = config.SubscriptionType;
                    existing.TransferToOtherOperator = config.TransferToOtherOperator;

                    _context.TransferConfig.Update(existing);
                    config = existing;
                }
                else
                {
                    // Add new
                    _context.TransferConfig.Add(config);
                }

                await _context.SaveChangesAsync();

                // Clear cache for this subscription type and all configs
                if (!string.IsNullOrEmpty(config.SubscriptionType))
                {
                    await ClearCacheAsync(config.SubscriptionType);
                }
                if (!string.IsNullOrEmpty(config.NobillSubscritpionType))
                {
                    await _cache.RemoveAsync($"{CACHE_KEY_NOBILL_PREFIX}{config.NobillSubscritpionType}");
                }
                await _cache.RemoveAsync(CACHE_KEY_ALL);

                _logger.LogInformation("Upserted transfer config for subscription type '{SubscriptionType}'", config.SubscriptionType);
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting transfer config for subscription type '{SubscriptionType}'", config.SubscriptionType);
                throw;
            }
        }

        /// <summary>
        /// Deletes a transfer configuration by ID
        /// </summary>
        public async Task<bool> DeleteConfigurationAsync(int id)
        {
            try
            {
                var config = await _context.TransferConfig
                    .FirstOrDefaultAsync(tc => tc.ID == id);

                if (config == null)
                    return false;

                _context.TransferConfig.Remove(config);
                await _context.SaveChangesAsync();

                // Clear cache for this subscription type and all configs
                if (!string.IsNullOrEmpty(config.SubscriptionType))
                {
                    await ClearCacheAsync(config.SubscriptionType);
                }
                if (!string.IsNullOrEmpty(config.NobillSubscritpionType))
                {
                    await _cache.RemoveAsync($"{CACHE_KEY_NOBILL_PREFIX}{config.NobillSubscritpionType}");
                }
                await _cache.RemoveAsync(CACHE_KEY_ALL);

                _logger.LogInformation("Deleted transfer config with ID {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting transfer config with ID {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Refreshes the cache for all transfer configurations
        /// </summary>
        public async Task RefreshCacheAsync()
        {
            try
            {
                // Remove all cached configs
                var allConfigs = await _context.TransferConfig
                    .ToListAsync();

                foreach (var config in allConfigs)
                {
                    if (!string.IsNullOrEmpty(config.SubscriptionType))
                    {
                        await ClearCacheAsync(config.SubscriptionType);
                    }
                    if (!string.IsNullOrEmpty(config.NobillSubscritpionType))
                    {
                        await _cache.RemoveAsync($"{CACHE_KEY_NOBILL_PREFIX}{config.NobillSubscritpionType}");
                    }
                }

                await _cache.RemoveAsync(CACHE_KEY_ALL);
                _logger.LogInformation("Refreshed cache for all transfer configs");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing transfer config cache");
                throw;
            }
        }

        /// <summary>
        /// Clears the cache for a specific subscription type
        /// </summary>
        public async Task ClearCacheAsync(string subscriptionType)
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}{subscriptionType}";
                await _cache.RemoveAsync(cacheKey);
                _logger.LogDebug("Cleared cache for subscription type '{SubscriptionType}'", subscriptionType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache for subscription type '{SubscriptionType}'", subscriptionType);
            }
        }

        /// <summary>
        /// Bulk insert or update transfer configurations
        /// </summary>
        public async Task BulkUpsertConfigurationsAsync(List<TransferConfig> configurations)
        {
            try
            {
                foreach (var config in configurations)
                {
                    var existing = await _context.TransferConfig
                        .FirstOrDefaultAsync(tc => tc.ID == config.ID || tc.SubscriptionType == config.SubscriptionType);

                    if (existing != null)
                    {
                        // Update existing
                        existing.NobillSubscritpionType = config.NobillSubscritpionType;
                        existing.TransferCounterName = config.TransferCounterName;
                        existing.CreditTransferCustomerService = config.CreditTransferCustomerService;
                        existing.DailyTransferCountLimit = config.DailyTransferCountLimit;
                        existing.DailyTransferCapLimit = config.DailyTransferCapLimit;
                        existing.MinTransferAmount = config.MinTransferAmount;
                        existing.TransferFeesEventId = config.TransferFeesEventId;
                        existing.MinPostTransferBalance = config.MinPostTransferBalance;
                        existing.MaxTransferAmount = config.MaxTransferAmount;
                        existing.SubscriptionType = config.SubscriptionType;
                        existing.TransferToOtherOperator = config.TransferToOtherOperator;

                        _context.TransferConfig.Update(existing);
                    }
                    else
                    {
                        // Add new
                        _context.TransferConfig.Add(config);
                    }
                }

                await _context.SaveChangesAsync();

                // Clear cache for all updated subscription types and all configs
                foreach (var config in configurations)
                {
                    if (!string.IsNullOrEmpty(config.SubscriptionType))
                    {
                        await ClearCacheAsync(config.SubscriptionType);
                    }
                    if (!string.IsNullOrEmpty(config.NobillSubscritpionType))
                    {
                        await _cache.RemoveAsync($"{CACHE_KEY_NOBILL_PREFIX}{config.NobillSubscritpionType}");
                    }
                }
                await _cache.RemoveAsync(CACHE_KEY_ALL);

                _logger.LogInformation("Bulk upserted {Count} transfer configs", configurations.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk upserting transfer configs");
                throw;
            }
        }
    }
}
using CreditTransfer.Core.Domain.Entities;

namespace CreditTransfer.Core.Application.Interfaces
{
    /// <summary>
    /// Repository interface for transfer configuration management with Redis caching
    /// Supports subscription type configurations and transfer limits
    /// </summary>
    public interface ITransferConfigRepository
    {
        /// <summary>
        /// Gets transfer configuration by subscription type with caching
        /// </summary>
        /// <param name="subscriptionType">Subscription type</param>
        /// <returns>Transfer configuration or null if not found</returns>
        Task<TransferConfig?> GetBySubscriptionTypeAsync(string subscriptionType);

        /// <summary>
        /// Gets transfer configuration by Nobill subscription type with caching
        /// </summary>
        /// <param name="nobillSubscriptionType">Nobill subscription type</param>
        /// <returns>Transfer configuration or null if not found</returns>
        Task<TransferConfig?> GetByNobillSubscriptionTypeAsync(string nobillSubscriptionType);

        /// <summary>
        /// Gets minimum transfer amount for a subscription type with caching
        /// </summary>
        /// <param name="subscriptionType">Subscription type</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Minimum transfer amount</returns>
        Task<decimal> GetMinTransferAmountAsync(string subscriptionType, decimal defaultValue = 0);

        /// <summary>
        /// Gets maximum transfer amount for a subscription type with caching
        /// </summary>
        /// <param name="subscriptionType">Subscription type</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Maximum transfer amount or null if unlimited</returns>
        Task<decimal?> GetMaxTransferAmountAsync(string subscriptionType, decimal? defaultValue = null);

        /// <summary>
        /// Gets daily transfer count limit for a subscription type with caching
        /// </summary>
        /// <param name="subscriptionType">Subscription type</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Daily transfer count limit or null if unlimited</returns>
        Task<int?> GetDailyTransferCountLimitAsync(string subscriptionType, int? defaultValue = null);

        /// <summary>
        /// Gets minimum post-transfer balance for a subscription type with caching
        /// </summary>
        /// <param name="subscriptionType">Subscription type</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Minimum post-transfer balance or null if not required</returns>
        Task<decimal?> GetMinPostTransferBalanceAsync(string subscriptionType, decimal? defaultValue = null);

        /// <summary>
        /// Gets credit transfer customer service name for a subscription type with caching
        /// </summary>
        /// <param name="subscriptionType">Subscription type</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Customer service name</returns>
        Task<string> GetCreditTransferCustomServiceAsync(string subscriptionType, string defaultValue = "CS.Credit_transfer");

        /// <summary>
        /// Adds or updates a transfer configuration
        /// </summary>
        /// <param name="config">Transfer configuration to add or update</param>
        /// <returns>Updated configuration with ID</returns>
        Task<TransferConfig> UpsertConfigurationAsync(TransferConfig config);

        /// <summary>
        /// Deletes a transfer configuration by ID
        /// </summary>
        /// <param name="id">Configuration ID to delete</param>
        /// <returns>True if deleted, false if not found</returns>
        Task<bool> DeleteConfigurationAsync(int id);

        /// <summary>
        /// Refreshes the cache for all transfer configurations
        /// </summary>
        Task RefreshCacheAsync();

        /// <summary>
        /// Clears the cache for a specific subscription type
        /// </summary>
        /// <param name="subscriptionType">Subscription type to clear from cache</param>
        Task ClearCacheAsync(string subscriptionType);

        /// <summary>
        /// Bulk insert or update transfer configurations
        /// </summary>
        /// <param name="configurations">List of configurations to upsert</param>
        Task BulkUpsertConfigurationsAsync(List<TransferConfig> configurations);
    }
} 
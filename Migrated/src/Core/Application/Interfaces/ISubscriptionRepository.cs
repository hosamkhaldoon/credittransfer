using CreditTransfer.Core.Domain.Enums;

namespace CreditTransfer.Core.Application.Interfaces
{
    /// <summary>
    /// Repository interface for subscription-related data operations
    /// Abstracts the data access layer for testability and flexibility
    /// </summary>
    public interface ISubscriptionRepository
    {
        /// <summary>
        /// Gets the subscription type for a given MSISDN
        /// </summary>
        Task<SubscriptionType> GetAccountTypeAsync(string msisdn);
        
        /// <summary>
        /// Gets the subscription block status
        /// </summary>
        Task<SubscriptionBlockStatus> GetSubscriptionBlockStatusAsync(string msisdn);
        
        /// <summary>
        /// Gets the subscription status
        /// </summary>
        Task<SubscriptionStatus> GetSubscriptionStatusAsync(string msisdn);
        
        /// <summary>
        /// Gets the account PIN for validation
        /// </summary>
        Task<string> GetAccountPinAsync(string msisdn);
        
        /// <summary>
        /// Gets the maximum transfer amount allowed for an account
        /// </summary>
        Task<decimal> GetAccountMaxTransferAmountAsync(string msisdn);
        
        /// <summary>
        /// Gets the account PIN by service name
        /// </summary>
        Task<string> GetAccountPinByServiceNameAsync(string msisdn, string serviceName);
        
        /// <summary>
        /// Gets the maximum transfer amount by service name
        /// </summary>
        Task<decimal> GetAccountMaxTransferAmountByServiceNameAsync(string msisdn, string serviceName);
        
        /// <summary>
        /// Gets the Nobill subscription type
        /// </summary>
        Task<string> GetNobillSubscriptionTypeAsync(string msisdn);

        /// <summary>
        /// Checks if both MSISDNs are on the same IN (Intelligent Network)
        /// </summary>
        Task<(bool bothOnSameIN, bool isOldToNew)> CheckBothOnSameINAsync(string sourceMsisdn, string destinationMsisdn);



    }
    
    /// <summary>
    /// Subscription details for PIN validation
    /// </summary>
    public class SubscriptionDetails
    {
        public string Msisdn { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
        public SubscriptionType SubscriptionType { get; set; }
        public SubscriptionStatus Status { get; set; }
        public SubscriptionBlockStatus BlockStatus { get; set; }
        public string? Language { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
} 
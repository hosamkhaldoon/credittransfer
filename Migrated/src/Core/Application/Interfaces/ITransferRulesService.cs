using CreditTransfer.Core.Domain.Enums;

namespace CreditTransfer.Core.Application.Interfaces
{
    /// <summary>
    /// Service for evaluating configurable credit transfer business rules
    /// Replaces hard-coded business logic with database-driven rules
    /// </summary>
    public interface ITransferRulesService
    {
        /// <summary>
        /// Evaluates whether a transfer is allowed based on configurable rules
        /// </summary>
        /// <param name="country">Country code (OM, KSA, etc.)</param>
        /// <param name="sourceSubscriptionType">Source subscription type</param>
        /// <param name="destinationSubscriptionType">Destination subscription type</param>
        /// <param name="configurationValues">Optional configuration values (e.g., TransferToOtherOperator)</param>
        /// <returns>Tuple containing whether transfer is allowed, error code, and error message</returns>
        Task<(bool isAllowed, int errorCode, string errorMessage)> EvaluateTransferRuleAsync(
            string country,
            SubscriptionType sourceSubscriptionType,
            SubscriptionType destinationSubscriptionType,
            Dictionary<string, string>? configurationValues = null);

        /// <summary>
        /// Gets all active transfer rules for a specific country
        /// </summary>
        /// <param name="country">Country code</param>
        /// <returns>List of active transfer rules</returns>
        Task<List<TransferRule>> GetActiveRulesAsync(string country);

        /// <summary>
        /// Adds or updates a transfer rule
        /// </summary>
        /// <param name="rule">Transfer rule to add or update</param>
        /// <returns>True if successful</returns>
        Task<bool> UpsertRuleAsync(TransferRule rule);

        /// <summary>
        /// Deactivates a transfer rule
        /// </summary>
        /// <param name="ruleId">Rule ID to deactivate</param>
        /// <returns>True if successful</returns>
        Task<bool> DeactivateRuleAsync(int ruleId);
    }

    /// <summary>
    /// Represents a configurable transfer rule
    /// </summary>
    public class TransferRule
    {
        public int Id { get; set; }
        public string Country { get; set; } = string.Empty;
        public string SourceSubscriptionType { get; set; } = string.Empty;
        public string DestinationSubscriptionType { get; set; } = string.Empty;
        public bool IsAllowed { get; set; }
        public int? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public bool RequiresConfiguration { get; set; }
        public string? ConfigurationKey { get; set; }
        public string? ConfigurationValue { get; set; }
        public int Priority { get; set; } = 100;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string ModifiedBy { get; set; } = string.Empty;
    }
} 
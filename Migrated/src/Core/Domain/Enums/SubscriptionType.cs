namespace CreditTransfer.Core.Domain.Enums
{
    /// <summary>
    /// Represents the different types of mobile subscriptions
    /// Migrated from original CreditTransferEngine.BusinessLogic
    /// </summary>
    public enum SubscriptionType
    {
        Customer = 0,
        Pos = 1,
        Distributor = 2,
        DataAccount = 3,
        HalafoniCustomer = 4,
        Prepaid = 5, // Added for compatibility
        VirginPrepaidCustomer = 6,
        VirginPostpaidCustomer = 7
    }
    
    /// <summary>
    /// Represents the subscription block status
    /// Preserves exact values from original system
    /// </summary>
    public enum SubscriptionBlockStatus
    {
        NO_BLOCK = 0,
        ALL_BLOCK = 1,
        CHARGED_ACTIVITY_BLOCKED = 2
    }
    
    /// <summary>
    /// Represents the subscription status
    /// Preserves exact values from original system
    /// </summary>
    public enum SubscriptionStatus
    {
        CREATED = 0,
        ASSOCIATED = 1,
        ACTIVE_BEFORE_FIRST_USE = 2,
        ACTIVE_IN_USE = 3,
        ACTIVE_COOLING = 4,
        ACTIVE_COLD = 5,
        ACTIVE_FROZEN = 6,
        ACTIVE = 7, // Added for compatibility
        INACTIVE = 8 // Added for compatibility
    }
    
    /// <summary>
    /// Represents ID number validation result status
    /// </summary>
    public enum IDNumberResultStatus
    {
        Valid = 0,
        Invalid = 1,
        NotFound = 2
    }
} 
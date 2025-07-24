namespace CreditTransferEngine.Utils
{
    public enum SubscribtionType
    {
        Pos,
        Distributor,
        Customer,
        DataAccount, 
        HalafoniCustomer       
    }

    public enum SubscriptionBlockStatus
    {
        NO_BLOCK = 0,
        ALL_BLOCK = 1,
        CHARGED_ACTIVITY_BLOCKED = 2
    }
    public enum TransactionStatus : byte
    {
        Succeeded = 1,
        ReservationFailed = 2,
        TransferFailed = 3,
        TransferFailedCancelFailed = 4,
        ExtensionFailed = 5,
        CommitFailed = 6,
        CommitFailedDueToAutoCancel = 7,
        ExtensionFailedAfterRetries = 8,
        Pending= 9
    }

    public enum VirginTransactionStatus
    {
        Pending= 1,
        Success= 2,
        RolledBack = 3,
        UnableToRollback = 4
        
    }

    public enum SubscriptionStatus
    {
        CREATED = 0,
        ASSOCIATED = 1,
        ACTIVE_BEFORE_FIRST_USE = 2,
        ACTIVE_IN_USE = 3,
        ACTIVE_COOLING = 4,
        ACTIVE_COLD = 5,
        ACTIVE_FROZEN = 6
    }

    public enum Language
    {
        ENGLISH=0,
        ARABIC=1,
        NOTFOUND =-1    
    }
}
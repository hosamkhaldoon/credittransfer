namespace CreditTransfer.Core.Domain.Entities
{
    /// <summary>
    /// Represents a credit transfer transaction
    /// Migrated from original Transaction class - matches actual database table structure
    /// </summary>
    public class Transaction
    {
        public long Id { get; set; }
        public string SourceMsisdn { get; set; } = string.Empty;
        public string DestinationMsisdn { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PIN { get; set; } = string.Empty;
        public bool IsFromCustomer { get; set; }
        public bool IsEventReserved { get; set; }
        public bool IsAmountTransfered { get; set; }
        public bool IsEventCharged { get; set; }
        public bool IsEventCancelled { get; set; }
        public bool IsExpiryExtended { get; set; }
        public int ExtensionDays { get; set; }
        public int ReservationId { get; set; }
        public byte StatusId { get; set; }
        public int NumberOfRetries { get; set; }
        public DateTime CreationDate { get; set; } = DateTime.Now; // Use CreationDate to match DB
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModificationDate { get; set; }
        public string? ModifiedBy { get; set; }

        // Additional properties for migration compatibility
        public string? TransferReason { get; set; }
        public string? AdjustmentReason { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string? ErrorMessage { get; set; }
        public string? TransactionReference { get; set; }

        // For backwards compatibility with existing code
        public DateTime CreatedDate
        {
            get => CreationDate;
            set => CreationDate = value;
        }
    }

    /// <summary>
    /// Transaction status enumeration
    /// </summary>
    public enum TransactionStatus : byte
    {
        Pending = 0,
        Succeeded = 1,
        Failed = 2,
        Cancelled = 3,
        Reserved = 4,
        TransferFailed = 5
    }
}
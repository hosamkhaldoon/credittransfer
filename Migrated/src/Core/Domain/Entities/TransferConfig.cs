namespace CreditTransfer.Core.Domain.Entities
{
    /// <summary>
    /// Represents transfer configuration for different subscription types
    /// Migrated from original TransferConfig class
    /// </summary>
    public class TransferConfig
    {
        public int ID { get; set; }
        public string NobillSubscritpionType { get; set; } = string.Empty;
        public string? TransferCounterName { get; set; }
        public string CreditTransferCustomerService { get; set; } = string.Empty;
        public int? DailyTransferCountLimit { get; set; }
        public decimal? DailyTransferCapLimit { get; set; }
        public decimal? MinTransferAmount { get; set; }
        public int? TransferFeesEventId { get; set; }
        public decimal? MinPostTransferBalance { get; set; }
        public decimal? MaxTransferAmount { get; set; }
        public string? SubscriptionType { get; set; }
        public bool? TransferToOtherOperator { get; set; }
        //public bool IsActive { get; set; } = true;
        //public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        //public DateTime? ModifiedDate { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace CreditTransfer.Core.Application.DTOs
{
    /// <summary>
    /// Represents a credit transfer request with all necessary validation and business rules
    /// </summary>
    public class CreditTransferRequest
    {
        public string SourceMsisdn { get; set; } = string.Empty;
        public string DestinationMsisdn { get; set; } = string.Empty;
        public int AmountRiyal { get; set; }
        public int AmountBaisa { get; set; }
        public string Pin { get; set; } = string.Empty;
        public string? AdjustmentReason { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Gets the total amount in decimal format (Riyal + Baisa/1000)
        /// </summary>
        public decimal TotalAmount => AmountRiyal + (AmountBaisa / 1000m);
        
        /// <summary>
        /// Validates the basic structure of the credit transfer request
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(SourceMsisdn) &&
                   !string.IsNullOrEmpty(DestinationMsisdn) &&
                   !string.IsNullOrEmpty(Pin) &&
                   !string.IsNullOrEmpty(UserName) &&
                   AmountRiyal >= 0 &&
                   AmountBaisa >= 0 &&
                   SourceMsisdn != DestinationMsisdn;
        }
    }
} 
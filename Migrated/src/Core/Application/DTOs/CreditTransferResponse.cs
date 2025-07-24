namespace CreditTransfer.Core.Application.DTOs
{
    /// <summary>
    /// Represents the response from a credit transfer operation
    /// </summary>
    public class CreditTransferResponse
    {
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public DateTime ResponseTimestamp { get; set; } = DateTime.UtcNow;
        public bool IsSuccess => StatusCode == 0;
        public string? TransactionId { get; set; }
        public decimal? ProcessedAmount { get; set; }
        
        /// <summary>
        /// Creates a successful response
        /// </summary>
        public static CreditTransferResponse Success(string requestId, string? transactionId = null, decimal? amount = null)
        {
            return new CreditTransferResponse
            {
                StatusCode = 0,
                StatusMessage = "Success",
                RequestId = requestId,
                TransactionId = transactionId,
                ProcessedAmount = amount
            };
        }
        
        /// <summary>
        /// Creates an error response
        /// </summary>
        public static CreditTransferResponse Error(int statusCode, string statusMessage, string requestId)
        {
            return new CreditTransferResponse
            {
                StatusCode = statusCode,
                StatusMessage = statusMessage,
                RequestId = requestId
            };
        }
    }
} 
using CreditTransfer.Core.Domain.Entities;

namespace CreditTransfer.Core.Application.Interfaces
{
    /// <summary>
    /// Modern application service interface that preserves the exact API surface 
    /// from the original WCF service contract for backward compatibility
    /// </summary>
    public interface ICreditTransferService
    {
        /// <summary>
        /// Transfers credit between two mobile numbers with PIN validation
        /// Preserves exact signature from original WCF service
        /// </summary>
        Task<(int statusCode, string statusMessage, long transactionId)> TransferCreditAsync(
            string sourceMsisdn, 
            string destinationMsisdn, 
            int amountRiyal, 
            int amountBaisa, 
            string pin);
        
        /// <summary>
        /// Transfers credit with an adjustment reason
        /// Preserves exact signature from original WCF service
        /// </summary>
        Task<(int statusCode, string statusMessage, long transactionId)> TransferCreditWithAdjustmentReasonAsync(
            string sourceMsisdn, 
            string destinationMsisdn, 
            int amountRiyal, 
            int amountBaisa, 
            string pin, 
            string adjustmentReason);
        
        /// <summary>
        /// Gets available denomination values
        /// Preserves exact signature from original WCF service
        /// </summary>
        Task<List<decimal>> GetDenominationsAsync();
        
        /// <summary>
        /// Transfers credit without PIN for service center operations
        /// Preserves exact signature from original WCF service
        /// </summary>
        Task<(int statusCode, string statusMessage, long transactionId)> TransferCreditWithoutPinAsync(
            string sourceMsisdn, 
            string destinationMsisdn, 
            decimal amountRiyal);
        
        /// <summary>
        /// Validates transfer inputs without performing the actual transfer
        /// Returns response with status code and message
        /// </summary>
        Task<(int statusCode, string statusMessage)> ValidateTransferInputsAsync(
            string sourceMsisdn, 
            string destinationMsisdn, 
            decimal amountRiyal);

        /// <summary>
        /// Performs comprehensive system health check including all dependencies
        /// Similar to health-check.ps1 script functionality
        /// </summary>
        Task<ComprehensiveHealthResponse> GetSystemHealthAsync();
    }

    /// <summary>
    /// Comprehensive health check response model
    /// </summary>
    public class ComprehensiveHealthResponse
    {
        public string OverallStatus { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public List<ComponentHealth> Components { get; set; } = new List<ComponentHealth>();
        public HealthSummary Summary { get; set; } = new HealthSummary();
        public string? ErrorDetails { get; set; }
    }

    /// <summary>
    /// Individual component health status
    /// </summary>
    public class ComponentHealth
    {
        public string Component { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? StatusMessage { get; set; }
        public double? ResponseTimeMs { get; set; }
        public string? ErrorCode { get; set; }
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Health check summary statistics
    /// </summary>
    public class HealthSummary
    {
        public int TotalComponents { get; set; }
        public int HealthyComponents { get; set; }
        public int DegradedComponents { get; set; }
        public int UnhealthyComponents { get; set; }
        public string OverallHealthPercentage { get; set; } = string.Empty;
    }
} 
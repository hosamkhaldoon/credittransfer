using CreditTransfer.Core.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CreditTransfer.Core.Authentication.Services;
using System.Diagnostics.Metrics;

namespace CreditTransfer.Services.RestApi.Controllers
{
    /// <summary>
    /// REST API Controller for Credit Transfer operations
    /// Provides modern HTTP endpoints while preserving the same business logic as WCF service
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for all endpoints
    [Produces("application/json")]
    public class CreditTransferController : ControllerBase
    {
        private readonly ICreditTransferService _creditTransferService;
        private readonly ITokenValidationService _tokenValidationService;
        private readonly ILogger<CreditTransferController> _logger;
        private readonly Meter _meter;
        private readonly Counter<long> _apiRequestsCounter;
        private readonly Counter<long> _apiSuccessCounter;
        private readonly Counter<long> _apiErrorCounter;
        private readonly Histogram<double> _apiDurationHistogram;

        public CreditTransferController(
            ICreditTransferService creditTransferService,
            ITokenValidationService tokenValidationService,
            ILogger<CreditTransferController> logger,
            Meter meter)
        {
            _creditTransferService = creditTransferService;
            _tokenValidationService = tokenValidationService;
            _logger = logger;
            _meter = meter;
            
            // Initialize OpenTelemetry Metrics using the injected meter
            _apiRequestsCounter = _meter.CreateCounter<long>(
                "api_requests_total",
                description: "Total number of API requests");
                
            _apiSuccessCounter = _meter.CreateCounter<long>(
                "api_success_total",
                description: "Total number of successful API responses");
                
            _apiErrorCounter = _meter.CreateCounter<long>(
                "api_errors_total",
                description: "Total number of API errors");
                
            _apiDurationHistogram = _meter.CreateHistogram<double>(
                "api_request_duration_seconds",
                description: "Duration of API requests in seconds");
        }

        /// <summary>
        /// Transfers credit from source to destination MSISDN with PIN validation
        /// </summary>
        /// <param name="request">Credit transfer request</param>
        /// <returns>Transfer result with status code and message</returns>
        [HttpPost("transfer")]
        [Authorize(Policy = "CreditTransferOperator")]
        [ProducesResponseType(typeof(CreditTransferApiResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> TransferCredit([FromBody] TransferCreditRequest request)
        {
            var startTime = DateTime.UtcNow;
            _apiRequestsCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "transfer"));
            
            try
            {
                // Convert decimal amount to Riyal and Baisa
                var amountRiyal = (int)Math.Floor(request.Amount);
                var amountBaisa = (int)((request.Amount - amountRiyal) * 1000);

                var result = await _creditTransferService.TransferCreditAsync(
                    request.SourceMsisdn, 
                    request.DestinationMsisdn, 
                    amountRiyal,
                    amountBaisa,
                    request.Pin);

                var response = new CreditTransferApiResponse
                {
                    StatusCode = result.statusCode,
                    StatusMessage = result.statusMessage,
                    Success = result.statusCode == 0,
                    TransactionId = result.transactionId.ToString(),
                    Timestamp = DateTime.UtcNow
                };

                if (response.Success)
                {
                    _apiSuccessCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "transfer"));
                    _apiDurationHistogram.Record((DateTime.UtcNow - startTime).TotalSeconds, 
                        new KeyValuePair<string, object?>("endpoint", "transfer"),
                        new KeyValuePair<string, object?>("result", "success"));
                    _logger.LogInformation("Credit transfer successful: {TransactionId}", result.transactionId);
                    return Ok(response);
                }
                else
                {
                    _apiErrorCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "transfer"),
                        new KeyValuePair<string, object?>("error_code", result.statusCode));
                    _apiDurationHistogram.Record((DateTime.UtcNow - startTime).TotalSeconds, 
                        new KeyValuePair<string, object?>("endpoint", "transfer"),
                        new KeyValuePair<string, object?>("result", "business_error"));
                    _logger.LogWarning("Credit transfer failed: {StatusCode} - {StatusMessage}", result.statusCode, result.statusMessage);
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _apiErrorCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "transfer"),
                    new KeyValuePair<string, object?>("error_type", "exception"));
                _apiDurationHistogram.Record((DateTime.UtcNow - startTime).TotalSeconds, 
                    new KeyValuePair<string, object?>("endpoint", "transfer"),
                    new KeyValuePair<string, object?>("result", "exception"));
                _logger.LogError(ex, "Error processing credit transfer request");
                return StatusCode(500, new ErrorResponse
                {
                    Error = "Internal server error",
                    Message = "An error occurred while processing the credit transfer request",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Transfers credit with adjustment reason for auditing purposes
        /// </summary>
        /// <param name="request">Credit transfer request with adjustment reason</param>
        /// <returns>Transfer result with status code and message</returns>
        [HttpPost("transfer-with-reason")]
        [Authorize(Policy = "CreditTransferOperator")]
        [ProducesResponseType(typeof(CreditTransferApiResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> TransferCreditWithAdjustmentReason([FromBody] TransferCreditWithReasonRequest request)
        {
            try
            {

                // Convert decimal amount to Riyal and Baisa
                var amountRiyal = (int)Math.Floor(request.Amount);
                var amountBaisa = (int)((request.Amount - amountRiyal) * 1000);

                var result = await _creditTransferService.TransferCreditWithAdjustmentReasonAsync(
                    request.SourceMsisdn, 
                    request.DestinationMsisdn, 
                    amountRiyal,
                    amountBaisa,
                    request.Pin, 
                    request.AdjustmentReason);

                var response = new CreditTransferApiResponse
                {
                    StatusCode = result.statusCode,
                    StatusMessage = result.statusMessage,
                    Success = result.statusCode == 0,
                    TransactionId = result.transactionId.ToString(),
                    Timestamp = DateTime.UtcNow,
                    AdjustmentReason = request.AdjustmentReason
                };

                if (response.Success)
                {
                    _logger.LogInformation("Credit transfer with reason successful: {TransactionId}", result.transactionId);
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning("Credit transfer with reason failed: {StatusCode} - {StatusMessage}", result.statusCode, result.statusMessage);
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing credit transfer with reason request");
                return StatusCode(500, new ErrorResponse
                {
                    Error = "Internal server error",
                    Message = "An error occurred while processing the credit transfer request",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Gets available denominations for credit transfer
        /// </summary>
        /// <returns>List of available denominations</returns>
        [HttpGet("denominations")]
        [Authorize(Policy = "CreditTransferUser")]
        [ProducesResponseType(typeof(List<decimal>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> GetDenominations()
        {
            try
            {
                var username = _tokenValidationService.GetUsername(User);
                _logger.LogDebug("Get denominations request from user {Username}", username);

                var result = await _creditTransferService.GetDenominationsAsync();

                _logger.LogInformation("REST GetDenominations completed: Count={Count}", result.Count);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving denominations");
                return StatusCode(500, new ErrorResponse
                {
                    Error = "Internal server error",
                    Message = "An error occurred while retrieving denominations",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Transfers credit without PIN validation for Service Center operations
        /// </summary>
        /// <param name="request">Credit transfer request without PIN</param>
        /// <returns>Transfer result with status code and message</returns>
        [HttpPost("transfer-without-pin")]
        [Authorize(Policy = "CreditTransferOperator")]
        [ProducesResponseType(typeof(CreditTransferApiResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> TransferCreditWithoutPin([FromBody] TransferCreditWithoutPinRequest request)
        {
            try
            {
                var username = _tokenValidationService.GetUsername(User);
                var userId = _tokenValidationService.GetUserId(User);
                
                _logger.LogInformation("Credit transfer without PIN request from system user {Username} ({UserId}): {SourceMsisdn} -> {DestinationMsisdn}, Amount: {Amount}",
                    username, userId, request.SourceMsisdn, request.DestinationMsisdn, request.Amount);

                var result = await _creditTransferService.TransferCreditWithoutPinAsync(
                    request.SourceMsisdn, 
                    request.DestinationMsisdn, 
                    request.Amount);

                var response = new CreditTransferApiResponse
                {
                    StatusCode = result.statusCode,
                    StatusMessage = result.statusMessage,
                    Success = result.statusCode == 0,
                    TransactionId = result.transactionId.ToString(),
                    Timestamp = DateTime.UtcNow
                };

                if (response.Success)
                {
                    _logger.LogInformation("Credit transfer without PIN successful: {TransactionId}", result.transactionId);
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning("Credit transfer without PIN failed: {StatusCode} - {StatusMessage}", result.statusCode, result.statusMessage);
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing credit transfer without PIN request");
                return StatusCode(500, new ErrorResponse
                {
                    Error = "Internal server error",
                    Message = "An error occurred while processing the credit transfer request",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Validates transfer inputs without performing the actual transfer
        /// </summary>
        /// <param name="request">Validation request</param>
        /// <returns>Validation result with status code and message</returns>
        [HttpPost("validate")]
        [Authorize(Policy = "CreditTransferUser")]
        [ProducesResponseType(typeof(CreditTransferApiResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> ValidateTransferInputs([FromBody] ValidateTransferRequest request)
        {
            try
            {
                var username = _tokenValidationService.GetUsername(User);
                _logger.LogDebug("Validate transfer inputs request from user {Username}: {SourceMsisdn} -> {DestinationMsisdn}, Amount: {Amount}",
                    username, request.SourceMsisdn, request.DestinationMsisdn, request.Amount);

                var result = await _creditTransferService.ValidateTransferInputsAsync(
                    request.SourceMsisdn, 
                    request.DestinationMsisdn, 
                    request.Amount);

                var response = new CreditTransferApiResponse
                {
                    StatusCode = result.statusCode,
                    StatusMessage = result.statusMessage,
                    Success = result.statusCode == 0,
                    Timestamp = DateTime.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating transfer inputs");
                return StatusCode(500, new ErrorResponse
                {
                    Error = "Internal server error",
                    Message = "An error occurred while validating transfer inputs",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        /// <returns>Service health status</returns>
        [HttpGet("health")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(HealthResponse), 200)]
        public IActionResult Health()
        {
            return Ok(new HealthResponse 
            { 
                Status = "Healthy", 
                Service = "Credit Transfer REST API",
                Version = "1.0.0",
                Timestamp = DateTime.UtcNow,
                Authentication = "Keycloak JWT"
            });
        }

        /// <summary>
        /// Comprehensive system health check endpoint
        /// Performs the same checks as health-check.ps1 script
        /// Tests database, Redis, NoBill service, configuration, and external dependencies
        /// </summary>
        /// <returns>Detailed system health status</returns>
        [HttpGet("health/system")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ComprehensiveHealthResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> GetSystemHealth()
        {
            var startTime = DateTime.UtcNow;
            _apiRequestsCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "health-system"));

            try
            {
                _logger.LogInformation("🔍 Starting comprehensive system health check via API");

                var healthResult = await _creditTransferService.GetSystemHealthAsync();

                var duration = (DateTime.UtcNow - startTime).TotalSeconds;
                _apiDurationHistogram.Record(duration, 
                    new KeyValuePair<string, object?>("endpoint", "health-system"),
                    new KeyValuePair<string, object?>("result", healthResult.OverallStatus.ToLower()));

                _logger.LogInformation("✅ System health check completed: {Status} in {Duration:F2}s", 
                    healthResult.OverallStatus, duration);

                // Return appropriate HTTP status based on health
                return healthResult.OverallStatus switch
                {
                    "HEALTHY" => Ok(healthResult),
                    "DEGRADED" => StatusCode(200, healthResult), // Still 200 but marked as degraded
                    "UNHEALTHY" => StatusCode(503, healthResult), // Service Unavailable
                    "CRITICAL_ERROR" => StatusCode(500, healthResult), // Internal Server Error
                    _ => StatusCode(500, healthResult)
                };
            }
            catch (Exception ex)
            {
                var duration = (DateTime.UtcNow - startTime).TotalSeconds;
                _apiErrorCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "health-system"),
                    new KeyValuePair<string, object?>("error_type", "exception"));
                _apiDurationHistogram.Record(duration, 
                    new KeyValuePair<string, object?>("endpoint", "health-system"),
                    new KeyValuePair<string, object?>("result", "exception"));

                _logger.LogError(ex, "❌ Error during comprehensive health check");

                return StatusCode(500, new ErrorResponse
                {
                    Error = "Health check system error",
                    Message = "An error occurred while performing the comprehensive health check",
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }

    #region Request/Response Models

    /// <summary>
    /// Request model for credit transfer
    /// </summary>
    public class TransferCreditRequest
    {
        public string SourceMsisdn { get; set; } = string.Empty;
        public string DestinationMsisdn { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Pin { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for credit transfer with adjustment reason
    /// </summary>
    public class TransferCreditWithReasonRequest : TransferCreditRequest
    {
        public string AdjustmentReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for credit transfer without PIN
    /// </summary>
    public class TransferCreditWithoutPinRequest
    {
        public string SourceMsisdn { get; set; } = string.Empty;
        public string DestinationMsisdn { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// Request model for transfer validation
    /// </summary>
    public class ValidateTransferRequest
    {
        public string SourceMsisdn { get; set; } = string.Empty;
        public string DestinationMsisdn { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// Response model for credit transfer operations
    /// </summary>
    public class CreditTransferApiResponse
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public DateTime Timestamp { get; set; }
        public string? AdjustmentReason { get; set; }
    }

    /// <summary>
    /// Error response model
    /// </summary>
    public class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Health response model
    /// </summary>
    public class HealthResponse
    {
        public string Status { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Authentication { get; set; } = string.Empty;
    }

    #endregion
} 
using Microsoft.Extensions.Logging;
using CreditTransfer.Core.Application.Interfaces;
using CreditTransfer.Core.Domain.Constants;

namespace CreditTransfer.Core.Application.Services
{
    /// <summary>
    /// Service for managing error configuration and message retrieval
    /// Now uses database-driven configuration instead of static app.config
    /// </summary>
    public class ErrorConfigurationService : IErrorConfigurationService
    {
        private readonly IApplicationConfigRepository _configRepository;
        private readonly ILogger<ErrorConfigurationService> _logger;

        public ErrorConfigurationService(
            IApplicationConfigRepository configRepository, 
            ILogger<ErrorConfigurationService> logger)
        {
            _configRepository = configRepository;
            _logger = logger;
        }

        /// <summary>
        /// Gets the error message for a specific error code from database configuration
        /// This preserves the exact same error mapping as the original Web.config
        /// Now uses descriptive keys instead of numeric ones for better maintainability
        /// </summary>
        /// <param name="errorCode">The error code</param>
        /// <returns>The corresponding error message</returns>
        public async Task<string> GetErrorMessageAsync(int errorCode)
        {
            try
            {
                // Map error codes to descriptive configuration keys
                var configKey = GetDescriptiveErrorKey(errorCode);
                var errorMessage = await _configRepository.GetConfigValueAsync(configKey);
                
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    _logger.LogDebug("Retrieved error message for code {ErrorCode} using key {ConfigKey}: {ErrorMessage}", 
                        errorCode, configKey, errorMessage);
                    return errorMessage;
                }

                // Fallback to hardcoded messages if database config is not available
                var fallbackMessage = GetFallbackErrorMessage(errorCode);
                _logger.LogWarning("Error message not found in database for code {ErrorCode} with key {ConfigKey}, using fallback: {FallbackMessage}", 
                    errorCode, configKey, fallbackMessage);
                
                return fallbackMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving error message for code {ErrorCode}", errorCode);
                return GetFallbackErrorMessage(errorCode);
            }
        }

        /// <summary>
        /// Maps numeric error codes to descriptive configuration keys
        /// </summary>
        /// <param name="errorCode">The numeric error code</param>
        /// <returns>Descriptive configuration key</returns>
        private static string GetDescriptiveErrorKey(int errorCode)
        {
            return errorCode switch
            {
                ErrorCodes.Success => "Message_Success",
                ErrorCodes.UnknownSubscriber => "ErrorMessage_UnknownSubscriber",
                ErrorCodes.SourceDestinationSame => "ErrorMessage_SourceDestinationSame",
                ErrorCodes.InvalidPin => "ErrorMessage_InvalidPin",
                ErrorCodes.TransferAmountBelowMin => "ErrorMessage_TransferAmountBelowMin",
                ErrorCodes.TransferAmountAboveMax => "ErrorMessage_TransferAmountAboveMax",
                ErrorCodes.MiscellaneousError => "ErrorMessage_MiscellaneousError",
                ErrorCodes.InvalidSourcePhone => "ErrorMessage_InvalidSourcePhone",
                ErrorCodes.InvalidDestinationPhone => "ErrorMessage_InvalidDestinationPhone",
                ErrorCodes.InvalidPIN => "ErrorMessage_InvalidPinCode",
                ErrorCodes.InsufficientBalance => "ErrorMessage_InsufficientCredit",
                ErrorCodes.SubscriptionNotFound => "ErrorMessage_SubscriptionNotFound",
                ErrorCodes.ConcurrentUpdateDetected => "ErrorMessage_ConcurrentUpdateDetected",
                ErrorCodes.SourcePhoneNotFound => "ErrorMessage_SourceSubscriptionNotFound",
                ErrorCodes.DestinationPhoneNotFound => "ErrorMessage_DestinationSubscriptionNotFound",
                ErrorCodes.UserNotAllowed => "ErrorMessage_UserNotAllowed",
                ErrorCodes.ConfigurationError => "ErrorMessage_ConfigurationError",
                ErrorCodes.PropertyNotFound => "ErrorMessage_PropertyNotFound",
                ErrorCodes.ExpiredReservationCode => "ErrorMessage_ExpiredReservationCode",
                ErrorCodes.BadRequest => "ErrorMessage_BadRequest",
                ErrorCodes.NotAllowedToTransfer => "ErrorMessage_TransferNotAllowed",
                ErrorCodes.ExceedsMaxPerDay => "ErrorMessage_ExceedsMaxPerDay",
                ErrorCodes.RemainingBalance => "ErrorMessage_RemainingBalance",
                ErrorCodes.AmountNotMultipleOfFive => "ErrorMessage_InvalidTransferAmount",
                ErrorCodes.SmsError => "ErrorMessage_SmsError",
                ErrorCodes.ReserveAmountError => "ErrorMessage_ReserveAmountError",
                ErrorCodes.CreditFailure => "ErrorMessage_CreditAmountError",
                ErrorCodes.RemainingBalanceHalf => "ErrorMessage_BalancePercentageError",
                _ => $"ErrorMessage_Unknown_{errorCode}"
            };
        }

        /// <summary>
        /// Gets business configuration values from database
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Configuration value</returns>
        public async Task<string?> GetConfigValueAsync(string key, string? defaultValue = null)
        {
            try
            {
                return await _configRepository.GetConfigValueAsync(key, defaultValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving configuration value for key {ConfigKey}", key);
                return defaultValue;
            }
        }

        /// <summary>
        /// Gets typed configuration values from database
        /// </summary>
        /// <typeparam name="T">Type to convert to</typeparam>
        /// <param name="key">Configuration key</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Typed configuration value</returns>
        public async Task<T> GetConfigValueAsync<T>(string key, T defaultValue = default!)
        {
            try
            {
                return await _configRepository.GetConfigValueAsync<T>(key, defaultValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving typed configuration value for key {ConfigKey}", key);
                return defaultValue;
            }
        }

        /// <summary>
        /// Provides fallback error messages that match the original Web.config exactly
        /// This ensures backward compatibility even if database is unavailable
        /// </summary>
        /// <param name="errorCode">The error code</param>
        /// <returns>Fallback error message</returns>
        private static string GetFallbackErrorMessage(int errorCode)
        {
            return errorCode switch
            {
                ErrorCodes.Success => "Success",
                ErrorCodes.UnknownSubscriber => "Unknown Subscriber",
                ErrorCodes.SourceDestinationSame => "A-party and B-party phone numbers are same",
                ErrorCodes.InvalidPin => "Invalid credit transfer password",
                ErrorCodes.TransferAmountBelowMin => "Amount requested is less than the minimum transferrable amount by A-party",
                ErrorCodes.TransferAmountAboveMax => "Amount requested is more than the maximum amount that can be transferred by the A-party",
                ErrorCodes.MiscellaneousError => "Miscellaneous error",
                ErrorCodes.InvalidSourcePhone => "Invalid Source Phone Number",
                ErrorCodes.InvalidDestinationPhone => "Invalid Destination Phone Number",
                ErrorCodes.InvalidPIN => "Invalid PIN",
                ErrorCodes.InsufficientBalance => "Insufficient Credit",
                ErrorCodes.SubscriptionNotFound => "Subscription Not Found",
                ErrorCodes.ConcurrentUpdateDetected => "Concurrent Update Detected",
                ErrorCodes.SourcePhoneNotFound => "Source Subscription Not Found",
                ErrorCodes.DestinationPhoneNotFound => "Destination Subscription Not Found",
                ErrorCodes.UserNotAllowed => "User Not Allowed To Call This Method",
                ErrorCodes.ConfigurationError => "Configuration Error",
                ErrorCodes.PropertyNotFound => "Property Not Found",
                ErrorCodes.ExpiredReservationCode => "Expired Reservation Code",
                ErrorCodes.BadRequest => "Bad Request",
                ErrorCodes.NotAllowedToTransfer => "Credit transfer is not allowed to this such account type",
                ErrorCodes.ExceedsMaxPerDay => "Subscription has been reached the number of transfers per day",
                ErrorCodes.RemainingBalance => "Insufficient Remaining Credit",
                ErrorCodes.AmountNotMultipleOfFive => "Transfer amount should be a multiple of 5",
                ErrorCodes.SmsError => "An error occurred while sending sms to the destination msisdn",
                ErrorCodes.ReserveAmountError => "An error occurred while reserve amount",
                ErrorCodes.CreditFailure => "Failed to credit the amount to the source msisdn",
                ErrorCodes.RemainingBalanceHalf => "Your balance remaining should be more than 50%",
                _ => $"Unknown error (Code: {errorCode})"
            };
        }
    }

    /// <summary>
    /// Interface for error configuration service
    /// </summary>
    public interface IErrorConfigurationService
    {
        /// <summary>
        /// Gets error message for a specific error code
        /// </summary>
        Task<string> GetErrorMessageAsync(int errorCode);
        
        /// <summary>
        /// Gets configuration value as string
        /// </summary>
        Task<string?> GetConfigValueAsync(string key, string? defaultValue = null);
        
        /// <summary>
        /// Gets configuration value as typed value
        /// </summary>
        Task<T> GetConfigValueAsync<T>(string key, T defaultValue = default!);
    }
} 
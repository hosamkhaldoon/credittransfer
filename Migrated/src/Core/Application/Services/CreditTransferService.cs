using CreditTransfer.Core.Application.DTOs;
using CreditTransfer.Core.Application.Exceptions;
using CreditTransfer.Core.Application.Interfaces;
using CreditTransfer.Core.Domain.Constants;
using CreditTransfer.Core.Domain.Entities;
using CreditTransfer.Core.Domain.Enums;
using IntegrationProxies.Nobill.Interfaces;
using IntegrationProxies.Nobill.Services;
using IntegrationProxies.Nobill.Services.NobillCalls;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using CreditTransferItem = IntegrationProxies.Nobill.Services.NobillCalls.CreditTransferItem;
using SubscriptionItem = IntegrationProxies.Nobill.Services.NobillCalls.SubscriptionItem;
using Transaction = CreditTransfer.Core.Domain.Entities.Transaction;
using TransactionStatus = CreditTransfer.Core.Domain.Entities.TransactionStatus;

namespace CreditTransfer.Core.Application.Services
{
    /// <summary>
    /// Main credit transfer service implementation that preserves exact business logic
    /// from the original CreditTransferEngine.BusinessLogic.CreditTransfer class
    /// Enhanced with OpenTelemetry instrumentation for comprehensive observability
    /// </summary>
    public class CreditTransferService : ICreditTransferService
    {
        private readonly IntegrationProxies.Nobill.Interfaces.INobillCallsService _nobillCallsService;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ITransferConfigRepository _transferConfigRepository;
        private readonly IErrorConfigurationService _errorConfigService;
        private readonly IApplicationConfigRepository _configRepository;
        private readonly ITransferRulesService _transferRulesService;
        private readonly ILogger<CreditTransferService> _logger;
        private readonly ActivitySource _activitySource;
        private readonly Meter _meter;

        // Configuration constants (migrated from original static fields)
        private List<int> _msisdnLength;
        private int? _refillPinLength;
        private bool? _enableExtendedDays;
        private string? _defaultPin;
        private decimal? _maximumPercentageAmount;
        private string? _customerToCustomerTransferMoneyReason;
        private bool _configurationLoaded = false;
        private readonly SemaphoreSlim _configurationLock = new(1, 1);

        // OpenTelemetry Metrics for Business KPIs
        private readonly Counter<long> _transferAttempts;
        private readonly Counter<long> _transferSuccesses;
        private readonly Counter<long> _transferFailures;
        private readonly Histogram<double> _transferAmounts;
        private readonly Histogram<double> _transferDuration;
        private readonly Counter<long> _validationErrors;
        private readonly Counter<long> _authenticationEvents;
        private readonly Counter<long> _externalServiceCalls;

        public CreditTransferService(
            IntegrationProxies.Nobill.Interfaces.INobillCallsService nobillCallsService,
            ISubscriptionRepository subscriptionRepository,
            ITransactionRepository transactionRepository,
            IErrorConfigurationService errorConfigService,
            ITransferConfigRepository transferConfigRepository,
            IApplicationConfigRepository configRepository,
            ITransferRulesService transferRulesService,
            ILogger<CreditTransferService> logger,
            ActivitySource activitySource)
        {
            _nobillCallsService = nobillCallsService;
            _subscriptionRepository = subscriptionRepository;
            _transactionRepository = transactionRepository;
            _errorConfigService = errorConfigService;
            _transferConfigRepository = transferConfigRepository;
            _configRepository = configRepository;
            _transferRulesService = transferRulesService;
            _logger = logger;
            _activitySource = activitySource;

            // Initialize OpenTelemetry Meter and Metrics
            _meter = new Meter("CreditTransfer.Core.Business", "1.0.0");

            // Business KPI Metrics
            _transferAttempts = _meter.CreateCounter<long>(
                "credit_transfer_attempts_total",
                description: "Total number of credit transfer attempts");

            _transferSuccesses = _meter.CreateCounter<long>(
                "credit_transfer_successes_total",
                description: "Total number of successful credit transfers");

            _transferFailures = _meter.CreateCounter<long>(
                "credit_transfer_failures_total",
                description: "Total number of failed credit transfers");

            _transferAmounts = _meter.CreateHistogram<double>(
                "credit_transfer_amount_distribution",
                description: "Distribution of credit transfer amounts in Riyal");

            _transferDuration = _meter.CreateHistogram<double>(
                "credit_transfer_duration_seconds",
                description: "Duration of credit transfer operations in seconds");

            _validationErrors = _meter.CreateCounter<long>(
                "credit_transfer_validation_errors_total",
                description: "Total number of validation errors");

            _authenticationEvents = _meter.CreateCounter<long>(
                "credit_transfer_authentication_events_total",
                description: "Total number of authentication events");

            _externalServiceCalls = _meter.CreateCounter<long>(
                "credit_transfer_external_service_calls_total",
                description: "Total number of external service calls");
        }

        private async Task ThrowCreditTransferExceptionAsync<T>() where T : CreditTransferException, new()
        {
            var ex = new T();
            await ex.SetResponseMessageAsync(_configRepository);
            throw ex;
        }

        /// <summary>
        /// Load configuration values asynchronously (preserving original behavior)
        /// </summary>
        private async Task EnsureConfigurationLoadedAsync()
        {
            if (_configurationLoaded) return;

            await _configurationLock.WaitAsync();
            try
            {
                if (_configurationLoaded) return;

                _msisdnLength = await _configRepository.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", new List<int> { 11 });
                _refillPinLength = await _configRepository.GetConfigValueAsync<int>("CreditTransfer_RefillPinLength", 4);
                _enableExtendedDays = await _configRepository.GetConfigValueAsync<bool>("CreditTransfer_EnableExtendedDays", false);
                _defaultPin = await _configRepository.GetConfigValueAsync<string>("CreditTransfer_DefaultPIN", "0000");
                _maximumPercentageAmount = await _configRepository.GetConfigValueAsync<decimal>("CreditTransfer_MaximumPercentageAmount", 1.0m);
                _customerToCustomerTransferMoneyReason = await _configRepository.GetConfigValueAsync<string>("CreditTransfer_CustomerToCustomerTransferMoneyReason", "Credit transfer");

                _configurationLoaded = true;
            }
            finally
            {
                _configurationLock.Release();
            }
        }

        /// <summary>
        /// Transfers credit between two mobile numbers with PIN validation
        /// Preserves exact signature from original WCF service
        /// Enhanced with OpenTelemetry instrumentation
        /// </summary>
        public async Task<(int statusCode, string statusMessage, long transactionId)> TransferCreditAsync(
            string sourceMsisdn,
            string destinationMsisdn,
            int amountRiyal,
            int amountBaisa,
            string pin)
        {
            return await TransferCreditInternalAsync(
                sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin, null);
        }

        /// <summary>
        /// Core credit transfer implementation preserving exact business logic
        /// from original CreditTransferEngine.BusinessLogic.CreditTransfer.TransferCredit
        /// Enhanced with OpenTelemetry instrumentation and metrics
        /// </summary>
        private async Task<(int statusCode, string statusMessage, long transactionId)> TransferCreditInternalAsync(
            string sourceMsisdn,
            string destinationMsisdn,
            int amountRiyal,
            int amountBaisa,
            string pin,
            string? adjustmentReason = null)
        {
            await EnsureConfigurationLoadedAsync();

            var operationStart = DateTime.UtcNow;
            var totalAmount = (decimal)amountRiyal + ((decimal)amountBaisa / 1000);

            using var activity = _activitySource.StartActivity("CreditTransfer.TransferCredit");
            activity?.SetTag("operation", "TransferCredit");
            activity?.SetTag("source.msisdn", sourceMsisdn);
            activity?.SetTag("destination.msisdn", destinationMsisdn);
            activity?.SetTag("amount.riyal", amountRiyal);
            activity?.SetTag("amount.baisa", amountBaisa);
            activity?.SetTag("amount.total", totalAmount);
            activity?.SetTag("has.adjustment.reason", !string.IsNullOrEmpty(adjustmentReason));

            // Record transfer attempt metric
            _transferAttempts.Add(1, new KeyValuePair<string, object?>("source_msisdn", sourceMsisdn),
                                     new KeyValuePair<string, object?>("destination_msisdn", destinationMsisdn),
                                     new KeyValuePair<string, object?>("amount_riyal", amountRiyal));

            try
            {
                _logger.LogInformation("Starting credit transfer from {Source} to {Destination}, amount: {Amount} Riyal",
                    sourceMsisdn, destinationMsisdn, totalAmount);

                // === Validation Phase ===
                using var validationActivity = _activitySource.StartActivity("CreditTransfer.Validation");
                validationActivity?.SetTag("phase", "validation");

                var (isValid, validationCode, validationMessage, sourceMsisdnType, destinationMsisdnType, sourceSubscriptionTypeConfig) = await ValidateTransferInputsInternalAsync(
                    sourceMsisdn, destinationMsisdn, totalAmount);

                if (!isValid)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, validationMessage);
                    activity?.SetTag("validation.result", "failed");
                    activity?.SetTag("validation.error.code", validationCode);
                    activity?.SetTag("validation.error.message", validationMessage);

                    // Record validation error metric
                    _validationErrors.Add(1, new KeyValuePair<string, object?>("error_code", validationCode),
                                             new KeyValuePair<string, object?>("source_msisdn", sourceMsisdn));

                    _transferFailures.Add(1, new KeyValuePair<string, object?>("failure_reason", "validation_failed"),
                                             new KeyValuePair<string, object?>("error_code", validationCode));

                    return (validationCode, validationMessage, 0);
                }

                activity?.SetTag("validation.result", "success");
                validationActivity?.SetTag("validation.passed", true);

                // === PIN Validation (after input validation like original) ===

                if (pin != _defaultPin)
                {
                    var accountPin = await GetAccountPinByServiceNameAsync(sourceMsisdn, sourceSubscriptionTypeConfig!.CreditTransferCustomerService ?? "");
                    if (!string.IsNullOrEmpty(accountPin) && accountPin != pin)
                    {
                        var errorMessage = await _errorConfigService.GetErrorMessageAsync(ErrorCodes.InvalidPin);
                        return (ErrorCodes.InvalidPin, errorMessage, 0);
                    }
                }

                // === EXACT ORIGINAL TRANSFER EXECUTION LOGIC ===

                int reservationCode = -1;
                bool isEventReserved = false;
                bool isAmountTransfered = false;


                int extendedDays = _enableExtendedDays.GetValueOrDefault() && (destinationMsisdnType == SubscriptionType.Customer) ?
                                   await GetDaysToExtendAsync(totalAmount) : 0;

                var transaction = new Domain.Entities.Transaction
                {
                    SourceMsisdn = sourceMsisdn,
                    DestinationMsisdn = destinationMsisdn,
                    Amount = totalAmount,
                    PIN = pin,
                    ExtensionDays = extendedDays,
                    IsFromCustomer = false,
                    CreatedBy = "CreditTransferService",
                    CreatedDate = operationStart,
                    AdjustmentReason = adjustmentReason
                };

                try
                {
                    // Transfer reason logic (exact original logic)
                    string transferReason;
                    bool? bothOnSameIN = null, isOldToNew = null;
                    if (!string.IsNullOrEmpty(adjustmentReason))
                    {
                        transferReason = adjustmentReason;
                    }
                    else
                    {
                        transferReason = _customerToCustomerTransferMoneyReason ?? "Credit transfer";
                        (bothOnSameIN, isOldToNew) = await _subscriptionRepository.CheckBothOnSameINAsync(sourceMsisdn, destinationMsisdn);

                        if (!bothOnSameIN!.Value)
                        {
                            if (isOldToNew!.Value)
                            {
                                transferReason = await _configRepository.GetConfigValueAsync<string>("CreditTransfer_CustomerToCustomerAdjustmentReasonOldToNew", "local_credit_transfer_from_oldIN_to_newIN");
                            }
                            else
                            {
                                transferReason = await _configRepository.GetConfigValueAsync<string>("CreditTransfer_CustomerToCustomerAdjustmentReasonNewToOld", "local_credit_transfer_from_newIN_to_oldIN");
                            }
                        }
                    }


                    // Event reservation for customer accounts (exact original logic)
                    if (sourceMsisdnType == SubscriptionType.Customer)
                    {
                        transaction.ExtensionDays = 0;
                        transaction.IsFromCustomer = true;

                        var transferFundEventId = sourceSubscriptionTypeConfig!.TransferFeesEventId ?? 1;
                        reservationCode = await ReserveEventAsync(sourceMsisdn, destinationMsisdn, totalAmount, pin, extendedDays, transferFundEventId, "CreditTransferService");
                        isEventReserved = true;
                    }
                    else if (destinationMsisdnType == SubscriptionType.Customer || destinationMsisdnType == SubscriptionType.DataAccount)
                    {
                        if (string.IsNullOrEmpty(adjustmentReason))
                        {
                            if (!bothOnSameIN.HasValue)
                                (bothOnSameIN, isOldToNew) = await _subscriptionRepository.CheckBothOnSameINAsync(sourceMsisdn, destinationMsisdn);
                            if (!bothOnSameIN.Value)
                            {
                                transferReason = await GetRelatedAdjustmentReasonAsync(totalAmount, isOldToNew.GetValueOrDefault());
                            }
                            else
                            {
                                transferReason = await GetRelatedTransferReasonAsync(totalAmount);
                            }
                        }
                    }
                    transaction.TransferReason = transferReason;
                    transaction.ReservationId = reservationCode;

                    // Execute fund transfer (exact original logic)
                    await TransferFundAsync(sourceMsisdn, destinationMsisdn, totalAmount, pin, "CreditTransferService", transferReason, !bothOnSameIN.GetValueOrDefault());
                    isAmountTransfered = true;

                    // Commit event if reserved (exact original logic)
                    if (transaction.IsFromCustomer && isEventReserved)
                    {
                        await CommitEventAsync(sourceMsisdn, destinationMsisdn, totalAmount, pin, extendedDays, reservationCode, null, "CreditTransferService");
                    }

                    // Extend account expiry (exact original logic)
                    if (_enableExtendedDays!.Value && sourceMsisdnType != SubscriptionType.Customer && destinationMsisdnType == SubscriptionType.Customer)
                    {
                        await ExtendDaysAsync(sourceMsisdn, destinationMsisdn, totalAmount, pin, extendedDays, null, "CreditTransferService");
                    }

                    // Update transaction status (exact original logic)
                    transaction.IsEventReserved = isEventReserved;
                    transaction.IsAmountTransfered = true;
                    transaction.IsEventCharged = isEventReserved;
                    transaction.IsEventCancelled = false;
                    transaction.IsExpiryExtended = true;
                    transaction.CompletedDate = DateTime.UtcNow;
                    transaction.StatusId = (byte)Domain.Entities.TransactionStatus.Succeeded;

                    var transactionId = await _transactionRepository.AddTransactionAsync(transaction);

                    // Send SMS notifications (exact original logic)
                    try
                    {
                        var aPartySMSEn = await _configRepository.GetConfigValueAsync<string>("CreditTransfer_APartySMSEn", "You have successfully transferred {0} RO to {1}.");
                        var bPartySMSEn = await _configRepository.GetConfigValueAsync<string>("CreditTransfer_BPartySMSEn", "You have received {0} RO from {1}");
                        var aPartySMSAr = await _configRepository.GetConfigValueAsync<string>("CreditTransfer_APartySMSAr", "تم تحويل {0} ر.ع بنجاح إلى الرقم {1}");
                        var bPartySMSAr = await _configRepository.GetConfigValueAsync<string>("CreditTransfer_BPartySMSAr", "لقد استلمت {0} ريال عماني من {1}");

                        await SendSMSAsync("FRiENDi", sourceMsisdn,
                                         string.Format(aPartySMSEn, totalAmount, destinationMsisdn),
                                         string.Format(aPartySMSAr, totalAmount, destinationMsisdn));

                        await SendSMSAsync("FRiENDi", destinationMsisdn,
                                         string.Format(bPartySMSEn, totalAmount, sourceMsisdn),
                                         string.Format(bPartySMSAr, totalAmount, sourceMsisdn));
                    }
                    catch (Exception smsEx)
                    {
                        _logger.LogWarning(smsEx, "Failed to send SMS notifications for transaction {TransactionId}", transactionId);
                    }

                    // Record success metrics
                    var operationDuration = (DateTime.UtcNow - operationStart).TotalSeconds;
                    _transferSuccesses.Add(1, new KeyValuePair<string, object?>("source_msisdn", sourceMsisdn));
                    _transferAmounts.Record((double)totalAmount, new KeyValuePair<string, object?>("transfer_type", "success"));
                    _transferDuration.Record(operationDuration, new KeyValuePair<string, object?>("operation", "TransferCredit"));

                    return (ErrorCodes.Success, await _errorConfigService.GetErrorMessageAsync(ErrorCodes.Success), transactionId);
                }
                catch (Exception ex)
                {
                    // Exact original rollback logic
                    if (!isAmountTransfered)
                    {
                        if (isEventReserved)
                        {
                            await CancelEventAsync(sourceMsisdn, destinationMsisdn, totalAmount, pin, extendedDays, reservationCode, "CreditTransferService");

                            transaction.IsFromCustomer = true;
                            transaction.IsEventReserved = true;
                            transaction.IsAmountTransfered = false;
                            transaction.IsEventCharged = false;
                            transaction.IsEventCancelled = true;
                            transaction.IsExpiryExtended = false;
                            transaction.StatusId = (byte)Domain.Entities.TransactionStatus.TransferFailed;
                            await _transactionRepository.AddTransactionAsync(transaction);
                        }
                        else if (sourceMsisdnType != SubscriptionType.Customer)
                        {
                            transaction.IsFromCustomer = false;
                            transaction.IsEventReserved = false;
                            transaction.IsAmountTransfered = false;
                            transaction.IsEventCharged = false;
                            transaction.IsEventCancelled = false;
                            transaction.IsExpiryExtended = false;
                            transaction.StatusId = (byte)Domain.Entities.TransactionStatus.TransferFailed;
                            await _transactionRepository.AddTransactionAsync(transaction);
                        }
                    }

                    // Record failure metrics
                    var operationDuration = (DateTime.UtcNow - operationStart).TotalSeconds;
                    _transferFailures.Add(1, new KeyValuePair<string, object?>("failure_reason", "transfer_exception"));
                    _transferDuration.Record(operationDuration, new KeyValuePair<string, object?>("result", "exception"));

                    _logger.LogError(ex, "Transfer failed for {Source} to {Destination}", sourceMsisdn, destinationMsisdn);
                    throw;
                }
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, "Exception during credit transfer from {Source} to {Destination}", sourceMsisdn, destinationMsisdn);
                return (ErrorCodes.MiscellaneousError, await _errorConfigService.GetErrorMessageAsync(ErrorCodes.MiscellaneousError), 0);
            }
        }

        /// <summary>
        /// Transfers credit with an adjustment reason
        /// Preserves exact signature and behavior from original WCF service
        /// Enhanced with OpenTelemetry activity tracking
        /// </summary>
        public async Task<(int statusCode, string statusMessage, long transactionId)> TransferCreditWithAdjustmentReasonAsync(
            string sourceMsisdn,
            string destinationMsisdn,
            int amountRiyal,
            int amountBaisa,
            string pin,
            string adjustmentReason)
        {
            return await TransferCreditInternalAsync(
                sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin, adjustmentReason);
        }

        /// <summary>
        /// Gets available denomination values for credit transfers
        /// Preserves exact signature from original WCF service
        /// Enhanced with OpenTelemetry activity tracking
        /// </summary>
        public async Task<List<decimal>> GetDenominationsAsync()
        {
            await EnsureConfigurationLoadedAsync();

            using var activity = _activitySource.StartActivity("CreditTransfer.GetDenominations");
            activity?.SetTag("operation", "GetDenominations");

            try
            {
                // Get VirginEventIds from configuration (preserving original behavior)
                var virginEventIds = await _configRepository.GetConfigValueAsync<string>("CreditTransfer_VirginEventIds", "5|1005,10|1010,15|1015,20|1020,25|1025,30|1030,35|1035,40|1040,45|1045,50|1050");

                var denominations = virginEventIds
                    .Split(',')
                    .Select(id => decimal.TryParse(id.Trim(), out var value) ? value : 0m)
                    .Where(value => value > 0)
                    .OrderBy(value => value)
                    .ToList();

                activity?.SetTag("denominations.count", denominations.Count);
                activity?.SetTag("denominations.values", string.Join(",", denominations));

                _logger.LogInformation("Retrieved {Count} denominations: {Values}",
                    denominations.Count, string.Join(", ", denominations));

                return denominations;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, "Error retrieving denominations");
                throw;
            }
        }

        /// <summary>
        /// Transfers credit without PIN for service center operations
        /// Uses default PIN for authentication, preserves original behavior
        /// Enhanced with OpenTelemetry activity tracking
        /// </summary>
        public async Task<(int statusCode, string statusMessage, long transactionId)> TransferCreditWithoutPinAsync(
            string sourceMsisdn,
            string destinationMsisdn,
            decimal amountRiyal)
        {
            using var activity = _activitySource.StartActivity("CreditTransfer.TransferWithoutPin");
            activity?.SetTag("operation", "TransferWithoutPin");
            activity?.SetTag("source.msisdn", sourceMsisdn);
            activity?.SetTag("destination.msisdn", destinationMsisdn);
            activity?.SetTag("amount.riyal", amountRiyal);
            activity?.SetTag("service.context", "customer_service");

            _logger.LogInformation("Processing customer service transfer without PIN from {Source} to {Destination}, amount: {Amount} OMR",
                sourceMsisdn, destinationMsisdn, amountRiyal);

            try
            {
                // Convert decimal to Riyal/Baisa for internal processing
                var riyal = (int)Math.Floor(amountRiyal);
                var baisa = (int)Math.Round((amountRiyal - riyal) * 1000);

                // Use default PIN for customer service transfers
                var (statusCode, statusMessage, transactionId) = await TransferCreditInternalAsync(
                    sourceMsisdn, destinationMsisdn, riyal, baisa, _defaultPin ?? "0000", null);

                activity?.SetTag("transfer.result", statusCode == 0 ? "success" : "failure");
                activity?.SetTag("transfer.status.code", statusCode);

                return (statusCode, statusMessage, transactionId);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, "Error in customer service transfer without PIN");
                return (14, await _errorConfigService.GetErrorMessageAsync(14), 0); // MiscellaneousErrorException
            }
        }

        /// <summary>
        /// Validates transfer inputs without performing the actual transfer
        /// Returns response with status code and message
        /// Enhanced with OpenTelemetry activity tracking
        /// </summary>
        public async Task<(int statusCode, string statusMessage)> ValidateTransferInputsAsync(
            string sourceMsisdn,
            string destinationMsisdn,
            decimal amountRiyal)
        {
            using var activity = _activitySource.StartActivity("CreditTransfer.ValidateInputs");
            activity?.SetTag("operation", "ValidateInputs");
            activity?.SetTag("source.msisdn", sourceMsisdn);
            activity?.SetTag("destination.msisdn", destinationMsisdn);
            activity?.SetTag("amount.riyal", amountRiyal);

            _logger.LogDebug("Validating transfer inputs from {Source} to {Destination}, amount: {Amount} OMR",
                sourceMsisdn, destinationMsisdn, amountRiyal);

            try
            {
                var (isValid, validationCode, validationMessage, sourceMsisdnType, destinationMsisdnType, sourceSubscriptionTypeConfig) = await ValidateTransferInputsInternalAsync(
                    sourceMsisdn, destinationMsisdn, amountRiyal);

                activity?.SetTag("validation.result", isValid ? "success" : "failure");
                activity?.SetTag("validation.code", validationCode);

                if (!isValid)
                {
                    // Record validation error metric
                    _validationErrors.Add(1, new KeyValuePair<string, object?>("error_code", validationCode),
                                             new KeyValuePair<string, object?>("error_type", "input_validation"));
                }

                return (validationCode, validationMessage);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, "Error validating transfer inputs");
                return (14, await _errorConfigService.GetErrorMessageAsync(14)); // MiscellaneousErrorException
            }
        }

        #region Private Methods - Preserving Original Business Logic

        /// <summary>
        /// Validates transfer inputs using exact logic from original ValidateTransferInputs method
        /// </summary>
        protected virtual async Task<
            (bool isValid, int validationCode, string validationMessage,
            SubscriptionType? sourceMsisdnType, SubscriptionType? destinationMsisdnType,
            TransferConfig? sourceSubscriptionTypeConfig)>
            ValidateTransferInputsInternalAsync(string sourceMsisdn, string destinationMsisdn, decimal amount)
        {
            await EnsureConfigurationLoadedAsync();

            SubscriptionType? sourceMsisdnType = null;
            SubscriptionType? destinationMsisdnType = null;
            TransferConfig? sourceSubscriptionTypeConfig = null;

            try
            {
                // Parameter validations (exact copy from original)
                if (!long.TryParse(sourceMsisdn, out _) || string.IsNullOrEmpty(sourceMsisdn) || !_msisdnLength.Contains(sourceMsisdn.Length))
                {
                    return (false, ErrorCodes.InvalidSourcePhone, await _errorConfigService.GetErrorMessageAsync(ErrorCodes.InvalidSourcePhone), sourceMsisdnType, destinationMsisdnType, sourceSubscriptionTypeConfig);
                }

                if (!long.TryParse(destinationMsisdn, out _) || string.IsNullOrEmpty(destinationMsisdn) || !_msisdnLength.Contains(destinationMsisdn.Length))
                {
                    return (false, ErrorCodes.InvalidDestinationPhone, await _errorConfigService.GetErrorMessageAsync(ErrorCodes.InvalidDestinationPhone), sourceMsisdnType, destinationMsisdnType, sourceSubscriptionTypeConfig);
                }

                // Check if source and destination numbers are the same
                if (sourceMsisdn == destinationMsisdn)
                {
                    return (false, ErrorCodes.SourceDestinationSame, await _errorConfigService.GetErrorMessageAsync(ErrorCodes.SourceDestinationSame), sourceMsisdnType, destinationMsisdnType, sourceSubscriptionTypeConfig);
                }

                // Get nobill subscription types (original logic)
                string sourceNobillSubscriptionType;
                string destinationNobillSubscriptionType;

                try
                {
                    sourceNobillSubscriptionType = await _subscriptionRepository.GetNobillSubscriptionTypeAsync(sourceMsisdn);
                }
                catch (Exception)
                {
                    return (false, ErrorCodes.InvalidSourcePhone, await _errorConfigService.GetErrorMessageAsync(ErrorCodes.InvalidSourcePhone), sourceMsisdnType, destinationMsisdnType, sourceSubscriptionTypeConfig);
                }

                try
                {
                    destinationNobillSubscriptionType = await _subscriptionRepository.GetNobillSubscriptionTypeAsync(destinationMsisdn);
                }
                catch (Exception)
                {
                    return (false, ErrorCodes.InvalidDestinationPhone, await _errorConfigService.GetErrorMessageAsync(ErrorCodes.InvalidDestinationPhone), sourceMsisdnType, destinationMsisdnType, sourceSubscriptionTypeConfig);
                }

                try
                {
                    sourceSubscriptionTypeConfig = await _transferConfigRepository.GetByNobillSubscriptionTypeAsync(sourceNobillSubscriptionType);
                    if (sourceSubscriptionTypeConfig == null)
                    {
                        return (false, ErrorCodes.SourcePhoneNotFound, await _errorConfigService.GetErrorMessageAsync(ErrorCodes.SourcePhoneNotFound), sourceMsisdnType, destinationMsisdnType, sourceSubscriptionTypeConfig);
                    }
                    sourceMsisdnType = await _subscriptionRepository.GetAccountTypeAsync(sourceSubscriptionTypeConfig?.SubscriptionType ?? "");

                }
                catch (Exception)
                {
                    return (false, ErrorCodes.SourcePhoneNotFound, await _errorConfigService.GetErrorMessageAsync(ErrorCodes.SourcePhoneNotFound), sourceMsisdnType, destinationMsisdnType, sourceSubscriptionTypeConfig);
                }

                try
                {
                    TransferConfig? destinationSubscriptionTypeConfig = await _transferConfigRepository.GetByNobillSubscriptionTypeAsync(destinationNobillSubscriptionType);
                    if (destinationSubscriptionTypeConfig == null)
                    {
                        return (false, ErrorCodes.DestinationPhoneNotFound, await _errorConfigService.GetErrorMessageAsync(ErrorCodes.DestinationPhoneNotFound), sourceMsisdnType, destinationMsisdnType, sourceSubscriptionTypeConfig);

                    }
                    destinationMsisdnType = await _subscriptionRepository.GetAccountTypeAsync(destinationSubscriptionTypeConfig?.SubscriptionType ?? "");
                }
                catch (Exception)
                {
                    return (false, ErrorCodes.DestinationPhoneNotFound, await _errorConfigService.GetErrorMessageAsync(ErrorCodes.DestinationPhoneNotFound), sourceMsisdnType, destinationMsisdnType, sourceSubscriptionTypeConfig);
                }

                // ===== CONFIGURABLE BUSINESS RULES VALIDATION =====
                // Replace hard-coded business logic with database-driven configurable rules
                var country = await _configRepository.GetConfigValueAsync<string>("Country", "OM"); // Default to Oman

                // Evaluate configurable business rules using simplified approach
                var (isTransferAllowed, ruleErrorCode, ruleErrorMessage) = await _transferRulesService.EvaluateTransferRuleAsync(
                    country,
                    sourceMsisdnType!.Value,
                    destinationMsisdnType!.Value);

                if (!isTransferAllowed)
                {
                    _logger.LogWarning("Transfer denied by business rule: {Country} {Source} -> {Destination}, ErrorCode: {ErrorCode}",
                        country, sourceMsisdnType, destinationMsisdnType, ruleErrorCode);

                    return (false, ruleErrorCode, ruleErrorMessage, sourceMsisdnType, destinationMsisdnType, sourceSubscriptionTypeConfig);
                }

                _logger.LogDebug("Transfer allowed by business rules: {Country} {Source} -> {Destination}",
                    country, sourceMsisdnType, destinationMsisdnType);

                // Get source balance for percentage validation (original logic)

                (int responseCode, string? itemValue) = await _nobillCallsService.GetAccountValueAsync(sourceMsisdn, AccountItem.balance);
                decimal sourceMsisdnBalance = 0;
                if (!string.IsNullOrEmpty(itemValue) && !decimal.TryParse(itemValue, out sourceMsisdnBalance))
                {
                    sourceMsisdnBalance = 0;
                }

                // MaximumPercentageAmount validation (exact original logic from ValidateTransferInputs)
                if (_maximumPercentageAmount!.Value != 1)
                {
                    if (sourceMsisdnBalance - amount < (sourceMsisdnBalance / _maximumPercentageAmount.Value))
                    {
                        return (false, ErrorCodes.RemainingBalanceHalf, await _errorConfigService.GetErrorMessageAsync(ErrorCodes.RemainingBalanceHalf), sourceMsisdnType, destinationMsisdnType, sourceSubscriptionTypeConfig);
                    }
                }

                // Check subscription block status (original logic)
                if (await _subscriptionRepository.GetSubscriptionBlockStatusAsync(sourceMsisdn) != SubscriptionBlockStatus.NO_BLOCK)
                {
                    return (false, ErrorCodes.MiscellaneousError, await _errorConfigService.GetErrorMessageAsync(ErrorCodes.MiscellaneousError), sourceMsisdnType, destinationMsisdnType, sourceSubscriptionTypeConfig);
                }

                // Check destination subscription status (original logic)
                if (await _subscriptionRepository.GetSubscriptionStatusAsync(destinationMsisdn) == SubscriptionStatus.ACTIVE_BEFORE_FIRST_USE)
                {
                    return (false, ErrorCodes.InvalidDestinationPhone, await _errorConfigService.GetErrorMessageAsync(ErrorCodes.InvalidDestinationPhone), sourceMsisdnType, destinationMsisdnType, sourceSubscriptionTypeConfig);
                }




                // Get maximum transfer amount by service name (original logic)
                decimal maxTransferAmount = await GetAccountMaxTransferAmountByServiceNameAsync(
                    sourceMsisdn, sourceSubscriptionTypeConfig!.CreditTransferCustomerService ?? "");

                if (amount > maxTransferAmount)
                {
                    return (false, ErrorCodes.TransferAmountAboveMax, await _errorConfigService.GetErrorMessageAsync(ErrorCodes.TransferAmountAboveMax), sourceMsisdnType, destinationMsisdnType, sourceSubscriptionTypeConfig);
                }

                // Check minimum transfer amount (original logic)
                if (sourceSubscriptionTypeConfig.MinTransferAmount.HasValue && amount < sourceSubscriptionTypeConfig.MinTransferAmount.Value)
                {
                    return (false, ErrorCodes.TransferAmountBelowMin, await _errorConfigService.GetErrorMessageAsync(ErrorCodes.TransferAmountBelowMin), sourceMsisdnType, destinationMsisdnType, sourceSubscriptionTypeConfig);
                }

                // Check daily transfer count limit (original logic)
                // Check daily transfer count limit (original logic)
                if (sourceSubscriptionTypeConfig.DailyTransferCountLimit.HasValue)
                {
                    int numberOfTransactions = await _transactionRepository.GetDailyTransferCountAsync(sourceMsisdn);
                    if (numberOfTransactions != 0 && sourceSubscriptionTypeConfig.DailyTransferCountLimit.Value <= numberOfTransactions)
                    {
                        return (false, ErrorCodes.ExceedsMaxPerDay, await _errorConfigService.GetErrorMessageAsync(ErrorCodes.ExceedsMaxPerDay), sourceMsisdnType, destinationMsisdnType, sourceSubscriptionTypeConfig);
                    }
                }

                // Check daily transfer cap limit (new validation) 
                if (sourceSubscriptionTypeConfig.DailyTransferCapLimit.HasValue)
                {
                    decimal totalTransferedAmount = await _transactionRepository.GetDailyTransferAmount(sourceMsisdn);
                    if (totalTransferedAmount != 0 && sourceSubscriptionTypeConfig.DailyTransferCapLimit.Value <= totalTransferedAmount)
                    {
                        return (false, ErrorCodes.ExceedsMaxCapPerDay, await _errorConfigService.GetErrorMessageAsync(ErrorCodes.ExceedsMaxCapPerDay), sourceMsisdnType, destinationMsisdnType, sourceSubscriptionTypeConfig);
                    }
                }

                // Check the balance after transfer credit action for customers only (original logic)
                if (sourceSubscriptionTypeConfig.MinPostTransferBalance.HasValue &&
                    (sourceMsisdnBalance - amount < sourceSubscriptionTypeConfig.MinPostTransferBalance.Value))
                {
                    return (false, ErrorCodes.RemainingBalance, await _errorConfigService.GetErrorMessageAsync(ErrorCodes.RemainingBalance), sourceMsisdnType, destinationMsisdnType, sourceSubscriptionTypeConfig);
                }

                return (true, ErrorCodes.Success, await _errorConfigService.GetErrorMessageAsync(ErrorCodes.Success), sourceMsisdnType, destinationMsisdnType, sourceSubscriptionTypeConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating transfer inputs");
                return (false, ErrorCodes.MiscellaneousError, await _errorConfigService.GetErrorMessageAsync(ErrorCodes.MiscellaneousError), sourceMsisdnType, destinationMsisdnType, sourceSubscriptionTypeConfig);
            }
        }

        // Helper methods for transaction management and business logic

        /// <summary>
        /// Gets days to extend based on amount (exact original logic)
        /// </summary>
        private async Task<int> GetDaysToExtendAsync(decimal amount)
        {
            var amountRanges = await _configRepository.GetConfigValueAsync<string>("CreditTransfer_AmountRanges", "0.1;0.5;1;3;5;10;50;100");
            var extendedDaysTypes = await _configRepository.GetConfigValueAsync<string>("CreditTransfer_ExtendedDaysTypes", "0;0;0;0;0;0;0");

            var amountRangeArray = amountRanges.Split(';');
            var extendedDaysArray = extendedDaysTypes.Split(';');

            if (amountRangeArray.Length > 0 || extendedDaysArray.Length > 0)
            {
                try
                {
                    if (amount >= Convert.ToDecimal(amountRangeArray[amountRangeArray.Length - 1]))
                    {
                        return Convert.ToInt32(extendedDaysArray[extendedDaysArray.Length - 1]);
                    }

                    for (int i = 0; i < amountRangeArray.Length - 1; i++)
                    {
                        if (amount >= Convert.ToDecimal(amountRangeArray[i]) && amount < Convert.ToDecimal(amountRangeArray[i + 1]))
                        {
                            return Convert.ToInt32(extendedDaysArray[i]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating extension days for amount {Amount}", amount);
                    await ThrowCreditTransferExceptionAsync<ConfigurationErrorException>();
                }
            }
            else
            {
                await ThrowCreditTransferExceptionAsync<ConfigurationErrorException>();
            }

            return 0;
        }


        /// <summary>
        /// Gets related transfer reason based on amount (exact original logic)
        /// </summary>
        private async Task<string> GetRelatedTransferReasonAsync(decimal amount)
        {
            var amountRanges = await _configRepository.GetConfigValueAsync<string>("CreditTransfer_AmountRanges", "0.1;0.5;1;3;5;10;50;100");
            var transferReasons = await _configRepository.GetConfigValueAsync<string>("CreditTransfer_TransferMonyReasonClassification", "POS_Transfer_0.500;POS_Transfer_0.500;POS_Transfer_1;POS_Transfer_3;POS_Transfer_5;POS_Transfer_10;POS_Transfer_50;POS_Transfer_100");

            var amountRangeArray = amountRanges.Split(';');
            var transferReasonArray = transferReasons.Split(';');

            if (amountRangeArray.Length > 0 || transferReasonArray.Length > 0)
            {
                try
                {
                    if (amount >= Convert.ToDecimal(amountRangeArray[amountRangeArray.Length - 1]))
                    {
                        return transferReasonArray[transferReasonArray.Length - 1];
                    }

                    for (int i = 0; i < amountRangeArray.Length - 1; i++)
                    {
                        if (amount >= Convert.ToDecimal(amountRangeArray[i]) && amount < Convert.ToDecimal(amountRangeArray[i + 1]))
                        {
                            return transferReasonArray[i];
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error determining transfer reason for amount {Amount}", amount);
                    await ThrowCreditTransferExceptionAsync<ConfigurationErrorException>();
                }
            }

            return _customerToCustomerTransferMoneyReason ?? "Credit transfer";
        }

        /// <summary>
        /// Gets related adjustment reason for old/new to new transfers (exact original logic)
        /// </summary>
        private async Task<string> GetRelatedAdjustmentReasonAsync(decimal amount, bool isOldToNew)
        {
            var amountRanges = await _configRepository.GetConfigValueAsync<string>("CreditTransfer_AmountRanges", "0.1;0.5;1;3;5;10;50;100");
            string adjustmentReasons;
            if (isOldToNew)
            {
                adjustmentReasons = await _configRepository.GetConfigValueAsync<string>("CreditTransfer_AdjustmentReasonClassificationFromOldToNew", "local_credit_transfer_from_oldIN_to_newIN");
            }
            else
            {
                adjustmentReasons = await _configRepository.GetConfigValueAsync<string>("CreditTransfer_AdjustmentReasonClassificationFromNewToOld", "local_credit_transfer_from_newIN_to_oldIN");
            }


            var amountRangeArray = amountRanges.Split(';');
            var adjustmentReasonArray = adjustmentReasons.Split(';');

            if (amountRangeArray.Length > 0 && adjustmentReasonArray.Length > 0)
            {
                try
                {
                    if (amount >= Convert.ToDecimal(amountRangeArray[amountRangeArray.Length - 1]))
                    {
                        return adjustmentReasonArray[adjustmentReasonArray.Length - 1];
                    }

                    for (int i = 0; i < amountRangeArray.Length - 1; i++)
                    {
                        if (amount >= Convert.ToDecimal(amountRangeArray[i]) && amount < Convert.ToDecimal(amountRangeArray[i + 1]))
                        {
                            return adjustmentReasonArray[i];
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error determining old-to-new adjustment reason for amount {Amount}", amount);
                    await ThrowCreditTransferExceptionAsync<ConfigurationErrorException>();
                }
            }

            return await _configRepository.GetConfigValueAsync<string>("CreditTransfer_CustomerToCustomerAdjustmentReasonOldToNew", "local_credit_transfer_from_oldIN_to_newIN");
        }


        private async Task<int> ReserveEventAsync(string sourceMsisdn, string destinationMsisdn, decimal amount,
                                                string pin, int numberOfDays, int eventId, string userName)
        {
            // Original logic from ReserveEvent method
            try
            {
                _externalServiceCalls.Add(1, new KeyValuePair<string, object?>("service", "NobillCalls"),
                                             new KeyValuePair<string, object?>("method", "ReserveEvent"));

                if (eventId == 0)
                    return -1;

                var responseCode = await _nobillCallsService.ReserveEventAsync(sourceMsisdn, eventId);

                if (responseCode.responseCode == 3)
                {
                    await ThrowCreditTransferExceptionAsync<MiscellaneousErrorException>();
                }
                if (responseCode.responseCode == 5)
                {
                    await ThrowCreditTransferExceptionAsync<InsuffientCreditException>();
                }
                if (responseCode.responseCode != 0)
                {
                    await ThrowCreditTransferExceptionAsync<MiscellaneousErrorException>();
                }

                return responseCode.reservationCode;
            }
            catch (Exception)
            {
                var transaction = new Domain.Entities.Transaction
                {
                    SourceMsisdn = sourceMsisdn,
                    DestinationMsisdn = destinationMsisdn,
                    Amount = amount,
                    PIN = pin,
                    IsFromCustomer = true,
                    IsEventReserved = false,
                    IsAmountTransfered = false,
                    IsEventCharged = false,
                    IsEventCancelled = false,
                    IsExpiryExtended = false,
                    ExtensionDays = numberOfDays,
                    ReservationId = -1,
                    StatusId = (byte)Domain.Entities.TransactionStatus.TransferFailed,
                    CreatedBy = userName,
                    CreatedDate = DateTime.UtcNow
                };

                await _transactionRepository.AddTransactionAsync(transaction);
                throw;
            }
        }

        private async Task CommitEventAsync(string sourceMsisdn, string destinationMsisdn, decimal amount,
                                          string pin, int numberOfDays, int reservationCode, long? transactionId, string userName)
        {
            // Original logic from CommitEvent method
            try
            {
                _externalServiceCalls.Add(1, new KeyValuePair<string, object?>("service", "NobillCalls"),
                                             new KeyValuePair<string, object?>("method", "ChargeReservedEvent"));

                if (reservationCode <= 0)
                    return;

                var responseCode = await _nobillCallsService.ChargeReservedEventAsync(sourceMsisdn, reservationCode);

                if (responseCode == 3)
                {
                    await ThrowCreditTransferExceptionAsync<MiscellaneousErrorException>();
                }
                // Nobill Auto Cancelled the Reserved Event
                if (responseCode == 6)
                {
                    await ThrowCreditTransferExceptionAsync<ExpiredReservationCodeException>();
                }
                if (responseCode != 0)
                {
                    await ThrowCreditTransferExceptionAsync<MiscellaneousErrorException>();
                }
            }
            catch (Exception)
            {
                if (!transactionId.HasValue)
                {
                    var transaction = new Domain.Entities.Transaction
                    {
                        SourceMsisdn = sourceMsisdn,
                        DestinationMsisdn = destinationMsisdn,
                        Amount = amount,
                        PIN = pin,
                        IsFromCustomer = true,
                        IsEventReserved = true,
                        IsAmountTransfered = true,
                        IsEventCharged = false,
                        IsEventCancelled = false,
                        IsExpiryExtended = false,
                        ExtensionDays = numberOfDays,
                        ReservationId = reservationCode,
                        StatusId = (byte)Domain.Entities.TransactionStatus.TransferFailed,
                        CreatedBy = userName,
                        CreatedDate = DateTime.UtcNow
                    };

                    await _transactionRepository.AddTransactionAsync(transaction);
                }
                throw;
            }
        }

        private async Task CancelEventAsync(string sourceMsisdn, string destinationMsisdn, decimal amount,
                                          string pin, int numberOfDays, int reservationCode, string userName)
        {
            // Original logic from CancelEvent method
            try
            {
                _externalServiceCalls.Add(1, new KeyValuePair<string, object?>("service", "NobillCalls"),
                                             new KeyValuePair<string, object?>("method", "CancelReservation"));

                var responseCode = await _nobillCallsService.CancelReservationAsync(sourceMsisdn, reservationCode);

                if (responseCode.responseCode == 3)
                {
                    await ThrowCreditTransferExceptionAsync<MiscellaneousErrorException>();
                }
                if (responseCode.responseCode != 0)
                {
                    await ThrowCreditTransferExceptionAsync<MiscellaneousErrorException>();
                }
            }
            catch (Exception)
            {
                var transaction = new Domain.Entities.Transaction
                {
                    SourceMsisdn = sourceMsisdn,
                    DestinationMsisdn = destinationMsisdn,
                    Amount = amount,
                    PIN = pin,
                    IsFromCustomer = true,
                    IsEventReserved = true,
                    IsAmountTransfered = false,
                    IsEventCharged = false,
                    IsEventCancelled = false,
                    IsExpiryExtended = false,
                    ExtensionDays = numberOfDays,
                    ReservationId = reservationCode,
                    StatusId = (byte)Domain.Entities.TransactionStatus.TransferFailed,
                    CreatedBy = userName,
                    CreatedDate = DateTime.UtcNow
                };

                await _transactionRepository.AddTransactionAsync(transaction);
                throw;
            }
        }

        private async Task TransferFundAsync(string sourceMsisdn, string destinationMsisdn, decimal amount,
                                           string pin, string userName, string transferReason, bool onDifferentINs)
        {
            // Original logic from TransferFund method
            try
            {
                _externalServiceCalls.Add(1, new KeyValuePair<string, object?>("service", "NobillCalls"),
                                             new KeyValuePair<string, object?>("method", onDifferentINs ? "AdjustAccountByReason" : "TransferMoney"));

                int responseCode = 0;
                var adjustmentType = await _configRepository.GetConfigValueAsync<string>("CreditTransfer_AdjustmentType", "account_adjustment_action") ?? "account_adjustment_action";

                if (onDifferentINs)
                {
                    // Debit source account
                    responseCode = await _nobillCallsService.AdjustAccountByReasonAsync(
                        sourceMsisdn,
                        -amount,
                        Guid.NewGuid().ToString(),
                        transferReason,
                        adjustmentType,
                        $"Fund transfer detail: {amount} OMR from phoneno {sourceMsisdn} to phoneno {destinationMsisdn}");

                    if (responseCode == 0)
                    {
                        // Credit destination account
                        responseCode = await _nobillCallsService.AdjustAccountByReasonAsync(
                            destinationMsisdn,
                            amount,
                            Guid.NewGuid().ToString(),
                            transferReason,
                            adjustmentType,
                            $"Fund transfer detail: {amount} OMR from phoneno {sourceMsisdn} to phoneno {destinationMsisdn}");

                        if (responseCode != 0)
                        {
                            // Rollback source account
                            await _nobillCallsService.AdjustAccountByReasonAsync(
                                sourceMsisdn,
                                amount,
                                Guid.NewGuid().ToString(),
                                transferReason,
                                adjustmentType,
                                $"Rollback Fund transfer detail: {amount} OMR from phoneno {sourceMsisdn} to phoneno {destinationMsisdn}");
                        }
                    }
                }
                else
                {
                    responseCode = await _nobillCallsService.TransferMoneyAsync(
                        sourceMsisdn,
                        destinationMsisdn,
                        amount,
                        transferReason,
                        userName ?? "System",
                        $"Fund transfer detail: {amount} OMR from phoneno {sourceMsisdn} to phoneno {destinationMsisdn}");
                }

                // Handle response codes (exact original logic)
                if (responseCode == 3)
                {
                    await ThrowCreditTransferExceptionAsync<MiscellaneousErrorException>();
                }
                else if (responseCode == 7)
                {
                    await ThrowCreditTransferExceptionAsync<SourcePhoneNumberNotFoundException>();
                }
                else if (responseCode == 8)
                {
                    await ThrowCreditTransferExceptionAsync<SourceAndDestinationSameException>();
                }
                else if (responseCode == 9)
                {
                    await ThrowCreditTransferExceptionAsync<DestinationPhoneNumberNotFoundException>();
                }
                else if (responseCode == 10 || responseCode == 16)
                {
                    await ThrowCreditTransferExceptionAsync<InsuffientCreditException>();
                }
                else if (responseCode != 0)
                {
                    await ThrowCreditTransferExceptionAsync<MiscellaneousErrorException>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fund transfer failed for {Source} to {Destination}, amount: {Amount}",
                    sourceMsisdn, destinationMsisdn, amount);
                throw;
            }
        }

        private async Task ExtendDaysAsync(string sourceMsisdn, string destinationMsisdn, decimal amount,
                                         string pin, int numberOfDays, long? transactionId, string userName)
        {
            // Original logic from ExtendDays method
            try
            {
                if (numberOfDays <= 0)
                    return;

                _externalServiceCalls.Add(1, new KeyValuePair<string, object?>("service", "NobillCalls"),
                                             new KeyValuePair<string, object?>("method", "ExtendSubscriptionExpiry"));

                var responseCode = await _nobillCallsService.ExtendSubscriptionExpiryAsync(destinationMsisdn, numberOfDays);

                switch (responseCode)
                {
                    case 3:
                        await ThrowCreditTransferExceptionAsync<MiscellaneousErrorException>();
                        break;
                    case 7:
                        await ThrowCreditTransferExceptionAsync<DestinationPhoneNumberNotFoundException>();
                        break;
                    case 13:
                        await ThrowCreditTransferExceptionAsync<ConcurrentUpdateDetectedException>();
                        break;
                    default:
                        if (responseCode != 0)
                        {
                            await ThrowCreditTransferExceptionAsync<MiscellaneousErrorException>();
                        }
                        break;
                }
            }
            catch (Exception)
            {
                if (!transactionId.HasValue)
                {
                    var transaction = new Domain.Entities.Transaction
                    {
                        SourceMsisdn = sourceMsisdn,
                        DestinationMsisdn = destinationMsisdn,
                        Amount = amount,
                        PIN = pin,
                        IsFromCustomer = true,
                        IsEventReserved = true,
                        IsAmountTransfered = true,
                        IsEventCharged = true,
                        IsEventCancelled = false,
                        IsExpiryExtended = false,
                        ExtensionDays = numberOfDays,
                        ReservationId = 0,
                        StatusId = (byte)Domain.Entities.TransactionStatus.TransferFailed,
                        CreatedBy = userName,
                        CreatedDate = DateTime.UtcNow
                    };

                    await _transactionRepository.AddTransactionAsync(transaction);
                }
                throw;
            }
        }

        private async Task SendSMSAsync(string from, string phoneNo, string messageEn, string messageAr)
        {
            // Original logic from SendSMS method
            try
            {
                _externalServiceCalls.Add(1, new KeyValuePair<string, object?>("service", "NobillCalls"),
                                             new KeyValuePair<string, object?>("method", "SendHTTPSMS"));

                var localeResponse = await _nobillCallsService.GetSubscriptionValueAsync(phoneNo, SubscriptionItem.locale);

                if (localeResponse.responseCode == 0)
                {
                    var locale = localeResponse.itemValue?.ToLower() ?? "";
                    int smsResponseCode;

                    if (locale.Contains("ar"))
                    {
                        smsResponseCode = await _nobillCallsService.SendHTTPSMSAsync(from, phoneNo, messageAr, true);
                    }
                    else
                    {
                        smsResponseCode = await _nobillCallsService.SendHTTPSMSAsync(from, phoneNo, messageEn, false);
                    }

                    _logger.LogInformation("SMS sent from {From} to {PhoneNo}: ResponseCode={ResponseCode}, Locale={Locale}",
                                         from, phoneNo, smsResponseCode, locale);
                }
                else
                {
                    _logger.LogWarning("Failed to get locale for {PhoneNo}, ResponseCode={ResponseCode}",
                                     phoneNo, localeResponse.responseCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS from {From} to {PhoneNo}", from, phoneNo);
                await ThrowCreditTransferExceptionAsync<SMSFailureException>();
            }
        }

        /// <summary>
        /// Gets account PIN by service name (exact original logic)
        /// </summary>
        private async Task<string> GetAccountPinByServiceNameAsync(string msisdn, string serviceName)
        {
            try
            {
                var responseCode = await _nobillCallsService.GetCreditTransferValueByServiceNameAsync(msisdn, CreditTransferItem.p_pin, serviceName);

                if (responseCode.responseCode == 3)
                {
                    await ThrowCreditTransferExceptionAsync<MiscellaneousErrorException>();
                }
                else if (responseCode.responseCode == 7)
                {
                    await ThrowCreditTransferExceptionAsync<SourcePhoneNumberNotFoundException>();
                }
                else if (responseCode.responseCode == 9)
                {
                    await ThrowCreditTransferExceptionAsync<PropertyNotFoundException>();
                }
                else if (responseCode.responseCode != 0)
                {
                    await ThrowCreditTransferExceptionAsync<MiscellaneousErrorException>();
                }

                return responseCode.value ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account PIN for MSISDN {Msisdn} with service {ServiceName}", msisdn, serviceName);
                throw;
            }
        }

        /// <summary>
        /// Gets account max transfer amount by service name (exact original logic)
        /// </summary>
        private async Task<decimal> GetAccountMaxTransferAmountByServiceNameAsync(string msisdn, string serviceName)
        {
            try
            {
                var responseCode = await _nobillCallsService.GetCreditTransferValueByServiceNameAsync(msisdn, CreditTransferItem.p_maxamount, serviceName);

                if (responseCode.responseCode == 3)
                {
                    await ThrowCreditTransferExceptionAsync<MiscellaneousErrorException>();
                }
                else if (responseCode.responseCode == 7)
                {
                    await ThrowCreditTransferExceptionAsync<SourcePhoneNumberNotFoundException>();
                }
                else if (responseCode.responseCode != 0)
                {
                    await ThrowCreditTransferExceptionAsync<MiscellaneousErrorException>();
                }

                if (string.IsNullOrEmpty(responseCode.value))
                {
                    return 50; // Default value from original
                }

                return Convert.ToDecimal(responseCode.value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting max transfer amount for MSISDN {Msisdn} with service {ServiceName}", msisdn, serviceName);
                throw;
            }
        }

        /// <summary>
        /// Performs comprehensive system health check including all dependencies
        /// Replicates functionality from health-check.ps1 script
        /// </summary>
        public async Task<ComprehensiveHealthResponse> GetSystemHealthAsync()
        {
            var startTime = DateTime.UtcNow;
            using var activity = _activitySource.StartActivity("CreditTransfer.SystemHealthCheck");
            activity?.SetTag("operation", "ComprehensiveHealthCheck");

            var response = new ComprehensiveHealthResponse
            {
                Timestamp = startTime,
                Components = new List<ComponentHealth>()
            };

            _logger.LogInformation("Starting comprehensive system health check");

            try
            {
                // 1. Database Connectivity Check
                await CheckDatabaseConnectivityAsync(response);

                // 2. Redis Connectivity Check  
                await CheckRedisConnectivityAsync(response);

                // 3. NoBill Service Connectivity Check
                await CheckNobillServiceConnectivityAsync(response);

                // 4. Configuration Validation Check
                await CheckConfigurationValidityAsync(response);

                // 5. External Dependencies Check
                await CheckExternalDependenciesAsync(response);

                // Calculate summary statistics
                CalculateHealthSummary(response);

                // Determine overall status
                DetermineOverallStatus(response);

                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                activity?.SetTag("health.check.duration.ms", duration);
                activity?.SetTag("health.overall.status", response.OverallStatus);

                _logger.LogInformation("Comprehensive health check completed in {Duration}ms with status: {Status}",
                    duration, response.OverallStatus);

                return response;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex, "Error during comprehensive health check");

                response.OverallStatus = "CRITICAL_ERROR";
                response.ErrorDetails = ex.Message;
                response.Summary = new HealthSummary
                {
                    TotalComponents = response.Components.Count,
                    UnhealthyComponents = response.Components.Count,
                    OverallHealthPercentage = "0%"
                };

                return response;
            }
        }

        private async Task CheckDatabaseConnectivityAsync(ComprehensiveHealthResponse response)
        {
            var startTime = DateTime.UtcNow;
            var component = new ComponentHealth
            {
                Component = "SQL_Server_Database",
                Details = new Dictionary<string, object>()
            };

            try
            {
                // Use the comprehensive database health check method
                var (isHealthy, statusMessage, details) = await _configRepository.PerformHealthCheckAsync();
                
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                component.Status = isHealthy ? "HEALTHY" : "UNHEALTHY";
                component.StatusMessage = statusMessage;
                component.ResponseTimeMs = responseTime;
                component.ErrorCode = isHealthy ? null : "SQL_SYSTEM_ERROR";
                
                // Merge all the detailed health check results
                foreach (var detail in details)
                {
                    component.Details[detail.Key] = detail.Value;
                }

                if (isHealthy)
                {
                    _logger.LogDebug("Database comprehensive health check passed in {ResponseTime}ms", responseTime);
                }
                else
                {
                    _logger.LogError("Database comprehensive health check failed: {StatusMessage}", statusMessage);
                }
            }
            catch (Exception ex)
            {
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                component.Status = "UNHEALTHY";
                component.StatusMessage = "Database health check failed";
                component.ResponseTimeMs = responseTime;
                component.ErrorCode = "SQL_CONNECTION_ERROR";
                component.Details["server"] = "10.1.133.31 (External SQL Server)";
                component.Details["error"] = ex.Message;
                component.Details["error_type"] = "Network/Connection/System";

                _logger.LogError(ex, "Database connectivity check failed");
            }

            response.Components.Add(component);
        }

        private async Task CheckRedisConnectivityAsync(ComprehensiveHealthResponse response)
        {
            var startTime = DateTime.UtcNow;
            var component = new ComponentHealth
            {
                Component = "Redis_Cache",
                Details = new Dictionary<string, object>()
            };

            try
            {
                // Test Redis connectivity by trying to get a cached configuration value
                var testKey = $"NobillCalls_ServiceUrl";
                var testValue = "health_test";
                
                // This will test Redis connectivity indirectly through the repository
                await _configRepository.GetConfigValueAsync<string>(testKey, testValue);

                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                component.Status = "HEALTHY";
                component.StatusMessage = "Redis cache accessible";
                component.ResponseTimeMs = responseTime;
                component.Details["cache_type"] = "Redis";
                component.Details["test_operation"] = "Cache lookup";

                _logger.LogDebug("Redis connectivity check passed in {ResponseTime}ms", responseTime);
            }
            catch (Exception ex)
            {
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                component.Status = "DEGRADED";
                component.StatusMessage = "Redis cache issues detected";
                component.ResponseTimeMs = responseTime;
                component.ErrorCode = "REDIS_CONNECTION_ERROR";
                component.Details["cache_type"] = "Redis";
                component.Details["error"] = ex.Message;
                component.Details["impact"] = "Performance degradation, fallback to database";

                _logger.LogWarning(ex, "Redis connectivity check failed - operating in degraded mode");
            }

            response.Components.Add(component);
        }

        private async Task CheckNobillServiceConnectivityAsync(ComprehensiveHealthResponse response)
        {
            var startTime = DateTime.UtcNow;
            var component = new ComponentHealth
            {
                Component = "NoBill_Service",
                Details = new Dictionary<string, object>()
            };

            try
            {
                // Test NoBill service connectivity with a simple test number
                var testMsisdn = await _configRepository.GetConfigValueAsync<string>("HealthCheck_TestMsisdn", "96898455550");
                
                var result = await _nobillCallsService.GetSubscriptionValueAsync(testMsisdn, SubscriptionItem.subscriptiontype);
                
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                if (result.responseCode == 0)
                {
                    component.Status = "HEALTHY";
                    component.StatusMessage = "NoBill service responsive";
                    component.Details["test_msisdn"] = testMsisdn;
                    component.Details["subscription_type"] = result.itemValue ?? "unknown";
                }
                else if (result.responseCode == 7) // Phone not found - service is working but test number invalid
                {
                    component.Status = "HEALTHY";
                    component.StatusMessage = "NoBill service responsive (test number not found)";
                    component.Details["test_msisdn"] = testMsisdn;
                    component.Details["note"] = "Service working, test number may be invalid";
                }
                else
                {
                    component.Status = "DEGRADED";
                    component.StatusMessage = $"NoBill service returned error code: {result.responseCode}";
                    component.ErrorCode = $"NOBILL_ERROR_{result.responseCode}";
                    component.Details["test_msisdn"] = testMsisdn;
                    component.Details["response_code"] = result.responseCode;
                }

                component.ResponseTimeMs = responseTime;
                component.Details["service"] = "NoBill Integration";

                _logger.LogDebug("NoBill service check completed in {ResponseTime}ms with code {ResponseCode}", 
                    responseTime, result.responseCode);
            }
            catch (Exception ex)
            {
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                component.Status = "UNHEALTHY";
                component.StatusMessage = "NoBill service connection failed";
                component.ResponseTimeMs = responseTime;
                component.ErrorCode = "NOBILL_CONNECTION_ERROR";
                component.Details["service"] = "NoBill Integration";
                component.Details["error"] = ex.Message;
                component.Details["impact"] = "Credit transfers will fail";

                _logger.LogError(ex, "NoBill service connectivity check failed");
            }

            response.Components.Add(component);
        }

        private async Task CheckConfigurationValidityAsync(ComprehensiveHealthResponse response)
        {
            var startTime = DateTime.UtcNow;
            var component = new ComponentHealth
            {
                Component = "Configuration_System",
                Details = new Dictionary<string, object>()
            };

            try
            {
                await EnsureConfigurationLoadedAsync();
                
                var configTests = new List<(string key, object value, bool isValid)>();
                
                // Test critical configuration values
                configTests.Add(("MsisdnLength", _msisdnLength, _msisdnLength?.Count > 0));
                configTests.Add(("RefillPinLength", _refillPinLength, _refillPinLength.HasValue && _refillPinLength > 0));
                configTests.Add(("DefaultPin", _defaultPin, !string.IsNullOrEmpty(_defaultPin)));
                configTests.Add(("MaximumPercentageAmount", _maximumPercentageAmount, _maximumPercentageAmount.HasValue && _maximumPercentageAmount > 0));

                var invalidConfigs = configTests.Where(c => !c.isValid).ToList();
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                if (invalidConfigs.Count == 0)
                {
                    component.Status = "HEALTHY";
                    component.StatusMessage = "All critical configurations valid";
                    component.Details["valid_configs"] = configTests.Count;
                }
                else
                {
                    component.Status = "DEGRADED";
                    component.StatusMessage = $"{invalidConfigs.Count} configuration issues found";
                    component.Details["valid_configs"] = configTests.Count - invalidConfigs.Count;
                    component.Details["invalid_configs"] = invalidConfigs.Select(c => c.key).ToList();
                }

                component.ResponseTimeMs = responseTime;
                component.Details["total_configs_checked"] = configTests.Count;
                component.Details["configuration_source"] = "Database + Redis Cache";

                _logger.LogDebug("Configuration validity check completed in {ResponseTime}ms", responseTime);
            }
            catch (Exception ex)
            {
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                component.Status = "UNHEALTHY";
                component.StatusMessage = "Configuration system failure";
                component.ResponseTimeMs = responseTime;
                component.ErrorCode = "CONFIG_SYSTEM_ERROR";
                component.Details["error"] = ex.Message;
                component.Details["impact"] = "Service may operate with default values";

                _logger.LogError(ex, "Configuration validity check failed");
            }

            response.Components.Add(component);
        }

        private async Task CheckExternalDependenciesAsync(ComprehensiveHealthResponse response)
        {
            var startTime = DateTime.UtcNow;
            var component = new ComponentHealth
            {
                Component = "External_Dependencies",
                Details = new Dictionary<string, object>()
            };

            try
            {
                var dependencyTests = new List<(string name, bool isHealthy, string? error)>();

                // Test Transfer Rules Service
                try
                {
                    var (isAllowed, errorCode, errorMessage) = await _transferRulesService.EvaluateTransferRuleAsync(
                        "OM", SubscriptionType.Customer, SubscriptionType.Customer);
                    dependencyTests.Add(("TransferRulesService", errorCode != 14, errorMessage)); // 14 = MiscellaneousError
                }
                catch (Exception ex)
                {
                    dependencyTests.Add(("TransferRulesService", false, ex.Message));
                }

                // Test Transaction Repository
                try
                {
                    var dailyCount = await _transactionRepository.GetDailyTransferCountAsync("96876325315");
                    dependencyTests.Add(("TransactionRepository", true, null));
                }
                catch (Exception ex)
                {
                    dependencyTests.Add(("TransactionRepository", false, ex.Message));
                }

                // Test Transfer Config Repository
                try
                {
                    var configs = await _transferConfigRepository.GetByNobillSubscriptionTypeAsync("customer");
                    dependencyTests.Add(("TransferConfigRepository", configs != null, null));
                }
                catch (Exception ex)
                {
                    dependencyTests.Add(("TransferConfigRepository", false, ex.Message));
                }

                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                var healthyDeps = dependencyTests.Count(d => d.isHealthy);
                var totalDeps = dependencyTests.Count;

                if (healthyDeps == totalDeps)
                {
                    component.Status = "HEALTHY";
                    component.StatusMessage = "All external dependencies responsive";
                }
                else if (healthyDeps > totalDeps / 2)
                {
                    component.Status = "DEGRADED";
                    component.StatusMessage = $"{totalDeps - healthyDeps} dependencies have issues";
                }
                else
                {
                    component.Status = "UNHEALTHY";
                    component.StatusMessage = "Multiple critical dependencies failing";
                }

                component.ResponseTimeMs = responseTime;
                component.Details["healthy_dependencies"] = healthyDeps;
                component.Details["total_dependencies"] = totalDeps;
                component.Details["dependency_status"] = dependencyTests.ToDictionary(
                    d => d.name, 
                    d => new { healthy = d.isHealthy, error = d.error });

                _logger.LogDebug("External dependencies check completed in {ResponseTime}ms: {Healthy}/{Total} healthy", 
                    responseTime, healthyDeps, totalDeps);
            }
            catch (Exception ex)
            {
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                component.Status = "UNHEALTHY";
                component.StatusMessage = "External dependencies check failed";
                component.ResponseTimeMs = responseTime;
                component.ErrorCode = "DEPENDENCIES_CHECK_ERROR";
                component.Details["error"] = ex.Message;

                _logger.LogError(ex, "External dependencies check failed");
            }

            response.Components.Add(component);
        }

        private void CalculateHealthSummary(ComprehensiveHealthResponse response)
        {
            var total = response.Components.Count;
            var healthy = response.Components.Count(c => c.Status == "HEALTHY");
            var degraded = response.Components.Count(c => c.Status == "DEGRADED");
            var unhealthy = response.Components.Count(c => c.Status == "UNHEALTHY");

            response.Summary = new HealthSummary
            {
                TotalComponents = total,
                HealthyComponents = healthy,
                DegradedComponents = degraded,
                UnhealthyComponents = unhealthy,
                OverallHealthPercentage = total > 0 ? $"{(healthy * 100 / total)}%" : "0%"
            };
        }

        private void DetermineOverallStatus(ComprehensiveHealthResponse response)
        {
            var unhealthyCount = response.Summary.UnhealthyComponents;
            var degradedCount = response.Summary.DegradedComponents;
            var totalCount = response.Summary.TotalComponents;

            if (unhealthyCount == 0 && degradedCount == 0)
            {
                response.OverallStatus = "HEALTHY";
            }
            else if (unhealthyCount == 0 && degradedCount <= totalCount / 2)
            {
                response.OverallStatus = "DEGRADED";
            }
            else if (unhealthyCount <= totalCount / 3)
            {
                response.OverallStatus = "DEGRADED";
            }
            else
            {
                response.OverallStatus = "UNHEALTHY";
            }

            // Special case: If critical services (Database or NoBill) are down, mark as UNHEALTHY
            var criticalServices = response.Components.Where(c => 
                c.Component == "SQL_Server_Database" || c.Component == "NoBill_Service");
            
            if (criticalServices.Any(c => c.Status == "UNHEALTHY"))
            {
                response.OverallStatus = "UNHEALTHY";
            }
        }

        #endregion
    }
}

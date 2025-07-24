using CreditTransfer.Core.Application.Exceptions;
using CreditTransfer.Core.Application.Interfaces;
using CreditTransfer.Core.Domain.Enums;
using IntegrationProxies.Nobill.Interfaces;
using IntegrationProxies.Nobill.Services.NobillCalls;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Transactions;
using static Azure.Core.HttpHeader;

namespace CreditTransfer.Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for subscription-related data operations
    /// Uses NobillCallsService to match original system architecture
    /// Enhanced with OpenTelemetry instrumentation for repository operation tracking
    /// </summary>
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly INobillCallsService _nobillCallsService;
        private readonly ITransferConfigRepository _transferConfigRepository;
        private readonly IApplicationConfigRepository _configRepository;
        private readonly ILogger<SubscriptionRepository> _logger;
        private readonly ActivitySource _activitySource;

        public SubscriptionRepository(
            INobillCallsService nobillCallsService,
            ITransferConfigRepository transferConfigRepository,
            IApplicationConfigRepository configRepository,
            ILogger<SubscriptionRepository> logger,
            ActivitySource activitySource)
        {
            _nobillCallsService = nobillCallsService;
            _transferConfigRepository = transferConfigRepository;
            _configRepository = configRepository;
            _logger = logger;
            _activitySource = activitySource;
        }

        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
        private async Task ThrowCreditTransferExceptionAsync<T>() where T : CreditTransferException, new()
        {
            var ex = new T();
            await ex.SetResponseMessageAsync(_configRepository);
            throw ex;
        }

        /// <summary>
        /// Gets the subscription type for a given MSISDN
        /// Uses NobillCalls web service to get subscription type
        /// Enhanced with OpenTelemetry activity tracking
        /// </summary>
        public async Task<SubscriptionType> GetAccountTypeAsync(string nobillSubscriptionType)
        {
            using var activity = _activitySource.StartActivity("SubscriptionRepository.GetAccountType");
            activity?.SetTag("operation", "GetAccountType");
            activity?.SetTag("repository", "SubscriptionRepository");
            activity?.SetTag("nobillSubscriptionType", nobillSubscriptionType);
            
            try
            {
                _logger.LogDebug("Map subscription type string to enum for nobillSubscriptionType: {nobillSubscriptionType}", nobillSubscriptionType);
                //var config = await _transferConfigRepository.GetByNobillSubscriptionTypeAsync(nobillSubscriptionType);
                //var accountType= config == null ? string.Empty : config.SubscriptionType;
                // Map subscription type string to enum 
                var subscriptionType = await MapNobillSubscriptionTypeToEnumAsync(nobillSubscriptionType);
                activity?.SetTag("subscription.type", subscriptionType.ToString());
                activity?.SetTag("result.success", true);
                
                return subscriptionType;
            }
            catch (Exception ex) when (!(ex is SubscriptionNotFoundException || ex is MiscellaneousErrorException || ex is PropertyNotFoundException))
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("error.type", ex.GetType().Name);
                _logger.LogError(ex, "Error getting account type for nobillSubscriptionType: {nobillSubscriptionType}", nobillSubscriptionType);
                await ThrowCreditTransferExceptionAsync<SubscriptionNotFoundException>();
                return default; // Never reached
            }
        }

        /// <summary>
        /// Gets the subscription block status
        /// Uses NobillCalls web service to get subscription status
        /// Enhanced with OpenTelemetry activity tracking
        /// </summary>
        public async Task<SubscriptionBlockStatus> GetSubscriptionBlockStatusAsync(string msisdn)
        {
            using var activity = _activitySource.StartActivity("SubscriptionRepository.GetSubscriptionBlockStatus");
            activity?.SetTag("operation", "GetSubscriptionBlockStatus");
            activity?.SetTag("repository", "SubscriptionRepository");
            activity?.SetTag("msisdn", msisdn);
            
            try
            {
                _logger.LogDebug("Getting subscription block status for MSISDN: {MSISDN}", msisdn);

                var (responseCode, itemValue) = await _nobillCallsService.GetSubscriptionValueAsync(msisdn, SubscriptionItem.blocked);

                activity?.SetTag("nobill.response.code", responseCode);
                activity?.SetTag("nobill.item.value", itemValue);
                
                switch (responseCode)
                {
                    case 3:
                        activity?.SetTag("error.type", "MiscellaneousError");
                        await ThrowCreditTransferExceptionAsync<MiscellaneousErrorException>();
                        return default; // Never reached
                    case 7:
                        activity?.SetTag("error.type", "SubscriptionNotFound");
                        await ThrowCreditTransferExceptionAsync<SubscriptionNotFoundException>();
                        return default; // Never reached
                    case 30:
                        activity?.SetTag("error.type", "PropertyNotFound");
                        await ThrowCreditTransferExceptionAsync<PropertyNotFoundException>();
                        return default; // Never reached
                    default:
                        if (responseCode != 0)
                        {
                            activity?.SetTag("default.to", "NO_BLOCK");
                            return SubscriptionBlockStatus.NO_BLOCK; // Default to no block on error
                        }
                        break;
                }

                // Map status string to block status enum
                var blockStatus = (SubscriptionBlockStatus)int.Parse(itemValue??"");
                activity?.SetTag("block.status", blockStatus.ToString());
                activity?.SetTag("result.success", true);
                
                return blockStatus;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("error.type", ex.GetType().Name);
                activity?.SetTag("default.to", "NO_BLOCK");
                _logger.LogError(ex, "Error getting subscription block status for MSISDN: {MSISDN}", msisdn);
                return SubscriptionBlockStatus.NO_BLOCK; // Default to no block on error
            }
        }

        /// <summary>
        /// Gets the subscription status
        /// Uses NobillCalls web service to get subscription status
        /// </summary>
        public async Task<SubscriptionStatus> GetSubscriptionStatusAsync(string msisdn)
        {
            try
            {
                _logger.LogDebug("Getting subscription status for MSISDN: {MSISDN}", msisdn);

                var (responseCode, itemValue) = await _nobillCallsService.GetSubscriptionValueAsync(msisdn, SubscriptionItem.status);

                switch (responseCode)
                {
                    case 3:
                        await ThrowCreditTransferExceptionAsync<MiscellaneousErrorException>();
                        return default; // Never reached
                    case 7:
                        await ThrowCreditTransferExceptionAsync<SubscriptionNotFoundException>();
                        return default; // Never reached
                    case 30:
                        await ThrowCreditTransferExceptionAsync<PropertyNotFoundException>();
                        return default; // Never reached
                    default:
                        if (responseCode != 0)
                        {
                            return SubscriptionStatus.INACTIVE; // Default to inactive on error
                        }
                        break;
                }

                // Map status string to subscription status enum
                return (SubscriptionStatus)int.Parse(itemValue?? ""); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription status for MSISDN: {MSISDN}", msisdn);
                return SubscriptionStatus.INACTIVE; // Default to inactive on error
            }
        }

        /// <summary>
        /// Gets the account PIN for validation
        /// Uses NobillCalls web service to get PIN
        /// </summary>
        /// 
        public async Task<string> GetAccountPinAsync(string msisdn)
        {
            try
            {
                _logger.LogDebug("Getting account PIN for MSISDN: {MSISDN}", msisdn);

                var (responseCode, itemValue) = await _nobillCallsService.GetCreditTransferValueAsync(msisdn, CreditTransferItem.p_pin);

                switch (responseCode)
                {
                    case 3:
                        await ThrowCreditTransferExceptionAsync<MiscellaneousErrorException>();
                        return default; // Never reached
                    case 7:
                        await ThrowCreditTransferExceptionAsync<SubscriptionNotFoundException>();
                        return default; // Never reached
                    case 30:
                        await ThrowCreditTransferExceptionAsync<PropertyNotFoundException>();
                        return default; // Never reached
                    default:
                        if (responseCode != 0)
                        {
                            return string.Empty;
                        }
                        break;
                }

                return itemValue ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account PIN for MSISDN: {MSISDN}", msisdn);
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the maximum transfer amount allowed for an account
        /// Uses NobillCalls web service to get max transfer amount
        /// </summary>
        public async Task<decimal> GetAccountMaxTransferAmountAsync(string msisdn)
        {
            try
            {
                _logger.LogDebug("Getting max transfer amount for MSISDN: {MSISDN}", msisdn);

                var (responseCode, itemValue) = await _nobillCallsService.GetCreditTransferValueAsync(msisdn, CreditTransferItem.p_maxamount);

                switch (responseCode)
                {
                    case 3:
                        await ThrowCreditTransferExceptionAsync<MiscellaneousErrorException>();
                        return default; // Never reached
                    case 7:
                        await ThrowCreditTransferExceptionAsync<SubscriptionNotFoundException>();
                        return default; // Never reached
                    case 30:
                        await ThrowCreditTransferExceptionAsync<PropertyNotFoundException>();
                        return default; // Never reached
                    default:
                        if (responseCode != 0)
                        {
                            return 0; // Default to 0 on error
                        }
                        break;
                }

                if (decimal.TryParse(itemValue, out var amount))
                {
                    return amount;
                }

                return 0; // Default to 0 if parsing fails
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting max transfer amount for MSISDN: {MSISDN}", msisdn);
                return 0; // Default to 0 on error
            }
        }

        /// <summary>
        /// Gets the account PIN by service name
        /// Note: This method may need to be implemented differently based on actual NobillCalls service capabilities
        /// </summary>
        public async Task<string> GetAccountPinByServiceNameAsync(string msisdn, string serviceName)
        {
            try
            {
                _logger.LogDebug("Getting account PIN by service name for MSISDN: {MSISDN}, Service: {ServiceName}", msisdn, serviceName);

                // For now, use the standard PIN method
                // This may need to be updated based on actual service capabilities
                return await GetAccountPinAsync(msisdn);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account PIN by service name for MSISDN: {MSISDN}", msisdn);
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the maximum transfer amount by service name
        /// Note: This method may need to be implemented differently based on actual NobillCalls service capabilities
        /// </summary>
        public async Task<decimal> GetAccountMaxTransferAmountByServiceNameAsync(string msisdn, string serviceName)
        {
            try
            {
                _logger.LogDebug("Getting max transfer amount by service name for MSISDN: {MSISDN}, Service: {ServiceName}", msisdn, serviceName);

                // For now, use the standard max transfer amount method
                // This may need to be updated based on actual service capabilities
                return await GetAccountMaxTransferAmountAsync(msisdn);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting max transfer amount by service name for MSISDN: {MSISDN}", msisdn);
                return 0;
            }
        }

        /// <summary>
        /// Gets the Nobill subscription type string
        /// Uses NobillCalls web service to get subscription type
        /// </summary>
        public async Task<string> GetNobillSubscriptionTypeAsync(string msisdn)
        {
            try
            {
                _logger.LogDebug("Getting Nobill subscription type for MSISDN: {MSISDN}", msisdn);

                var (responseCode, itemValue) = await _nobillCallsService.GetSubscriptionValueAsync(msisdn, SubscriptionItem.subscriptiontype);

                switch (responseCode)
                {
                    case 3:
                        await ThrowCreditTransferExceptionAsync<MiscellaneousErrorException>();
                        return default; // Never reached
                    case 7:
                        await ThrowCreditTransferExceptionAsync<SubscriptionNotFoundException>();
                        return default; // Never reached
                    case 9:
                        await ThrowCreditTransferExceptionAsync<PropertyNotFoundException>();
                        return default; // Never reached
                    default:
                        if (responseCode != 0)
                        {
                            throw new Exception("Service unavailable");
                        }
                        break;
                }
                return itemValue ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Nobill subscription type for MSISDN: {MSISDN}", msisdn);
                throw;
            }
        }

        /// <summary>
        /// Checks if both MSISDNs are on the same IN (Intelligent Network)
        /// This logic may need to be implemented based on business rules
        /// </summary>
        public async Task<(bool bothOnSameIN, bool isOldToNew)> CheckBothOnSameINAsync(string sourceMsisdn, string destinationMsisdn)
        {
            var subscriptionTypes = await _configRepository.GetConfigValueAsync<string>("CreditTransfer_SubscriptionTypes", "friendi-2,TouristSim");
            var subscriptionTypesArray = subscriptionTypes.Split(',').Select(s => s.ToLower().Trim()).ToArray();

            bool isOldToNew = false;
            try
            {
                var sourceSubscriptionType = await _nobillCallsService.GetSubscriptionValueAsync(sourceMsisdn, SubscriptionItem.subscriptiontype);
                var destSubscriptionType = await _nobillCallsService.GetSubscriptionValueAsync(destinationMsisdn, SubscriptionItem.subscriptiontype);

                if (sourceSubscriptionType.responseCode == 0 && destSubscriptionType.responseCode == 0)
                {
                    var sourceType = sourceSubscriptionType.itemValue?.ToLower() ?? "";
                    var destType = destSubscriptionType.itemValue?.ToLower() ?? "";

                    // Check if source contains any of the new subscription types
                    bool sourceIsNew = subscriptionTypesArray.Any(type => sourceType.Contains(type));
                    bool destIsNew = subscriptionTypesArray.Any(type => destType.Contains(type));

                    if (sourceIsNew)
                    {
                        if (!destIsNew)
                        {
                            return (false, false); // Source is new, dest is old = new to old
                        }
                    }

                    if (destIsNew)
                    {
                        if (!sourceIsNew)
                        {
                            isOldToNew = true;
                            return (false, true); // Source is old, dest is new = old to new
                        }
                    }
                }

                return (true, isOldToNew); // Both on same IN or couldn't determine
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking IN status for {Source} and {Destination}", sourceMsisdn, destinationMsisdn);
                return (true, false); // Default to same IN
            }
        }



        #region Private Helper Methods

        private async Task<SubscriptionType> MapNobillSubscriptionTypeToEnumAsync(string nobillSubscriptionType)
        {


            // Map Nobill subscription type strings to enum values
            // This mapping should match the original system's logic
            SubscriptionType? subscriptionType = nobillSubscriptionType.ToLowerInvariant() switch
            {
                "pos" => SubscriptionType.Pos,
                "distributor" => SubscriptionType.Distributor, // Map to Customer for postpaid
                "customer" => SubscriptionType.Customer, // Map to Customer for hybrid
                "halafonicustomer" => SubscriptionType.HalafoniCustomer, // Map to Halafoni Customer
                "virgin_prepaid_customer" => SubscriptionType.VirginPrepaidCustomer,
                "virgin_postpaid_customer" => SubscriptionType.VirginPostpaidCustomer,
                "data" => SubscriptionType.DataAccount, // Map to Data Account
                _ => null
            };
            if (subscriptionType.HasValue)
            {
                return subscriptionType.Value;
            }
            else
            {
                await ThrowCreditTransferExceptionAsync<UnknownSubscriberException>(); return default;// never reach 
            }
        }


        #endregion
    }
} 
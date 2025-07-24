using IntegrationProxies.Nobill.Interfaces;
using IntegrationProxies.Nobill.Services.NobillCalls;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.ServiceModel;

namespace IntegrationProxies.Nobill.Services;

/// <summary>
/// Implementation of NobillCalls web service wrapper for .NET 8
/// Uses database configuration via ApplicationConfigRepository with fallback to appsettings.json
/// Enhanced with OpenTelemetry instrumentation for external service call tracking
/// </summary>
public class NobillCallsService : INobillCallsService, IDisposable
{
    private readonly ILogger<NobillCallsService> _logger;
    private readonly ActivitySource _activitySource;
    private readonly IConfigurationRepository _configRepository;
    private readonly IConfiguration _fallbackConfiguration;
    private NobillCalls.NobillCallsSoapClient? _client;
    private bool _disposed = false;
    private bool _initialized = false;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    /// <summary>
    /// NobillCallsService constructor with database configuration support
    /// </summary>
    /// <param name="configRepository"></param>
    /// <param name="fallbackConfiguration"></param>
    /// <param name="logger"></param>
    /// <param name="activitySource"></param>
    public NobillCallsService(
        IConfigurationRepository configRepository,
        IConfiguration fallbackConfiguration,
        ILogger<NobillCallsService> logger, 
        ActivitySource activitySource)
    {
        _configRepository = configRepository;
        _fallbackConfiguration = fallbackConfiguration;
        _logger = logger;
        _activitySource = activitySource;
        
        _logger.LogDebug("NobillCallsService created with database configuration support");
    }

    /// <summary>
    /// Initializes the SOAP client with database configuration (lazy initialization)
    /// </summary>
    private async Task InitializeClientAsync()
    {
        if (_initialized)
            return;

        await _initializationLock.WaitAsync();
        try
        {
            if (_initialized)
                return;

            // Get configuration from database with fallback to appsettings.json
            var serviceUrl = await _configRepository.GetConfigValueAsync("NobillCalls_ServiceUrl") 
                             ?? _fallbackConfiguration.GetValue<string>("NobillCalls:ServiceUrl") 
                             ?? "http://10.1.132.98/NobillProxy/NobillCalls.asmx";

            var userName = await _configRepository.GetConfigValueAsync("NobillCalls_UserName") 
                           ?? _fallbackConfiguration.GetValue<string>("NobillCalls:UserName") 
                           ?? "";

            var password = await _configRepository.GetConfigValueAsync("NobillCalls_Password") 
                           ?? _fallbackConfiguration.GetValue<string>("NobillCalls:Password") 
                           ?? "";

            var timeoutSeconds = await _configRepository.GetConfigValueAsync<int>("NobillCalls_TimeoutSeconds", 30);

            // Create binding
            var binding = new BasicHttpBinding();
            binding.Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
            binding.SendTimeout = TimeSpan.FromSeconds(timeoutSeconds);
            binding.ReceiveTimeout = TimeSpan.FromSeconds(timeoutSeconds);
            binding.OpenTimeout = TimeSpan.FromSeconds(timeoutSeconds);
            binding.CloseTimeout = TimeSpan.FromSeconds(timeoutSeconds);
            
            // Increase message size limits for large responses
            binding.MaxBufferSize = int.MaxValue;
            binding.MaxReceivedMessageSize = int.MaxValue;
            
            // Create endpoint
            var endpoint = new EndpointAddress(serviceUrl);
            
            // Create client
            _client = new NobillCalls.NobillCallsSoapClient(binding, endpoint);
            
            // Set credentials
            _client.ClientCredentials.UserName.UserName = userName;
            _client.ClientCredentials.UserName.Password = password;
            
            _initialized = true;
            _logger.LogInformation("NobillCallsService initialized with database configuration - ServiceUrl: {ServiceUrl}, Timeout: {TimeoutSeconds}s", 
                serviceUrl, timeoutSeconds);
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    /// <summary>
    /// Ensures the client is initialized before use
    /// </summary>
    private async Task EnsureInitializedAsync()
    {
        if (!_initialized)
        {
            await InitializeClientAsync();
        }
    }
    /// <summary>
    /// gets account data for a given MSISDN
    /// </summary>
    /// <param name="msisdn"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<(int responseCode, AccountData? accountData)> GetAccountDataAsync(string msisdn)
    {
        await EnsureInitializedAsync();
        
        using var activity = _activitySource.StartActivity("NobillCalls.GetAccountData");
        activity?.SetTag("operation", "GetAccountData");
        activity?.SetTag("service.name", "NobillCalls");
        activity?.SetTag("msisdn", msisdn);
        activity?.SetTag("external.service.endpoint", _client!.Endpoint.Address.Uri.ToString());
        
        // Ensure proper span context propagation
        activity?.SetTag("trace.id", Activity.Current?.TraceId.ToString() ?? "none");
        activity?.SetTag("span.id", activity?.SpanId.ToString() ?? "none");
        
        try
        {
            _logger.LogDebug("Getting account data for MSISDN: {Msisdn}", msisdn);

            var response = await _client!.GetAccountDataAsync(msisdn);
            int responseCode = response.Body.GetAccountDataResult;
            AccountData? accountData = response.Body.details;

            activity?.SetTag("response.code", responseCode);
            
            if (responseCode == 0 && accountData != null)
            {
                activity?.SetTag("account.balance", accountData.Balance);
                activity?.SetTag("account.subscription_type", accountData.AccountType);
                activity?.SetTag("result.success", true);
                
                _logger.LogDebug("Successfully retrieved account data for MSISDN: {Msisdn}, Balance: {Balance}", 
                    msisdn, accountData.Balance);
                
                return (responseCode, accountData);
            }

            activity?.SetTag("result.success", false);
            activity?.SetTag("result.reason", "Response code indicates failure");
            
            _logger.LogWarning("Failed to get account data for MSISDN: {Msisdn}, ResponseCode: {ResponseCode}", 
                msisdn, responseCode);
            
            return (responseCode, null);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("error.message", ex.Message);
            
            _logger.LogError(ex, "Error getting account data for MSISDN: {Msisdn}. Error: {ErrorMessage}", 
                msisdn, ex.Message);
            
            // Re-throw with more context
            throw new InvalidOperationException($"Failed to get account data for MSISDN {msisdn}: {ex.Message}", ex);
        }
    }
    /// <summary>
    /// transfers money from one MSISDN to another
    /// </summary>
    /// <param name="sourceMsisdn"></param>
    /// <param name="destinationMsisdn"></param>
    /// <param name="amount"></param>
    /// <param name="transferReason"></param>
    /// <param name="userName"></param>
    /// <param name="description"></param>
    /// <returns></returns>
    public async Task<int> TransferMoneyAsync(string sourceMsisdn, string destinationMsisdn, decimal amount, string transferReason, string userName, string description = "")
    {
        await EnsureInitializedAsync();
        
        using var activity = _activitySource.StartActivity("NobillCalls.TransferMoney");
        activity?.SetTag("operation", "TransferMoney");
        activity?.SetTag("service.name", "NobillCalls");
        activity?.SetTag("source.msisdn", sourceMsisdn);
        activity?.SetTag("destination.msisdn", destinationMsisdn);
        activity?.SetTag("amount", amount.ToString());
        activity?.SetTag("transfer.reason", transferReason);
        activity?.SetTag("user.name", userName);
        activity?.SetTag("external.service.endpoint", _client!.Endpoint.Address.Uri.ToString());
        
        try
        {
            _logger.LogDebug("Transferring money from {Source} to {Destination}, amount: {Amount}", sourceMsisdn, destinationMsisdn, amount);

            var response = await _client!.TransferMoneyAsync(sourceMsisdn, destinationMsisdn, amount, transferReason, userName, description);
            var responseCode = response.Body.TransferMoneyResult;

            activity?.SetTag("response.code", responseCode);
            activity?.SetTag("result.success", responseCode == 0);
            
            if (responseCode != 0)
            {
                activity?.SetTag("result.reason", "Transfer failed with response code " + responseCode);
            }

            return responseCode;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            _logger.LogError(ex, "Error transferring money from {Source} to {Destination}", sourceMsisdn, destinationMsisdn);
            throw;
        }
    }
    /// <summary>
    /// gets account counters for a given MSISDN
    /// </summary>
    /// <param name="msisdn"></param>
    /// <returns></returns>
    public async Task<(int responseCode, Counter[]? counters)> GetAccountCountersAsync(string msisdn)
    {
        await EnsureInitializedAsync();
        
        try
        {
            _logger.LogDebug("Getting account counters for MSISDN: {Msisdn}", msisdn);

            var response = await _client!.GetAccountCountersAsync(msisdn);
            var responseCode = response.Body.GetAccountCountersResult;

            if (responseCode == 0 && response.Body.counters != null)
            {
                return (responseCode, response.Body.counters);
            }

            return (responseCode, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account counters for MSISDN: {Msisdn}", msisdn);
            throw;
        }
    }
    /// <summary>
    /// adjusts account balance for a given MSISDN
    /// </summary>
    /// <param name="msisdn"></param>
    /// <param name="amount"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task<int> AdjustAccountBalanceAsync(string msisdn, decimal amount, string message)
    {
        await EnsureInitializedAsync();
        
        try
        {
            _logger.LogDebug("Adjusting account balance for MSISDN: {Msisdn}, Amount: {Amount}", msisdn, amount);

            var response = await _client!.AdjustAccountBalanceAsync(msisdn, amount, message);
            return response.Body.AdjustAccountBalanceResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting account balance for MSISDN: {Msisdn}", msisdn);
            throw;
        }
    }
    /// <summary>
    /// asjusts account by reason for a given MSISDN
    /// </summary>
    /// <param name="msisdn"></param>
    /// <param name="amount"></param>
    /// <param name="message"></param>
    /// <param name="adjustmentReason"></param>
    /// <param name="adjustmentType"></param>
    /// <param name="description"></param>
    /// <returns></returns>
    public async Task<int> AdjustAccountByReasonAsync(string msisdn, decimal amount, string message, string adjustmentReason, string adjustmentType, string description)
    {
        await EnsureInitializedAsync();
        
        try
        {
            _logger.LogDebug("Adjusting account by reason for MSISDN: {Msisdn}, Amount: {Amount}, Reason: {Reason}", msisdn, amount, adjustmentReason);

            var response = await _client!.AdjustAccountByReasonAsync(msisdn, amount, message, adjustmentReason, adjustmentType, description);
            return response.Body.AdjustAccountByReasonResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting account by reason for MSISDN: {Msisdn}", msisdn);
            throw;
        }
    }
    /// <summary>
    /// gets subscription value for a given MSISDN and item
    /// </summary>
    /// <param name="msisdn"></param>
    /// <param name="item"></param>
    /// <returns></returns>
    public async Task<(int responseCode, string? itemValue)> GetSubscriptionValueAsync(string msisdn, SubscriptionItem item)
    {
        await EnsureInitializedAsync();
        
        using var activity = _activitySource.StartActivity("NobillCalls.GetSubscriptionValue");
        activity?.SetTag("operation", "GetSubscriptionValue");
        activity?.SetTag("service.name", "NobillCalls");
        activity?.SetTag("msisdn", msisdn);
        activity?.SetTag("item", item.ToString());
        activity?.SetTag("external.service.endpoint", _client!.Endpoint.Address.Uri.ToString());
        
        try
        {
            _logger.LogDebug("Getting subscription value for MSISDN: {Msisdn}, Item: {Item}", msisdn, item);

            var response = await _client!.GetSubscriptionValueAsync(msisdn, item);
            var responseCode = response.Body.GetSubscriptionValueResult;
            var itemValue = response.Body.itemValue;

            // Special handling for subscription type - check dealer flag like in original code
            if (item == SubscriptionItem.subscriptiontype && responseCode == 0 && !string.IsNullOrEmpty(itemValue))
            {
                var dealerCheckCode = await CheckCustomerServiceAsync(msisdn, "CS.Dealer_Flag");
                if (dealerCheckCode == 0)
                {
                    itemValue = "dealer";
                    _logger.LogDebug("Dealer flag check successful, setting subscription type to dealer for MSISDN: {Msisdn}", msisdn);
                }
                else
                {
                    _logger.LogDebug("Dealer flag check failed with code: {DealerCheckCode} for MSISDN: {Msisdn}", dealerCheckCode, msisdn);
                }
            }

            activity?.SetTag("response.code", responseCode);
            activity?.SetTag("item.value", itemValue);
            activity?.SetTag("result.success", responseCode == 0);
            
            return (responseCode, itemValue);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            _logger.LogError(ex, "Error getting subscription value for MSISDN: {Msisdn}, Item: {Item}", msisdn, item);
            throw;
        }
    }
    /// <summary>
    /// gets credit transfer value for a given MSISDN and item
    /// </summary>
    /// <param name="msisdn"></param>
    /// <param name="item"></param>
    /// <returns></returns>
    public async Task<(int responseCode, string? value)> GetCreditTransferValueAsync(string msisdn, CreditTransferItem item)
    {
        await EnsureInitializedAsync();
        
        try
        {
            _logger.LogDebug("Getting credit transfer value for MSISDN: {Msisdn}, Item: {Item}", msisdn, item);


            var response = await _client!.GetCreditTransferValueAsync(msisdn, item);
            
            return (response.Body.GetCreditTransferValueResult, response.Body.itemValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting credit transfer value for MSISDN: {Msisdn}, Item: {Item}", msisdn, item);
            throw;
        }
    }
    /// <summary>
    /// get credit transfer value by service name for a given MSISDN and item
    /// </summary>
    /// <param name="msisdn"></param>
    /// <param name="item"></param>
    /// <param name="serviceName"></param>
    /// <returns></returns>
    public async Task<(int responseCode, string? value)> GetCreditTransferValueByServiceNameAsync(string msisdn, CreditTransferItem item, string serviceName)
    {
        await EnsureInitializedAsync();
        
        try
        {
            _logger.LogDebug("Getting credit transfer value by service name for MSISDN: {Msisdn}, Item: {Item}, Service: {ServiceName}", msisdn, item, serviceName);


            var response = await _client!.GetCreditTransferValueByServiceNameAsync(msisdn, item, serviceName);
            
            return (response.Body.GetCreditTransferValueByServiceNameResult, response.Body.itemValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting credit transfer value by service name for MSISDN: {Msisdn}, Item: {Item}, Service: {ServiceName}", msisdn, item, serviceName);
            throw;
        }
    }
    /// <summary>
    /// get account value for a given MSISDN and item
    /// </summary>
    /// <param name="msisdn"></param>
    /// <param name="item"></param>
    /// <returns></returns>
    public async Task<(int responseCode, string? itemValue)> GetAccountValueAsync(string msisdn, AccountItem item)
    {
        await EnsureInitializedAsync();
        
        try
        {
            _logger.LogDebug("Getting account value for MSISDN: {Msisdn}, Item: {Item}", msisdn, item);

            // Convert interface enum to generated enum
            var response = await _client!.GetAccountValueAsync(msisdn, item);
            
            return (response.Body.GetAccountValueResult, response.Body.itemValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account value for MSISDN: {Msisdn}, Item: {Item}", msisdn, item);
            throw;
        }
    }
    /// <summary>
    /// checks if customer has a specific service
    /// </summary>
    /// <param name="msisdn"></param>
    /// <param name="customerServiceName"></param>
    /// <returns></returns>
    public async Task<int> CheckCustomerServiceAsync(string msisdn, string customerServiceName)
    {
        await EnsureInitializedAsync();
        
        try
        {
            _logger.LogDebug("Checking customer service for MSISDN: {Msisdn}, Service: {ServiceName}", msisdn, customerServiceName);

            var response = await _client!.CheckCustomerServiceAsync(msisdn, customerServiceName);
            return response.Body.CheckCustomerServiceResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking customer service for MSISDN: {Msisdn}, Service: {ServiceName}", msisdn, customerServiceName);
            throw;
        }
    }
    /// <summary>
    /// reserves an event for a given MSISDN and event ID
    /// </summary>
    /// <param name="sourceMsisdn"></param>
    /// <param name="eventId"></param>
    /// <returns></returns>
    public async Task<(int responseCode, int reservationCode)> ReserveEventAsync(string sourceMsisdn, int eventId)
    {
        await EnsureInitializedAsync();
        
        try
        {
            _logger.LogDebug("Reserving event for MSISDN: {Msisdn}, EventId: {EventId}", sourceMsisdn, eventId);

            var response = await _client!.ReserveEventAsync(sourceMsisdn, eventId);
            var responseCode = response.Body.ReserveEventResult;
            var reservationCode = response.Body.reservationCode;
            
            return (responseCode, reservationCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving event for MSISDN: {Msisdn}, EventId: {EventId}", sourceMsisdn, eventId);
            throw;
        }
    }
    /// <summary>
    /// credits an event for a given MSISDN, charge reference number, and event ID
    /// </summary>
    /// <param name="msisdn"></param>
    /// <param name="chargeReferenceNo"></param>
    /// <param name="eventId"></param>
    /// <returns></returns>
    public async Task<int> CreditEventAsync(string msisdn, string chargeReferenceNo, int eventId)
    {
        await EnsureInitializedAsync();
        
        try
        {
            _logger.LogDebug("Crediting event for MSISDN: {Msisdn}, ChargeRef: {ChargeRef}, EventId: {EventId}", msisdn, chargeReferenceNo, eventId);

            var response = await _client!.CreditEventAsync(msisdn, chargeReferenceNo, eventId);
            return response.Body.CreditEventResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crediting event for MSISDN: {Msisdn}, EventId: {EventId}", msisdn, eventId);
            throw;
        }
    }
    /// <summary>
    /// charges an event with credit ability for a given MSISDN and event ID
    /// </summary>
    /// <param name="msisdn"></param>
    /// <param name="eventId"></param>
    /// <returns></returns>
    public async Task<(int responseCode, string? chargeReference)> ChargeEventWithCreditAbilityAsync(string msisdn, int eventId)
    {
        await EnsureInitializedAsync();
        
        try
        {
            _logger.LogDebug("Charging event with credit ability for MSISDN: {Msisdn}, EventId: {EventId}", msisdn, eventId);

            var response = await _client!.ChargeEventWithCreditAbilityAsync(msisdn, eventId);
            var responseCode = response.Body.ChargeEventWithCreditAbilityResult;
            var chargeReference = response.Body.chargeReference;
            
            return (responseCode, chargeReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error charging event with credit ability for MSISDN: {Msisdn}, EventId: {EventId}", msisdn, eventId);
            throw;
        }
    }
    /// <summary>
    /// sends an HTTP SMS from a source MSISDN to a destination MSISDN
    /// </summary>
    /// <param name="sourceMsisdn"></param>
    /// <param name="destinationMsisdn"></param>
    /// <param name="content"></param>
    /// <param name="isArabic"></param>
    /// <returns></returns>
    public async Task<int> SendHTTPSMSAsync(string sourceMsisdn, string destinationMsisdn, string content, bool isArabic)
    {
        await EnsureInitializedAsync();
        
        try
        {
            _logger.LogDebug("Sending HTTP SMS from {Source} to {Destination}, Arabic: {IsArabic}", sourceMsisdn, destinationMsisdn, isArabic);

            var response = await _client!.SendHTTPSMSAsync(sourceMsisdn, destinationMsisdn, content, isArabic);
            return response.Body.SendHTTPSMSResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending HTTP SMS from {Source} to {Destination}", sourceMsisdn, destinationMsisdn);
            throw;
        }
    }
    /// <summary>
    /// gets subscription data for a given MSISDN
    /// </summary>
    /// <param name="msisdn"></param>
    /// <returns></returns>
    public async Task<(int responseCode, SubscribtionData? subscriptionData)> GetSubscriptionDataAsync(string msisdn)
    {
        await EnsureInitializedAsync();
        
        try
        {
            _logger.LogDebug("Getting subscription data for MSISDN: {Msisdn}", msisdn);

            var response = await _client!.GetSubscribtionDataAsync(msisdn);
            var responseCode = response.Body.GetSubscribtionDataResult;

            if (responseCode == 0 && response.Body.details != null)
            {
                var details = response.Body.details;
               
                return (responseCode, details);
            }

            return (responseCode, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription data for MSISDN: {Msisdn}", msisdn);
            throw;
        }
    }
    /// <summary>
    /// extends the subscription expiry for a given MSISDN by a specified number of days
    /// </summary>
    /// <param name="msisdn"></param>
    /// <param name="numberOfDays"></param>
    /// <returns></returns>
    public async Task<int> ExtendSubscriptionExpiryAsync(string msisdn, int numberOfDays)
    {
        await EnsureInitializedAsync();
        
        try
        {
            _logger.LogDebug("Extending subscription expiry for MSISDN: {Msisdn}, Days: {Days}", msisdn, numberOfDays);

            var response = await _client!.ExtendSubscriptionExpiryAsync(msisdn, numberOfDays);
            return response.Body.ExtendSubscriptionExpiryResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending subscription expiry for MSISDN: {Msisdn}, Days: {Days}", msisdn, numberOfDays);
            throw;
        }
    }
    /// <summary>
    /// cancels a reservation for a given MSISDN and reservation code
    /// </summary>
    /// <param name="sourceMsisdn"></param>
    /// <param name="reservationCode"></param>
    /// <returns></returns>
    public async Task<(int responseCode, int reservationCode)> CancelReservationAsync(string sourceMsisdn, int reservationCode)
    {
        await EnsureInitializedAsync();
        
        try
        {
            _logger.LogDebug("Cancelling reservation for MSISDN: {Msisdn}, ReservationCode: {Code}", sourceMsisdn, reservationCode);

            var response = await _client!.CancelReservationAsync(sourceMsisdn, reservationCode);
            var responseCodeResult = response.Body.CancelReservationResult;
            
            return (responseCodeResult, reservationCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling reservation for MSISDN: {Msisdn}, ReservationCode: {Code}", sourceMsisdn, reservationCode);
            throw;
        }
    }
    /// <summary>
    /// charges a reserved event for a given MSISDN and reservation code
    /// </summary>
    /// <param name="sourceMsisdn"></param>
    /// <param name="reservationCode"></param>
    /// <returns></returns>
    public async Task<int> ChargeReservedEventAsync(string sourceMsisdn, int reservationCode)
    {
        await EnsureInitializedAsync();
        
        try
        {
            _logger.LogDebug("Charging reserved event for MSISDN: {Msisdn}, ReservationCode: {Code}", sourceMsisdn, reservationCode);

            var response = await _client!.ChargeReservedEventAsync(sourceMsisdn, reservationCode);
            return response.Body.ChargeReservedEventResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error charging reserved event for MSISDN: {Msisdn}, ReservationCode: {Code}", sourceMsisdn, reservationCode);
            throw;
        }
    }


    /// <summary>
    /// recharges an amount for a given MSISDN
    /// </summary>
    /// <param name="msisdn"></param>
    /// <param name="amount"></param>
    /// <param name="adjustmentReason"></param>
    /// <returns></returns>
    public async Task<int> RechargeAmountAsync(string msisdn, decimal amount, string adjustmentReason)
    {
        await EnsureInitializedAsync();
        
        try
        {
            _logger.LogDebug("Recharging amount for MSISDN: {Msisdn}, Amount: {Amount}, AdjustmentReason: {AdjustmentReason}", msisdn, amount, adjustmentReason);

            // RechargeAmountAsync requires 5 parameters in generated client
            var response = await _client!.RechargeAmountAsync(msisdn, amount, adjustmentReason, "CREDIT", "Credit transfer");
            return response.Body.RechargeAmountResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recharging amount for MSISDN: {Msisdn}, Amount: {Amount}", msisdn, amount);
            throw;
        }
    }
    /// <summary>
    /// sends an SMS to a given MSISDN
    /// </summary>
    /// <param name="msisdn"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task<int> SendSMSAsync(string msisdn, string message)
    {
        await EnsureInitializedAsync();
        
        _logger.LogWarning("SendSMSAsync not available in generated client, using SendHTTPSMSAsync instead");
        // Use SendHTTPSMSAsync as fallback (which already has EnsureInitializedAsync)
        return await SendHTTPSMSAsync("", msisdn, message, false);
    }
    /// <summary>
    /// disposes the service and cleans up resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    /// <summary>
    /// disposes the service resources
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                _client?.Close();
            }
            catch
            {
                _client?.Abort();
            }
            _activitySource?.Dispose();
            _initializationLock?.Dispose();
            _disposed = true;
        }
    }
} 

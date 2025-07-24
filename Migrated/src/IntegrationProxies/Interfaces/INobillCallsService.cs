

using IntegrationProxies.Nobill.Services.NobillCalls;

namespace IntegrationProxies.Nobill.Interfaces
{
    /// <summary>
    /// Interface for NobillCalls web service operations
    /// This wraps the original SOAP-based NobillCalls service for .NET 8 compatibility
    /// </summary>
    public interface INobillCallsService
    {
        /// <summary>
        /// Gets subscription value by item type
        /// Original: nobillCalls.GetSubscriptionValue(msisdn, SubscriptionItem.blocked, out value)
        /// </summary>
        Task<(int responseCode, string? itemValue)> GetSubscriptionValueAsync(string msisdn, SubscriptionItem item);

        /// <summary>
        /// Gets credit transfer value by item type
        /// Original: nobillCalls.GetCreditTransferValue(msisdn, CreditTransferItem.p_pin, out value)
        /// </summary>
        Task<(int responseCode, string? value)> GetCreditTransferValueAsync(string msisdn, CreditTransferItem item);

        /// <summary>
        /// Gets credit transfer value by service name
        /// Original: nobillCalls.GetCreditTransferValueByServiceName(msisdn, CreditTransferItem.p_pin, serviceName, out value)
        /// </summary>
        Task<(int responseCode, string? value)> GetCreditTransferValueByServiceNameAsync(string msisdn, CreditTransferItem item, string serviceName);

        /// <summary>
        /// Checks if customer has a specific service
        /// Original: nobillCalls.CheckCustomerService(msisdn, serviceName)
        /// </summary>
        Task<int> CheckCustomerServiceAsync(string msisdn, string customerServiceName);

        /// <summary>
        /// Reserves an event for transaction processing
        /// Original: nobillCalls.ReserveEvent(sourceMsisdn, eventId, out reservationCode)
        /// </summary>
        Task<(int responseCode, int reservationCode)> ReserveEventAsync(string sourceMsisdn, int eventId);

        /// <summary>
        /// Credits an event using reservation code
        /// Original: nobillCalls.CreditEvent(msisdn, chargeReferenceNo, eventId)
        /// </summary>
        Task<int> CreditEventAsync(string msisdn, string chargeReferenceNo, int eventId);

        /// <summary>
        /// Charges an event with credit ability check
        /// Original: nobillCalls.ChargeEventWithCreditAbility(msisdn, eventId, out chargeReference)
        /// </summary>
        Task<(int responseCode, string? chargeReference)> ChargeEventWithCreditAbilityAsync(string msisdn, int eventId);

        /// <summary>
        /// Sends HTTP SMS
        /// Original: nobillCalls.SendHTTPSMS(sourceMsisdn, destinationMsisdn, content, isArabic)
        /// </summary>
        Task<int> SendHTTPSMSAsync(string sourceMsisdn, string destinationMsisdn, string content, bool isArabic);

        /// <summary>
        /// Gets account data
        /// Original: nobillCalls.GetAccountData(msisdn, out accountData)
        /// </summary>
        Task<(int responseCode, AccountData? accountData)> GetAccountDataAsync(string msisdn);

        /// <summary>
        /// Gets account counters
        /// Original: nobillCalls.GetAccountCounters(msisdn, out counters)
        /// </summary>
        Task<(int responseCode, Counter[]? counters)> GetAccountCountersAsync(string msisdn);

        /// <summary>
        /// Gets subscription data
        /// Original: nobillCalls.GetSubscribtionData(msisdn, out subscribtionData)
        /// </summary>
        Task<(int responseCode, SubscribtionData? subscriptionData)> GetSubscriptionDataAsync(string msisdn);

        /// <summary>
        /// Adjusts account balance
        /// Original: nobillCalls.AdjustAccountBalance(msisdn, amount, message)
        /// </summary>
        Task<int> AdjustAccountBalanceAsync(string msisdn, decimal amount, string message);

        /// <summary>
        /// Adjusts account by reason
        /// Original: nobillCalls.AdjustAccountByReason(msisdn, amount, message, adjustmentReason, adjustmentType, comment)
        /// </summary>
        Task<int> AdjustAccountByReasonAsync(string msisdn, decimal amount, string message, string adjustmentReason, string adjustmentType, string description);

        /// <summary>
        /// Transfers money between accounts
        /// Original: nobillCalls.TransferMoney(sourceMsisdn, destinationMsisdn, amount, transferReason, userName, comment)
        /// </summary>
        Task<int> TransferMoneyAsync(string sourceMsisdn, string destinationMsisdn, decimal amount, string transferReason, string userName, string description);

        /// <summary>
        /// Extends subscription expiry
        /// Original: nobillCalls.ExtendSubscriptionExpiry(destinationMsisdn, numberOfDays)
        /// </summary>
        Task<int> ExtendSubscriptionExpiryAsync(string msisdn, int numberOfDays);

        /// <summary>
        /// Cancels a reservation
        /// Original: nobillCalls.CancelReservation(sourceMsisdn, reservationCode)
        /// </summary>
        Task<(int responseCode, int reservationCode)> CancelReservationAsync(string sourceMsisdn, int reservationCode);

        /// <summary>
        /// Charges a reserved event
        /// Original: nobillCalls.ChargeReservedEvent(sourceMsisdn, reservationCode)
        /// </summary>
        Task<int> ChargeReservedEventAsync(string sourceMsisdn, int reservationCode);

        /// <summary>
        /// Gets account value by item type
        /// Original: nobillCalls.GetAccountValue(msisdn, AccountItem.account_type, out value)
        /// </summary>
        Task<(int responseCode, string? itemValue)> GetAccountValueAsync(string msisdn, AccountItem item);

    }


} 
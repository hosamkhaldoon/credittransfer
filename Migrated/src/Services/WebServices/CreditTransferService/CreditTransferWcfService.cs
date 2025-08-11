using CoreWCF;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CreditTransfer.Core.Application.Interfaces;
using CreditTransfer.Infrastructure.Configuration;
using CreditTransfer.Core.Authentication.Services;
using System.Diagnostics;
using System.Text;

namespace CreditTransfer.Services.WcfService;

/// <summary>
/// WCF Service implementation for Credit Transfer operations
/// Wraps the migrated business logic while preserving exact API behavior
/// Enhanced with JWT authentication support
/// </summary>
[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
[WcfBasicAuthenticationBehavior(requireAuthentication: false)]
public class CreditTransferWcfService : ICreditTransferWcfService
{
    private readonly ICreditTransferService _creditTransferService;
    private readonly ITokenValidationService _tokenValidationService;
    private readonly CreditTransferOptions _options;
    private readonly ActivitySource _activitySource;

    public CreditTransferWcfService(
        ICreditTransferService creditTransferService,
        ITokenValidationService tokenValidationService,
        IOptions<CreditTransferOptions> options,
        ActivitySource activitySource)
    {
        _creditTransferService = creditTransferService;
        _tokenValidationService = tokenValidationService;
        _options = options.Value;
        _activitySource = activitySource;
    }

    /// <summary>
    /// Transfers credit from source to destination MSISDN with PIN validation
    /// Preserves exact behavior from original WCF service
    /// </summary>
    public void TransferCredit(string sourceMsisdn, string destinationMsisdn, int amountRiyal, int amountBaisa, string pin, out int statusCode, out string statusMessage)
    {
        using var activity = _activitySource.StartActivity("WCF.TransferCredit");

        
        activity?.SetTag("wcf.operation", "TransferCredit");
        activity?.SetTag("wcf.source_msisdn", sourceMsisdn);
        activity?.SetTag("wcf.destination_msisdn", destinationMsisdn);
        activity?.SetTag("wcf.amount", $"{amountRiyal}.{amountBaisa:D3}");
        

        try
        {
            // Require authentication for transfer operations
            WcfAuthenticationHelper.RequireAuthentication();

            var result = _creditTransferService.TransferCreditAsync(sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin).GetAwaiter().GetResult();

            statusCode = result.statusCode;
            statusMessage = result.statusMessage;

            activity?.SetTag("wcf.result.status_code", statusCode);
            activity?.SetTag("wcf.result.transaction_id", result.transactionId);
        }
        catch (FaultException)
        {
            // Re-throw WCF fault exceptions (including authentication errors)
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("wcf.error.type", ex.GetType().Name);
            statusCode = -1;
            statusMessage = "Internal server error occurred";
        }
    }

    /// <summary>
    /// Transfers credit with adjustment reason for auditing purposes
    /// Preserves exact behavior from original WCF service
    /// </summary>
    public void TransferCreditWithAdjustmentReason(string sourceMsisdn, string destinationMsisdn, int amountRiyal, int amountBaisa, string pin, string adjustmentReason, out int statusCode, out string statusMessage)
    {
        using var activity = _activitySource.StartActivity("WCF.TransferCreditWithAdjustmentReason");

        
        activity?.SetTag("wcf.operation", "TransferCreditWithAdjustmentReason");
        activity?.SetTag("wcf.source_msisdn", sourceMsisdn);
        activity?.SetTag("wcf.destination_msisdn", destinationMsisdn);
        activity?.SetTag("wcf.amount", $"{amountRiyal}.{amountBaisa:D3}");


        try
        {
            // Require authentication for transfer operations
            WcfAuthenticationHelper.RequireAuthentication();

            var result = _creditTransferService.TransferCreditWithAdjustmentReasonAsync(sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin, adjustmentReason).GetAwaiter().GetResult();

            statusCode = result.statusCode;
            statusMessage = result.statusMessage;

            activity?.SetTag("wcf.result.status_code", statusCode);
            activity?.SetTag("wcf.result.transaction_id", result.transactionId);

        }
        catch (FaultException)
        {
            // Re-throw WCF fault exceptions (including authentication errors)
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("wcf.error.type", ex.GetType().Name);
            statusCode = -1;
            statusMessage = "Internal server error occurred";
        }
    }

    /// <summary>
    /// Gets available denominations for credit transfer
    /// Preserves exact behavior from original WCF service
    /// </summary>
    public List<decimal> GetDenomination()
    {
        using var activity = _activitySource.StartActivity("WCF.GetDenomination");
        activity?.SetTag("wcf.operation", "GetDenomination");


        try
        {
            // GetDenomination does not require authentication - public information
            var result = _creditTransferService.GetDenominationsAsync().GetAwaiter().GetResult();
            
            activity?.SetTag("wcf.result.denominations_count", result.Count);
            
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("wcf.error.type", ex.GetType().Name);
            return new List<decimal>();
        }
    }

    /// <summary>
    /// Transfers credit without PIN validation for Service Center operations
    /// Preserves exact behavior from original WCF service
    /// </summary>
    public void TransferCreditWithoutPinforSC(string sourceMsisdn, string destinationMsisdn, decimal amountRiyal, out int statusCode, out string statusMessage)
    {
        using var activity = _activitySource.StartActivity("WCF.TransferCreditWithoutPinforSC");
        
        activity?.SetTag("wcf.operation", "TransferCreditWithoutPinforSC");
        activity?.SetTag("wcf.source_msisdn", sourceMsisdn);
        activity?.SetTag("wcf.destination_msisdn", destinationMsisdn);
        activity?.SetTag("wcf.amount", amountRiyal.ToString("F3"));


        try
        {
            // Require authentication for Service Center operations
            WcfAuthenticationHelper.RequireAuthentication();

            // Optional: Require specific role for Service Center operations
            // WcfAuthenticationHelper.RequireRole("credit-transfer-operator");

            var result = _creditTransferService.TransferCreditWithoutPinAsync(sourceMsisdn, destinationMsisdn, amountRiyal).GetAwaiter().GetResult();

            statusCode = result.statusCode;
            statusMessage = result.statusMessage;

            activity?.SetTag("wcf.result.status_code", statusCode);
            activity?.SetTag("wcf.result.transaction_id", result.transactionId);
        }
        catch (FaultException)
        {
            // Re-throw WCF fault exceptions (including authentication errors)
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("wcf.error.type", ex.GetType().Name);
            statusCode = -1;
            statusMessage = "Internal server error occurred";
        }
    }

    /// <summary>
    /// Validates transfer inputs without performing the actual transfer
    /// Preserves exact behavior from original WCF service
    /// </summary>
    public void ValidateTransferInputs(string sourceMsisdn, string destinationMsisdn, decimal amountRiyal, out int statusCode, out string statusMessage)
    {
        using var activity = _activitySource.StartActivity("WCF.ValidateTransferInputs");
        
        activity?.SetTag("wcf.operation", "ValidateTransferInputs");
        activity?.SetTag("wcf.source_msisdn", sourceMsisdn);
        activity?.SetTag("wcf.destination_msisdn", destinationMsisdn);
        activity?.SetTag("wcf.amount", $"{amountRiyal:F3}");


        try
        {
            // ValidateTransferInputs can be called without authentication for validation purposes
            var result = _creditTransferService.ValidateTransferInputsAsync(sourceMsisdn, destinationMsisdn, amountRiyal).GetAwaiter().GetResult();

            statusCode = result.statusCode;
            statusMessage = result.statusMessage;

            activity?.SetTag("wcf.result.status_code", statusCode);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("wcf.error.type", ex.GetType().Name);
            statusCode = -1;
            statusMessage = "Internal server error occurred";
        }
    }
} 
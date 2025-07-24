using CoreWCF;
using System.Collections.Generic;

namespace CreditTransfer.Services.WcfService
{
    /// <summary>
    /// WCF Service Contract for Credit Transfer operations
    /// Enhanced with JWT authentication support
    /// 
    /// Authentication:
    /// - JWT tokens should be passed in the Authorization header as "Bearer {token}"
    /// - Alternative header: X-Authorization
    /// - Anonymous access allowed for GetDenomination and ValidateTransferInputs
    /// - Authentication required for all transfer operations
    /// </summary>
    [ServiceContract(Namespace = "http://tempuri.org/")]
    [WcfJwtAuthenticationBehavior(requireAuthentication: false)] // Applied at service level, operations can override
    public interface ICreditTransferWcfService
    {
        /// <summary>
        /// Transfers credit from source to destination MSISDN with PIN validation
        /// Requires JWT authentication with valid token
        /// </summary>
        /// <param name="sourceMsisdn">Source phone number</param>
        /// <param name="destinationMsisdn">Destination phone number</param>
        /// <param name="amountRiyal">Amount in Riyal</param>
        /// <param name="amountBaisa">Amount in Baisa (1/1000 of Riyal)</param>
        /// <param name="pin">PIN for verification</param>
        /// <param name="statusCode">Operation status code</param>
        /// <param name="statusMessage">Operation status message</param>
        [OperationContract(Action = "http://tempuri.org/ICreditTransferService/TransferCredit")]
        void TransferCredit(string sourceMsisdn, string destinationMsisdn, int amountRiyal, int amountBaisa, string pin, out int statusCode, out string statusMessage);
        
        /// <summary>
        /// Transfers credit with adjustment reason for auditing purposes
        /// Requires JWT authentication with valid token
        /// </summary>
        /// <param name="sourceMsisdn">Source phone number</param>
        /// <param name="destinationMsisdn">Destination phone number</param>
        /// <param name="amountRiyal">Amount in Riyal</param>
        /// <param name="amountBaisa">Amount in Baisa (1/1000 of Riyal)</param>
        /// <param name="pin">PIN for verification</param>
        /// <param name="adjustmentReason">Reason for the transfer adjustment</param>
        /// <param name="statusCode">Operation status code</param>
        /// <param name="statusMessage">Operation status message</param>
        [OperationContract(Action = "http://tempuri.org/ICreditTransferService/TransferCreditWithAdjustmentReason")]
        void TransferCreditWithAdjustmentReason(string sourceMsisdn, string destinationMsisdn, int amountRiyal, int amountBaisa, string pin, string adjustmentReason, out int statusCode, out string statusMessage);
        
        /// <summary>
        /// Gets available denominations for credit transfer
        /// No authentication required - public information
        /// </summary>
        /// <returns>List of available denominations</returns>
        [OperationContract(Action = "http://tempuri.org/ICreditTransferService/GetDenomination")]
        List<decimal> GetDenomination();
        
        /// <summary>
        /// Transfers credit without PIN validation for Service Center operations
        /// Requires JWT authentication with Service Center role
        /// </summary>
        /// <param name="sourceMsisdn">Source phone number</param>
        /// <param name="destinationMsisdn">Destination phone number</param>
        /// <param name="amountRiyal">Amount in Riyal</param>
        /// <param name="statusCode">Operation status code</param>
        /// <param name="statusMessage">Operation status message</param>
        [OperationContract(Action = "http://tempuri.org/ICreditTransferService/TransferCreditWithoutPinforSC")]
        void TransferCreditWithoutPinforSC(string sourceMsisdn, string destinationMsisdn, decimal amountRiyal, out int statusCode, out string statusMessage);
        
        /// <summary>
        /// Validates transfer inputs without performing the actual transfer
        /// No authentication required - validation only
        /// </summary>
        /// <param name="sourceMsisdn">Source phone number</param>
        /// <param name="destinationMsisdn">Destination phone number</param>
        /// <param name="amountRiyal">Amount in Riyal</param>
        /// <param name="statusCode">Validation status code</param>
        /// <param name="statusMessage">Validation status message</param>
        [OperationContract(Action = "http://tempuri.org/ICreditTransferService/ValidateTransferInputs")]
        void ValidateTransferInputs(string sourceMsisdn, string destinationMsisdn, decimal amountRiyal, out int statusCode, out string statusMessage);
    }
} 
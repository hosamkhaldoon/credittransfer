using Microsoft.Extensions.Configuration;

namespace CreditTransfer.Core.Domain.Exceptions
{
    /// <summary>
    /// Base exception for all credit transfer related errors
    /// Preserves exact structure from original system with ResponseCode and ResponseMessage
    /// </summary>
    public abstract class CreditTransferException : Exception
    {
        public int ResponseCode { get; set; }
        public string ResponseMessage { get; set; } = string.Empty;
        
        protected CreditTransferException(string message) : base(message) { }
        protected CreditTransferException(string message, Exception innerException) : base(message, innerException) { }
        
        /// <summary>
        /// Sets the response message from configuration using the response code as key
        /// </summary>
        protected void SetResponseMessageFromConfiguration(IConfiguration? configuration)
        {
            if (configuration != null)
            {
                // Look for response message in ResponseMessages section first, then fall back to direct key lookup
                var responseMessage = configuration.GetValue<string>($"ResponseMessages:{ResponseCode}") 
                                    ?? configuration.GetValue<string>(ResponseCode.ToString());
                
                if (!string.IsNullOrEmpty(responseMessage))
                {
                    ResponseMessage = responseMessage;
                }
            }
        }
    }
    
    /// <summary>
    /// Thrown when source phone number is invalid
    /// Preserves original exception from legacy system
    /// </summary>
    public class InvalidSourcePhoneException : CreditTransferException
    {
        public InvalidSourcePhoneException(IConfiguration? configuration = null) : base("Invalid source phone number") 
        { 
            ResponseCode = 20;
            ResponseMessage = "Invalid Source Phone Number";
            SetResponseMessageFromConfiguration(configuration);
        }
    }
    
    /// <summary>
    /// Thrown when destination phone number is invalid
    /// </summary>
    public class InvalidDestinationPhoneException : CreditTransferException
    {
        public InvalidDestinationPhoneException(IConfiguration? configuration = null) : base("Invalid destination phone number") 
        { 
            ResponseCode = 21;
            ResponseMessage = "Invalid Destination Phone Number";
            SetResponseMessageFromConfiguration(configuration);
        }
    }
    
    /// <summary>
    /// Thrown when PIN is invalid
    /// </summary>
    public class InvalidPinException : CreditTransferException
    {
        public InvalidPinException(IConfiguration? configuration = null) : base("Invalid PIN") 
        { 
            ResponseCode = 22;
            ResponseMessage = "Invalid PIN";
            SetResponseMessageFromConfiguration(configuration);
        }
    }
    
    /// <summary>
    /// Thrown when source and destination numbers are the same
    /// </summary>
    public class SourceAndDestinationSameException : CreditTransferException
    {
        public SourceAndDestinationSameException(IConfiguration? configuration = null) : base("Source and destination numbers cannot be the same") 
        { 
            ResponseCode = 3;
            ResponseMessage = "A-party and B-party phone numbers are same";
            SetResponseMessageFromConfiguration(configuration);
        }
    }
    
    /// <summary>
    /// Thrown when source phone number is not found in the system
    /// </summary>
    public class SourcePhoneNumberNotFoundException : CreditTransferException
    {
        public SourcePhoneNumberNotFoundException(IConfiguration? configuration = null) : base("Source phone number not found") 
        { 
            ResponseCode = 26;
            ResponseMessage = "Source phone number not found";
            SetResponseMessageFromConfiguration(configuration);
        }
    }
    
    /// <summary>
    /// Thrown when destination phone number is not found in the system
    /// </summary>
    public class DestinationPhoneNumberNotFoundException : CreditTransferException
    {
        public DestinationPhoneNumberNotFoundException(IConfiguration? configuration = null) : base("Destination phone number not found") 
        { 
            ResponseCode = 27;
            ResponseMessage = "Destination phone number not found";
            SetResponseMessageFromConfiguration(configuration);
        }
    }
    
    /// <summary>
    /// Thrown when subscription is not found
    /// </summary>
    public class SubscriptionNotFoundException : CreditTransferException
    {
        public SubscriptionNotFoundException(IConfiguration? configuration = null) : base("Subscription not found") 
        { 
            ResponseCode = 24;
            ResponseMessage = "Subscription Not Found";
            SetResponseMessageFromConfiguration(configuration);
        }
    }
    
    /// <summary>
    /// Thrown when a property is not found in NobillCalls service
    /// Maps to response code 9 from original system
    /// </summary>
    public class PropertyNotFoundException : CreditTransferException
    {
        public PropertyNotFoundException(IConfiguration? configuration = null) : base("Property not found") 
        { 
            ResponseCode = 30;
            ResponseMessage = "Property Not Found";
            SetResponseMessageFromConfiguration(configuration);
        }
    }
    
    /// <summary>
    /// Thrown when transfer is not allowed to the destination account type
    /// </summary>
    public class NotAllowedToTransferCreditToTheDestinationAccountException : CreditTransferException
    {
        public NotAllowedToTransferCreditToTheDestinationAccountException(IConfiguration? configuration = null) : base("Transfer not allowed to this destination account type") 
        { 
            ResponseCode = 33;
            ResponseMessage = "Transfer not allowed to this destination account type";
            SetResponseMessageFromConfiguration(configuration);
        }
    }
    
    /// <summary>
    /// Thrown when PIN doesn't match
    /// </summary>
    public class PinMismatchException : CreditTransferException
    {
        public PinMismatchException(IConfiguration? configuration = null) : base("PIN mismatch") 
        { 
            ResponseCode = 4;
            ResponseMessage = "Invalid credit transfer password";
            SetResponseMessageFromConfiguration(configuration);
        }
    }
    
    /// <summary>
    /// Thrown for miscellaneous errors (like blocked accounts)
    /// </summary>
    public class MiscellaneousErrorException : CreditTransferException
    {
        public MiscellaneousErrorException(IConfiguration? configuration = null) : base("Miscellaneous error occurred") 
        { 
            ResponseCode = 14;
            ResponseMessage = "Miscellaneous error";
            SetResponseMessageFromConfiguration(configuration);
        }
        
        public MiscellaneousErrorException(string message, IConfiguration? configuration = null) : base(message) 
        { 
            ResponseCode = 14;
            ResponseMessage = "Miscellaneous error";
            SetResponseMessageFromConfiguration(configuration);
        }
    }
    
    /// <summary>
    /// Thrown when transfer amount is above maximum allowed
    /// </summary>
    public class TransferAmountAboveMaxException : CreditTransferException
    {
        public TransferAmountAboveMaxException(IConfiguration? configuration = null) : base("Transfer amount exceeds maximum allowed") 
        { 
            ResponseCode = 7;
            ResponseMessage = "Amount requested is more than the maximum amount that can be transferred by the A-party";
            SetResponseMessageFromConfiguration(configuration);
        }
    }
    
    /// <summary>
    /// Thrown when transfer amount is below minimum required
    /// </summary>
    public class TransferAmountBelowMinException : CreditTransferException
    {
        public TransferAmountBelowMinException(IConfiguration? configuration = null) : base("Transfer amount is below minimum required") 
        { 
            ResponseCode = 5;
            ResponseMessage = "Amount requested is less than the minimum transferrable amount by A-party";
            SetResponseMessageFromConfiguration(configuration);
        }
    }
    
    /// <summary>
    /// Thrown when user is not allowed to call a specific method
    /// </summary>
    public class UserNotAllowedToCallThisMethodException : CreditTransferException
    {
        public UserNotAllowedToCallThisMethodException(IConfiguration? configuration = null) : base("User is not authorized to call this method") 
        { 
            ResponseCode = 28;
            ResponseMessage = "User is not authorized to call this method";
            SetResponseMessageFromConfiguration(configuration);
        }
    }
    
    /// <summary>
    /// Thrown when remaining balance should be greater than half balance
    /// Found in original ValidateTransferInputs method
    /// </summary>
    public class RemainingBalanceShouldBeGreaterThanHalfBalance : CreditTransferException
    {
        public RemainingBalanceShouldBeGreaterThanHalfBalance(IConfiguration? configuration = null) : base("Remaining balance should be greater than half of current balance") 
        { 
            ResponseCode = 40;
            ResponseMessage = "Remaining balance should be greater than half of current balance";
            SetResponseMessageFromConfiguration(configuration);
        }
    }
    
    /// <summary>
    /// Thrown when exceeds maximum per day transactions
    /// Found in original business logic
    /// </summary>
    public class ExceedsMaxPerDayTransactionsException : CreditTransferException
    {
        public ExceedsMaxPerDayTransactionsException(IConfiguration? configuration = null) : base("Exceeds maximum number of transactions per day") 
        { 
            ResponseCode = 34;
            ResponseMessage = "Exceeds maximum number of transactions per day";
            SetResponseMessageFromConfiguration(configuration);
        }
    }
    
    /// <summary>
    /// Thrown when remaining balance is insufficient
    /// Found in original business logic
    /// </summary>
    public class RemainingBalanceException : CreditTransferException
    {
        public RemainingBalanceException(IConfiguration? configuration = null) : base("Insufficient remaining balance after transfer") 
        { 
            ResponseCode = 35;
            ResponseMessage = "Insufficient remaining balance after transfer";
            SetResponseMessageFromConfiguration(configuration);
        }
    }
    
    /// <summary>
    /// Thrown when there is insufficient credit for transfer
    /// Found in original business logic
    /// </summary>
    public class InsuffientCreditException : CreditTransferException
    {
        public InsuffientCreditException(IConfiguration? configuration = null) : base("Insufficient credit for transfer") 
        { 
            ResponseCode = 23;
            ResponseMessage = "Insufficient Credit";
            SetResponseMessageFromConfiguration(configuration);
        }
    }
    
    /// <summary>
    /// Thrown for unknown subscriber errors
    /// Found in original business logic
    /// </summary>
    public class UnknownSubscriberException : CreditTransferException
    {
        public UnknownSubscriberException(IConfiguration? configuration = null) : base("Unknown subscriber") 
        { 
            ResponseCode = 2;
            ResponseMessage = "Unknown Subscriber";
            SetResponseMessageFromConfiguration(configuration);
        }
    }

    /// <summary>
    /// Thrown for ConfigurationErrorException
    /// </summary>
    public class ConfigurationErrorException : CreditTransferException
    {
        public ConfigurationErrorException(IConfiguration? configuration = null) : base("ConfigurationError")
        {
            ResponseCode = -1;
            ResponseMessage = "Unknown Subscriber";
            SetResponseMessageFromConfiguration(configuration);
        }
    }

    /// <summary>
    /// Thrown for ExpiredReservationCodeException
    /// </summary>
    public class ExpiredReservationCodeException : CreditTransferException
    {
        public ExpiredReservationCodeException(IConfiguration? configuration = null) : base("ExpiredReservationCodeException")
        {
            ResponseCode = 31;
            ResponseMessage = "ExpiredReservationCodeException";
            SetResponseMessageFromConfiguration(configuration);
        }
    }
    /// <summary>
    /// Thrown for ConcurrentUpdateDetectedException
    /// </summary>
    public class ConcurrentUpdateDetectedException : CreditTransferException
    {
        public ConcurrentUpdateDetectedException(IConfiguration? configuration = null) : base("ConcurrentUpdateDetectedException")
        {
            ResponseCode = 25;
            ResponseMessage = "ConcurrentUpdateDetectedException";
            SetResponseMessageFromConfiguration(configuration);
        }
    }
    /// <summary>
    /// Thrown for SMSFailureException
    /// </summary>
    public class SMSFailureException : CreditTransferException
    {
        public SMSFailureException(IConfiguration? configuration = null) : base("SMSFailureException")
        {
            ResponseCode = 37;
            ResponseMessage = "SMSFailureException";
            SetResponseMessageFromConfiguration(configuration);
        }
    }
    

} 
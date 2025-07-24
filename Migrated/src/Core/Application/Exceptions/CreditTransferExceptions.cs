using CreditTransfer.Core.Application.Interfaces;

namespace CreditTransfer.Core.Application.Exceptions
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
        /// Sets the response message from the application config repository.
        /// This method should be called before the exception is thrown.
        /// </summary>
        public async Task SetResponseMessageAsync(IApplicationConfigRepository configRepository)
        {
            if (configRepository != null)
            {
                // Look for response message in ResponseMessages section first, then fall back to direct key lookup
                var responseMessage = await configRepository.GetConfigValueAsync($"ResponseMessages:{ResponseCode}") 
                                    ?? await configRepository.GetConfigValueAsync(ResponseCode.ToString());
                
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
        public InvalidSourcePhoneException() : base("Invalid source phone number") 
        { 
            ResponseCode = 20;
            ResponseMessage = "Invalid Source Phone Number";
        }
    }
    
    /// <summary>
    /// Thrown when destination phone number is invalid
    /// </summary>
    public class InvalidDestinationPhoneException : CreditTransferException
    {
        public InvalidDestinationPhoneException() : base("Invalid destination phone number") 
        { 
            ResponseCode = 21;
            ResponseMessage = "Invalid Destination Phone Number";
        }
    }
    
    /// <summary>
    /// Thrown when PIN is invalid
    /// </summary>
    public class InvalidPinException : CreditTransferException
    {
        public InvalidPinException() : base("Invalid PIN") 
        { 
            ResponseCode = 22;
            ResponseMessage = "Invalid PIN";
        }
    }
    
    /// <summary>
    /// Thrown when source and destination numbers are the same
    /// </summary>
    public class SourceAndDestinationSameException : CreditTransferException
    {
        public SourceAndDestinationSameException() : base("Source and destination numbers cannot be the same") 
        { 
            ResponseCode = 3;
            ResponseMessage = "A-party and B-party phone numbers are same";
        }
    }
    
    /// <summary>
    /// Thrown when source phone number is not found in the system
    /// </summary>
    public class SourcePhoneNumberNotFoundException : CreditTransferException
    {
        public SourcePhoneNumberNotFoundException() : base("Source phone number not found") 
        { 
            ResponseCode = 26;
            ResponseMessage = "Source phone number not found";
        }
    }
    
    /// <summary>
    /// Thrown when destination phone number is not found in the system
    /// </summary>
    public class DestinationPhoneNumberNotFoundException : CreditTransferException
    {
        public DestinationPhoneNumberNotFoundException() : base("Destination phone number not found") 
        { 
            ResponseCode = 27;
            ResponseMessage = "Destination phone number not found";
        }
    }
    
    /// <summary>
    /// Thrown when subscription is not found
    /// </summary>
    public class SubscriptionNotFoundException : CreditTransferException
    {
        public SubscriptionNotFoundException() : base("Subscription not found") 
        { 
            ResponseCode = 24;
            ResponseMessage = "Subscription Not Found";
        }
    }
    
    /// <summary>
    /// Thrown when a property is not found in NobillCalls service
    /// Maps to response code 9 from original system
    /// </summary>
    public class PropertyNotFoundException : CreditTransferException
    {
        public PropertyNotFoundException() : base("Property not found") 
        { 
            ResponseCode = 30;
            ResponseMessage = "Property Not Found";
        }
    }
    
    /// <summary>
    /// Thrown when transfer is not allowed to the destination account type
    /// </summary>
    public class NotAllowedToTransferCreditToTheDestinationAccountException : CreditTransferException
    {
        public NotAllowedToTransferCreditToTheDestinationAccountException() : base("Transfer not allowed to this destination account type") 
        { 
            ResponseCode = 33;
            ResponseMessage = "Transfer not allowed to this destination account type";
        }
    }
    
    /// <summary>
    /// Thrown when PIN doesn't match
    /// </summary>
    public class PinMismatchException : CreditTransferException
    {
        public PinMismatchException() : base("PIN mismatch") 
        { 
            ResponseCode = 4;
            ResponseMessage = "Invalid credit transfer password";
        }
    }
    
    /// <summary>
    /// Thrown for miscellaneous errors (like blocked accounts)
    /// </summary>
    public class MiscellaneousErrorException : CreditTransferException
    {
        public MiscellaneousErrorException() : base("Miscellaneous error occurred") 
        { 
            ResponseCode = 14;
            ResponseMessage = "Miscellaneous error";
        }
        
        public MiscellaneousErrorException(string message) : base(message) 
        { 
            ResponseCode = 14;
            ResponseMessage = "Miscellaneous error";
        }
    }
    
    /// <summary>
    /// Thrown when transfer amount is above maximum allowed
    /// </summary>
    public class TransferAmountAboveMaxException : CreditTransferException
    {
        public TransferAmountAboveMaxException() : base("Transfer amount exceeds maximum allowed") 
        { 
            ResponseCode = 7;
            ResponseMessage = "Amount requested is more than the maximum amount that can be transferred by the A-party";
        }
    }
    
    /// <summary>
    /// Thrown when transfer amount is below minimum required
    /// </summary>
    public class TransferAmountBelowMinException : CreditTransferException
    {
        public TransferAmountBelowMinException() : base("Transfer amount is below minimum required") 
        { 
            ResponseCode = 5;
            ResponseMessage = "Amount requested is less than the minimum transferrable amount by A-party";
        }
    }
    
    /// <summary>
    /// Thrown when user is not allowed to call a specific method
    /// </summary>
    public class UserNotAllowedToCallThisMethodException : CreditTransferException
    {
        public UserNotAllowedToCallThisMethodException() : base("User is not authorized to call this method") 
        { 
            ResponseCode = 28;
            ResponseMessage = "User is not authorized to call this method";
        }
    }
    
    /// <summary>
    /// Thrown when remaining balance should be greater than half balance
    /// Found in original ValidateTransferInputs method
    /// </summary>
    public class RemainingBalanceShouldBeGreaterThanHalfBalance : CreditTransferException
    {
        public RemainingBalanceShouldBeGreaterThanHalfBalance() : base("Remaining balance should be greater than half of current balance") 
        { 
            ResponseCode = 40;
            ResponseMessage = "Remaining balance should be greater than half of current balance";
        }
    }
    
    /// <summary>
    /// Thrown when exceeds maximum per day transactions
    /// Found in original business logic
    /// </summary>
    public class ExceedsMaxPerDayTransactionsException : CreditTransferException
    {
        public ExceedsMaxPerDayTransactionsException() : base("Exceeds maximum number of transactions per day") 
        { 
            ResponseCode = 34;
            ResponseMessage = "Exceeds maximum number of transactions per day";
        }
    }
    
        /// <summary>
        /// Thrown when credit transfer service is blocked
        /// </summary>
        public class CreditTransferServiceIsBlocked : CreditTransferException
        {
            public CreditTransferServiceIsBlocked() : base("Credit transfer service is blocked")
            {
                ResponseCode = 41;
                ResponseMessage = "Credit transfer service is blocked";
            }
        }

        /// <summary>
        /// Thrown when OCS times out
        /// </summary>
        public class OCSTimeoutException : CreditTransferException
        {
            public OCSTimeoutException() : base("OCS timeout occurred")
            {
                ResponseCode = 42;
                ResponseMessage = "OCS timeout occurred";
            }
        }
    
    /// <summary>
    /// Thrown when exceeds maximum daily transfer amount cap
    /// Used for validating total daily transfer amount limits
    /// </summary>
    public class ExceedsMaxCapPerDayTransactionsException : CreditTransferException
    {
        public ExceedsMaxCapPerDayTransactionsException() : base("Subscription has been reached the cap of transfers per day") 
        { 
            ResponseCode = 43;
            ResponseMessage = "Subscription has been reached the cap of transfers per day";
        }
    }
    
    /// <summary>
    /// Thrown when remaining balance is insufficient
    /// Found in original business logic
    /// </summary>
    public class RemainingBalanceException : CreditTransferException
    {
        public RemainingBalanceException() : base("Insufficient remaining balance after transfer") 
        { 
            ResponseCode = 35;
            ResponseMessage = "Insufficient remaining balance after transfer";
        }
    }
    
    /// <summary>
    /// Thrown when there is insufficient credit for transfer
    /// Found in original business logic
    /// </summary>
    public class InsuffientCreditException : CreditTransferException
    {
        public InsuffientCreditException() : base("Insufficient credit for transfer") 
        { 
            ResponseCode = 23;
            ResponseMessage = "Insufficient Credit";
        }
    }
    
    /// <summary>
    /// Thrown for unknown subscriber errors
    /// Found in original business logic
    /// </summary>
    public class UnknownSubscriberException : CreditTransferException
    {
        public UnknownSubscriberException() : base("Unknown subscriber") 
        { 
            ResponseCode = 2;
            ResponseMessage = "Unknown Subscriber";
        }
    }

    /// <summary>
    /// Thrown for ConfigurationErrorException
    /// </summary>
    public class ConfigurationErrorException : CreditTransferException
    {
        public ConfigurationErrorException() : base("ConfigurationError")
        {
            ResponseCode = -1;
            ResponseMessage = "Unknown Subscriber";
        }
    }

    /// <summary>
    /// Thrown for ExpiredReservationCodeException
    /// </summary>
    public class ExpiredReservationCodeException : CreditTransferException
    {
        public ExpiredReservationCodeException() : base("ExpiredReservationCodeException")
        {
            ResponseCode = 31;
            ResponseMessage = "ExpiredReservationCodeException";
        }
    }
    /// <summary>
    /// Thrown for ConcurrentUpdateDetectedException
    /// </summary>
    public class ConcurrentUpdateDetectedException : CreditTransferException
    {
        public ConcurrentUpdateDetectedException() : base("ConcurrentUpdateDetectedException")
        {
            ResponseCode = 25;
            ResponseMessage = "ConcurrentUpdateDetectedException";
        }
    }
    /// <summary>
    /// Thrown for SMSFailureException
    /// </summary>
    public class SMSFailureException : CreditTransferException
    {
        public SMSFailureException() : base("SMSFailureException")
        {
            ResponseCode = 37;
            ResponseMessage = "SMSFailureException";
        }
    }
} 
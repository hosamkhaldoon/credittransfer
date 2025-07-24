namespace CreditTransfer.Core.Domain.Constants;

/// <summary>
/// Constants for credit transfer error codes
/// These codes correspond exactly to the original exception response codes
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// Operation completed successfully
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// Unknown Subscriber - Maps to UnknownSubscriberException (original error code 2)
    /// </summary>
    public const int UnknownSubscriber = 2;

    /// <summary>
    /// A-party and B-party phone numbers are same - Maps to SourceAndDestinationSameException (original error code 3)
    /// </summary>
    public const int SourceDestinationSame = 3;

    /// <summary>
    /// Invalid credit transfer password - Maps to PinMismatchException (original error code 4)
    /// </summary>
    public const int InvalidPin = 4;

    /// <summary>
    /// Amount requested is less than the minimum transferrable amount by A-party - Maps to TransferAmountBelowMinException (original error code 5)
    /// </summary>
    public const int TransferAmountBelowMin = 5;

    /// <summary>
    /// Amount requested is more than the maximum amount that can be transferred by the A-party - Maps to TransferAmountAboveMaxException (original error code 7)
    /// </summary>
    public const int TransferAmountAboveMax = 7;

    /// <summary>
    /// Miscellaneous error - Maps to MiscellaneousErrorException (original error code 14)
    /// </summary>
    public const int MiscellaneousError = 14;

    /// <summary>
    /// Invalid Source Phone Number - Maps to InvalidSourcePhoneException (original error code 20)
    /// </summary>
    public const int InvalidSourcePhone = 20;

    /// <summary>
    /// Invalid Destination Phone Number - Maps to InvalidDestinationPhoneException (original error code 21)
    /// </summary>
    public const int InvalidDestinationPhone = 21;

    /// <summary>
    /// Invalid PIN - Maps to InvalidPinException (original error code 22)
    /// </summary>
    public const int InvalidPIN = 22;

    /// <summary>
    /// Insufficient Credit - Maps to InsuffientCreditException (original error code 23)
    /// </summary>
    public const int InsufficientBalance = 23;

    /// <summary>
    /// Subscription Not Found - Maps to SubscriptionNotFoundException (original error code 24)
    /// </summary>
    public const int SubscriptionNotFound = 24;

    /// <summary>
    /// Concurrent Update Detected - Maps to ConcurrentUpdateDetectedException (original error code 25)
    /// </summary>
    public const int ConcurrentUpdateDetected = 25;

    /// <summary>
    /// Source Subscription Not Found - Maps to SourcePhoneNumberNotFoundException (original error code 26)
    /// </summary>
    public const int SourcePhoneNotFound = 26;

    /// <summary>
    /// Destination Subscription Not Found - Maps to DestinationPhoneNumberNotFoundException (original error code 27)
    /// </summary>
    public const int DestinationPhoneNotFound = 27;

    /// <summary>
    /// User Not Allowed To Call This Method - Maps to UserNotAllowedToCallThisMethodException (original error code 28)
    /// </summary>
    public const int UserNotAllowed = 28;

    /// <summary>
    /// Configuration Error - Maps to ConfigurationErrorException (original error code 29)
    /// </summary>
    public const int ConfigurationError = 29;

    /// <summary>
    /// Property Not Found - Maps to PropertyNotFoundException (original error code 30)
    /// </summary>
    public const int PropertyNotFound = 30;

    /// <summary>
    /// Expired Reservation Code - Maps to ExpiredReservationCodeException (original error code 31)
    /// </summary>
    public const int ExpiredReservationCode = 31;

    /// <summary>
    /// Bad Request - No corresponding original exception (error code 32)
    /// Note: This code exists in ErrorCodes but has no matching original exception
    /// </summary>
    public const int BadRequest = 32;

    /// <summary>
    /// Credit transfer is not allowed to this such account type - Maps to NotAllowedToTransferCreditToTheDestinationAccountException (original error code 33)
    /// </summary>
    public const int NotAllowedToTransfer = 33;

    /// <summary>
    /// Subscription has been reached the number of transfers per day - Maps to ExceedsMaxPerDayTransactionsException (original error code 34)
    /// </summary>
    public const int ExceedsMaxPerDay = 34;

    /// <summary>
    /// Insufficient Remaining Credit - Maps to RemainingBalanceException (original error code 35)
    /// </summary>
    public const int RemainingBalance = 35;

    /// <summary>
    /// Transfer amount should be a multiple of 5 - Maps to TransferAmountNotValid (original error code 36)
    /// </summary>
    public const int AmountNotMultipleOfFive = 36;

    /// <summary>
    /// An error occurred while sending sms to the destination msisdn - Maps to SMSFailureException (original error code 37)
    /// </summary>
    public const int SmsError = 37;

    /// <summary>
    /// An error occurred while reserve amount - Maps to ReserveAmountException (original error code 38)
    /// </summary>
    public const int ReserveAmountError = 38;

    /// <summary>
    /// Failed to credit the amount to the source msisdn - Maps to FailedToCreditAmountToTheSourceMsisdn (original error code 39)
    /// </summary>
    public const int CreditFailure = 39;

    /// <summary>
    /// Your balance remaining should be more than 50% - Maps to RemainingBalanceShouldBeGreaterThanHalfBalance (original error code 40)
    /// </summary>
    public const int RemainingBalanceHalf = 40;

    /// <summary>
    /// Credit Transfer Service Is Blocked - Maps to CreditTransferServiceIsBlocked (original error code 41)
    /// </summary>
    public const int CreditTransferServiceIsBlocked = 41;

    /// <summary>
    /// OCS Timeout - Maps to OCSTimeoutException (original error code 42)
    /// </summary>
    public const int OCSTimeout = 42;

    /// <summary>
    /// Exceeds Maximum Capacity Per Day - Maps to ExceedsMaxCapPerDayTransactionsException (original error code 43)
    /// </summary>
    public const int ExceedsMaxCapPerDay = 43;

    /// <summary>
    /// External service unavailable or parsing error
    /// Note: This is a new error code not present in the original system
    /// </summary>
    public const int ServiceUnavailable = 999;
}
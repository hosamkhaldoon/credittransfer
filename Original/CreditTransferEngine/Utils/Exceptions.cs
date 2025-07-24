using System;
using System.Configuration;

namespace CreditTransferEngine.Utils
{
    public class CreditTransferException : Exception
    {
        public int ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
    }

    public class UnknownSubscriberException : CreditTransferException
    {
        public UnknownSubscriberException()
        {
            ResponseCode = 2;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }

    public class SourceAndDestinationSameException : CreditTransferException
    {
        public SourceAndDestinationSameException()
        {
            ResponseCode = 3;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];

        }
    }

    public class PinMismatchException : CreditTransferException
    {
        public PinMismatchException()
        {
            ResponseCode = 4;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }

    public class TransferAmountBelowMinException : CreditTransferException
    {
        public TransferAmountBelowMinException()
        {
            ResponseCode = 5;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }

    public class TransferAmountAboveMaxException : CreditTransferException
    {
        public TransferAmountAboveMaxException()
        {
            ResponseCode = 7;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }
    
    public class MiscellaneousErrorException : CreditTransferException
    {
        public MiscellaneousErrorException()
        {
            ResponseCode = 14;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }

    public class InvalidSourcePhoneException : CreditTransferException
    {
        public InvalidSourcePhoneException()
        {
            ResponseCode = 20;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }

    public class InvalidDestinationPhoneException : CreditTransferException
    {
        public InvalidDestinationPhoneException()
        {
            ResponseCode = 21;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }

    public class InvalidPinException : CreditTransferException
    {
        public InvalidPinException()
        {
            ResponseCode = 22;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }

    public class InsuffientCreditException : CreditTransferException
    {
        public InsuffientCreditException()
        {
            ResponseCode = 23;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }

    public class SubscriptionNotFoundException : CreditTransferException
    {
        public SubscriptionNotFoundException()
        {
            ResponseCode = 24;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }

    public class ConcurrentUpdateDetectedException : CreditTransferException
    {
        public ConcurrentUpdateDetectedException()
        {
            ResponseCode = 25;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }

    public class SourcePhoneNumberNotFoundException : CreditTransferException
    {
        public SourcePhoneNumberNotFoundException()
        {
            ResponseCode = 26;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }

    public class DestinationPhoneNumberNotFoundException : CreditTransferException
    {
        public DestinationPhoneNumberNotFoundException()
        {
            ResponseCode = 27;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }

    public class UserNotAllowedToCallThisMethodException : CreditTransferException
    {
        public UserNotAllowedToCallThisMethodException()
        {
            ResponseCode = 28;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }
    
    public class ConfigurationErrorException : CreditTransferException
    {
        public ConfigurationErrorException()
        {
            ResponseCode = 29;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }
    
    public class PropertyNotFoundException : CreditTransferException
    {
        public PropertyNotFoundException()
        {
            ResponseCode = 30;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }

    public class ExpiredReservationCodeException : CreditTransferException
    {
        public ExpiredReservationCodeException()
        {
            ResponseCode = 31;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }

    public class NotAllowedToTransferCreditToTheDestinationAccountException : CreditTransferException
    {
        public NotAllowedToTransferCreditToTheDestinationAccountException()
        {
            ResponseCode = 33;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }

    public class ExceedsMaxPerDayTransactionsException : CreditTransferException
    {
        public ExceedsMaxPerDayTransactionsException()
        {
            ResponseCode = 34;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }

    public class RemainingBalanceException : CreditTransferException
    {
        public RemainingBalanceException()
        {
            ResponseCode = 35;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }

    public class TransferAmountNotValid : CreditTransferException
    {
        public TransferAmountNotValid()
        {
            ResponseCode = 36;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }

    public class SMSFailureException : CreditTransferException
    {
        public SMSFailureException()
        {
            ResponseCode = 37;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }

    public class ReserveAmountException : CreditTransferException
    {
        public ReserveAmountException()
        {
            ResponseCode = 38;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }

    public class FailedToCreditAmountToTheSourceMsisdn : CreditTransferException
    {
        public FailedToCreditAmountToTheSourceMsisdn()
        {
            ResponseCode = 39;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }

    public class RemainingBalanceShouldBeGreaterThanHalfBalance : CreditTransferException
    {
        public RemainingBalanceShouldBeGreaterThanHalfBalance()
        {
            ResponseCode = 40;
            ResponseMessage = ConfigurationManager.AppSettings[ResponseCode.ToString()];
        }
    }

}
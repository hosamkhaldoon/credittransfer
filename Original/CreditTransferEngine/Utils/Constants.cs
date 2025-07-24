namespace CreditTransferEngine.Utils
{
    public class Constants
    {
        public const int ServiceUnavailableCode = -1;
        public const string ServiceUnavailableMessage = "Service Unavailable";
        public const string CreditTransferEngineUserName = "CreditTransferEngine";
        public const string SuccessMessage = "OK";
        public const int SuccessCode = 0;
    }

    public enum IDNumberResultStatus
    {
        SUCCESS = 0,
        ID_IS_NOT_MATCHED = 1,
        CIVIL_ID_IS_NOT_MATCHING = 2,
        UNKNOWN_ERROR = 3,
    }

}
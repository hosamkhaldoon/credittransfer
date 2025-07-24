using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace CreditTransfer.Utils
{
    public class HttpAPICommands
    {
        public const string USSD = "ussd";
        public const string VirginUSSD = "virgincredittransfer";
    }

    public class ServiceHttpResponse
    {
        public const string USSDPositive = "OK";
        public const string USSDNegative = "NOK";
        public const int USSDGenericErrorCode = -1;
    }

    public class XmlUSSDParams
    {
        public const string SourceMsisdn = "SourceMsisdn";
        public const string DestinationMsisdn = "DestinationMsisdn";
        public const string AmountRiyal = "AmountRiyal";
        public const string AmountBaisa = "AmountBaisa";
        public const string PIN = "PINNumber";
    }

    public class HttpResponseTemplates
    {
        public const string USSD = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><umsprot version=\"1\"><exec_rsp result=\"{0}\" diagnostic=\"\"><data name=\"failreason\">{1}</data></exec_rsp></umsprot>";
    }

    public class HandlerResponse
    {
        public const int BadRequestCode = 32;
        public static string BadRequestMessage = ConfigurationManager.AppSettings[BadRequestCode.ToString()];

        public const int ServiceUnavailableCode = -1;
        public const string ServiceUnavailableMessage = "NOK";
    }
}
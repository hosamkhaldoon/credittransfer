using System.Net;
using System.Configuration;

namespace CreditTransferEngine.Utils
{
    public class Common
    {
        public static NetworkCredential GetServiceCredentials()
        {
            string userName = ConfigurationManager.AppSettings.Get("NobillCallsServiceUserName");
            string password = ConfigurationManager.AppSettings.Get("NobillCallsServicePassword");
            return new NetworkCredential(userName, password);
        }
    }
}
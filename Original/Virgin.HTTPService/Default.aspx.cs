using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using System.Configuration;

namespace Virgin.HTTPService
{
    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.QueryString.HasKeys())
            {
                string content = Request.QueryString["idnumber"];
                string from = Request.QueryString["from"];
                string to = Request.QueryString["to"];

                //Validate the IdNumber
                string destinationMsisdn = from.Trim(' ');
                string destinationIdNumber = content.Trim(' ');
                

                using (CreditTransfer.CreditTransferServiceClient client = new CreditTransfer.CreditTransferServiceClient())
                {
                    client.ClientCredentials.Windows.ClientCredential.UserName = ConfigurationManager.AppSettings["TransferCreditServiceUserName"];
                    client.ClientCredentials.Windows.ClientCredential.Password = ConfigurationManager.AppSettings["TransferCreditServicePassword"];
                    client.GetVirginPendingTransaction(destinationMsisdn, destinationIdNumber);
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.IO;
using System.Configuration;

namespace Virgin.HTTPIVRService
{
    //<?xml version="1.0" encoding="UTF-8" ?><umsprot version="1"><exec_req><data name="SourceMsisdn">95875454544</data><data name="DestinationMsisdn">95875454545</data><data name="AmountRiyal">3</data><data name="AmountHalala">0</data><data name="PIN">0000</data></exec_req></umsprot>
    public class CreditTransferHandler : IHttpHandler
    {
        #region IHttpHandler Members

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            int responseCode = -1;
            string sourceMsisdn = string.Empty;
            string destMsisdn = string.Empty;
            string amountRiyal = string.Empty;
            string amountBaisa = string.Empty;
            string pin = string.Empty;
            try
            {
                System.Diagnostics.EventLog.WriteEntry("IVR_USSD_REQUEST", context.Request.Url.ToString());

                if (context.Request.QueryString.HasKeys())
                {

                    sourceMsisdn = context.Request.QueryString["sourcemsisdn"];
                    System.Diagnostics.EventLog.WriteEntry("IVR_USSD_REQUEST", "sourceMsisdn:" + sourceMsisdn);
                    destMsisdn = context.Request.QueryString["destinationmsisdn"];
                    System.Diagnostics.EventLog.WriteEntry("IVR_USSD_REQUEST", "destMsisdn:" + destMsisdn);
                    amountRiyal = context.Request.QueryString["amountriyal"];
                    System.Diagnostics.EventLog.WriteEntry("IVR_USSD_REQUEST", "amountRiyal:" + amountRiyal);
                    amountBaisa = context.Request.QueryString["amountbaisa"];
                    System.Diagnostics.EventLog.WriteEntry("IVR_USSD_REQUEST", "amountBaisa:" + amountBaisa);
                    pin = context.Request.QueryString["pin"];
                    System.Diagnostics.EventLog.WriteEntry("IVR_USSD_REQUEST", "pin:"+ pin);

                    //Validate the IdNumber
                    sourceMsisdn = sourceMsisdn.Trim(' ');
                    destMsisdn = destMsisdn.Trim(' ');
                    amountRiyal = amountRiyal.Trim(' ');
                    amountBaisa = amountBaisa.Trim(' ');
                    pin = pin.Trim(' ');

                    System.Diagnostics.EventLog.WriteEntry("IVR_USSD_REQUEST", "After Trimming");

                    string message = string.Empty;


                    using (HTTPIVRService.CreditTransfer.CreditTransferServiceClient client = new HTTPIVRService.CreditTransfer.CreditTransferServiceClient())
                    {
                        System.Diagnostics.EventLog.WriteEntry("IVR_USSD_REQUEST", "Before Calling Service");

                        client.ClientCredentials.Windows.ClientCredential.UserName = ConfigurationManager.AppSettings["TransferCreditServiceUserName"];
                        client.ClientCredentials.Windows.ClientCredential.Password = ConfigurationManager.AppSettings["TransferCreditServicePassword"];

                        System.Diagnostics.EventLog.WriteEntry("IVR_USSD_REQUEST", "Setting Credentails");

                        string requestPath = context.Request.Path;


                        responseCode = client.TransferCredit(out message, sourceMsisdn, destMsisdn, Convert.ToInt32(amountRiyal), Convert.ToInt32(amountBaisa), pin);

                    }
                }
            }


            catch (Exception exp)
            {
                System.Diagnostics.EventLog.WriteEntry("IVRCreditTransfer", string.Format("from {0} to {1} amount riyal {2} amount Baisa {3} pin {4}", sourceMsisdn, destMsisdn, amountRiyal, amountBaisa, pin) + " " + exp.Message + (exp.InnerException == null ? "" : exp.InnerException.Message));
            }

            //Response.Write(responseCode);

            context.Response.ContentType = "text/html";
            context.Response.Write(string.Format("response_code={0}", responseCode));
        }
        #endregion

    }
}
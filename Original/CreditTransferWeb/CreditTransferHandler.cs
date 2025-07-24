using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.IO;
using CreditTransfer.Utils;
using System.Configuration;

namespace CreditTransfer
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
            //System.IO.File.AppendAllText(@"C:\inetpub\wwwroot\CreditTransferHTTPService\CTLog.txt", "\n\n\n\n\r\r1- Request Received  --  " + context.Request.Path);
            string requestPath = context.Request.Path;
            //System.IO.File.AppendAllText(@"C:\inetpub\wwwroot\CreditTransferHTTPService\CTLog.txt", "\n\rpath: " + requestPath);
           
                //System.IO.File.AppendAllText(@"C:\inetpub\wwwroot\CreditTransferHTTPService\CTLog.txt", "\n\rpassed");
                context.Response.Write(ProcessUSSD(context));
           
        }

        private string ProcessUSSD(HttpContext context)
        {
            
            //System.IO.File.AppendAllText(@"C:\inetpub\wwwroot\CreditTransferHTTPService\CTLog.txt", "\n\r2- Enter ProcessUSSD");
            context.Response.AddHeader("Content-type", "application / xml");

            string responseResult = ServiceHttpResponse.USSDNegative;
            int responseCode = ServiceHttpResponse.USSDGenericErrorCode;
            bool isValidXML = false;

            XmlDocument document = new System.Xml.XmlDocument();

            //using (StreamReader reader = new StreamReader("c:/test.xml"))
            try
            {
                using (StreamReader reader = new StreamReader(context.Request.InputStream))
                {
                    string encodedString = reader.ReadToEnd();
                    string request = HttpUtility.UrlDecode(encodedString);

                    document.LoadXml(request);
                }

                //System.IO.File.AppendAllText(@"C:\inetpub\wwwroot\CreditTransferHTTPService\CTLog.txt", "\n\r3- document.LoadXml(request) done");

                XmlNodeList nodess = document.GetElementsByTagName("exec_req");
                if (nodess == null || nodess.Count == 0)
                {
                    responseCode = HandlerResponse.BadRequestCode;
                    responseResult = HandlerResponse.BadRequestMessage;
                    //System.IO.File.AppendAllText(@"C:\inetpub\wwwroot\CreditTransferHTTPService\CTLog.txt", "\n\r4- if (nodess == null || nodess.Count == 0) bad request");
                }
                else
                {
                    string sourceMsisdn = string.Empty;
                    string destinationMsisdn = string.Empty;
                    int amountRiyal = 0;
                    int amountHalala = 0;
                    string pin = string.Empty;

                    XmlNodeList xmlList = document.GetElementsByTagName("data");
                    if (xmlList == null || xmlList.Count == 0)
                    {
                        responseCode = HandlerResponse.BadRequestCode;
                        responseResult = HandlerResponse.BadRequestMessage;
                        //System.IO.File.AppendAllText(@"C:\inetpub\wwwroot\CreditTransferHTTPService\CTLog.txt", "\n\r5- if (xmlList == null || xmlList.Count == 0) bad request");
                    }
                    else
                    {
                        foreach (XmlNode node in xmlList)
                        {
                            switch (node.Attributes["name"].Value)
                            {
                                case XmlUSSDParams.SourceMsisdn:
                                    sourceMsisdn = node.InnerText;
                                    break;
                                case XmlUSSDParams.DestinationMsisdn:
                                    destinationMsisdn = node.InnerText;
                                    break;
                                case XmlUSSDParams.AmountRiyal:
                                    amountRiyal = Convert.ToInt32(node.InnerText);
                                    break;
                                case XmlUSSDParams.AmountBaisa:
                                    amountHalala = Convert.ToInt32(node.InnerText);
                                    break;
                                case XmlUSSDParams.PIN:
                                    pin = node.InnerText;
                                    break;
                            }

                            //System.IO.File.AppendAllText(@"C:\inetpub\wwwroot\CreditTransferHTTPService\CTLog.txt", "\n\r6,7,8,9,10- foreach (XmlNode node in xmlList)  -- name=" + node.Attributes["name"].Value + "   - value=" + node.InnerText);
                        }

                        isValidXML = true;
                        
                        //as the distination MSISDN recieved from USSD in national format
                        destinationMsisdn = ConfigurationManager.AppSettings["CountryCode"] + destinationMsisdn.TrimStart('0');

                        using (CreditTransferService.CreditTransferServiceClient creditTransferServiceClient = new CreditTransferService.CreditTransferServiceClient())
                        {
                            //creditTransferServiceClient.ClientCredentials.UserName.UserName = ConfigurationManager.AppSettings["TransferCreditServiceUserName"];
                            //creditTransferServiceClient.ClientCredentials.UserName.Password = ConfigurationManager.AppSettings["TransferCreditServicePassword"];
                            creditTransferServiceClient.ClientCredentials.Windows.ClientCredential.UserName = ConfigurationManager.AppSettings["TransferCreditServiceUserName"];
                            creditTransferServiceClient.ClientCredentials.Windows.ClientCredential.Password = ConfigurationManager.AppSettings["TransferCreditServicePassword"];
                            responseCode = creditTransferServiceClient.TransferCredit(out responseResult, sourceMsisdn, destinationMsisdn, amountRiyal, amountHalala, pin);
                        }

                        //System.IO.File.AppendAllText(@"C:\inetpub\wwwroot\CreditTransferHTTPService\CTLog.txt", "\n\r11- Service Called");
                    }
                }
            }
            catch (Exception ex)
            {
                //System.IO.File.AppendAllText(@"C:\inetpub\wwwroot\CreditTransferHTTPService\CTLog.txt", "\n\r12- exception");
                if (!isValidXML)
                {
                    responseCode = HandlerResponse.BadRequestCode;
                    responseResult = HandlerResponse.BadRequestMessage;
                }
                else
                {
                    responseCode = HandlerResponse.ServiceUnavailableCode;
                    responseResult = HandlerResponse.ServiceUnavailableMessage;
                }
            }

           
             responseResult = "OK";
            

            return string.Format(HttpResponseTemplates.USSD, responseResult, responseCode);
        }

        #endregion

    }
}
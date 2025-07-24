using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace CreditTransferTestClient
{
    public partial class frmTest : Form
    {
        public frmTest()
        {
            InitializeComponent();
        }

        private void btnProcessTransactions_Click(object sender, EventArgs e)
        {
            CreditTransferEngine.BusinessLogic.TransactionManager.ProcessTransactions();
        }

        private void btnTestHTTPService_Click(object sender, EventArgs e)
        {
//            string ussdMessage = @"<?xml version='1.0' encoding='UTF-8' ?><umsprot version='1'>
//                                <exec_req><data name='SourceMsisdn'>966570001239</data>
//                                <data name='DestinationMsisdn'>0571402236</data>
//                                <data name='AmountRiyal'>51</data>
//                                <data name='AmountHalala'>0</data>
//                                <data name='PINNumber'>0000</data>
//                                </exec_req></umsprot>";

            string ussdMessage = @"<?xml version='1.0' encoding='UTF-8' ?><umsprot version='1'>
                                <exec_req><data name='SourceMsisdn'>966570281949</data>
                                <data name='DestinationMsisdn'>0570745115</data>
                                <data name='AmountRiyal'>5</data>
                                <data name='AmountHalala'>0</data>
                                <data name='PINNumber'>1234</data>
                                </exec_req></umsprot>";

            

            System.Net.WebClient client = new System.Net.WebClient();
            client.Headers.Add("Content-type", "application / xml");
            //string result = client.UploadString("http://10.4.1.48/CreditTransferHttpServiceAquaUAT/virgincredittransfer", ussdMessage);
            //string result = client.UploadString("http://localhost/CreditTransfer/ussd", ussdMessage);
            //string result = client.UploadString("http://10.4.1.48/CreditTransferHttpServiceAquaUAT/ussd", ussdMessage);
            string result = client.UploadString("http://localhost/CreditTransfer/virgincredittransfer", ussdMessage);
            MessageBox.Show(result);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                EventLog.Delete("koko");
            }
            catch (Exception) { }

            try
            {
                EventLog.Delete("CreditTransfer");
            }
            catch (Exception) { }

            try
            {
                EventLog.Delete("CreditTransferService");
            }
            catch (Exception) { }

            try
            {
                EventLog.DeleteEventSource("koko");
            }
            catch (Exception) { }
            try
            {
                EventLog.DeleteEventSource("CreditTransfer");
            }
            catch (Exception) { }
            try
            {
                EventLog.DeleteEventSource("CreditTransferService");
            }
            catch (Exception) { }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController("CreditTransfer");
            //sc.Start();

            if (!EventLog.Exists("CreditTransferService"))
            {
                EventLog.CreateEventSource("CreditTransferService", "CreditTransferService");
            }

            CreditTransferWindowsService.CreditTransfer ct = new CreditTransferWindowsService.CreditTransfer();
            ct.ProcessTransactions();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //using (ProductSelling.ProductSellingServiceClient  client = new ProductSelling.ProductSellingServiceClient())
            //{
            //var xx=    client.SubmitProductSelling(new ProductSelling.ProductSellingRequest()
            //    {
            //        DealerCode = "99999",
            //        MSISDN = "96878752511",
            //        ProductId = 3
            //    });

            //    var xxx = 10;
            //}
            decimal amount = 0.5m;
            int ryals = (int)amount;
            int baisa = (int)(amount % 1 * 1000);

            string refStr=string.Empty;

            CreditTransferEngine.BusinessLogic.CreditTransfer.TransferCredit("96879568730", "96878931574", amount, "0000", "wsc" ,ref refStr);

            ////decimal amount = Convert.ToDecimal(amountRiyal) + Convert.ToDecimal(amountHalala) / Convert.ToDecimal(100);
            //string receivedRequest = "";

            ////CreditTransferEngine.BusinessLogic.CreditTransfer.ValidateTransferInputs("96879544251", "96878021653", 1);
            ////CreditTransferEngine.BusinessLogic.CreditTransfer.TransferCreditWithoutPin("96879544251", "96878021653", 1, "wsc", ref receivedRequest);
            //using (CreditTransfer.CreditTransferServiceClient client = new CreditTransfer.CreditTransferServiceClient())
            //{
            //    client.ClientCredentials.Windows.ClientCredential.UserName = "itgma";
            //    client.ClientCredentials.Windows.ClientCredential.Password = "#0!tgma0";
            //    string msg;
            //    var rst = client.ValidateTransferInputs(out msg, "96879544251", "96878021653", 1);

            //    CreditTransferEngine.BusinessLogic.CreditTransfer.TransferCreditWithAdjustmentReason(out msg1, "96879544251", "96878021653", ryals, baisa, "0000", "POS_DealerExclusivePlan");
            //    var rst1 = client.TransferCreditWithAdjustmentReason(out msg, "96879544251", "96878021653", ryals,baisa,"0000", "POS_DealerExclusivePlan");

            //    var x = 10;


            //}
        }
    }
}

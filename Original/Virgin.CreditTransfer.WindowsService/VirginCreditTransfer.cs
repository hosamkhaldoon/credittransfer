using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Configuration;

namespace Virgin.CreditTransfer.WindowsService
{
    public partial class VirginCreditTransfer : ServiceBase
    {
        bool _isServiceRunning = false;
        private int _waitTimeInSeconds;
        private Thread _checkVirginPendingTransactions = null;
        private string smsShortCode = string.Empty;
        public VirginCreditTransfer()
        {
            InitializeComponent();
        }

        public void start()
        {
            OnStart(null);
        }
        protected override void OnStart(string[] args)
        {
            try
            {
                _isServiceRunning = true;
                _waitTimeInSeconds = Convert.ToInt32(ConfigurationManager.AppSettings["WaitTimeSeconds"]);
                
            }
            catch (Exception ex)
            {
                int logId = CreditTransferEngine.BusinessLogic.Logger.LogAction("Virgin.CreditTransfer.WindowsService", "OnStart", ex.Message, -1, ex.ToString(), DateTime.Now);

                CreditTransferEngine.BusinessLogic.Logger.LogError(ex, logId);
                return;
            }

            CreditTransferEngine.BusinessLogic.Logger.LogAction("Virgin.CreditTransfer service has been started successfully.", "UpdateVirginPendingTransactions", "Virgin.CreditTransfer.WindowsService", -1, "Virgin.CreditTransfer service has been started successfully.", DateTime.Now);


            _checkVirginPendingTransactions = new Thread(UpdateVirginPendingTransactions);
            _checkVirginPendingTransactions.Start();
        }

        private void UpdateVirginPendingTransactions()
        {
            while (_isServiceRunning)
            {
                try
                {
                    smsShortCode = ConfigurationManager.AppSettings["smsShortCode"];
                    //Call UpdateVirginExpiredTransactions
                    //CreditTransferEngine.BusinessLogic.CreditTransfer.UpdateVirginExpiredTransactions(smsShortCode);

                }
                catch (Exception ex)
                {
                    int logId = CreditTransferEngine.BusinessLogic.Logger.LogAction("Virgin.CreditTransfer.WindowsService", "UpdateVirginPendingTransactions", ex.Message, -1, ex.ToString(), DateTime.Now);

                    CreditTransferEngine.BusinessLogic.Logger.LogError(ex, logId);
                }

                if (_isServiceRunning)
                {
                    Thread.Sleep(_waitTimeInSeconds * 1000);
                }
            }
        }

        protected override void OnStop()
        {
            _isServiceRunning = false;

            CreditTransferEngine.BusinessLogic.Logger.LogAction("Virgin.CreditTransfer service has been stopped successfully.", "OnStop", "Virgin.CreditTransfer.WindowsService", -1, "Virgin.CreditTransfer service has been stopped successfully.", DateTime.Now);

        }
    }
}

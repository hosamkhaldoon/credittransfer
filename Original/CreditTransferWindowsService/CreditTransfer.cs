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

namespace CreditTransferWindowsService
{
    public partial class CreditTransfer : ServiceBase
    {
        private bool _isServiceRunning = false;
        private Thread _workerThread = null;

        public CreditTransfer()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                if (!EventLog.Exists("CreditTransfer"))
                {
                    EventLog.CreateEventSource("CreditTransferService", "CreditTransfer");
                }
            }
            catch (Exception) { }

            EventLog.WriteEntry("CreditTransferService", "Credit Transfer Service Started");
            _isServiceRunning = true;

            _workerThread = new Thread(new ThreadStart(ProcessTransactions));
            _workerThread.Start();
        }

        protected override void OnStop()
        {
            _isServiceRunning = false;

            RequestAdditionalTime(4000);

            if ((_workerThread != null) && (_workerThread.IsAlive))
            {
                Thread.Sleep(5000);
                _workerThread.Abort();
            }

            //Indicate a successful exit.
            ExitCode = 0;
            EventLog.WriteEntry("CreditTransferService", "Credit Transfer Service Stopped");
        }

        public void ProcessTransactions()
        {
            EventLog.WriteEntry("CreditTransferService", "Process Transactions Started");

            int waitTimeInMilliseconds = 0;

            try
            {
                int waitTimeInHours = Convert.ToInt32(ConfigurationManager.AppSettings["WaitTimeInHours"]);
                waitTimeInMilliseconds = waitTimeInHours * 3600000;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("CreditTransferService", "Service failed while initializing, review next error for more information!");
                EventLog.WriteEntry("CreditTransferService", ex.ToString());
                return;
            }

            while (_isServiceRunning)
            {
                try
                {
                    CreditTransferEngine.BusinessLogic.TransactionManager.ProcessTransactions();
                }
                catch (Exception ex)
                {
                    //Log the error and continue..
                    EventLog.WriteEntry("CreditTransferService", string.Format("Generic Error: {0} -- CreditTransferService.ProcessTransactions", ex.ToString()));
                }

                EventLog.WriteEntry("CreditTransferService", "Transactions Processed");
                System.Threading.Thread.Sleep(waitTimeInMilliseconds);
            }
        }
    }
}

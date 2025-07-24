using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Diagnostics;


namespace CreditTransferWindowsService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        void ServiceInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            ServiceController sc = new ServiceController("CreditTransfer");
            sc.Start();
        }

        private void serviceInstaller1_BeforeUninstall(object sender, InstallEventArgs e)
        {
            deleteEventLogs();
        }

        private void serviceInstaller1_BeforeInstall(object sender, InstallEventArgs e)
        {
            deleteEventLogs();
        }

        private void deleteEventLogs()
        {
            try
            {
                System.Diagnostics.EventLog.Delete("CreditTransfer");
            }
            catch (Exception) { }

            try
            {
                System.Diagnostics.EventLog.Delete("CreditTransferService");
            }
            catch (Exception) { }

            try
            {
                System.Diagnostics.EventLog.DeleteEventSource("CreditTransfer");
            }
            catch (Exception) { }

            try
            {
                System.Diagnostics.EventLog.DeleteEventSource("CreditTransferService");
            }
            catch (Exception) { }
        }
    }
}

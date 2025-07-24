namespace Virgin.CreditTransfer.WindowsService
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.virginCreditTransferInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.VirginCreditTransfer = new System.ServiceProcess.ServiceInstaller();
            // 
            // virginCreditTransferInstaller
            // 
            this.virginCreditTransferInstaller.Account = System.ServiceProcess.ServiceAccount.LocalService;
            this.virginCreditTransferInstaller.Password = null;
            this.virginCreditTransferInstaller.Username = null;
            // 
            // VirginCreditTransfer
            // 
            this.VirginCreditTransfer.ServiceName = "VirginCreditTransfer";
            this.VirginCreditTransfer.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.virginCreditTransferInstaller,
            this.VirginCreditTransfer});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller virginCreditTransferInstaller;
        private System.ServiceProcess.ServiceInstaller VirginCreditTransfer;
    }
}
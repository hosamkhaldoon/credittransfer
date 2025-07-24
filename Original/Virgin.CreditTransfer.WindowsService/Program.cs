using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace Virgin.CreditTransfer.WindowsService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            //ServiceBase[] ServicesToRun;
            //ServicesToRun = new ServiceBase[] 
            //{ 
            //    new VirginCreditTransfer() 
            //};
            //ServiceBase.Run(ServicesToRun);

            VirginCreditTransfer virginCreditTransfer = new VirginCreditTransfer();
            virginCreditTransfer.start();

        }
    }
}

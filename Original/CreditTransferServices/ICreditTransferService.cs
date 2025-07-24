using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace CreditTransferServices
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ICreditTransferService" in both code and config file together.
    [ServiceContract]
    public interface ICreditTransferService
    {
        [OperationContract]
        void TransferCredit(string sourceMsisdn, string destinationMsisdn, int amountRiyal, int amountBaisa, string pin, out int statusCode, out string statusMessage);

        [OperationContract]
        void TransferCreditWithAdjustmentReason(string sourceMsisdn, string destinationMsisdn, int amountRiyal, int amountBaisa, string pin,string adjustmentReason, out int statusCode, out string statusMessage);

        [OperationContract]
        List<decimal> GetDenomination();

        [OperationContract]
        void TransferCreditWithoutPinforSC(string sourceMsisdn, string destinationMsisdn, decimal amountRiyal, out int statusCode, out string statusMessage);
        [OperationContract]
        void ValidateTransferInputs(string sourceMsisdn, string destinationMsisdn, decimal amountRiyal, out int statusCode, out string statusMessage);

    }
}

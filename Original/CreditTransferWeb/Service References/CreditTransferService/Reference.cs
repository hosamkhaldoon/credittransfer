﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CreditTransfer.CreditTransferService {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="CreditTransferService.ICreditTransferService")]
    public interface ICreditTransferService {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ICreditTransferService/TransferCredit", ReplyAction="http://tempuri.org/ICreditTransferService/TransferCreditResponse")]
        [return: System.ServiceModel.MessageParameterAttribute(Name="statusCode")]
        int TransferCredit(out string statusMessage, string sourceMsisdn, string destinationMsisdn, int amountRiyal, int amountBaisa, string pin);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ICreditTransferService/GetDenomination", ReplyAction="http://tempuri.org/ICreditTransferService/GetDenominationResponse")]
        System.Collections.Generic.List<decimal> GetDenomination();
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface ICreditTransferServiceChannel : CreditTransfer.CreditTransferService.ICreditTransferService, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class CreditTransferServiceClient : System.ServiceModel.ClientBase<CreditTransfer.CreditTransferService.ICreditTransferService>, CreditTransfer.CreditTransferService.ICreditTransferService {
        
        public CreditTransferServiceClient() {
        }
        
        public CreditTransferServiceClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public CreditTransferServiceClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public CreditTransferServiceClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public CreditTransferServiceClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public int TransferCredit(out string statusMessage, string sourceMsisdn, string destinationMsisdn, int amountRiyal, int amountBaisa, string pin) {
            return base.Channel.TransferCredit(out statusMessage, sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin);
        }
        
        public System.Collections.Generic.List<decimal> GetDenomination() {
            return base.Channel.GetDenomination();
        }
    }
}

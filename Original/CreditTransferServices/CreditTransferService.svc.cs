using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Configuration;
using System.Security.Permissions;
using CreditTransferEngine.Utils;

namespace CreditTransferServices
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "CreditTransferService" in code, svc and config file together.
    public class CreditTransferService : ICreditTransferService
    {
        private static readonly int CurrencyUnitConversion;
        private static readonly string AuthenticatedUserGroup;
       

        static CreditTransferService()
        {
            CurrencyUnitConversion = int.Parse(ConfigurationManager.AppSettings["CurrencyUnitConversion"]);
            AuthenticatedUserGroup = ConfigurationManager.AppSettings["AuthenticatedUserGroup"];

           

        }
        //[PrincipalPermission(SecurityAction.PermitOnly, Role = "CreditTransferGroup")]
        public void TransferCredit(string sourceMsisdn, string destinationMsisdn, int amountRiyal, int amountBaisa, string pin, out int statusCode, out string statusMessage)
        {
            //sourceMsisdn = sourceMsisdn.TrimStart('0');
            //destinationMsisdn = destinationMsisdn.TrimStart('0');
            decimal amount = Convert.ToDecimal(amountRiyal) + Convert.ToDecimal(amountBaisa) / Convert.ToDecimal(CurrencyUnitConversion);

            string receivedRequest = string.Format("sourceMsisdn: {0}; destinationMsisdn: {1}; amount: {2}; pin: {3}", sourceMsisdn, destinationMsisdn, amount, pin);
            Exception exception = null;

            statusCode = Constants.SuccessCode;
            statusMessage = Constants.SuccessMessage;
            bool? isInternalException = false;

            string userName = ServiceSecurityContext.Current.WindowsIdentity.Name;
            //string ss = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            try
            {
                System.Security.Principal.WindowsPrincipal windowsPrincipal = new System.Security.Principal.WindowsPrincipal(ServiceSecurityContext.Current.WindowsIdentity);
                if (!windowsPrincipal.IsInRole(AuthenticatedUserGroup))
                {
                    throw new UserNotAllowedToCallThisMethodException();
                }

                CreditTransferEngine.BusinessLogic.CreditTransfer.TransferCredit(sourceMsisdn, destinationMsisdn, amount, pin, userName, ref receivedRequest);

            }
            catch (InvalidSourcePhoneException invalidSourcePhoneException)
            {
                statusCode = invalidSourcePhoneException.ResponseCode;
                statusMessage = invalidSourcePhoneException.ResponseMessage;
                exception = invalidSourcePhoneException;
            }
            catch (InvalidDestinationPhoneException invalidDestinationPhoneException)
            {
                statusCode = invalidDestinationPhoneException.ResponseCode;
                statusMessage = invalidDestinationPhoneException.ResponseMessage;
                exception = invalidDestinationPhoneException;
            }
            catch (InvalidPinException invalidPinException)
            {
                statusCode = invalidPinException.ResponseCode;
                statusMessage = invalidPinException.ResponseMessage;
                exception = invalidPinException;
            }
            catch (SourceAndDestinationSameException sourceAndDestinationSameException)
            {
                statusCode = sourceAndDestinationSameException.ResponseCode;
                statusMessage = sourceAndDestinationSameException.ResponseMessage;
                exception = sourceAndDestinationSameException;
            }
            catch (PinMismatchException refillPinMismatchException)
            {
                statusCode = refillPinMismatchException.ResponseCode;
                statusMessage = refillPinMismatchException.ResponseMessage;
                exception = refillPinMismatchException;
            }
            catch (InsuffientCreditException insuffientCreditException)
            {
                statusCode = insuffientCreditException.ResponseCode;
                statusMessage = insuffientCreditException.ResponseMessage;
                exception = insuffientCreditException;
            }
            catch (TransferAmountBelowMinException transferAmountBelowMinOrAboveMaxException)
            {
                statusCode = transferAmountBelowMinOrAboveMaxException.ResponseCode;
                statusMessage = transferAmountBelowMinOrAboveMaxException.ResponseMessage;
                exception = transferAmountBelowMinOrAboveMaxException;
            }
            catch (SubscriptionNotFoundException subscriptionNotFoundException)
            {
                statusCode = subscriptionNotFoundException.ResponseCode;
                statusMessage = subscriptionNotFoundException.ResponseMessage;
                exception = subscriptionNotFoundException;
            }
            catch (ConcurrentUpdateDetectedException concurrentUpdateDetectedException)
            {
                //this is an internal exception, but it will be exposed as success 
                //as it occurs on the extend days after transfer completed.
                statusCode = concurrentUpdateDetectedException.ResponseCode;
                statusMessage = concurrentUpdateDetectedException.ResponseMessage;
                isInternalException = null;
                exception = concurrentUpdateDetectedException;
            }
            catch (SourcePhoneNumberNotFoundException sourceSubscriptionNotFoundException)
            {
                statusCode = sourceSubscriptionNotFoundException.ResponseCode;
                statusMessage = sourceSubscriptionNotFoundException.ResponseMessage;
                exception = sourceSubscriptionNotFoundException;
            }
            catch (DestinationPhoneNumberNotFoundException destinationPhoneNumberNotFoundException)
            {
                statusCode = destinationPhoneNumberNotFoundException.ResponseCode;
                statusMessage = destinationPhoneNumberNotFoundException.ResponseMessage;
                exception = destinationPhoneNumberNotFoundException;
            }
            catch (UserNotAllowedToCallThisMethodException userNotAllowedToCallThisMethodException)
            {
                //this is an internal exception, it will be exposed as service unavailable
                statusCode = userNotAllowedToCallThisMethodException.ResponseCode;
                statusMessage = userNotAllowedToCallThisMethodException.ResponseMessage;
                isInternalException = true;
                exception = userNotAllowedToCallThisMethodException;
            }
            catch (TransferAmountAboveMaxException transferAmountAboveMaxException)
            {
                statusCode = transferAmountAboveMaxException.ResponseCode;
                statusMessage = transferAmountAboveMaxException.ResponseMessage;
                exception = transferAmountAboveMaxException;
            }
            catch (UnknownSubscriberException unknownSubscriberException)
            {
                statusCode = unknownSubscriberException.ResponseCode;
                statusMessage = unknownSubscriberException.ResponseMessage;
                exception = unknownSubscriberException;
            }
            catch (ConfigurationErrorException configurationErrorException)
            {
                //this is an internal exception, it will be exposed as service unavailable
                statusCode = configurationErrorException.ResponseCode;
                statusMessage = configurationErrorException.ResponseMessage;
                isInternalException = true;
                exception = configurationErrorException;
            }
            catch (MiscellaneousErrorException miscellaneousErrorException)
            {
                //this is an internal exception, it will be exposed as service unavailable
                statusCode = miscellaneousErrorException.ResponseCode;
                statusMessage = miscellaneousErrorException.ResponseMessage;
                isInternalException = true;
                exception = miscellaneousErrorException;
            }
            catch (PropertyNotFoundException propertyNotFoundException)
            {
                //this is an internal exception, it will be exposed as service unavailable
                statusCode = propertyNotFoundException.ResponseCode;
                statusMessage = propertyNotFoundException.ResponseMessage;
                isInternalException = true;
                exception = propertyNotFoundException;
            }
            catch (ExceedsMaxPerDayTransactionsException exceedsMaxPerDayTransactionsException)
            {
                statusCode = exceedsMaxPerDayTransactionsException.ResponseCode;
                statusMessage = exceedsMaxPerDayTransactionsException.ResponseMessage;
                exception = exceedsMaxPerDayTransactionsException;
            }
            catch (RemainingBalanceException remainingBalanceException)
            {
                statusCode = remainingBalanceException.ResponseCode;
                statusMessage = remainingBalanceException.ResponseMessage;
                exception = remainingBalanceException;
            }
            catch (NotAllowedToTransferCreditToTheDestinationAccountException remainingBalanceException)
            {
                statusCode = remainingBalanceException.ResponseCode;
                statusMessage = remainingBalanceException.ResponseMessage;
                exception = remainingBalanceException;
            }            
            catch (Exception ex)
            {
                statusCode = Constants.ServiceUnavailableCode;
                statusMessage = Constants.ServiceUnavailableMessage;
                exception = ex;
            }

            int logId = CreditTransferEngine.BusinessLogic.Logger.LogAction(receivedRequest, "TransferCredit", userName, statusCode, statusMessage, DateTime.Now);
            if (statusCode != 0)
            {
                CreditTransferEngine.BusinessLogic.Logger.LogError(exception, logId);
            }

            if (isInternalException.HasValue)
            {
                if (isInternalException.Value)
                {
                    statusCode = Constants.ServiceUnavailableCode;
                    statusMessage = Constants.ServiceUnavailableMessage;
                }
            }
            else
            {
                statusCode = Constants.SuccessCode;
                statusMessage = Constants.SuccessMessage;
            }
        }

        public void TransferCreditWithAdjustmentReason(string sourceMsisdn, string destinationMsisdn, int amountRiyal, int amountBaisa, string pin, string adjustmentReason, out int statusCode, out string statusMessage)
        {
            //sourceMsisdn = sourceMsisdn.TrimStart('0');
            //destinationMsisdn = destinationMsisdn.TrimStart('0');
            decimal amount = Convert.ToDecimal(amountRiyal) + Convert.ToDecimal(amountBaisa) / Convert.ToDecimal(CurrencyUnitConversion);

            string receivedRequest = string.Format("sourceMsisdn: {0}; destinationMsisdn: {1}; amount: {2}; pin: {3}", sourceMsisdn, destinationMsisdn, amount, pin);
            Exception exception = null;

            statusCode = Constants.SuccessCode;
            statusMessage = Constants.SuccessMessage;
            bool? isInternalException = false;

            string userName = ServiceSecurityContext.Current.WindowsIdentity.Name;
            //string ss = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            try
            {
                System.Security.Principal.WindowsPrincipal windowsPrincipal = new System.Security.Principal.WindowsPrincipal(ServiceSecurityContext.Current.WindowsIdentity);
                if (!windowsPrincipal.IsInRole(AuthenticatedUserGroup))
                {
                    throw new UserNotAllowedToCallThisMethodException();
                }

                CreditTransferEngine.BusinessLogic.CreditTransfer.TransferCreditWithAdjustmentReason(sourceMsisdn, destinationMsisdn, amount, pin, userName, adjustmentReason, ref receivedRequest);

            }
            catch (InvalidSourcePhoneException invalidSourcePhoneException)
            {
                statusCode = invalidSourcePhoneException.ResponseCode;
                statusMessage = invalidSourcePhoneException.ResponseMessage;
                exception = invalidSourcePhoneException;
            }
            catch (InvalidDestinationPhoneException invalidDestinationPhoneException)
            {
                statusCode = invalidDestinationPhoneException.ResponseCode;
                statusMessage = invalidDestinationPhoneException.ResponseMessage;
                exception = invalidDestinationPhoneException;
            }
            catch (InvalidPinException invalidPinException)
            {
                statusCode = invalidPinException.ResponseCode;
                statusMessage = invalidPinException.ResponseMessage;
                exception = invalidPinException;
            }
            catch (SourceAndDestinationSameException sourceAndDestinationSameException)
            {
                statusCode = sourceAndDestinationSameException.ResponseCode;
                statusMessage = sourceAndDestinationSameException.ResponseMessage;
                exception = sourceAndDestinationSameException;
            }
            catch (PinMismatchException refillPinMismatchException)
            {
                statusCode = refillPinMismatchException.ResponseCode;
                statusMessage = refillPinMismatchException.ResponseMessage;
                exception = refillPinMismatchException;
            }
            catch (InsuffientCreditException insuffientCreditException)
            {
                statusCode = insuffientCreditException.ResponseCode;
                statusMessage = insuffientCreditException.ResponseMessage;
                exception = insuffientCreditException;
            }
            catch (TransferAmountBelowMinException transferAmountBelowMinOrAboveMaxException)
            {
                statusCode = transferAmountBelowMinOrAboveMaxException.ResponseCode;
                statusMessage = transferAmountBelowMinOrAboveMaxException.ResponseMessage;
                exception = transferAmountBelowMinOrAboveMaxException;
            }
            catch (SubscriptionNotFoundException subscriptionNotFoundException)
            {
                statusCode = subscriptionNotFoundException.ResponseCode;
                statusMessage = subscriptionNotFoundException.ResponseMessage;
                exception = subscriptionNotFoundException;
            }
            catch (ConcurrentUpdateDetectedException concurrentUpdateDetectedException)
            {
                //this is an internal exception, but it will be exposed as success 
                //as it occurs on the extend days after transfer completed.
                statusCode = concurrentUpdateDetectedException.ResponseCode;
                statusMessage = concurrentUpdateDetectedException.ResponseMessage;
                isInternalException = null;
                exception = concurrentUpdateDetectedException;
            }
            catch (SourcePhoneNumberNotFoundException sourceSubscriptionNotFoundException)
            {
                statusCode = sourceSubscriptionNotFoundException.ResponseCode;
                statusMessage = sourceSubscriptionNotFoundException.ResponseMessage;
                exception = sourceSubscriptionNotFoundException;
            }
            catch (DestinationPhoneNumberNotFoundException destinationPhoneNumberNotFoundException)
            {
                statusCode = destinationPhoneNumberNotFoundException.ResponseCode;
                statusMessage = destinationPhoneNumberNotFoundException.ResponseMessage;
                exception = destinationPhoneNumberNotFoundException;
            }
            catch (UserNotAllowedToCallThisMethodException userNotAllowedToCallThisMethodException)
            {
                //this is an internal exception, it will be exposed as service unavailable
                statusCode = userNotAllowedToCallThisMethodException.ResponseCode;
                statusMessage = userNotAllowedToCallThisMethodException.ResponseMessage;
                isInternalException = true;
                exception = userNotAllowedToCallThisMethodException;
            }
            catch (TransferAmountAboveMaxException transferAmountAboveMaxException)
            {
                statusCode = transferAmountAboveMaxException.ResponseCode;
                statusMessage = transferAmountAboveMaxException.ResponseMessage;
                exception = transferAmountAboveMaxException;
            }
            catch (UnknownSubscriberException unknownSubscriberException)
            {
                statusCode = unknownSubscriberException.ResponseCode;
                statusMessage = unknownSubscriberException.ResponseMessage;
                exception = unknownSubscriberException;
            }
            catch (ConfigurationErrorException configurationErrorException)
            {
                //this is an internal exception, it will be exposed as service unavailable
                statusCode = configurationErrorException.ResponseCode;
                statusMessage = configurationErrorException.ResponseMessage;
                isInternalException = true;
                exception = configurationErrorException;
            }
            catch (MiscellaneousErrorException miscellaneousErrorException)
            {
                //this is an internal exception, it will be exposed as service unavailable
                statusCode = miscellaneousErrorException.ResponseCode;
                statusMessage = miscellaneousErrorException.ResponseMessage;
                isInternalException = true;
                exception = miscellaneousErrorException;
            }
            catch (PropertyNotFoundException propertyNotFoundException)
            {
                //this is an internal exception, it will be exposed as service unavailable
                statusCode = propertyNotFoundException.ResponseCode;
                statusMessage = propertyNotFoundException.ResponseMessage;
                isInternalException = true;
                exception = propertyNotFoundException;
            }
            catch (ExceedsMaxPerDayTransactionsException exceedsMaxPerDayTransactionsException)
            {
                statusCode = exceedsMaxPerDayTransactionsException.ResponseCode;
                statusMessage = exceedsMaxPerDayTransactionsException.ResponseMessage;
                exception = exceedsMaxPerDayTransactionsException;
            }
            catch (RemainingBalanceException remainingBalanceException)
            {
                statusCode = remainingBalanceException.ResponseCode;
                statusMessage = remainingBalanceException.ResponseMessage;
                exception = remainingBalanceException;
            }
            catch (NotAllowedToTransferCreditToTheDestinationAccountException remainingBalanceException)
            {
                statusCode = remainingBalanceException.ResponseCode;
                statusMessage = remainingBalanceException.ResponseMessage;
                exception = remainingBalanceException;
            }
            catch (Exception ex)
            {
                statusCode = Constants.ServiceUnavailableCode;
                statusMessage = Constants.ServiceUnavailableMessage;
                exception = ex;
            }

            int logId = CreditTransferEngine.BusinessLogic.Logger.LogAction(receivedRequest, "TransferCredit", userName, statusCode, statusMessage, DateTime.Now);
            if (statusCode != 0)
            {
                CreditTransferEngine.BusinessLogic.Logger.LogError(exception, logId);
            }

            if (isInternalException.HasValue)
            {
                if (isInternalException.Value)
                {
                    statusCode = Constants.ServiceUnavailableCode;
                    statusMessage = Constants.ServiceUnavailableMessage;
                }
            }
            else
            {
                statusCode = Constants.SuccessCode;
                statusMessage = Constants.SuccessMessage;
            }
        }



        public List<decimal> GetDenomination()
        {
            return CreditTransferEngine.BusinessLogic.CreditTransfer.GetDenomination();
        }

        public void ValidateTransferInputs(string sourceMsisdn, string destinationMsisdn, decimal amountRiyal, out int statusCode, out string statusMessage)
        {
            //sourceMsisdn = sourceMsisdn.TrimStart('0');
            //destinationMsisdn = destinationMsisdn.TrimStart('0');
            //int amountHalala = 0; string pin = string.Empty;
            //decimal amount = Convert.ToDecimal(amountRiyal) + Convert.ToDecimal(amountHalala) / Convert.ToDecimal(CurrencyUnitConversion);

            //sourceMsisdn = sourceMsisdn.TrimStart('0');
            //destinationMsisdn = destinationMsisdn.TrimStart('0');
            //decimal amount = Convert.ToDecimal(amountRiyal) * Convert.ToDecimal(CurrencyUnitConversion) / Convert.ToDecimal(CurrencyUnitConversion);

            string receivedRequest = string.Format("sourceMsisdn: {0}; destinationMsisdn: {1}; amount: {2}; pin: {3}", sourceMsisdn, destinationMsisdn, amountRiyal, "0000");
            Exception exception = null;

            statusCode = Constants.SuccessCode;
            statusMessage = Constants.SuccessMessage;
            bool? isInternalException = false;

            string userName = ServiceSecurityContext.Current.WindowsIdentity.Name;
            //string ss = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            try
            {
                System.Security.Principal.WindowsPrincipal windowsPrincipal = new System.Security.Principal.WindowsPrincipal(ServiceSecurityContext.Current.WindowsIdentity);
                if (!windowsPrincipal.IsInRole(AuthenticatedUserGroup))
                {
                    throw new UserNotAllowedToCallThisMethodException();
                }

                CreditTransferEngine.BusinessLogic.CreditTransfer.ValidateTransferInputs(sourceMsisdn, destinationMsisdn, amountRiyal);

            }
            catch (InvalidSourcePhoneException invalidSourcePhoneException)
            {
                statusCode = invalidSourcePhoneException.ResponseCode;
                statusMessage = invalidSourcePhoneException.ResponseMessage;
                exception = invalidSourcePhoneException;
            }
            catch (InvalidDestinationPhoneException invalidDestinationPhoneException)
            {
                statusCode = invalidDestinationPhoneException.ResponseCode;
                statusMessage = invalidDestinationPhoneException.ResponseMessage;
                exception = invalidDestinationPhoneException;
            }
            catch (InvalidPinException invalidPinException)
            {
                statusCode = invalidPinException.ResponseCode;
                statusMessage = invalidPinException.ResponseMessage;
                exception = invalidPinException;
            }
            catch (SourceAndDestinationSameException sourceAndDestinationSameException)
            {
                statusCode = sourceAndDestinationSameException.ResponseCode;
                statusMessage = sourceAndDestinationSameException.ResponseMessage;
                exception = sourceAndDestinationSameException;
            }
            catch (PinMismatchException refillPinMismatchException)
            {
                statusCode = refillPinMismatchException.ResponseCode;
                statusMessage = refillPinMismatchException.ResponseMessage;
                exception = refillPinMismatchException;
            }
            catch (InsuffientCreditException insuffientCreditException)
            {
                statusCode = insuffientCreditException.ResponseCode;
                statusMessage = insuffientCreditException.ResponseMessage;
                exception = insuffientCreditException;
            }
            catch (TransferAmountBelowMinException transferAmountBelowMinOrAboveMaxException)
            {
                statusCode = transferAmountBelowMinOrAboveMaxException.ResponseCode;
                statusMessage = transferAmountBelowMinOrAboveMaxException.ResponseMessage;
                exception = transferAmountBelowMinOrAboveMaxException;
            }
            catch (SubscriptionNotFoundException subscriptionNotFoundException)
            {
                statusCode = subscriptionNotFoundException.ResponseCode;
                statusMessage = subscriptionNotFoundException.ResponseMessage;
                exception = subscriptionNotFoundException;
            }
            catch (ConcurrentUpdateDetectedException concurrentUpdateDetectedException)
            {
                //this is an internal exception, but it will be exposed as success 
                //as it occurs on the extend days after transfer completed.
                statusCode = concurrentUpdateDetectedException.ResponseCode;
                statusMessage = concurrentUpdateDetectedException.ResponseMessage;
                isInternalException = null;
                exception = concurrentUpdateDetectedException;
            }
            catch (SourcePhoneNumberNotFoundException sourceSubscriptionNotFoundException)
            {
                statusCode = sourceSubscriptionNotFoundException.ResponseCode;
                statusMessage = sourceSubscriptionNotFoundException.ResponseMessage;
                exception = sourceSubscriptionNotFoundException;
            }
            catch (DestinationPhoneNumberNotFoundException destinationPhoneNumberNotFoundException)
            {
                statusCode = destinationPhoneNumberNotFoundException.ResponseCode;
                statusMessage = destinationPhoneNumberNotFoundException.ResponseMessage;
                exception = destinationPhoneNumberNotFoundException;
            }
            catch (UserNotAllowedToCallThisMethodException userNotAllowedToCallThisMethodException)
            {
                //this is an internal exception, it will be exposed as service unavailable
                statusCode = userNotAllowedToCallThisMethodException.ResponseCode;
                statusMessage = userNotAllowedToCallThisMethodException.ResponseMessage;
                isInternalException = true;
                exception = userNotAllowedToCallThisMethodException;
            }
            catch (TransferAmountAboveMaxException transferAmountAboveMaxException)
            {
                statusCode = transferAmountAboveMaxException.ResponseCode;
                statusMessage = transferAmountAboveMaxException.ResponseMessage;
                exception = transferAmountAboveMaxException;
            }
            catch (UnknownSubscriberException unknownSubscriberException)
            {
                statusCode = unknownSubscriberException.ResponseCode;
                statusMessage = unknownSubscriberException.ResponseMessage;
                exception = unknownSubscriberException;
            }
            catch (ConfigurationErrorException configurationErrorException)
            {
                //this is an internal exception, it will be exposed as service unavailable
                statusCode = configurationErrorException.ResponseCode;
                statusMessage = configurationErrorException.ResponseMessage;
                isInternalException = true;
                exception = configurationErrorException;
            }
            catch (MiscellaneousErrorException miscellaneousErrorException)
            {
                //this is an internal exception, it will be exposed as service unavailable
                statusCode = miscellaneousErrorException.ResponseCode;
                statusMessage = miscellaneousErrorException.ResponseMessage;
                isInternalException = true;
                exception = miscellaneousErrorException;
            }
            catch (PropertyNotFoundException propertyNotFoundException)
            {
                //this is an internal exception, it will be exposed as service unavailable
                statusCode = propertyNotFoundException.ResponseCode;
                statusMessage = propertyNotFoundException.ResponseMessage;
                isInternalException = true;
                exception = propertyNotFoundException;
            }
            catch (ExceedsMaxPerDayTransactionsException exceedsMaxPerDayTransactionsException)
            {
                statusCode = exceedsMaxPerDayTransactionsException.ResponseCode;
                statusMessage = exceedsMaxPerDayTransactionsException.ResponseMessage;
                exception = exceedsMaxPerDayTransactionsException;
            }
            catch (RemainingBalanceException remainingBalanceException)
            {
                statusCode = remainingBalanceException.ResponseCode;
                statusMessage = remainingBalanceException.ResponseMessage;
                exception = remainingBalanceException;
            }
            catch (NotAllowedToTransferCreditToTheDestinationAccountException remainingBalanceException)
            {
                statusCode = remainingBalanceException.ResponseCode;
                statusMessage = remainingBalanceException.ResponseMessage;
                exception = remainingBalanceException;
            }
            catch (Exception ex)
            {
                statusCode = Constants.ServiceUnavailableCode;
                statusMessage = Constants.ServiceUnavailableMessage;
                exception = ex;
            }


            if (isInternalException.HasValue)
            {
                if (isInternalException.Value)
                {
                    statusCode = Constants.ServiceUnavailableCode;
                    statusMessage = Constants.ServiceUnavailableMessage;
                }
            }
            else
            {
                statusCode = Constants.SuccessCode;
                statusMessage = Constants.SuccessMessage;
            }
        }


        public void TransferCreditWithoutPinforSC(string sourceMsisdn, string destinationMsisdn, decimal amountRiyal, out int statusCode, out string statusMessage)
        {
            //sourceMsisdn = sourceMsisdn.TrimStart('0');
            //destinationMsisdn = destinationMsisdn.TrimStart('0');
            //int amountHalala = 0; string pin = string.Empty;
            //decimal amount = Convert.ToDecimal(amountRiyal) + Convert.ToDecimal(amountHalala) / Convert.ToDecimal(CurrencyUnitConversion);

            //sourceMsisdn = sourceMsisdn.TrimStart('0');
            //destinationMsisdn = destinationMsisdn.TrimStart('0');
            //decimal amount = Convert.ToDecimal(amountRiyal) * Convert.ToDecimal(CurrencyUnitConversion) / Convert.ToDecimal(CurrencyUnitConversion);

            string receivedRequest = string.Format("sourceMsisdn: {0}; destinationMsisdn: {1}; amount: {2}; pin: {3}", sourceMsisdn, destinationMsisdn, amountRiyal, "0000");
            Exception exception = null;

            statusCode = Constants.SuccessCode;
            statusMessage = Constants.SuccessMessage;
            bool? isInternalException = false;

            string userName = ServiceSecurityContext.Current.WindowsIdentity.Name;
            //string ss = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            try
            {
                System.Security.Principal.WindowsPrincipal windowsPrincipal = new System.Security.Principal.WindowsPrincipal(ServiceSecurityContext.Current.WindowsIdentity);
                if (!windowsPrincipal.IsInRole(AuthenticatedUserGroup))
                {
                    throw new UserNotAllowedToCallThisMethodException();
                }

                CreditTransferEngine.BusinessLogic.CreditTransfer.TransferCreditWithoutPin(sourceMsisdn, destinationMsisdn, amountRiyal, userName, ref receivedRequest);

            }
            catch (InvalidSourcePhoneException invalidSourcePhoneException)
            {
                statusCode = invalidSourcePhoneException.ResponseCode;
                statusMessage = invalidSourcePhoneException.ResponseMessage;
                exception = invalidSourcePhoneException;
            }
            catch (InvalidDestinationPhoneException invalidDestinationPhoneException)
            {
                statusCode = invalidDestinationPhoneException.ResponseCode;
                statusMessage = invalidDestinationPhoneException.ResponseMessage;
                exception = invalidDestinationPhoneException;
            }
            catch (InvalidPinException invalidPinException)
            {
                statusCode = invalidPinException.ResponseCode;
                statusMessage = invalidPinException.ResponseMessage;
                exception = invalidPinException;
            }
            catch (SourceAndDestinationSameException sourceAndDestinationSameException)
            {
                statusCode = sourceAndDestinationSameException.ResponseCode;
                statusMessage = sourceAndDestinationSameException.ResponseMessage;
                exception = sourceAndDestinationSameException;
            }
            catch (PinMismatchException refillPinMismatchException)
            {
                statusCode = refillPinMismatchException.ResponseCode;
                statusMessage = refillPinMismatchException.ResponseMessage;
                exception = refillPinMismatchException;
            }
            catch (InsuffientCreditException insuffientCreditException)
            {
                statusCode = insuffientCreditException.ResponseCode;
                statusMessage = insuffientCreditException.ResponseMessage;
                exception = insuffientCreditException;
            }
            catch (TransferAmountBelowMinException transferAmountBelowMinOrAboveMaxException)
            {
                statusCode = transferAmountBelowMinOrAboveMaxException.ResponseCode;
                statusMessage = transferAmountBelowMinOrAboveMaxException.ResponseMessage;
                exception = transferAmountBelowMinOrAboveMaxException;
            }
            catch (SubscriptionNotFoundException subscriptionNotFoundException)
            {
                statusCode = subscriptionNotFoundException.ResponseCode;
                statusMessage = subscriptionNotFoundException.ResponseMessage;
                exception = subscriptionNotFoundException;
            }
            catch (ConcurrentUpdateDetectedException concurrentUpdateDetectedException)
            {
                //this is an internal exception, but it will be exposed as success 
                //as it occurs on the extend days after transfer completed.
                statusCode = concurrentUpdateDetectedException.ResponseCode;
                statusMessage = concurrentUpdateDetectedException.ResponseMessage;
                isInternalException = null;
                exception = concurrentUpdateDetectedException;
            }
            catch (SourcePhoneNumberNotFoundException sourceSubscriptionNotFoundException)
            {
                statusCode = sourceSubscriptionNotFoundException.ResponseCode;
                statusMessage = sourceSubscriptionNotFoundException.ResponseMessage;
                exception = sourceSubscriptionNotFoundException;
            }
            catch (DestinationPhoneNumberNotFoundException destinationPhoneNumberNotFoundException)
            {
                statusCode = destinationPhoneNumberNotFoundException.ResponseCode;
                statusMessage = destinationPhoneNumberNotFoundException.ResponseMessage;
                exception = destinationPhoneNumberNotFoundException;
            }
            catch (UserNotAllowedToCallThisMethodException userNotAllowedToCallThisMethodException)
            {
                //this is an internal exception, it will be exposed as service unavailable
                statusCode = userNotAllowedToCallThisMethodException.ResponseCode;
                statusMessage = userNotAllowedToCallThisMethodException.ResponseMessage;
                isInternalException = true;
                exception = userNotAllowedToCallThisMethodException;
            }
            catch (TransferAmountAboveMaxException transferAmountAboveMaxException)
            {
                statusCode = transferAmountAboveMaxException.ResponseCode;
                statusMessage = transferAmountAboveMaxException.ResponseMessage;
                exception = transferAmountAboveMaxException;
            }
            catch (UnknownSubscriberException unknownSubscriberException)
            {
                statusCode = unknownSubscriberException.ResponseCode;
                statusMessage = unknownSubscriberException.ResponseMessage;
                exception = unknownSubscriberException;
            }
            catch (ConfigurationErrorException configurationErrorException)
            {
                //this is an internal exception, it will be exposed as service unavailable
                statusCode = configurationErrorException.ResponseCode;
                statusMessage = configurationErrorException.ResponseMessage;
                isInternalException = true;
                exception = configurationErrorException;
            }
            catch (MiscellaneousErrorException miscellaneousErrorException)
            {
                //this is an internal exception, it will be exposed as service unavailable
                statusCode = miscellaneousErrorException.ResponseCode;
                statusMessage = miscellaneousErrorException.ResponseMessage;
                isInternalException = true;
                exception = miscellaneousErrorException;
            }
            catch (PropertyNotFoundException propertyNotFoundException)
            {
                //this is an internal exception, it will be exposed as service unavailable
                statusCode = propertyNotFoundException.ResponseCode;
                statusMessage = propertyNotFoundException.ResponseMessage;
                isInternalException = true;
                exception = propertyNotFoundException;
            }
            catch (ExceedsMaxPerDayTransactionsException exceedsMaxPerDayTransactionsException)
            {
                statusCode = exceedsMaxPerDayTransactionsException.ResponseCode;
                statusMessage = exceedsMaxPerDayTransactionsException.ResponseMessage;
                exception = exceedsMaxPerDayTransactionsException;
            }
            catch (RemainingBalanceException remainingBalanceException)
            {
                statusCode = remainingBalanceException.ResponseCode;
                statusMessage = remainingBalanceException.ResponseMessage;
                exception = remainingBalanceException;
            }
            catch (NotAllowedToTransferCreditToTheDestinationAccountException remainingBalanceException)
            {
                statusCode = remainingBalanceException.ResponseCode;
                statusMessage = remainingBalanceException.ResponseMessage;
                exception = remainingBalanceException;
            }
            catch (Exception ex)
            {
                statusCode = Constants.ServiceUnavailableCode;
                statusMessage = Constants.ServiceUnavailableMessage;
                exception = ex;
            }

            int logId = CreditTransferEngine.BusinessLogic.Logger.LogAction(receivedRequest, "TransferCredit", userName, statusCode, statusMessage, DateTime.Now);
            if (statusCode != 0)
            {
                CreditTransferEngine.BusinessLogic.Logger.LogError(exception, logId);
            }

            if (isInternalException.HasValue)
            {
                if (isInternalException.Value)
                {
                    statusCode = Constants.ServiceUnavailableCode;
                    statusMessage = Constants.ServiceUnavailableMessage;
                }
            }
            else
            {
                statusCode = Constants.SuccessCode;
                statusMessage = Constants.SuccessMessage;
            }
        }

    }
}
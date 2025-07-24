using System;
using CreditTransferEngine.Utils;
using System.Configuration;
using CreditTransferEngine.DataAccess;
using System.Resources;
using System.Collections.Generic;
using System.Xml;
using System.Linq;

namespace CreditTransferEngine.BusinessLogic
{
    public class CreditTransfer
    {

        private static readonly int MsisdnLength;
        private static readonly int RefillPinLength;
        //private static readonly decimal PosMinTransferAmounts;
        //private static readonly decimal DistributorMinTransferAmounts;
        //private static readonly decimal CustomerMinTransferAmounts;
        private static readonly bool EnableExtendedDays;
        //private static readonly int TransferFundEventId;
        private static readonly string CustomerToCustomerTransferMonyReason;
        private static readonly string CustomerToCustomerAdjustmentReasonOldToNew;
        private static readonly string CustomerToCustomerAdjustmentReasonNewToOld;
        private static readonly string AmountRanges;
        private static readonly string ExtendedDaysTypes;
        private static readonly string TransferMonyReasonClassification;
        private static readonly string AdjustmentReasonClassificationFromOldToNew;
        private static readonly string AdjustmentReasonClassificationFromNewToOld;
        private static readonly string VirginEventIds;
        private static readonly string AdjustmentType;
        private static readonly string shortCodeSMS;
        private static readonly string APartySMSEn;
        private static readonly string BPartySMSEn;
        private static readonly string APartySMSAr;
        private static readonly string BPartySMSAr;
        private static readonly string DefaultPIN;
        private static readonly decimal MaximumPercentageAmount;

        //private static readonly int DistributorTotalNumberOfTransfers;
        //private static readonly int CustomerTotalNumberOfTransfers;
        //private static readonly int MinAmountAfterTransfer;

        static CreditTransfer()
        {
            MsisdnLength = Convert.ToInt32(ConfigurationManager.AppSettings["MsisdnLength"]);
            RefillPinLength = Convert.ToInt32(ConfigurationManager.AppSettings["RefillPinLength"]);
            //PosMinTransferAmounts = Convert.ToDecimal(ConfigurationManager.AppSettings["POSMinTransferAmounts"]);
            //DistributorMinTransferAmounts = Convert.ToDecimal(ConfigurationManager.AppSettings["DistributorMinTransferAmounts"]);
            //CustomerMinTransferAmounts = Convert.ToDecimal(ConfigurationManager.AppSettings["CustomerMinTransferAmounts"]);
            EnableExtendedDays = Convert.ToBoolean(ConfigurationManager.AppSettings["EnableExtendedDays"]);
            //TransferFundEventId = Convert.ToInt32(ConfigurationManager.AppSettings["TransferFundEventId"]);
            CustomerToCustomerTransferMonyReason = ConfigurationManager.AppSettings["CustomerToCustomerTransferMonyReason"];
            CustomerToCustomerAdjustmentReasonOldToNew = ConfigurationManager.AppSettings["CustomerToCustomerAdjustmentReasonOldToNew"];
            CustomerToCustomerAdjustmentReasonNewToOld = ConfigurationManager.AppSettings["CustomerToCustomerAdjustmentReasonNewToOld"];
            AmountRanges = ConfigurationManager.AppSettings["AmountRanges"];
            ExtendedDaysTypes = ConfigurationManager.AppSettings["ExtendedDaysTypes"];
            TransferMonyReasonClassification = ConfigurationManager.AppSettings["TransferMonyReasonClassification"];
            AdjustmentReasonClassificationFromOldToNew = ConfigurationManager.AppSettings["AdjustmentReasonClassificationFromOldToNew"];
            AdjustmentReasonClassificationFromNewToOld = ConfigurationManager.AppSettings["AdjustmentReasonClassificationFromNewToOld"];

            VirginEventIds = ConfigurationManager.AppSettings["VirginEventIds"];
            AdjustmentType = ConfigurationManager.AppSettings["AdjustmentType"];
            shortCodeSMS = ConfigurationManager.AppSettings["shortCodeSMS"];
            APartySMSEn = ConfigurationManager.AppSettings["A_Party_SMS_EN"];
            BPartySMSEn = ConfigurationManager.AppSettings["B_Party_SMS_EN"];
            APartySMSAr = ConfigurationManager.AppSettings["A_Party_SMS_AR"];
            BPartySMSAr = ConfigurationManager.AppSettings["B_Party_SMS_AR"];
            DefaultPIN = ConfigurationManager.AppSettings["DefaultPIN"];
            MaximumPercentageAmount = 1;
            if (ConfigurationManager.AppSettings["MaximumPercentageAmount"] != null)
            {
                decimal.TryParse(ConfigurationManager.AppSettings["MaximumPercentageAmount"], out MaximumPercentageAmount);
                if (MaximumPercentageAmount == 0)
                    MaximumPercentageAmount = 1;
            }
            //DistributorTotalNumberOfTransfers = Convert.ToInt32(ConfigurationManager.AppSettings["DistributorTotalNumberOfTransfers"]);
            //CustomerTotalNumberOfTransfers = Convert.ToInt32(ConfigurationManager.AppSettings["CustomerTotalNumberOfTransfers"]);
            //MinAmountAfterTransfer = Convert.ToInt32(ConfigurationManager.AppSettings["MinAmountAfterTransfer"]);
        }
        #region Old Credit transfer Logic
        //public static void TransferCredit(string sourceMsisdn, string destinationMsisdn, decimal amount, string pin, string userName, ref string recievedRequest)
        //{
        //    #region Parameters Validations

        //    Int64 number = Int64.MinValue;
        //    // source phone number should not be empty & should be 12 digit numeric only & should be integer..
        //    if (string.IsNullOrEmpty(sourceMsisdn) || sourceMsisdn.Length != MsisdnLength || !Int64.TryParse(sourceMsisdn, out number))
        //    {
        //        throw new InvalidSourcePhoneException();
        //    }

        //    number = Int64.MinValue;
        //    // destination phone number should not be empty & should be 12 digit numeric only & should be integer..
        //    if (string.IsNullOrEmpty(destinationMsisdn) || destinationMsisdn.Length != MsisdnLength || !Int64.TryParse(destinationMsisdn, out number))
        //    {
        //        throw new InvalidDestinationPhoneException();
        //    }

        //    number = Int64.MinValue;
        //    // refill pin should not be empty & should be integer & should be 4 digit numeric only
        //    if (string.IsNullOrEmpty(pin) || !Int64.TryParse(pin, out number) || pin.Trim().Length != RefillPinLength)
        //    {
        //        throw new InvalidPinException();
        //    }

        //    // check if source and destination numbers are the same...
        //    if (sourceMsisdn == destinationMsisdn)
        //    {
        //        throw new SourceAndDestinationSameException();
        //    }

        //    SubscribtionType sourceMsisdnType;
        //    try
        //    {
        //        sourceMsisdnType = GetAccountType(sourceMsisdn);
        //    }
        //    catch (SubscriptionNotFoundException)
        //    {
        //        throw new SourcePhoneNumberNotFoundException();
        //    }

        //    //DATA sim is not allowed to transfer money 
        //    if (sourceMsisdnType == SubscribtionType.DataAccount)
        //    {
        //        throw new NotAllowedToTransferCreditToTheDestinationAccountException();
        //    }

        //    SubscribtionType destinationMsisdnType;

        //    try
        //    {
        //        destinationMsisdnType = GetAccountType(destinationMsisdn);
        //    }
        //    catch (SubscriptionNotFoundException)
        //    {
        //        throw new DestinationPhoneNumberNotFoundException();
        //    }

        //    //this is to prevent the customer from transfering amount to dealer or distributer
        //    if (sourceMsisdnType == SubscribtionType.Customer && (destinationMsisdnType == SubscribtionType.Pos || destinationMsisdnType == SubscribtionType.Distributor))
        //    {
        //        throw new NotAllowedToTransferCreditToTheDestinationAccountException();
        //    }

        //    string accountPin = GetAccountPin(sourceMsisdn);

        //    if (accountPin != pin)
        //    {
        //        throw new PinMismatchException();
        //    }

        //    if (GetSubscriptionBlockStatus(sourceMsisdn) != SubscriptionBlockStatus.NO_BLOCK)
        //    {
        //        throw new MiscellaneousErrorException();
        //    }

        //    if (GetSubscriptionStatus(destinationMsisdn) == SubscriptionStatus.ACTIVE_BEFORE_FIRST_USE)
        //    {
        //        throw new InvalidDestinationPhoneException();
        //    }

        //    decimal maxTransferAmount = GetAccountMaxTransferAmount(sourceMsisdn);

        //    if (amount > maxTransferAmount)
        //    {
        //        throw new TransferAmountAboveMaxException();
        //    }

        //    decimal minAmount = 0;

        //    switch (sourceMsisdnType)
        //    {
        //        case SubscribtionType.Pos:
        //            minAmount = PosMinTransferAmounts;
        //            break;
        //        case SubscribtionType.Distributor:
        //            minAmount = DistributorMinTransferAmounts;
        //            break;
        //        case SubscribtionType.Customer:
        //            minAmount = CustomerMinTransferAmounts;
        //            break;
        //    }

        //    if (amount < minAmount)
        //    {
        //        throw new TransferAmountBelowMinException();
        //    }

        //    //Check if source reach the maximum number of transactions per day
        //    int numberOfTransactions;

        //    TransactionManager.GetDailyTransferCount(sourceMsisdn, out numberOfTransactions);

        //    int maxNumberOfTransfers = 0;

        //    switch (sourceMsisdnType)
        //    {
        //        //case SubscribtionType.Pos:
        //        //    minNumberOfTransfers = PosTotalNumberOfTransfers;
        //        //    break;
        //        case SubscribtionType.Distributor:
        //            maxNumberOfTransfers = DistributorTotalNumberOfTransfers;
        //            break;
        //        case SubscribtionType.Customer:
        //            maxNumberOfTransfers = CustomerTotalNumberOfTransfers;
        //            break;
        //    }

        //    if (numberOfTransactions != 0 && maxNumberOfTransfers > numberOfTransactions)
        //    {
        //        throw new ExceedsMaxPerDayTransactionsException();
        //    }


        //    //Check the balance after transfer credit action  for customers only.

        //    if (sourceMsisdnType == SubscribtionType.Customer)
        //    {
        //        if (MinAmountAfterTransfer != 0 && TransactionManager.GetAccountBalance(sourceMsisdn) - amount < MinAmountAfterTransfer)
        //        {
        //            throw new InsuffientCreditException();
        //        }
        //    }

        //    //check the pin and the maximum amount for transfer credit through the customer service.


        //    #endregion

        //    int reservationCode = -1;
        //    bool isEventReserved = false;
        //    bool isAmountTransfered = false;
        //    int extendedDays = EnableExtendedDays && destinationMsisdnType == SubscribtionType.Customer ? GetDaysToExtend(amount) : 0;

        //    Transaction transaction = new Transaction();
        //    transaction.SourceMsisdn = sourceMsisdn;
        //    transaction.DestinationMsisdn = destinationMsisdn;
        //    transaction.Amount = amount;
        //    transaction.PIN = pin;
        //    transaction.ExtensionDays = extendedDays;
        //    transaction.IsFromCustomer = false;
        //    transaction.CreatedBy = userName;

        //    try
        //    {
        //        //transfer fund from A to B
        //        string transferReason = CustomerToCustomerTransferMonyReason;



        //        if (sourceMsisdnType == SubscribtionType.Customer)
        //        {
        //            transaction.ExtensionDays = 0;
        //            transaction.IsFromCustomer = true;

        //            int transferFundEventId = TransferFundEventId;
        //            ReserveEvent(sourceMsisdn, destinationMsisdn, amount, pin, extendedDays, transferFundEventId, out reservationCode, userName);
        //            isEventReserved = true;
        //        }
        //        else if (destinationMsisdnType == SubscribtionType.Customer || destinationMsisdnType == SubscribtionType.DataAccount)
        //        {
        //            transferReason = GetRelatedTransferReason(amount);
        //        }

        //        recievedRequest += "Transfer Reason:" + transferReason;

        //        transaction.ReservationId = reservationCode;

        //        TransferFund(sourceMsisdn, destinationMsisdn, amount, pin, userName, transferReason);
        //        isAmountTransfered = true;

        //        if (transaction.IsFromCustomer)
        //        {
        //            CommitEvent(sourceMsisdn, destinationMsisdn, amount, pin, extendedDays, reservationCode, null, userName);
        //        }

        //        // Extend Account expiry

        //        if (EnableExtendedDays)
        //        {
        //            if (sourceMsisdnType != SubscribtionType.Customer && destinationMsisdnType == SubscribtionType.Customer )
        //            {
        //                ExtendDays(sourceMsisdn, destinationMsisdn, amount, pin, extendedDays, null, userName);
        //            }
        //        }

        //        transaction.IsEventReserved = isEventReserved;
        //        transaction.IsAmountTransfered = true;
        //        transaction.IsEventCharged = isEventReserved;
        //        transaction.IsEventCancelled = false;
        //        transaction.IsExpiryExtended = true;
        //        transaction.StatusId = (byte)TransactionStatus.Succeeded;

        //        TransactionManager.AddTransaction(transaction);

        //    }
        //    catch (Exception)
        //    {
        //        if (!isAmountTransfered)
        //        {
        //            if (isEventReserved)
        //            {
        //                CancelEvent(sourceMsisdn, destinationMsisdn, amount, pin, extendedDays, reservationCode, userName);
        //                transaction.IsFromCustomer = true;
        //                transaction.IsEventReserved = true;
        //                transaction.IsAmountTransfered = false;
        //                transaction.IsEventCharged = false;
        //                transaction.IsEventCancelled = true;
        //                transaction.IsExpiryExtended = false;
        //                transaction.StatusId = (byte)TransactionStatus.TransferFailed;
        //                TransactionManager.AddTransaction(transaction);
        //            }
        //            else if (sourceMsisdnType != SubscribtionType.Customer)
        //            {
        //                transaction.IsFromCustomer = false;
        //                transaction.IsEventReserved = false;
        //                transaction.IsAmountTransfered = false;
        //                transaction.IsEventCharged = false;
        //                transaction.IsEventCancelled = false;
        //                transaction.IsExpiryExtended = false;
        //                transaction.StatusId = (byte)TransactionStatus.TransferFailed;
        //                TransactionManager.AddTransaction(transaction);
        //            }
        //        }

        //        throw;
        //    }
        //}
        #endregion


        public static void TransferCredit(string sourceMsisdn, string destinationMsisdn, decimal amount, string pin, string userName, ref string recievedRequest)
        {
            #region Parameters Validations

            Int64 number = Int64.MinValue;
            // source phone number should not be empty & should be 12 digit numeric only & should be integer..
            if (string.IsNullOrEmpty(sourceMsisdn) || sourceMsisdn.Length != MsisdnLength || !Int64.TryParse(sourceMsisdn, out number))
            {
                throw new InvalidSourcePhoneException();
            }

            number = Int64.MinValue;
            // destination phone number should not be empty & should be 12 digit numeric only & should be integer..
            if (string.IsNullOrEmpty(destinationMsisdn) || destinationMsisdn.Length != MsisdnLength || !Int64.TryParse(destinationMsisdn, out number))
            {
                throw new InvalidDestinationPhoneException();
            }

            number = Int64.MinValue;
            // refill pin should not be empty & should be integer & should be 4 digit numeric only
            if (string.IsNullOrEmpty(pin) || !Int64.TryParse(pin, out number) || pin.Trim().Length != RefillPinLength)
            {
                throw new InvalidPinException();
            }

            // check if source and destination numbers are the same...
            if (sourceMsisdn == destinationMsisdn)
            {
                throw new SourceAndDestinationSameException();
            }

            string nobillSubscriptionType = GetNobillSubscriptionType(sourceMsisdn);
            string destinationNobillSubscriptionType = GetNobillSubscriptionType(destinationMsisdn);

            SubscribtionType sourceMsisdnType;
            try
            {
                sourceMsisdnType = GetAccountType(nobillSubscriptionType);
            }
            catch (SubscriptionNotFoundException)
            {
                throw new SourcePhoneNumberNotFoundException();
            }

            //DATA sim is not allowed to transfer money 
            if (sourceMsisdnType == SubscribtionType.DataAccount)
            {
                throw new NotAllowedToTransferCreditToTheDestinationAccountException();
            }

            SubscribtionType destinationMsisdnType;

            try
            {
                destinationMsisdnType = GetAccountType(destinationNobillSubscriptionType);
            }
            catch (SubscriptionNotFoundException)
            {
                throw new DestinationPhoneNumberNotFoundException();
            }

            //this is to prevent the customer from transfering amount to dealer or distributer
            if ((sourceMsisdnType == SubscribtionType.Customer || sourceMsisdnType == SubscribtionType.HalafoniCustomer) && (destinationMsisdnType == SubscribtionType.Pos ||
                destinationMsisdnType == SubscribtionType.Distributor))
            {
                throw new NotAllowedToTransferCreditToTheDestinationAccountException();
            }

            if ((sourceMsisdnType == SubscribtionType.HalafoniCustomer && destinationMsisdnType == SubscribtionType.Customer) || (sourceMsisdnType == SubscribtionType.Customer && destinationMsisdnType == SubscribtionType.HalafoniCustomer))
            {
                throw new NotAllowedToTransferCreditToTheDestinationAccountException();
            }

            if (sourceMsisdnType == SubscribtionType.Pos && destinationMsisdnType == SubscribtionType.HalafoniCustomer)
            {
                throw new NotAllowedToTransferCreditToTheDestinationAccountException();
            }

            if (GetSubscriptionBlockStatus(sourceMsisdn) != SubscriptionBlockStatus.NO_BLOCK)
            {
                throw new MiscellaneousErrorException();
            }

            if (GetSubscriptionStatus(destinationMsisdn) == SubscriptionStatus.ACTIVE_BEFORE_FIRST_USE)
            {
                throw new InvalidDestinationPhoneException();
            }
            //#################- New changes ################//
            //Get Subscription type configurations

            TransferConfig subscriptionTypeConfig = TransactionManager.GetSubscriptionTypeConfigurations(nobillSubscriptionType);

            //Check if source reach the maximum number of transactions per day


            if (subscriptionTypeConfig == null)
            {
                throw new SubscriptionNotFoundException();
            }



            //check the pin and the maximum amount for transfer credit through the customer service.

            if (pin != DefaultPIN)
            {
                string accountPin = GetAccountPinByServiceName(sourceMsisdn, subscriptionTypeConfig.CreditTransferCustomerService);

                if (!string.IsNullOrEmpty(accountPin))
                {
                    if (accountPin != pin)
                    {
                        throw new PinMismatchException();
                    }
                }
            }

            decimal maxTransferAmount = GetAccountMaxTransferAmountByServiceName(sourceMsisdn, subscriptionTypeConfig.CreditTransferCustomerService);

            if (amount > maxTransferAmount)
            {
                throw new TransferAmountAboveMaxException();
            }

            if (subscriptionTypeConfig.MinTransferAmount.HasValue && amount < subscriptionTypeConfig.MinTransferAmount.Value)
            {
                throw new TransferAmountBelowMinException();
            }

            int numberOfTransactions;

            TransactionManager.GetDailyTransferCount(sourceMsisdn, out numberOfTransactions);

            //check DailyTransferCountLimit
            if (numberOfTransactions != 0 && subscriptionTypeConfig.DailyTransferCountLimit.HasValue && subscriptionTypeConfig.DailyTransferCountLimit.Value <= numberOfTransactions)
            {
                throw new ExceedsMaxPerDayTransactionsException();
            }

            //Check the balance after transfer credit action  for customers only.

            if (subscriptionTypeConfig.MinPostTransferBalance.HasValue && (TransactionManager.GetAccountBalance(sourceMsisdn) - amount < subscriptionTypeConfig.MinPostTransferBalance.Value))
            {
                throw new RemainingBalanceException();
            }

            #endregion

            int reservationCode = -1;
            bool isEventReserved = false;
            bool isAmountTransfered = false;
            int extendedDays = EnableExtendedDays && (destinationMsisdnType == SubscribtionType.Customer )? GetDaysToExtend(amount) : 0;

            Transaction transaction = new Transaction();
            transaction.SourceMsisdn = sourceMsisdn;
            transaction.DestinationMsisdn = destinationMsisdn;
            transaction.Amount = amount;
            transaction.PIN = pin;
            transaction.ExtensionDays = extendedDays;
            transaction.IsFromCustomer = false;
            transaction.CreatedBy = userName == null ? "Debug":userName;

            try
            {
                //transfer fund from A to B
                string transferReason = CustomerToCustomerTransferMonyReason;
                bool isOldtoNew = false;
                bool bothOnSameIN = CheckBothOnSameIN(sourceMsisdn, destinationMsisdn, out isOldtoNew);

                if (!bothOnSameIN)
                {
                    if (isOldtoNew)
                    {
                        transferReason = CustomerToCustomerAdjustmentReasonOldToNew;
                    }
                    else
                    {
                        transferReason = CustomerToCustomerAdjustmentReasonNewToOld;
                    }
                }

                if (sourceMsisdnType == SubscribtionType.Customer)
                {
                    transaction.ExtensionDays = 0;
                    transaction.IsFromCustomer = true;

                    //int transferFundEventId = TransferFundEventId;
                    int transferFundEventId = subscriptionTypeConfig.TransferFeesEventId.Value;
                    ReserveEvent(sourceMsisdn, destinationMsisdn, amount, pin, extendedDays, transferFundEventId, out reservationCode, userName);
                    isEventReserved = true;
                }
                else if (destinationMsisdnType == SubscribtionType.Customer || destinationMsisdnType == SubscribtionType.DataAccount)
                {
                    if (!bothOnSameIN)
                    {
                        if (isOldtoNew)
                        {
                            transferReason = GetRelatedAdjustmentReasonOldToNew(amount);
                        }
                        else
                        {
                            transferReason = GetRelatedAdjustmentReasonNewToOld(amount);
                        }
                    }
                    else
                    {
                        transferReason = GetRelatedTransferReason(amount);
                    }
                }

                recievedRequest += "Transfer Reason:" + transferReason;

                transaction.ReservationId = reservationCode;

                TransferFund(sourceMsisdn, destinationMsisdn, amount, pin, userName, transferReason, !bothOnSameIN);
                isAmountTransfered = true;

                if (transaction.IsFromCustomer)
                {
                    CommitEvent(sourceMsisdn, destinationMsisdn, amount, pin, extendedDays, reservationCode, null, userName);
                }

                // Extend Account expiry

                if (EnableExtendedDays)
                {
                    if (sourceMsisdnType != SubscribtionType.Customer && (destinationMsisdnType == SubscribtionType.Customer))
                    {
                        ExtendDays(sourceMsisdn, destinationMsisdn, amount, pin, extendedDays, null, userName);
                    }
                }

                transaction.IsEventReserved = isEventReserved;
                transaction.IsAmountTransfered = true;
                transaction.IsEventCharged = isEventReserved;
                transaction.IsEventCancelled = false;
                transaction.IsExpiryExtended = true;
                transaction.StatusId = (byte)TransactionStatus.Succeeded;

                TransactionManager.AddTransaction(transaction);

                try
                {
                    SendSMS("FRiENDi", sourceMsisdn, string.Format(APartySMSEn, amount, destinationMsisdn), string.Format(APartySMSAr, amount, destinationMsisdn));
                    SendSMS("FRiENDi", destinationMsisdn, string.Format(BPartySMSEn, amount, sourceMsisdn), string.Format(BPartySMSAr, amount, sourceMsisdn));
                }
                catch (Exception exp)
                {

                }

            }
            catch (Exception)
            {
                if (!isAmountTransfered)
                {
                    if (isEventReserved)
                    {
                        CancelEvent(sourceMsisdn, destinationMsisdn, amount, pin, extendedDays, reservationCode, userName);
                        transaction.IsFromCustomer = true;
                        transaction.IsEventReserved = true;
                        transaction.IsAmountTransfered = false;
                        transaction.IsEventCharged = false;
                        transaction.IsEventCancelled = true;
                        transaction.IsExpiryExtended = false;
                        transaction.StatusId = (byte)TransactionStatus.TransferFailed;
                        TransactionManager.AddTransaction(transaction);
                    }
                    else if (sourceMsisdnType != SubscribtionType.Customer)
                    {
                        transaction.IsFromCustomer = false;
                        transaction.IsEventReserved = false;
                        transaction.IsAmountTransfered = false;
                        transaction.IsEventCharged = false;
                        transaction.IsEventCancelled = false;
                        transaction.IsExpiryExtended = false;
                        transaction.StatusId = (byte)TransactionStatus.TransferFailed;
                        TransactionManager.AddTransaction(transaction);
                    }
                }

                throw;
            }



        }

        public static void TransferCreditWithAdjustmentReason(string sourceMsisdn, string destinationMsisdn, decimal amount, string pin, string userName,string adjustmentReason, ref string recievedRequest)
        {
            #region Parameters Validations

            Int64 number = Int64.MinValue;
            // source phone number should not be empty & should be 12 digit numeric only & should be integer..
            if (string.IsNullOrEmpty(sourceMsisdn) || sourceMsisdn.Length != MsisdnLength || !Int64.TryParse(sourceMsisdn, out number))
            {
                throw new InvalidSourcePhoneException();
            }

            number = Int64.MinValue;
            // destination phone number should not be empty & should be 12 digit numeric only & should be integer..
            if (string.IsNullOrEmpty(destinationMsisdn) || destinationMsisdn.Length != MsisdnLength || !Int64.TryParse(destinationMsisdn, out number))
            {
                throw new InvalidDestinationPhoneException();
            }

            number = Int64.MinValue;
            // refill pin should not be empty & should be integer & should be 4 digit numeric only
            if (string.IsNullOrEmpty(pin) || !Int64.TryParse(pin, out number) || pin.Trim().Length != RefillPinLength)
            {
                throw new InvalidPinException();
            }

            // check if source and destination numbers are the same...
            if (sourceMsisdn == destinationMsisdn)
            {
                throw new SourceAndDestinationSameException();
            }

            string nobillSubscriptionType = GetNobillSubscriptionType(sourceMsisdn);
            string destinationNobillSubscriptionType = GetNobillSubscriptionType(destinationMsisdn);

            SubscribtionType sourceMsisdnType;
            try
            {
                sourceMsisdnType = GetAccountType(nobillSubscriptionType);
            }
            catch (SubscriptionNotFoundException)
            {
                throw new SourcePhoneNumberNotFoundException();
            }

            //DATA sim is not allowed to transfer money 
            if (sourceMsisdnType == SubscribtionType.DataAccount)
            {
                throw new NotAllowedToTransferCreditToTheDestinationAccountException();
            }

            SubscribtionType destinationMsisdnType;

            try
            {
                destinationMsisdnType = GetAccountType(destinationNobillSubscriptionType);
            }
            catch (SubscriptionNotFoundException)
            {
                throw new DestinationPhoneNumberNotFoundException();
            }

            //this is to prevent the customer from transfering amount to dealer or distributer
            if ((sourceMsisdnType == SubscribtionType.Customer || sourceMsisdnType == SubscribtionType.HalafoniCustomer) && (destinationMsisdnType == SubscribtionType.Pos ||
                destinationMsisdnType == SubscribtionType.Distributor))
            {
                throw new NotAllowedToTransferCreditToTheDestinationAccountException();
            }

            if ((sourceMsisdnType == SubscribtionType.HalafoniCustomer && destinationMsisdnType == SubscribtionType.Customer) || (sourceMsisdnType == SubscribtionType.Customer && destinationMsisdnType == SubscribtionType.HalafoniCustomer))
            {
                throw new NotAllowedToTransferCreditToTheDestinationAccountException();
            }

            if (sourceMsisdnType == SubscribtionType.Pos && destinationMsisdnType == SubscribtionType.HalafoniCustomer)
            {
                throw new NotAllowedToTransferCreditToTheDestinationAccountException();
            }

            if (GetSubscriptionBlockStatus(sourceMsisdn) != SubscriptionBlockStatus.NO_BLOCK)
            {
                throw new MiscellaneousErrorException();
            }

            if (GetSubscriptionStatus(destinationMsisdn) == SubscriptionStatus.ACTIVE_BEFORE_FIRST_USE)
            {
                throw new InvalidDestinationPhoneException();
            }
            //#################- New changes ################//
            //Get Subscription type configurations

            TransferConfig subscriptionTypeConfig = TransactionManager.GetSubscriptionTypeConfigurations(nobillSubscriptionType);

            //Check if source reach the maximum number of transactions per day


            if (subscriptionTypeConfig == null)
            {
                throw new SubscriptionNotFoundException();
            }



            //check the pin and the maximum amount for transfer credit through the customer service.

            if (pin != DefaultPIN)
            {
                string accountPin = GetAccountPinByServiceName(sourceMsisdn, subscriptionTypeConfig.CreditTransferCustomerService);

                if (!string.IsNullOrEmpty(accountPin))
                {
                    if (accountPin != pin)
                    {
                        throw new PinMismatchException();
                    }
                }
            }

            decimal maxTransferAmount = GetAccountMaxTransferAmountByServiceName(sourceMsisdn, subscriptionTypeConfig.CreditTransferCustomerService);

            if (amount > maxTransferAmount)
            {
                throw new TransferAmountAboveMaxException();
            }

            if (subscriptionTypeConfig.MinTransferAmount.HasValue && amount < subscriptionTypeConfig.MinTransferAmount.Value)
            {
                throw new TransferAmountBelowMinException();
            }

            int numberOfTransactions;

            TransactionManager.GetDailyTransferCount(sourceMsisdn, out numberOfTransactions);

            //check DailyTransferCountLimit
            if (numberOfTransactions != 0 && subscriptionTypeConfig.DailyTransferCountLimit.HasValue && subscriptionTypeConfig.DailyTransferCountLimit.Value <= numberOfTransactions)
            {
                throw new ExceedsMaxPerDayTransactionsException();
            }

            //Check the balance after transfer credit action  for customers only.

            if (subscriptionTypeConfig.MinPostTransferBalance.HasValue && (TransactionManager.GetAccountBalance(sourceMsisdn) - amount < subscriptionTypeConfig.MinPostTransferBalance.Value))
            {
                throw new RemainingBalanceException();
            }

            #endregion

            int reservationCode = -1;
            bool isEventReserved = false;
            bool isAmountTransfered = false;
            int extendedDays = EnableExtendedDays && (destinationMsisdnType == SubscribtionType.Customer) ? GetDaysToExtend(amount) : 0;

            Transaction transaction = new Transaction();
            transaction.SourceMsisdn = sourceMsisdn;
            transaction.DestinationMsisdn = destinationMsisdn;
            transaction.Amount = amount;
            transaction.PIN = pin;
            transaction.ExtensionDays = extendedDays;
            transaction.IsFromCustomer = false;
            transaction.CreatedBy = userName == null ? "Debug" : userName;

            try
            {
                //transfer fund from A to B

                bool isOldtoNew = false;
                bool bothOnSameIN = CheckBothOnSameIN(sourceMsisdn, destinationMsisdn, out isOldtoNew);


                if (sourceMsisdnType == SubscribtionType.Customer)
                {
                    transaction.ExtensionDays = 0;
                    transaction.IsFromCustomer = true;

                    //int transferFundEventId = TransferFundEventId;
                    int transferFundEventId = subscriptionTypeConfig.TransferFeesEventId.Value;
                    ReserveEvent(sourceMsisdn, destinationMsisdn, amount, pin, extendedDays, transferFundEventId, out reservationCode, userName);
                    isEventReserved = true;
                }
             


                transaction.ReservationId = reservationCode;

                TransferFund(sourceMsisdn, destinationMsisdn, amount, pin, userName, adjustmentReason, !bothOnSameIN);
                isAmountTransfered = true;

                if (transaction.IsFromCustomer)
                {
                    CommitEvent(sourceMsisdn, destinationMsisdn, amount, pin, extendedDays, reservationCode, null, userName);
                }

                // Extend Account expiry

                if (EnableExtendedDays)
                {
                    if (sourceMsisdnType != SubscribtionType.Customer && (destinationMsisdnType == SubscribtionType.Customer))
                    {
                        ExtendDays(sourceMsisdn, destinationMsisdn, amount, pin, extendedDays, null, userName);
                    }
                }

                transaction.IsEventReserved = isEventReserved;
                transaction.IsAmountTransfered = true;
                transaction.IsEventCharged = isEventReserved;
                transaction.IsEventCancelled = false;
                transaction.IsExpiryExtended = true;
                transaction.StatusId = (byte)TransactionStatus.Succeeded;

                TransactionManager.AddTransaction(transaction);

                try
                {
                    SendSMS("FRiENDi", sourceMsisdn, string.Format(APartySMSEn, amount, destinationMsisdn), string.Format(APartySMSAr, amount, destinationMsisdn));
                    SendSMS("FRiENDi", destinationMsisdn, string.Format(BPartySMSEn, amount, sourceMsisdn), string.Format(BPartySMSAr, amount, sourceMsisdn));
                }
                catch (Exception exp)
                {

                }

            }
            catch (Exception)
            {
                if (!isAmountTransfered)
                {
                    if (isEventReserved)
                    {
                        CancelEvent(sourceMsisdn, destinationMsisdn, amount, pin, extendedDays, reservationCode, userName);
                        transaction.IsFromCustomer = true;
                        transaction.IsEventReserved = true;
                        transaction.IsAmountTransfered = false;
                        transaction.IsEventCharged = false;
                        transaction.IsEventCancelled = true;
                        transaction.IsExpiryExtended = false;
                        transaction.StatusId = (byte)TransactionStatus.TransferFailed;
                        TransactionManager.AddTransaction(transaction);
                    }
                    else if (sourceMsisdnType != SubscribtionType.Customer)
                    {
                        transaction.IsFromCustomer = false;
                        transaction.IsEventReserved = false;
                        transaction.IsAmountTransfered = false;
                        transaction.IsEventCharged = false;
                        transaction.IsEventCancelled = false;
                        transaction.IsExpiryExtended = false;
                        transaction.StatusId = (byte)TransactionStatus.TransferFailed;
                        TransactionManager.AddTransaction(transaction);
                    }
                }

                throw;
            }



        }

        #region CommentedCode
        //private static void FRiENDiTransferCredit(string sourceMsisdn, string destinationMsisdn, decimal amount, string pin, string userName, TransferConfig subscriptionTypeConfig, ref string recievedRequest)
        //{

        //    SubscribtionType sourceMsisdnType;
        //    try
        //    {
        //        sourceMsisdnType = GetAccountType(sourceMsisdn);
        //    }
        //    catch (SubscriptionNotFoundException)
        //    {
        //        throw new SourcePhoneNumberNotFoundException();
        //    }

        //    //DATA sim is not allowed to transfer money 
        //    if (sourceMsisdnType == SubscribtionType.DataAccount)
        //    {
        //        throw new NotAllowedToTransferCreditToTheDestinationAccountException();
        //    }

        //    SubscribtionType destinationMsisdnType;

        //    try
        //    {
        //        destinationMsisdnType = GetAccountType(destinationMsisdn);
        //    }
        //    catch (SubscriptionNotFoundException)
        //    {
        //        throw new DestinationPhoneNumberNotFoundException();
        //    }

        //    //this is to prevent the customer from transfering amount to dealer or distributer
        //    if (sourceMsisdnType == SubscribtionType.Customer && (destinationMsisdnType == SubscribtionType.Pos || destinationMsisdnType == SubscribtionType.Distributor))
        //    {
        //        throw new NotAllowedToTransferCreditToTheDestinationAccountException();
        //    }

        //    if (GetSubscriptionBlockStatus(sourceMsisdn) != SubscriptionBlockStatus.NO_BLOCK)
        //    {
        //        throw new MiscellaneousErrorException();
        //    }

        //    if (GetSubscriptionStatus(destinationMsisdn) == SubscriptionStatus.ACTIVE_BEFORE_FIRST_USE)
        //    {
        //        throw new InvalidDestinationPhoneException();
        //    }
        //    //#################- New changes ################//
        //    //Get Subscription type configurations

        //    //Check if source reach the maximum number of transactions per day

        //    if (subscriptionTypeConfig == null)
        //    {
        //        throw new SubscriptionNotFoundException();
        //    }

        //    //check the pin and the maximum amount for transfer credit through the customer service.

        //    string accountPin = GetAccountPinByServiceName(sourceMsisdn, subscriptionTypeConfig.CreditTransferCustomerService);

        //    if (accountPin != pin)
        //    {
        //        throw new PinMismatchException();
        //    }

        //    decimal maxTransferAmount = GetAccountMaxTransferAmountByServiceName(sourceMsisdn, subscriptionTypeConfig.CreditTransferCustomerService);

        //    if (amount > maxTransferAmount)
        //    {
        //        throw new TransferAmountAboveMaxException();
        //    }

        //    if (subscriptionTypeConfig.MinTransferAmount.HasValue && amount < subscriptionTypeConfig.MinTransferAmount.Value)
        //    {
        //        throw new TransferAmountBelowMinException();
        //    }

        //    int numberOfTransactions;

        //    TransactionManager.GetDailyTransferCount(sourceMsisdn, out numberOfTransactions);

        //    //check DailyTransferCountLimit
        //    if (numberOfTransactions != 0 && subscriptionTypeConfig.DailyTransferCountLimit.HasValue && subscriptionTypeConfig.DailyTransferCountLimit.Value <= numberOfTransactions)
        //    {
        //        throw new ExceedsMaxPerDayTransactionsException();
        //    }

        //    //Check the balance after transfer credit action  for customers only.

        //    if (subscriptionTypeConfig.MinPostTransferBalance.HasValue && (TransactionManager.GetAccountBalance(sourceMsisdn) - amount < subscriptionTypeConfig.MinPostTransferBalance.Value))
        //    {
        //        throw new RemainingBalanceException();
        //    }

        //    #endregion

        //    int reservationCode = -1;
        //    bool isEventReserved = false;
        //    bool isAmountTransfered = false;
        //    int extendedDays = EnableExtendedDays && destinationMsisdnType == SubscribtionType.Customer ? GetDaysToExtend(amount) : 0;

        //    Transaction transaction = new Transaction();
        //    transaction.SourceMsisdn = sourceMsisdn;
        //    transaction.DestinationMsisdn = destinationMsisdn;
        //    transaction.Amount = amount;
        //    transaction.PIN = pin;
        //    transaction.ExtensionDays = extendedDays;
        //    transaction.IsFromCustomer = false;
        //    transaction.CreatedBy = userName;

        //    try
        //    {
        //        //transfer fund from A to B
        //        string transferReason = CustomerToCustomerTransferMonyReason;



        //        if (sourceMsisdnType == SubscribtionType.Customer)
        //        {
        //            transaction.ExtensionDays = 0;
        //            transaction.IsFromCustomer = true;

        //            //int transferFundEventId = TransferFundEventId;
        //            int transferFundEventId = subscriptionTypeConfig.TransferFeesEventId.Value;
        //            ReserveEvent(sourceMsisdn, destinationMsisdn, amount, pin, extendedDays, transferFundEventId, out reservationCode, userName);
        //            isEventReserved = true;
        //        }
        //        else if (destinationMsisdnType == SubscribtionType.Customer || destinationMsisdnType == SubscribtionType.DataAccount)
        //        {
        //            transferReason = GetRelatedTransferReason(amount);
        //        }

        //        recievedRequest += "Transfer Reason:" + transferReason;

        //        transaction.ReservationId = reservationCode;

        //        TransferFund(sourceMsisdn, destinationMsisdn, amount, pin, userName, transferReason);
        //        isAmountTransfered = true;

        //        if (transaction.IsFromCustomer)
        //        {
        //            CommitEvent(sourceMsisdn, destinationMsisdn, amount, pin, extendedDays, reservationCode, null, userName);
        //        }

        //        // Extend Account expiry

        //        if (EnableExtendedDays)
        //        {
        //            if (sourceMsisdnType != SubscribtionType.Customer && destinationMsisdnType == SubscribtionType.Customer)
        //            {
        //                ExtendDays(sourceMsisdn, destinationMsisdn, amount, pin, extendedDays, null, userName);
        //            }
        //        }

        //        transaction.IsEventReserved = isEventReserved;
        //        transaction.IsAmountTransfered = true;
        //        transaction.IsEventCharged = isEventReserved;
        //        transaction.IsEventCancelled = false;
        //        transaction.IsExpiryExtended = true;
        //        transaction.StatusId = (byte)TransactionStatus.Succeeded;

        //        TransactionManager.AddTransaction(transaction);

        //    }
        //    catch (Exception)
        //    {
        //        if (!isAmountTransfered)
        //        {
        //            if (isEventReserved)
        //            {
        //                CancelEvent(sourceMsisdn, destinationMsisdn, amount, pin, extendedDays, reservationCode, userName);
        //                transaction.IsFromCustomer = true;
        //                transaction.IsEventReserved = true;
        //                transaction.IsAmountTransfered = false;
        //                transaction.IsEventCharged = false;
        //                transaction.IsEventCancelled = true;
        //                transaction.IsExpiryExtended = false;
        //                transaction.StatusId = (byte)TransactionStatus.TransferFailed;
        //                TransactionManager.AddTransaction(transaction);
        //            }
        //            else if (sourceMsisdnType != SubscribtionType.Customer)
        //            {
        //                transaction.IsFromCustomer = false;
        //                transaction.IsEventReserved = false;
        //                transaction.IsAmountTransfered = false;
        //                transaction.IsEventCharged = false;
        //                transaction.IsEventCancelled = false;
        //                transaction.IsExpiryExtended = false;
        //                transaction.StatusId = (byte)TransactionStatus.TransferFailed;
        //                TransactionManager.AddTransaction(transaction);
        //            }
        //        }

        //        throw;
        //    }
        //}
        #endregion

        private static void SendSMS(string from, string phoneNo, string messageEn, string messageAr)
        {
            int responseCode;

            using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
            {
                nobillCalls.Credentials = Common.GetServiceCredentials();
                string locale = string.Empty;
                responseCode = nobillCalls.GetSubscriptionValue(phoneNo, NobillCalls.SubscriptionItem.locale, out locale);

                if (responseCode == 0)
                {
                    if (locale.ToLower().Contains("ar"))
                    {
                        responseCode = nobillCalls.SendHTTPSMS(from, phoneNo, messageAr, true);
                    }
                    else
                    {
                        responseCode = nobillCalls.SendHTTPSMS(from, phoneNo, messageEn, false);
                    }
                }
            }
        }
        private static void TransferFund(string sourceMsisdn, string destinationMsisdn, decimal amount, string pin, string userName, string transferReason, bool onDifferentINs = false)
        {
            try
            {
               
                int responseCode = 0;

                using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
                {

                    nobillCalls.Credentials = Common.GetServiceCredentials();

                    if (onDifferentINs)
                    {
                        responseCode = nobillCalls.AdjustAccountByReason(sourceMsisdn, -amount, Guid.NewGuid().ToString(), transferReason, AdjustmentType, string.Format("Fund transfer detail: {0} OMR from phoneno {1} to phoneno {2}", amount, sourceMsisdn, destinationMsisdn));

                        if (responseCode == 0)
                        {
                            responseCode = nobillCalls.AdjustAccountByReason(destinationMsisdn, amount, Guid.NewGuid().ToString(), transferReason, AdjustmentType, string.Format("Fund transfer detail: {0} OMR from phoneno {1} to phoneno {2}", amount, sourceMsisdn, destinationMsisdn));

                            if (responseCode != 0)
                            {
                                responseCode = nobillCalls.AdjustAccountByReason(sourceMsisdn, amount, Guid.NewGuid().ToString(), transferReason, AdjustmentType, string.Format("Rollback Fund transfer detail: {0} OMR from phoneno {1} to phoneno {2}", amount, sourceMsisdn, destinationMsisdn));
                            }
                        }
                    }
                    else
                    {
                        responseCode = nobillCalls.TransferMoney(sourceMsisdn, destinationMsisdn, amount, transferReason, userName, string.Format("Fund transfer detail: {0} OMR from phoneno {1} to phoneno {2}",amount, sourceMsisdn, destinationMsisdn));
                    }

                }

                if (responseCode == 3)
                {
                    throw new MiscellaneousErrorException();
                }
                else if (responseCode == 7)
                {
                    throw new SourcePhoneNumberNotFoundException();
                }
                else if (responseCode == 8)
                {
                    throw new SourceAndDestinationSameException();
                }
                else if (responseCode == 9)
                {
                    throw new DestinationPhoneNumberNotFoundException();
                }
                else if (responseCode == 10 || responseCode == 16)
                {
                    throw new InsuffientCreditException();
                }
                else if (responseCode != 0)
                {
                    throw new Exception(Constants.ServiceUnavailableMessage);
                }
            }
            finally
            {
            }
        }

        private static int GetEventId(decimal amount)
        {
            string[] eventIds = VirginEventIds.Split(',');
            decimal _amount;

            foreach (var item in eventIds)
            {
                _amount = Convert.ToDecimal(item.Split('|')[0]);

                if (_amount == amount)
                {
                    return int.Parse(item.Split('|')[1]);
                }
            }
            return -1;
        }

        public static void ExtendDays(string sourceMsisdn, string destinationMsisdn, decimal amount, string pin, int numberOfDays, long? transactionId, string userName)
        {
            try
            {
                if (numberOfDays <= 0)
                    return;

                int responseCode;

                using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
                {
                    nobillCalls.Credentials = Common.GetServiceCredentials();
                    responseCode = nobillCalls.ExtendSubscriptionExpiry(destinationMsisdn, numberOfDays);
                }

                switch (responseCode)
                {
                    case 3:
                        throw new MiscellaneousErrorException();
                    case 7:
                        throw new DestinationPhoneNumberNotFoundException();
                    case 13:
                        throw new ConcurrentUpdateDetectedException();
                    default:
                        if (responseCode != 0)
                        {
                            throw new Exception(Constants.ServiceUnavailableMessage);
                        }
                        break;
                }
            }
            catch (Exception)
            {
                if (!transactionId.HasValue)
                {
                    Transaction transaction = new Transaction();
                    transaction.SourceMsisdn = sourceMsisdn;
                    transaction.DestinationMsisdn = destinationMsisdn;
                    transaction.Amount = amount;
                    transaction.PIN = pin;
                    transaction.IsFromCustomer = true;
                    transaction.IsEventReserved = true;
                    transaction.IsAmountTransfered = true;
                    transaction.IsEventCharged = true;
                    transaction.IsEventCancelled = false;
                    transaction.IsExpiryExtended = false;
                    transaction.ExtensionDays = numberOfDays;
                    transaction.ReservationId = 0;
                    transaction.StatusId = (byte)TransactionStatus.ExtensionFailed;
                    transaction.CreatedBy = userName;

                    TransactionManager.AddTransaction(transaction);
                }

                throw;
            }
        }

        public bool  CheckCustomerService(string sourceMsisdn, string customerServiceName)
        {
            try
            {
               

                int responseCode;

                using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
                {
                    nobillCalls.Credentials = Common.GetServiceCredentials();
                    responseCode = nobillCalls.CheckCustomerService(sourceMsisdn, customerServiceName);

                    if (responseCode == 0)
                        return true;

                    return false;
                }

            }
            catch (Exception)
            {
                return false;
            }
        }
        //public static SubscribtionType GetAccountType(string msisdn)
        //{
        //    try
        //    {
        //        int responseCode;
        //        string accountType;

                //        using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
                //        {
                //            nobillCalls.Credentials = Common.GetServiceCredentials();
                //            responseCode = nobillCalls.GetAccountValue(msisdn, NobillCalls.AccountItem.account_type, out accountType);
                //        }

                //        if (responseCode == 3)
                //        {
                //            throw new MiscellaneousErrorException();
                //        }
                //        else if (responseCode == 7)
                //        {
                //            throw new SubscriptionNotFoundException();
                //        }
                //        else if (responseCode == 9)
                //        {
                //            throw new PropertyNotFoundException();
                //        }
                //        else if (responseCode != 0)
                //        {
                //            throw new Exception(Constants.ServiceUnavailableMessage);
                //        }
                //        accountType = accountType.ToLower();

                //        if (accountType.Contains("dealer"))
                //        {
                //            return SubscribtionType.Pos;
                //        }
                //        else if (accountType.Contains("distributor"))
                //        {
                //            return SubscribtionType.Distributor;
                //        }
                //        else if (accountType.Contains("customer_account") || accountType.Contains("prepaid_cust")
                //            || accountType.Contains("postpaid_cust"))
                //        {
                //            return SubscribtionType.Customer;
                //        }
                //        else if (accountType.Contains("data_account"))
                //        {
                //            return SubscribtionType.DataAccount;
                //        }

                //        //For sure test account type will always be captured as customer based on the above customer account check 
                //        //this is not by mistake we want this to be happend however at some point if we requested to deal with test 
                //        //account using a different business rules than Customer account type just take below condition above the 
                //        //customer account type check 
                //        //
                //        else if (accountType.Contains("customer_account_test"))
                //        {
                //            return SubscribtionType.Test;
                //        }
                //        else
                //        {
                //            throw new UnknownSubscriberException();
                //        }
                //    }
                //    finally
                //    {
                //    }
                //}

        public static SubscribtionType GetAccountType(string nobillSubscriptionType)
        {
            string accountType = TransactionManager.GetAccountType(nobillSubscriptionType);

            accountType = accountType.ToLower();

            if (accountType == "pos")
            {
                return SubscribtionType.Pos;
            }
            else if (accountType == "distributor")
            {
                return SubscribtionType.Distributor;
            }
            else if (accountType == "customer") //which mean it's friendi customer
            {
                return SubscribtionType.Customer;
            }
            else if (accountType == "halafonicustomer")
            {
                return SubscribtionType.HalafoniCustomer;
            }
            else if (accountType == "data")
            {
                return SubscribtionType.DataAccount;
            }

            //For sure test account type will always be captured as customer based on the above customer account check 
            //this is not by mistake we want this to be happend however at some point if we requested to deal with test 
            //account using a different business rules than Customer account type just take below condition above the 
            //customer account type check 
            //
            else
            {
                throw new UnknownSubscriberException();
            }
        }

        public static SubscriptionBlockStatus GetSubscriptionBlockStatus(string msisdn)
        {
            try
            {
                int responseCode;
                string blockStatus;

                using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
                {
                    nobillCalls.Credentials = Common.GetServiceCredentials();
                    responseCode = nobillCalls.GetSubscriptionValue(msisdn, NobillCalls.SubscriptionItem.blocked, out blockStatus);
                }

                switch (responseCode)
                {
                    case 3:
                        throw new MiscellaneousErrorException();
                    case 7:
                        throw new SubscriptionNotFoundException();
                    case 9:
                        throw new PropertyNotFoundException();
                    default:
                        if (responseCode != 0)
                        {
                            throw new Exception(Constants.ServiceUnavailableMessage);
                        }
                        break;
                }

                return (SubscriptionBlockStatus)int.Parse(blockStatus);
            }
            finally
            {
            }
        }

        public static SubscriptionStatus GetSubscriptionStatus(string msisdn)
        {
            try
            {
                int responseCode;
                string subscriptionStatus;

                using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
                {
                    nobillCalls.Credentials = Common.GetServiceCredentials();
                    responseCode = nobillCalls.GetSubscriptionValue(msisdn, NobillCalls.SubscriptionItem.status, out subscriptionStatus);
                }

                switch (responseCode)
                {
                    case 3:
                        throw new MiscellaneousErrorException();
                    case 7:
                        throw new SubscriptionNotFoundException();
                    case 9:
                        throw new PropertyNotFoundException();
                    default:
                        if (responseCode != 0)
                        {
                            throw new Exception(Constants.ServiceUnavailableMessage);
                        }
                        break;
                }

                return (SubscriptionStatus)int.Parse(subscriptionStatus);
            }
            finally
            {
            }
        }

        private static string GetAccountPin(string msisdn)
        {
            try
            {
                int responseCode;
                string pin;
                using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
                {
                    nobillCalls.Credentials = Common.GetServiceCredentials();
                    responseCode = nobillCalls.GetCreditTransferValue(msisdn, NobillCalls.CreditTransferItem.p_pin, out pin);
                }

                switch (responseCode)
                {
                    case 3:
                        throw new MiscellaneousErrorException();
                    case 7:
                        throw new SourcePhoneNumberNotFoundException();
                    case 9:
                        throw new PropertyNotFoundException();
                    default:
                        if (responseCode != 0)
                        {
                            throw new Exception(Constants.ServiceUnavailableMessage);
                        }
                        break;
                }

                return pin;
            }
            finally
            {
            }
        }

        private static decimal GetAccountMaxTransferAmount(string msisdn)
        {
            try
            {
                int responseCode;
                string maxTransferAmount;
                using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
                {

                    nobillCalls.Credentials = Common.GetServiceCredentials();
                    responseCode = nobillCalls.GetCreditTransferValue(msisdn, NobillCalls.CreditTransferItem.p_maxamount, out maxTransferAmount);
                }

                switch (responseCode)
                {
                    case 3:
                        throw new MiscellaneousErrorException();
                    case 7:
                        throw new SourcePhoneNumberNotFoundException();
                    default:
                        if (responseCode != 0)
                        {
                            throw new Exception(Constants.ServiceUnavailableMessage);
                        }
                        break;
                }

                return Convert.ToDecimal(maxTransferAmount);
            }
            finally
            {
            }
        }

        private static void ReserveEvent(string sourceMsisdn, string destinationMsisdn, decimal amount, string pin, int numberOfDays, int eventId, out int reservationCode, string userName)
        {
            reservationCode = -1;
            try
            {
                int responseCode;

                if (eventId == 0)
                    return;

                using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
                {
                    nobillCalls.Credentials = Common.GetServiceCredentials();
                    responseCode = nobillCalls.ReserveEvent(sourceMsisdn, eventId, out reservationCode);
                }

                if (responseCode == 3)
                {
                    throw new MiscellaneousErrorException();
                }
                if (responseCode == 5)
                {
                    throw new InsuffientCreditException();
                }
                if (responseCode != 0)
                {
                    throw new Exception(Constants.ServiceUnavailableMessage);
                }
            }
            catch (Exception)
            {
                Transaction transaction = new Transaction();
                transaction.SourceMsisdn = sourceMsisdn;
                transaction.DestinationMsisdn = destinationMsisdn;
                transaction.Amount = amount;
                transaction.PIN = pin;
                transaction.IsFromCustomer = true;
                transaction.IsEventReserved = false;
                transaction.IsAmountTransfered = false;
                transaction.IsEventCharged = false;
                transaction.IsEventCancelled = false;
                transaction.IsExpiryExtended = false;
                transaction.ExtensionDays = numberOfDays;
                transaction.ReservationId = reservationCode;
                transaction.StatusId = (byte)TransactionStatus.ReservationFailed;
                transaction.CreatedBy = userName;

                TransactionManager.AddTransaction(transaction);

                throw;
            }
        }

        public static void CommitEvent(string sourceMsisdn, string destinationMsisdn, decimal amount, string pin, int numberOfDays, int reservationCode, Nullable<long> transactionId, string userName)
        {
            try
            {
                int responseCode;

                if (reservationCode <= 0)
                    return;

                using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
                {
                    nobillCalls.Credentials = Common.GetServiceCredentials();
                    responseCode = nobillCalls.ChargeReservedEvent(sourceMsisdn, reservationCode);
                }

                if (responseCode == 3)
                {
                    throw new MiscellaneousErrorException();
                }
                //Nobill Auto Cancelled the the Reserved Event
                if (responseCode == 6)
                {
                    throw new ExpiredReservationCodeException();
                }
                if (responseCode != 0)
                {
                    throw new Exception(Constants.ServiceUnavailableMessage);
                }
            }
            catch (Exception)
            {
                if (!transactionId.HasValue)
                {
                    Transaction transaction = new Transaction();
                    transaction.SourceMsisdn = sourceMsisdn;
                    transaction.DestinationMsisdn = destinationMsisdn;
                    transaction.Amount = amount;
                    transaction.PIN = pin;
                    transaction.IsFromCustomer = true;
                    transaction.IsEventReserved = true;
                    transaction.IsAmountTransfered = true;
                    transaction.IsEventCharged = false;
                    transaction.IsEventCancelled = false;
                    transaction.IsExpiryExtended = false;
                    transaction.ExtensionDays = numberOfDays;
                    transaction.ReservationId = reservationCode;
                    transaction.StatusId = (byte)TransactionStatus.CommitFailed;
                    transaction.CreatedBy = userName;

                    TransactionManager.AddTransaction(transaction);
                }

                throw;
            }
        }

        private static void CancelEvent(string sourceMsisdn, string destinationMsisdn, decimal amount, string pin, int numberOfDays, int reservationCode, string userName)
        {
            try
            {
                int responseCode;

                using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
                {
                    nobillCalls.Credentials = Common.GetServiceCredentials();
                    responseCode = nobillCalls.CancelReservation(sourceMsisdn, reservationCode);
                }

                if (responseCode == 3)
                {
                    throw new MiscellaneousErrorException();
                }
                if (responseCode != 0)
                {
                    throw new Exception(Constants.ServiceUnavailableMessage);
                }
            }
            catch (Exception)
            {
                Transaction transaction = new Transaction();
                transaction.SourceMsisdn = sourceMsisdn;
                transaction.DestinationMsisdn = destinationMsisdn;
                transaction.Amount = amount;
                transaction.PIN = pin;
                transaction.IsFromCustomer = true;
                transaction.IsEventReserved = true;
                transaction.IsAmountTransfered = false;
                transaction.IsEventCharged = false;
                transaction.IsEventCancelled = false;
                transaction.IsExpiryExtended = false;
                transaction.ExtensionDays = numberOfDays;
                transaction.ReservationId = reservationCode;
                transaction.StatusId = (byte)TransactionStatus.TransferFailedCancelFailed;
                transaction.CreatedBy = userName;

                TransactionManager.AddTransaction(transaction);

                throw;
            }
        }

        private static int GetDaysToExtend(decimal amount)
        {
            string amountTypes = AmountRanges;
            string[] amountTypesList = amountTypes.Split(';');
            string extendedDays = ExtendedDaysTypes;
            string[] extendedDaysList = extendedDays.Split(';');

            if (amountTypesList.Length > 0 || extendedDaysList.Length > 0)
            {
                try
                {
                    if (amount >= Convert.ToDecimal(amountTypesList[amountTypesList.Length - 1]))
                    {
                        return Convert.ToInt32(extendedDaysList[extendedDaysList.Length - 1]);
                    }

                    for (int i = 0; i < amountTypesList.Length - 1; i++)
                    {
                        if (amount >= Convert.ToDecimal(amountTypesList[i]) && amount < Convert.ToDecimal(amountTypesList[i + 1]))
                        {
                            return Convert.ToInt32(extendedDaysList[i]);
                        }
                    }
                }
                catch (Exception)
                {
                    throw new ConfigurationErrorException();
                }
            }
            else
            {
                throw new ConfigurationErrorException();
            }

            return 0;
        }

        private static string GetRelatedTransferReason(decimal amount)
        {
            string amountTypes = AmountRanges;
            string[] amountTypesList = amountTypes.Split(';');
            string transferMonyReasonClassification = TransferMonyReasonClassification;
            string[] transferMonyReasonClassificationList = transferMonyReasonClassification.Split(';');

            if (amountTypesList.Length > 0 || transferMonyReasonClassificationList.Length > 0)
            {
                try
                {
                    if (amount >= Convert.ToDecimal(amountTypesList[amountTypesList.Length - 1]))
                    {
                        return transferMonyReasonClassificationList[transferMonyReasonClassificationList.Length - 1];
                    }

                    for (int i = 0; i < amountTypesList.Length - 1; i++)
                    {
                        if (amount >= Convert.ToDecimal(amountTypesList[i]) && amount < Convert.ToDecimal(amountTypesList[i + 1]))
                        {
                            return transferMonyReasonClassificationList[i];
                        }
                    }
                }
                catch (Exception)
                {
                    throw new ConfigurationErrorException();
                }
            }
            else
            {
                throw new ConfigurationErrorException();
            }

            return "";
        }
        private static string GetRelatedAdjustmentReasonOldToNew(decimal amount)
        {
            string amountTypes = AmountRanges;
            string[] amountTypesList = amountTypes.Split(';');
            string adjustmentReasonClassification = AdjustmentReasonClassificationFromOldToNew;
            string[] transferMonyReasonClassificationList = adjustmentReasonClassification.Split(';');

            if (amountTypesList.Length > 0 || transferMonyReasonClassificationList.Length > 0)
            {
                try
                {
                    if (amount >= Convert.ToDecimal(amountTypesList[amountTypesList.Length - 1]))
                    {
                        return transferMonyReasonClassificationList[transferMonyReasonClassificationList.Length - 1];
                    }

                    for (int i = 0; i < amountTypesList.Length - 1; i++)
                    {
                        if (amount >= Convert.ToDecimal(amountTypesList[i]) && amount < Convert.ToDecimal(amountTypesList[i + 1]))
                        {
                            return transferMonyReasonClassificationList[i];
                        }
                    }
                }
                catch (Exception)
                {
                    throw new ConfigurationErrorException();
                }
            }
            else
            {
                throw new ConfigurationErrorException();
            }

            return "";
        }
        private static string GetRelatedAdjustmentReasonNewToOld(decimal amount)
        {
            string amountTypes = AmountRanges;
            string[] amountTypesList = amountTypes.Split(';');
            string adjustmentReasonClassification = AdjustmentReasonClassificationFromNewToOld;
            string[] transferMonyReasonClassificationList = adjustmentReasonClassification.Split(';');

            if (amountTypesList.Length > 0 || transferMonyReasonClassificationList.Length > 0)
            {
                try
                {
                    if (amount >= Convert.ToDecimal(amountTypesList[amountTypesList.Length - 1]))
                    {
                        return transferMonyReasonClassificationList[transferMonyReasonClassificationList.Length - 1];
                    }

                    for (int i = 0; i < amountTypesList.Length - 1; i++)
                    {
                        if (amount >= Convert.ToDecimal(amountTypesList[i]) && amount < Convert.ToDecimal(amountTypesList[i + 1]))
                        {
                            return transferMonyReasonClassificationList[i];
                        }
                    }
                }
                catch (Exception)
                {
                    throw new ConfigurationErrorException();
                }
            }
            else
            {
                throw new ConfigurationErrorException();
            }

            return "";
        }

        private static string GetAccountPinByServiceName(string msisdn, string serviceName)
        {
            try
            {
                int responseCode;
                string pin;
                using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
                {
                    nobillCalls.Credentials = Common.GetServiceCredentials();
                    responseCode = nobillCalls.GetCreditTransferValueByServiceName(msisdn, NobillCalls.CreditTransferItem.p_pin, serviceName, out pin);
                }

                switch (responseCode)
                {
                    case 3:
                        throw new MiscellaneousErrorException();
                    case 7:
                        throw new SourcePhoneNumberNotFoundException();
                    case 9:
                        throw new PropertyNotFoundException();
                    default:
                        if (responseCode != 0)
                        {
                            throw new Exception(Constants.ServiceUnavailableMessage);
                        }
                        break;
                }

                return pin;
            }
            finally
            {
            }
        }

        private static decimal GetAccountMaxTransferAmountByServiceName(string msisdn, string serviceName)
        {
            try
            {
                int responseCode;
                string maxTransferAmount;
                using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
                {

                    nobillCalls.Credentials = Common.GetServiceCredentials();
                    responseCode = nobillCalls.GetCreditTransferValueByServiceName(msisdn, NobillCalls.CreditTransferItem.p_maxamount, serviceName, out maxTransferAmount);
                }

                switch (responseCode)
                {
                    case 3:
                        throw new MiscellaneousErrorException();
                    case 7:
                        throw new SourcePhoneNumberNotFoundException();
                    default:
                        if (responseCode != 0)
                        {
                            throw new Exception(Constants.ServiceUnavailableMessage);
                        }
                        break;
                }

                if(string.IsNullOrEmpty(maxTransferAmount))
                {
                    return 50;
                }

                return Convert.ToDecimal(maxTransferAmount);
            }
            finally
            {
            }
        }

        public static string GetNobillSubscriptionType(string msisdn)
        {
            try
            {
                int responseCode;
                string subscriptionType;

                using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
                {
                    nobillCalls.Credentials = Common.GetServiceCredentials();
                    responseCode = nobillCalls.GetSubscriptionValue(msisdn, NobillCalls.SubscriptionItem.subscriptiontype, out subscriptionType);

                    if (responseCode == 0)
                    {
                        responseCode = nobillCalls.CheckCustomerService(msisdn, "CS.Dealer_Flag");

                        if (responseCode == 0)
                        {
                            subscriptionType = "dealer";
                        }
                        else
                        {
                            responseCode = 0;
                        }
                    }
                }

                if (responseCode == 3)
                {
                    throw new MiscellaneousErrorException();
                }
                else if (responseCode == 7)
                {
                    throw new SubscriptionNotFoundException();
                }
                else if (responseCode == 9)
                {
                    throw new PropertyNotFoundException();
                }
                else if (responseCode != 0)
                {
                    throw new Exception(Constants.ServiceUnavailableMessage);
                }

                return subscriptionType;
            }
            finally
            {
            }

        }

        //public static bool CheckBothOnSameIN(string msisdnA, string msisdnB, out bool isOldToNew)
        //{
        //    isOldToNew = false;
        //    try
        //    {
        //        int responseCode;
        //        string subscriptionTypeA;
        //        string subscriptionTypeB;

        //        using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
        //        {
        //            nobillCalls.Credentials = Common.GetServiceCredentials();
        //            responseCode = nobillCalls.GetSubscriptionValue(msisdnA, NobillCalls.SubscriptionItem.subscriptiontype, out subscriptionTypeA);

        //            if (responseCode == 0)
        //            {
        //                responseCode = nobillCalls.GetSubscriptionValue(msisdnB, NobillCalls.SubscriptionItem.subscriptiontype, out subscriptionTypeB);

        //                if (responseCode == 0)
        //                {
        //                    if (subscriptionTypeA.ToLower().Contains("friendi-2"))
        //                    {
        //                        if (!subscriptionTypeB.ToLower().Contains("friendi-2"))
        //                        {
        //                            return false;
        //                        }
        //                    }

        //                    if (subscriptionTypeB.ToLower().Contains("friendi-2"))
        //                    {
        //                        if (!subscriptionTypeA.ToLower().Contains("friendi-2"))
        //                        {
        //                            isOldToNew = true;
        //                            return false;
        //                        }
        //                    }

        //                }
        //            }
        //        }

        //        return true;
        //    }
        //    finally
        //    {
        //    }

        //}

        public static bool CheckBothOnSameIN(string msisdnA, string msisdnB, out bool isOldToNew)
        {
            string SubscriptionTypes = ConfigurationManager.AppSettings["SubscriptionTypes"];
            string[] SubscriptionTypesAfterSplit = SubscriptionTypes.Split(',');
            SubscriptionTypesAfterSplit = SubscriptionTypesAfterSplit.Select(s => s.ToLower()).ToArray();

            isOldToNew = false;
            try
            {
                int responseCode;
                string subscriptionTypeA;
                string subscriptionTypeB;

                using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
                {
                    nobillCalls.Credentials = Common.GetServiceCredentials();
                    responseCode = nobillCalls.GetSubscriptionValue(msisdnA, NobillCalls.SubscriptionItem.subscriptiontype, out subscriptionTypeA);

                    if (responseCode == 0)
                    {
                        responseCode = nobillCalls.GetSubscriptionValue(msisdnB, NobillCalls.SubscriptionItem.subscriptiontype, out subscriptionTypeB);

                        if (responseCode == 0)
                        {
                            if (SubscriptionTypesAfterSplit.Any(subscriptionType => subscriptionTypeA.ToLower().Contains(subscriptionType.ToLower())))
                            {
                                if (!SubscriptionTypesAfterSplit.Any(subscriptionType => subscriptionTypeB.ToLower().Contains(subscriptionType.ToLower())))
                                {
                                    return false;
                                }
                            }

                            if (SubscriptionTypesAfterSplit.Any(subscriptionType => subscriptionTypeB.ToLower().Contains(subscriptionType.ToLower())))
                            {
                                if (!SubscriptionTypesAfterSplit.Any(subscriptionType => subscriptionTypeA.ToLower().Contains(subscriptionType.ToLower())))
                                {
                                    isOldToNew = true;
                                    return false;
                                }
                            }

                        }
                    }
                }

                return true;
            }
            finally
            {
            }

        }


        private static IDNumberResultStatus GetIDNumber(string msisdn, string IDnumber)
        {
            string ussdMessage = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?><umsprot version=\"1\"><exec_req function=\"ProcessUSSD\" locale=\"en\" msisdn=\"{0}\" location=\"{0}\"> <data name=\"MSISDN\">{0}</data><data name=\"CIVILID\">{1}</data></exec_req></umsprot>";

            ussdMessage = string.Format(ussdMessage, msisdn, IDnumber);
            System.Net.WebClient client = new System.Net.WebClient();
            client.Headers.Add("Content-type", "application / xml");
            string result = client.UploadString("http://10.4.1.54/IDVerification/IDVerification.aspx", ussdMessage);

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(result);

            string xpath = "umsprot/exec_rsp";
            var nodes = xmlDoc.SelectNodes(xpath);
            string childNode = "2";
            foreach (XmlNode childrenNode in nodes)
            {
                childNode = childrenNode.SelectSingleNode("//data").InnerXml;
            }
            IDNumberResultStatus resultStatus = (IDNumberResultStatus)Convert.ToInt32(childNode);

            return resultStatus;
        }

       

        public static List<decimal> GetDenomination()
        {
            List<decimal> denominations = new List<decimal>();
            string virginAmounts =  ConfigurationManager.AppSettings["VirginEventIds"];
            foreach (var item in virginAmounts.Split(','))
            {
                denominations.Add(Convert.ToDecimal(item.Split('|')[0]));
            }
            return denominations;
        }

        public static void ValidateTransferInputs(string sourceMsisdn, string destinationMsisdn, decimal amount)
        {
            #region Parameters Validations

            Int64 number = Int64.MinValue;
            // source phone number should not be empty & should be 12 digit numeric only & should be integer..
            if (string.IsNullOrEmpty(sourceMsisdn) || sourceMsisdn.Length != MsisdnLength || !Int64.TryParse(sourceMsisdn, out number))
            {
                throw new InvalidSourcePhoneException();
            }

            number = Int64.MinValue;
            // destination phone number should not be empty & should be 12 digit numeric only & should be integer..
            if (string.IsNullOrEmpty(destinationMsisdn) || destinationMsisdn.Length != MsisdnLength || !Int64.TryParse(destinationMsisdn, out number))
            {
                throw new InvalidDestinationPhoneException();
            }

            // check if source and destination numbers are the same...
            if (sourceMsisdn == destinationMsisdn)
            {
                throw new SourceAndDestinationSameException();
            }

            string nobillSubscriptionType = string.Empty;
            string destinationNobillSubscriptionType = string.Empty;
            try
            {
                nobillSubscriptionType = GetNobillSubscriptionType(sourceMsisdn);

            }
            catch (Exception ex)
            {
                throw new InvalidSourcePhoneException();
            }

            try
            {
                destinationNobillSubscriptionType = GetNobillSubscriptionType(destinationMsisdn);
            }
            catch (Exception ex)
            {
                throw new InvalidDestinationPhoneException();
            }
            SubscribtionType sourceMsisdnType;
            try
            {
                sourceMsisdnType = GetAccountType(nobillSubscriptionType);
            }
            catch (SubscriptionNotFoundException)
            {
                throw new SourcePhoneNumberNotFoundException();
            }

            //DATA sim is not allowed to transfer money 
            if (sourceMsisdnType == SubscribtionType.DataAccount)
            {
                throw new NotAllowedToTransferCreditToTheDestinationAccountException();
            }

            SubscribtionType destinationMsisdnType;

            try
            {
                destinationMsisdnType = GetAccountType(destinationNobillSubscriptionType);
            }
            catch (SubscriptionNotFoundException)
            {
                throw new DestinationPhoneNumberNotFoundException();
            }

            //this is to prevent the customer from transfering amount to dealer or distributer
            if ((sourceMsisdnType == SubscribtionType.Customer || sourceMsisdnType == SubscribtionType.HalafoniCustomer) && (destinationMsisdnType == SubscribtionType.Pos ||
                destinationMsisdnType == SubscribtionType.Distributor))
            {
                throw new NotAllowedToTransferCreditToTheDestinationAccountException();
            }

            if ((sourceMsisdnType == SubscribtionType.HalafoniCustomer && destinationMsisdnType == SubscribtionType.Customer) || (sourceMsisdnType == SubscribtionType.Customer && destinationMsisdnType == SubscribtionType.HalafoniCustomer))
            {
                throw new NotAllowedToTransferCreditToTheDestinationAccountException();
            }

            if (sourceMsisdnType == SubscribtionType.Pos && destinationMsisdnType == SubscribtionType.HalafoniCustomer)
            {
                throw new NotAllowedToTransferCreditToTheDestinationAccountException();
            }


            decimal SourceMsisdnBalance = TransactionManager.GetAccountBalance(sourceMsisdn);
            if (MaximumPercentageAmount != 1)
            {
                if (SourceMsisdnBalance - amount < (SourceMsisdnBalance / MaximumPercentageAmount))
                {
                    throw new RemainingBalanceShouldBeGreaterThanHalfBalance();
                }
            }



            if (GetSubscriptionBlockStatus(sourceMsisdn) != SubscriptionBlockStatus.NO_BLOCK)
            {
                throw new MiscellaneousErrorException();
            }

            if (GetSubscriptionStatus(destinationMsisdn) == SubscriptionStatus.ACTIVE_BEFORE_FIRST_USE)
            {
                throw new InvalidDestinationPhoneException();
            }
            //#################- New changes ################//
            //Get Subscription type configurations

            TransferConfig subscriptionTypeConfig = TransactionManager.GetSubscriptionTypeConfigurations(nobillSubscriptionType);

            //Check if source reach the maximum number of transactions per day


            if (subscriptionTypeConfig == null)
            {
                throw new SubscriptionNotFoundException();
            }




            decimal maxTransferAmount = GetAccountMaxTransferAmountByServiceName(sourceMsisdn, subscriptionTypeConfig.CreditTransferCustomerService);

            if (amount > maxTransferAmount)
            {
                throw new TransferAmountAboveMaxException();
            }

            if (subscriptionTypeConfig.MinTransferAmount.HasValue && amount < subscriptionTypeConfig.MinTransferAmount.Value)
            {
                throw new TransferAmountBelowMinException();
            }

            int numberOfTransactions;

            TransactionManager.GetDailyTransferCount(sourceMsisdn, out numberOfTransactions);

            //check DailyTransferCountLimit
            if (numberOfTransactions != 0 && subscriptionTypeConfig.DailyTransferCountLimit.HasValue && subscriptionTypeConfig.DailyTransferCountLimit.Value <= numberOfTransactions)
            {
                throw new ExceedsMaxPerDayTransactionsException();
            }

            //Check the balance after transfer credit action  for customers only.

            if (subscriptionTypeConfig.MinPostTransferBalance.HasValue && (SourceMsisdnBalance - amount < subscriptionTypeConfig.MinPostTransferBalance.Value))
            {
                throw new RemainingBalanceException();
            }

            #endregion



        }

        public static void TransferCreditWithoutPin(string sourceMsisdn, string destinationMsisdn, decimal amount,  string userName, ref string recievedRequest)
        {
            #region Parameters Validations
            string pin = DefaultPIN;
            ValidateTransferInputs(sourceMsisdn, destinationMsisdn, amount);
            string nobillSubscriptionType = GetNobillSubscriptionType(sourceMsisdn);
            string destinationNobillSubscriptionType = GetNobillSubscriptionType(destinationMsisdn);

            SubscribtionType sourceMsisdnType= GetAccountType(nobillSubscriptionType);
            SubscribtionType destinationMsisdnType= GetAccountType(destinationNobillSubscriptionType);
            TransferConfig subscriptionTypeConfig = TransactionManager.GetSubscriptionTypeConfigurations(nobillSubscriptionType);


            //check the pin and the maximum amount for transfer credit through the customer service.

            //if (pin != DefaultPIN)
            //{
            //    string accountPin = GetAccountPinByServiceName(sourceMsisdn, subscriptionTypeConfig.CreditTransferCustomerService);

            //    if (!string.IsNullOrEmpty(accountPin))
            //    {
            //        if (accountPin != pin)
            //        {
            //            throw new PinMismatchException();
            //        }
            //    }
            //}

            #endregion

            int reservationCode = -1;
            bool isEventReserved = false;
            bool isAmountTransfered = false;
            int extendedDays = EnableExtendedDays && (destinationMsisdnType == SubscribtionType.Customer) ? GetDaysToExtend(amount) : 0;

            Transaction transaction = new Transaction();
            transaction.SourceMsisdn = sourceMsisdn;
            transaction.DestinationMsisdn = destinationMsisdn;
            transaction.Amount = amount;
            transaction.PIN = pin;
            transaction.ExtensionDays = extendedDays;
            transaction.IsFromCustomer = false;
            transaction.CreatedBy = userName == null ? "Debug" : userName;

            try
            {
                //transfer fund from A to B
                string transferReason = CustomerToCustomerTransferMonyReason;
                bool isOldtoNew = false;
                bool bothOnSameIN = CheckBothOnSameIN(sourceMsisdn, destinationMsisdn, out isOldtoNew);

                if (!bothOnSameIN)
                {
                    if (isOldtoNew)
                    {
                        transferReason = CustomerToCustomerAdjustmentReasonOldToNew;
                    }
                    else
                    {
                        transferReason = CustomerToCustomerAdjustmentReasonNewToOld;
                    }
                }

                if (sourceMsisdnType == SubscribtionType.Customer)
                {
                    transaction.ExtensionDays = 0;
                    transaction.IsFromCustomer = true;

                    //int transferFundEventId = TransferFundEventId;
                    int transferFundEventId = subscriptionTypeConfig.TransferFeesEventId.Value;
                    ReserveEvent(sourceMsisdn, destinationMsisdn, amount, pin, extendedDays, transferFundEventId, out reservationCode, userName);
                    isEventReserved = true;
                }
                else if (destinationMsisdnType == SubscribtionType.Customer || destinationMsisdnType == SubscribtionType.DataAccount)
                {
                    if (!bothOnSameIN)
                    {
                        if (isOldtoNew)
                        {
                            transferReason = GetRelatedAdjustmentReasonOldToNew(amount);
                        }
                        else
                        {
                            transferReason = GetRelatedAdjustmentReasonNewToOld(amount);
                        }
                    }
                    else
                    {
                        transferReason = GetRelatedTransferReason(amount);
                    }
                }

                recievedRequest += "Transfer Reason:" + transferReason;

                transaction.ReservationId = reservationCode;

                TransferFund(sourceMsisdn, destinationMsisdn, amount, pin, userName, transferReason, !bothOnSameIN);
                isAmountTransfered = true;

                if (transaction.IsFromCustomer)
                {
                    CommitEvent(sourceMsisdn, destinationMsisdn, amount, pin, extendedDays, reservationCode, null, userName);
                }

                // Extend Account expiry

                if (EnableExtendedDays)
                {
                    if (sourceMsisdnType != SubscribtionType.Customer && (destinationMsisdnType == SubscribtionType.Customer))
                    {
                        ExtendDays(sourceMsisdn, destinationMsisdn, amount, pin, extendedDays, null, userName);
                    }
                }

                transaction.IsEventReserved = isEventReserved;
                transaction.IsAmountTransfered = true;
                transaction.IsEventCharged = isEventReserved;
                transaction.IsEventCancelled = false;
                transaction.IsExpiryExtended = true;
                transaction.StatusId = (byte)TransactionStatus.Succeeded;

                TransactionManager.AddTransaction(transaction);

                try
                {
                    SendSMS("FRiENDi", sourceMsisdn, string.Format(APartySMSEn, amount, destinationMsisdn), string.Format(APartySMSAr, amount, destinationMsisdn));
                    SendSMS("FRiENDi", destinationMsisdn, string.Format(BPartySMSEn, amount, sourceMsisdn), string.Format(BPartySMSAr, amount, sourceMsisdn));
                }
                catch (Exception exp)
                {

                }

            }
            catch (Exception)
            {
                if (!isAmountTransfered)
                {
                    if (isEventReserved)
                    {
                        CancelEvent(sourceMsisdn, destinationMsisdn, amount, pin, extendedDays, reservationCode, userName);
                        transaction.IsFromCustomer = true;
                        transaction.IsEventReserved = true;
                        transaction.IsAmountTransfered = false;
                        transaction.IsEventCharged = false;
                        transaction.IsEventCancelled = true;
                        transaction.IsExpiryExtended = false;
                        transaction.StatusId = (byte)TransactionStatus.TransferFailed;
                        TransactionManager.AddTransaction(transaction);
                    }
                    else if (sourceMsisdnType != SubscribtionType.Customer)
                    {
                        transaction.IsFromCustomer = false;
                        transaction.IsEventReserved = false;
                        transaction.IsAmountTransfered = false;
                        transaction.IsEventCharged = false;
                        transaction.IsEventCancelled = false;
                        transaction.IsExpiryExtended = false;
                        transaction.StatusId = (byte)TransactionStatus.TransferFailed;
                        TransactionManager.AddTransaction(transaction);
                    }
                }

                throw;
            }



        }

    }
}
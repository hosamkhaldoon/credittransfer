using System;
using System.Collections.Generic;
using System.Linq;
using CreditTransferEngine.DataAccess;
using System.Configuration;
using CreditTransferEngine.Utils;

namespace CreditTransferEngine.BusinessLogic
{
    public class TransactionManager
    {
        public static List<Transaction> GetAllTransactionProcessing()
        {
            using (CreditTransferEntities context = new CreditTransferEntities())
            {
                return context.Transactions.Where(x => x.StatusId == (byte)Utils.TransactionStatus.CommitFailed || x.StatusId == (byte)Utils.TransactionStatus.ExtensionFailed).ToList();
            }
        }

        public static long AddTransaction(Transaction transaction)
        {
            using (CreditTransferEntities context = new CreditTransferEntities())
            {
                transaction.CreationDate = DateTime.Now;
                transaction.NumberOfRetries = 0;
                context.AddToTransactions(transaction);
                context.SaveChanges();

                return transaction.Id;
            }
        }

        public static void UpdateTransaction(Transaction transaction)
        {
            using (CreditTransferEntities context = new CreditTransferEntities())
            {
                Transaction _transaction = (from result in context.Transactions
                                            where result.Id == transaction.Id
                                            select result).First();

                _transaction.SourceMsisdn = transaction.SourceMsisdn;
                _transaction.DestinationMsisdn = transaction.DestinationMsisdn;
                _transaction.Amount = transaction.Amount;
                _transaction.PIN = transaction.PIN;
                _transaction.IsFromCustomer = transaction.IsFromCustomer;
                _transaction.IsEventReserved = transaction.IsEventReserved;
                _transaction.IsAmountTransfered = transaction.IsAmountTransfered;
                _transaction.IsEventCharged = transaction.IsEventCharged;
                _transaction.IsEventCancelled = transaction.IsEventCancelled;
                _transaction.IsExpiryExtended = transaction.IsExpiryExtended;
                _transaction.ExtensionDays = transaction.ExtensionDays;
                _transaction.ReservationId = transaction.ReservationId;
                _transaction.StatusId = transaction.StatusId;
                _transaction.CreationDate = transaction.CreationDate;
                _transaction.CreatedBy = transaction.CreatedBy;
                _transaction.ModifiedBy = transaction.ModifiedBy;
                _transaction.ModificationDate = DateTime.Now;
                _transaction.NumberOfRetries = transaction.NumberOfRetries;

                context.SaveChanges();
            }
        }

        public static void ProcessTransactions()
        {
            List<Transaction> transactionsList = GetAllTransactionProcessing();

            foreach (Transaction transaction in transactionsList)
            {
                transaction.ModifiedBy = Utils.Constants.CreditTransferEngineUserName;
                transaction.ModificationDate = DateTime.Now;

                Utils.TransactionStatus transactionStatus = (Utils.TransactionStatus)transaction.StatusId;

                switch (transactionStatus)
                {
                    case Utils.TransactionStatus.CommitFailed:
                        try
                        {
                            CreditTransfer.CommitEvent(transaction.SourceMsisdn, transaction.DestinationMsisdn, transaction.Amount, transaction.PIN, transaction.ExtensionDays, transaction.ReservationId, transaction.Id, string.Empty);
                            transaction.IsEventCharged = true;

                            if (ConfigurationManager.AppSettings["EnableExtendedDays"] == "true")
                            {
                                if (!transaction.IsFromCustomer && CreditTransfer.GetAccountType(transaction.DestinationMsisdn) == Utils.SubscribtionType.Customer)
                                {
                                    CreditTransfer.ExtendDays(transaction.SourceMsisdn, transaction.DestinationMsisdn, transaction.Amount, transaction.PIN, transaction.ExtensionDays, transaction.Id, string.Empty);
                                    transaction.IsExpiryExtended = true;
                                }
                            }

                            transaction.StatusId = (byte)Utils.TransactionStatus.Succeeded;
                            UpdateTransaction(transaction);
                        }
                        catch (Utils.ExpiredReservationCodeException)
                        {
                            transaction.NumberOfRetries++;
                            transaction.StatusId = (byte)Utils.TransactionStatus.CommitFailedDueToAutoCancel;
                            UpdateTransaction(transaction);

                            throw;
                        }
                        catch (Exception)
                        {
                            if (!transaction.IsEventCharged)
                            {
                                transaction.NumberOfRetries++;
                                UpdateTransaction(transaction);
                            }
                            else
                            {
                                //reset the number of retries due to new status
                                transaction.NumberOfRetries = 0;
                                transaction.StatusId = (byte)Utils.TransactionStatus.ExtensionFailed;
                                UpdateTransaction(transaction);
                            }

                            throw;
                        }
                        break;
                    case Utils.TransactionStatus.ExtensionFailed:
                        try
                        {
                            CreditTransfer.ExtendDays(transaction.SourceMsisdn, transaction.DestinationMsisdn, transaction.Amount, transaction.PIN, transaction.ExtensionDays, transaction.Id, string.Empty);
                            transaction.IsExpiryExtended = true;
                            transaction.StatusId = (byte)Utils.TransactionStatus.Succeeded;
                            UpdateTransaction(transaction);
                        }
                        catch (Exception)
                        {
                            transaction.NumberOfRetries++;
                            if (transaction.NumberOfRetries >= Convert.ToInt32(ConfigurationManager.AppSettings["MaxNumberOfRetries"]))
                            {
                                transaction.StatusId = (byte)Utils.TransactionStatus.ExtensionFailedAfterRetries;
                            }
                            UpdateTransaction(transaction);

                            throw;
                        }
                        break;
                }
            }
        }

        public static void GetDailyTransferCount(string sourceMsisdn /*, out decimal totalTransferedAmount*/, out int numberOfTransactions)
        {
            //totalTransferedAmount = 0;
            numberOfTransactions = 0;

            DateTime startDate, endDate;
            startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
            endDate = startDate.AddDays(1).AddMilliseconds(-1);

            using (CreditTransferEntities context = new CreditTransferEntities())
            {
                var transactions = context.Transactions.Where(x => x.SourceMsisdn == sourceMsisdn && x.CreationDate >= startDate && x.CreationDate <= endDate && x.StatusId == (int)TransactionStatus.Succeeded);
                if (transactions.Count() != 0)
                {
                    numberOfTransactions = transactions.Count();
                    //totalTransferedAmount = transactions.Sum(x => x.Amount);
                }
            }
        }

        public static decimal GetAccountBalance(string msisdn)
        {
            int responseCode;
            NobillCalls.AccountData accountData;

            using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
            {
                nobillCalls.Credentials = Common.GetServiceCredentials();

                responseCode = nobillCalls.GetAccountData(msisdn, out accountData);
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

            return Convert.ToDecimal(accountData.Balance);
        }

        public static decimal GetAccountBalanceForPostpaid(string msisdn, string counterName)
        {
            int responseCode;
            NobillCalls.Counter[] counters;
            decimal balance = 0;
            using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
            {
                nobillCalls.Credentials = Common.GetServiceCredentials();

                responseCode = nobillCalls.GetAccountCounters(msisdn, out counters);
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

            for (int i = 0; i < counters.Length; i++)
            {
                if (counters[i].Name.Trim() == counterName.Trim())
                {
                    balance = Convert.ToDecimal(counters[i].CounterValue);
                }

            }
            return balance;
        }

        public static DataAccess.TransferConfig GetSubscriptionTypeConfigurations(string nobillSubscriptionType)
        {
            using (CreditTransferEntities context = new CreditTransferEntities())
            {
                return context.TransferConfigs.First(p => p.NobillSubscritpionType == nobillSubscriptionType);
            }
        }

        public static int ChargeEventWithCreditAbility(string msisdn, int eventId, out string chargeReference)
        {
            int responseCode;

            using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
            {
                nobillCalls.Credentials = Common.GetServiceCredentials();

                responseCode = nobillCalls.ChargeEventWithCreditAbility(msisdn, eventId, out chargeReference);

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
            return responseCode;
        }

        public static int CreditEvent(string msisdn, string chargeReferenceNo, int eventId)
        {
            int responseCode;

            using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
            {
                nobillCalls.Credentials = Common.GetServiceCredentials();

                responseCode = nobillCalls.CreditEvent(msisdn, chargeReferenceNo, eventId);
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
            return responseCode;
        }

        public static int SendSMS(string sourceMsisdn, string destinationMsisdn, string content, bool isArabic = false)
        {
            int responseCode = 0;
            using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
            {
                nobillCalls.Credentials = Common.GetServiceCredentials();

                responseCode = nobillCalls.SendHTTPSMS(sourceMsisdn, destinationMsisdn, content, isArabic);
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
            return responseCode;
        }

     

        public static int GetCustomerLanguage(string msisdn, out Language language)
        {
            int responseCode = 0;
            NobillCalls.SubscribtionData subscribtionData = null;

            using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
            {
                nobillCalls.Credentials = Common.GetServiceCredentials();

                responseCode = nobillCalls.GetSubscribtionData(msisdn, out subscribtionData);
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

            if (subscribtionData.locale != "en_GB")
            {
                language = Language.ARABIC;
            }
            else
            {
                language = Language.ENGLISH;
            }
            return responseCode;
        }

        public static int AdjustAccountBalance(string msisdn, decimal amount, string message, out string transactionNo)
        {

            int responseCode = 0;
            using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
            {
                nobillCalls.Credentials = Common.GetServiceCredentials();
                transactionNo = Guid.NewGuid().ToString();
                message = message + ", " + transactionNo;
                responseCode = nobillCalls.AdjustAccountBalance(msisdn, amount, message);
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
            return responseCode;
        }

        public static int AdjustAccountBalanceByReason(string msisdn, decimal amount, string adjustmentReason, string adjustmentType, string message, out string transactionNo)
        {

            int responseCode = 0;
            using (NobillCalls.NobillCalls nobillCalls = new NobillCalls.NobillCalls())
            {
                nobillCalls.Credentials = Common.GetServiceCredentials();
                transactionNo = Guid.NewGuid().ToString();
                message = message + ", " + transactionNo;
                responseCode = nobillCalls.AdjustAccountByReason(msisdn, amount, message, adjustmentReason, adjustmentType,string.Format("Adjustment {0} msisn {1}", amount,msisdn));
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
            return responseCode;
        }

   
        public static Message GetMessageText(string messageKey)
        {
            using (CreditTransferEntities context = new CreditTransferEntities())
            {
                return context.Messages.FirstOrDefault(p => p.Key == messageKey);
            }
        }

        public static string GetAccountType(string nobillSubscriptionType)
        {
            using (CreditTransferEntities context = new CreditTransferEntities())
            {
                return context.TransferConfigs.FirstOrDefault(p => p.NobillSubscritpionType == nobillSubscriptionType) == null ? string.Empty :
                    context.TransferConfigs.FirstOrDefault(p => p.NobillSubscritpionType == nobillSubscriptionType).SubscriptionType;
            }
        }
    }
}

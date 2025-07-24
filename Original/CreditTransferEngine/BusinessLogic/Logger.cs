using System;
using CreditTransferEngine.DataAccess;

namespace CreditTransferEngine.BusinessLogic
{
    public class Logger
    {
        public static int LogAction(string receivedRequest, string methodName, string requestedBy, int statusCode, string statusText, DateTime requestedDate)
        {
            using (CreditTransferEntities context = new CreditTransferEntities())
            {
                Log log = new Log
                              {
                                  ReceivedRequest = receivedRequest,
                                  MethodName = methodName,
                                  RequestedBy = requestedBy,
                                  Status = statusCode,
                                  StatusText = statusText,
                                  RequestDate = requestedDate
                              };
                context.AddToLogs(log);
                context.SaveChanges();

                return log.LogId;
            }
        }

        public static int LogError(Exception exception, int logId)
        {
            using (CreditTransferEntities context = new CreditTransferEntities())
            {
                ErrorLog errorLog = new ErrorLog
                                        {
                                            Exception = exception.Message,
                                            StackTrace = exception.StackTrace,
                                            LogId = logId
                                        };
                context.AddToErrorLogs(errorLog);
                context.SaveChanges();

                return errorLog.ErrorLogId;
            }
        }
    }
}
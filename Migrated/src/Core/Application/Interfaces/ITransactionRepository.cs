using CreditTransfer.Core.Domain.Entities;

namespace CreditTransfer.Core.Application.Interfaces
{
    /// <summary>
    /// Repository interface for transaction-related data operations
    /// Abstracts the transaction management layer for testability and flexibility
    /// </summary>
    public interface ITransactionRepository
    {


        /// <summary>
        /// Gets the daily transfer count for a given MSISDN
        /// </summary>
        Task<int> GetDailyTransferCountAsync(string msisdn);

        /// <summary>
        /// Adds a new transaction record
        /// </summary>
        Task<long> AddTransactionAsync(Transaction transaction);

        /// <summary>
        /// Updates an existing transaction
        /// </summary>
        Task UpdateTransactionAsync(Transaction transaction);

        Task<decimal> GetDailyTransferAmount(string sourceMsisdn);
    }
}
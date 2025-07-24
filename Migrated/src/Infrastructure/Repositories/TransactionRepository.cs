using CreditTransfer.Core.Application.Interfaces;
using CreditTransfer.Core.Domain.Entities;
using CreditTransfer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CreditTransfer.Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for transaction-related data operations
    /// Uses Entity Framework with CreditTransferDbContext for data access
    /// Enhanced with OpenTelemetry instrumentation for database operation tracking
    /// </summary>
    public class TransactionRepository : ITransactionRepository
    {
        private readonly CreditTransferDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TransactionRepository> _logger;
        private readonly ActivitySource _activitySource;

        public TransactionRepository(
            CreditTransferDbContext context,
            IConfiguration configuration, 
            ILogger<TransactionRepository> logger, 
            ActivitySource activitySource)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _activitySource = activitySource;
        }

        /// <summary>
        /// Gets the daily transfer count for a given MSISDN
        /// Uses Entity Framework query instead of stored procedure
        /// Enhanced with OpenTelemetry activity tracking
        /// </summary>
        public async Task<int> GetDailyTransferCountAsync(string msisdn)
        {
            using var activity = _activitySource.StartActivity("TransactionRepository.GetDailyTransferCount");
            activity?.SetTag("operation", "GetDailyTransferCount");
            activity?.SetTag("repository", "TransactionRepository");
            activity?.SetTag("database.operation", "Query");
            activity?.SetTag("msisdn", msisdn);
            activity?.SetTag("date", DateTime.Today.ToString("yyyy-MM-dd"));
            
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                
                var count = await _context.Transactions
                    .Where(t => t.SourceMsisdn == msisdn && 
                               t.CreatedDate >= today && 
                               t.CreatedDate < tomorrow &&
                               t.StatusId == (int)TransactionStatus.Succeeded)
                    .CountAsync();
                
                activity?.SetTag("count.found", count > 0);
                activity?.SetTag("count.value", count);
                activity?.SetTag("result.success", true);
                
                _logger.LogDebug("Daily transfer count for MSISDN {MSISDN}: {Count}", msisdn, count);
                return count;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Failed to get daily transfer count");
                activity?.SetTag("error.type", ex.GetType().Name);
                activity?.SetTag("default.to", 0);
                _logger.LogError(ex, "Error getting daily transfer count for MSISDN: {MSISDN}", msisdn);
                return 0; // Default to 0 on error
            }
        }

      
        /// <summary>
        /// Adds a new transaction record using Entity Framework
        /// Enhanced with OpenTelemetry activity tracking
        /// </summary>
        public async Task<long> AddTransactionAsync(Transaction transaction)
        {
            using var activity = _activitySource.StartActivity("TransactionRepository.AddTransaction");
            activity?.SetTag("operation", "AddTransaction");
            activity?.SetTag("repository", "TransactionRepository");
            activity?.SetTag("database.operation", "Insert");
            activity?.SetTag("transaction.source.msisdn", transaction.SourceMsisdn);
            activity?.SetTag("transaction.destination.msisdn", transaction.DestinationMsisdn);
            activity?.SetTag("transaction.amount", transaction.Amount);
            activity?.SetTag("transaction.created_by", transaction.CreatedBy);
            activity?.SetTag("transaction.is_from_customer", transaction.IsFromCustomer);
            
            try
            {
                // Ensure CreatedDate is set if not provided
                if (transaction.CreatedDate == default)
                {
                    transaction.CreatedDate = DateTime.UtcNow;
                }
                
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();
                
                activity?.SetTag("transaction.id", transaction.Id);
                activity?.SetTag("result.success", true);
                
                _logger.LogInformation("Added transaction with ID: {TransactionId} for source: {SourceMsisdn}, destination: {DestinationMsisdn}", 
                    transaction.Id, transaction.SourceMsisdn, transaction.DestinationMsisdn);
                
                return transaction.Id;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Failed to add transaction");
                activity?.SetTag("error.type", ex.GetType().Name);
                _logger.LogError(ex, "Error adding transaction for source: {SourceMsisdn}, destination: {DestinationMsisdn}", 
                    transaction.SourceMsisdn, transaction.DestinationMsisdn);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing transaction using Entity Framework
        /// </summary>
        public async Task UpdateTransactionAsync(Transaction transaction)
        {
            using var activity = _activitySource.StartActivity("TransactionRepository.UpdateTransaction");
            activity?.SetTag("operation", "UpdateTransaction");
            activity?.SetTag("transaction.id", transaction.Id);
            
            try
            {
                var existingTransaction = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.Id == transaction.Id);
                
                if (existingTransaction == null)
                {
                    throw new InvalidOperationException($"Transaction with ID {transaction.Id} not found");
                }
                
                // Update only the fields that should be updated
                existingTransaction.IsAmountTransfered = transaction.IsAmountTransfered;
                existingTransaction.IsEventCharged = transaction.IsEventCharged;
                existingTransaction.IsEventCancelled = transaction.IsEventCancelled;
                existingTransaction.IsExpiryExtended = transaction.IsExpiryExtended;
                existingTransaction.StatusId = transaction.StatusId;
                existingTransaction.CompletedDate = transaction.CompletedDate;
                existingTransaction.ErrorMessage = transaction.ErrorMessage;
                existingTransaction.TransactionReference = transaction.TransactionReference;
                
                await _context.SaveChangesAsync();
                
                activity?.SetTag("result.success", true);
                _logger.LogInformation("Updated transaction with ID: {TransactionId}", transaction.Id);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Failed to update transaction");
                activity?.SetTag("error.type", ex.GetType().Name);
                _logger.LogError(ex, "Error updating transaction with ID: {TransactionId}", transaction.Id);
                throw;
            }
        }

        /// <summary>
        /// Gets a transaction by ID
        /// </summary>
        public async Task<Transaction?> GetTransactionByIdAsync(long id)
        {
            using var activity = _activitySource.StartActivity("TransactionRepository.GetTransactionById");
            activity?.SetTag("operation", "GetTransactionById");
            activity?.SetTag("transaction.id", id);
            
            try
            {
                var transaction = await _context.Transactions
                    .FirstOrDefaultAsync(t => t.Id == id);
                
                activity?.SetTag("transaction.found", transaction != null);
                activity?.SetTag("result.success", true);
                
                return transaction;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Failed to get transaction by ID");
                activity?.SetTag("error.type", ex.GetType().Name);
                _logger.LogError(ex, "Error getting transaction by ID: {TransactionId}", id);
                return null;
            }
        }

        /// <summary>
        /// Gets transactions by source MSISDN within a date range
        /// </summary>
        public async Task<decimal> GetDailyTransferAmount(string sourceMsisdn)
        {
            using var activity = _activitySource.StartActivity("TransactionRepository.GetTransactionsBySourceMsisdn");
            activity?.SetTag("operation", "GetTransactionsBySourceMsisdn");
            activity?.SetTag("source.msisdn", sourceMsisdn);
            
            try
            {
                decimal totalTransferedAmount = 0;

                DateTime startDate, endDate;
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
                endDate = startDate.AddDays(1).AddMilliseconds(-1);


                totalTransferedAmount = await _context.Transactions.Where(x => x.SourceMsisdn == sourceMsisdn && x.CreationDate >= startDate && x.CreationDate <= endDate && x.StatusId == (int)TransactionStatus.Succeeded).SumAsync(x => x.Amount);


                activity?.SetTag("totalTransferedAmount", totalTransferedAmount);
                
                return totalTransferedAmount;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Failed to get transactions by source MSISDN");
                activity?.SetTag("error.type", ex.GetType().Name);
                _logger.LogError(ex, "Error getting transactions for source MSISDN: {SourceMsisdn}", sourceMsisdn);
                return 0;
            }
        }
    }
} 
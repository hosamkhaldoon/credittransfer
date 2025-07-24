using CreditTransfer.Core.Domain.Entities;

namespace CreditTransfer.Core.Application.Interfaces
{
    /// <summary>
    /// Repository interface for message template management with Redis caching
    /// Supports bilingual SMS templates and system messages
    /// </summary>
    public interface IMessageRepository
    {
        /// <summary>
        /// Gets a message by key with caching support
        /// </summary>
        /// <param name="key">Message key</param>
        /// <returns>Message entity or null if not found</returns>
        Task<Message?> GetMessageByKeyAsync(string key);

        /// <summary>
        /// Gets English text for a message key with caching
        /// </summary>
        /// <param name="key">Message key</param>
        /// <param name="defaultValue">Default value if key not found</param>
        /// <returns>English text or default</returns>
        Task<string> GetEnglishTextAsync(string key, string? defaultValue = null);

        /// <summary>
        /// Gets Arabic text for a message key with caching
        /// </summary>
        /// <param name="key">Message key</param>
        /// <param name="defaultValue">Default value if key not found</param>
        /// <returns>Arabic text or default</returns>
        Task<string> GetArabicTextAsync(string key, string? defaultValue = null);

        /// <summary>
        /// Gets formatted English message with parameters
        /// </summary>
        /// <param name="key">Message key</param>
        /// <param name="parameters">Format parameters</param>
        /// <returns>Formatted English message</returns>
        Task<string> GetFormattedEnglishMessageAsync(string key, params object[] parameters);

        /// <summary>
        /// Gets formatted Arabic message with parameters
        /// </summary>
        /// <param name="key">Message key</param>
        /// <param name="parameters">Format parameters</param>
        /// <returns>Formatted Arabic message</returns>
        Task<string> GetFormattedArabicMessageAsync(string key, params object[] parameters);

        /// <summary>
        /// Gets all active messages with caching
        /// </summary>
        /// <returns>List of active messages</returns>
        Task<List<Message>> GetAllActiveMessagesAsync();

        /// <summary>
        /// Adds or updates a message template
        /// </summary>
        /// <param name="message">Message to add or update</param>
        /// <returns>Updated message with ID</returns>
        Task<Message> UpsertMessageAsync(Message message);

        /// <summary>
        /// Deletes a message by key
        /// </summary>
        /// <param name="key">Message key to delete</param>
        /// <returns>True if deleted, false if not found</returns>
        Task<bool> DeleteMessageAsync(string key);

        /// <summary>
        /// Refreshes the cache for all messages
        /// </summary>
        Task RefreshCacheAsync();

        /// <summary>
        /// Clears the cache for a specific message key
        /// </summary>
        /// <param name="key">Message key to clear from cache</param>
        Task ClearCacheAsync(string key);

        /// <summary>
        /// Bulk insert or update message templates
        /// </summary>
        /// <param name="messages">List of messages to upsert</param>
        Task BulkUpsertMessagesAsync(List<Message> messages);
    }
} 
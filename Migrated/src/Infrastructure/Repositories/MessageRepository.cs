using System.Text.Json;
using CreditTransfer.Core.Application.Interfaces;
using CreditTransfer.Core.Domain.Entities;
using CreditTransfer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CreditTransfer.Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for message template management with Redis caching
    /// Handles bilingual SMS templates and system messages
    /// </summary>
    public class MessageRepository : IMessageRepository
    {
        private readonly CreditTransferDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<MessageRepository> _logger;
        private const string CACHE_KEY_PREFIX = "Message:";
        private const string CACHE_KEY_ALL = "Message:All";
        private const int CACHE_EXPIRY_MINUTES = 43200; // Messages change infrequent

        public MessageRepository(
            CreditTransferDbContext context,
            IDistributedCache cache,
            ILogger<MessageRepository> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Gets a message by key with caching support
        /// </summary>
        public async Task<Message?> GetMessageByKeyAsync(string key)
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}{key}";
                
                // Try to get from cache first
                var cachedValue = await _cache.GetStringAsync(cacheKey);
                if (cachedValue != null)
                {
                    var cachedMessage = JsonSerializer.Deserialize<Message>(cachedValue);
                    if (cachedMessage != null)
                    {
                        _logger.LogDebug("Retrieved message '{Key}' from cache", key);
                        return cachedMessage;
                    }
                }

                // Get from database
                var message = await _context.Messages
                    .Where(m => m.Key == key && m.IsActive)
                    .FirstOrDefaultAsync();

                // Cache the result
                if (message != null)
                {
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES)
                    };
                    var serializedMessage = JsonSerializer.Serialize(message);
                    await _cache.SetStringAsync(cacheKey, serializedMessage, cacheOptions);
                    _logger.LogDebug("Cached message '{Key}'", key);
                }

                return message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving message '{Key}'", key);
                return null;
            }
        }

        /// <summary>
        /// Gets English text for a message key with caching
        /// </summary>
        public async Task<string> GetEnglishTextAsync(string key, string? defaultValue = null)
        {
            var message = await GetMessageByKeyAsync(key);
            return message?.TextEn ?? defaultValue ?? $"Message not found: {key}";
        }

        /// <summary>
        /// Gets Arabic text for a message key with caching
        /// </summary>
        public async Task<string> GetArabicTextAsync(string key, string? defaultValue = null)
        {
            var message = await GetMessageByKeyAsync(key);
            return message?.TextAr ?? defaultValue ?? $"Message not found: {key}";
        }

        /// <summary>
        /// Gets formatted English message with parameters
        /// </summary>
        public async Task<string> GetFormattedEnglishMessageAsync(string key, params object[] parameters)
        {
            try
            {
                var template = await GetEnglishTextAsync(key);
                return parameters.Length > 0 ? string.Format(template, parameters) : template;
            }
            catch (FormatException ex)
            {
                _logger.LogWarning(ex, "Format error for English message '{Key}' with {Count} parameters", key, parameters.Length);
                return await GetEnglishTextAsync(key);
            }
        }

        /// <summary>
        /// Gets formatted Arabic message with parameters
        /// </summary>
        public async Task<string> GetFormattedArabicMessageAsync(string key, params object[] parameters)
        {
            try
            {
                var template = await GetArabicTextAsync(key);
                return parameters.Length > 0 ? string.Format(template, parameters) : template;
            }
            catch (FormatException ex)
            {
                _logger.LogWarning(ex, "Format error for Arabic message '{Key}' with {Count} parameters", key, parameters.Length);
                return await GetArabicTextAsync(key);
            }
        }

        /// <summary>
        /// Gets all active messages with caching
        /// </summary>
        public async Task<List<Message>> GetAllActiveMessagesAsync()
        {
            try
            {
                // Try to get from cache first
                var cachedValue = await _cache.GetStringAsync(CACHE_KEY_ALL);
                if (cachedValue != null)
                {
                    var cachedMessages = JsonSerializer.Deserialize<List<Message>>(cachedValue);
                    if (cachedMessages != null)
                    {
                        _logger.LogDebug("Retrieved {Count} messages from cache", cachedMessages.Count);
                        return cachedMessages;
                    }
                }

                // Get from database
                var messages = await _context.Messages
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.Key)
                    .ToListAsync();

                // Cache the result
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES)
                };
                var serializedMessages = JsonSerializer.Serialize(messages);
                await _cache.SetStringAsync(CACHE_KEY_ALL, serializedMessages, cacheOptions);

                _logger.LogDebug("Retrieved and cached {Count} active messages", messages.Count);
                return messages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all active messages");
                return new List<Message>();
            }
        }

        /// <summary>
        /// Adds or updates a message template
        /// </summary>
        public async Task<Message> UpsertMessageAsync(Message message)
        {
            try
            {
                var existing = await _context.Messages
                    .FirstOrDefaultAsync(m => m.Key == message.Key);

                if (existing != null)
                {
                    // Update existing
                    existing.TextEn = message.TextEn;
                    existing.TextAr = message.TextAr;
                    existing.IsActive = message.IsActive;
                    existing.LastModified = DateTime.UtcNow;
                    
                    _context.Messages.Update(existing);
                    message = existing;
                }
                else
                {
                    // Add new
                    message.CreatedDate = DateTime.UtcNow;
                    _context.Messages.Add(message);
                }

                await _context.SaveChangesAsync();
                
                // Clear cache for this key and all messages
                await ClearCacheAsync(message.Key);
                await _cache.RemoveAsync(CACHE_KEY_ALL);
                
                _logger.LogInformation("Upserted message '{Key}'", message.Key);
                return message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting message '{Key}'", message.Key);
                throw;
            }
        }

        /// <summary>
        /// Deletes a message by key
        /// </summary>
        public async Task<bool> DeleteMessageAsync(string key)
        {
            try
            {
                var message = await _context.Messages
                    .FirstOrDefaultAsync(m => m.Key == key);

                if (message == null)
                    return false;

                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();
                
                // Clear cache for this key and all messages
                await ClearCacheAsync(key);
                await _cache.RemoveAsync(CACHE_KEY_ALL);
                
                _logger.LogInformation("Deleted message '{Key}'", key);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message '{Key}'", key);
                throw;
            }
        }

        /// <summary>
        /// Refreshes the cache for all messages
        /// </summary>
        public async Task RefreshCacheAsync()
        {
            try
            {
                // Remove all cached messages
                var allMessages = await _context.Messages
                    .Where(m => m.IsActive)
                    .ToListAsync();

                foreach (var message in allMessages)
                {
                    await ClearCacheAsync(message.Key);
                }

                await _cache.RemoveAsync(CACHE_KEY_ALL);
                _logger.LogInformation("Refreshed cache for all messages");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing message cache");
                throw;
            }
        }

        /// <summary>
        /// Clears the cache for a specific message key
        /// </summary>
        public async Task ClearCacheAsync(string key)
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}{key}";
                await _cache.RemoveAsync(cacheKey);
                _logger.LogDebug("Cleared cache for message '{Key}'", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache for message '{Key}'", key);
            }
        }

        /// <summary>
        /// Bulk insert or update message templates
        /// </summary>
        public async Task BulkUpsertMessagesAsync(List<Message> messages)
        {
            try
            {
                foreach (var message in messages)
                {
                    var existing = await _context.Messages
                        .FirstOrDefaultAsync(m => m.Key == message.Key);

                    if (existing != null)
                    {
                        // Update existing
                        existing.TextEn = message.TextEn;
                        existing.TextAr = message.TextAr;
                        existing.IsActive = message.IsActive;
                        existing.LastModified = DateTime.UtcNow;
                        
                        _context.Messages.Update(existing);
                    }
                    else
                    {
                        // Add new
                        message.CreatedDate = DateTime.UtcNow;
                        _context.Messages.Add(message);
                    }
                }

                await _context.SaveChangesAsync();
                
                // Clear cache for all updated keys and all messages
                foreach (var message in messages)
                {
                    await ClearCacheAsync(message.Key);
                }
                await _cache.RemoveAsync(CACHE_KEY_ALL);
                
                _logger.LogInformation("Bulk upserted {Count} messages", messages.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk upserting messages");
                throw;
            }
        }
    }
} 
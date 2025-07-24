using System.Text.Json;
using CreditTransfer.Core.Application.Interfaces;
using CreditTransfer.Core.Domain.Enums;
using CreditTransfer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics;

namespace CreditTransfer.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for managing configurable credit transfer business rules
    /// Replaces hard-coded business logic with database-driven configuration
    /// Uses Entity Framework for optimal performance and maintainability
    /// Enhanced with OpenTelemetry instrumentation and Redis caching
    /// </summary>
    public class TransferRulesRepository : ITransferRulesService
    {
        private readonly CreditTransferDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ActivitySource _activitySource;
        
        // Cache configuration
        private const string CACHE_KEY_PREFIX = "TransferRule:";
        private const string CACHE_KEY_COUNTRY_PREFIX = "TransferRule:Country:";
        private const string CACHE_KEY_EVALUATION_PREFIX = "TransferRule:Eval:";
        private const int CACHE_EXPIRY_MINUTES = 43200; // Transfer rules change infrequently (30 days)

        public TransferRulesRepository(
            CreditTransferDbContext context,
            IDistributedCache cache,
            ActivitySource activitySource)
        {
            _context = context;
            _cache = cache;
            _activitySource = activitySource;
        }

        /// <summary>
        /// Evaluates transfer rules using Entity Framework with priority-based evaluation
        /// Enhanced with OpenTelemetry tracing and Redis caching
        /// </summary>
        public async Task<(bool isAllowed, int errorCode, string errorMessage)> EvaluateTransferRuleAsync(
            string country,
            SubscriptionType sourceSubscriptionType,
            SubscriptionType destinationSubscriptionType,
            Dictionary<string, string>? configurationValues = null)
        {
            using var activity = _activitySource.StartActivity("TransferRulesRepository.EvaluateTransferRule");
            activity?.SetTag("operation", "EvaluateTransferRule");
            activity?.SetTag("repository", "TransferRulesRepository");
            activity?.SetTag("country", country);
            activity?.SetTag("source.subscription.type", sourceSubscriptionType.ToString());
            activity?.SetTag("destination.subscription.type", destinationSubscriptionType.ToString());
            
            try
            {
                var sourceType = sourceSubscriptionType.ToString();
                var destType = destinationSubscriptionType.ToString();

                // Create cache key for this specific evaluation
                var evaluationCacheKey = $"{CACHE_KEY_EVALUATION_PREFIX}{country}:{sourceType}:{destType}";
                
                // Try to get cached evaluation result first
                var cachedResult = await _cache.GetStringAsync(evaluationCacheKey);
                if (cachedResult != null)
                {
                    var cachedEvaluation = JsonSerializer.Deserialize<EvaluationResult>(cachedResult);
                    if (cachedEvaluation != null)
                    {
                        activity?.SetTag("cache.hit", true);
                        activity?.SetTag("result.allowed", cachedEvaluation.IsAllowed);
                        activity?.SetTag("result.error.code", cachedEvaluation.ErrorCode);
                        return (cachedEvaluation.IsAllowed, cachedEvaluation.ErrorCode, cachedEvaluation.ErrorMessage);
                    }
                }

                activity?.SetTag("cache.hit", false);

                // First, check for wildcard rules (DataAccount -> *)
                var wildcardRule = await _context.TransferRules
                    .Where(r => r.Country == country && 
                               r.SourceSubscriptionType == sourceType && 
                               r.DestinationSubscriptionType == "*" && 
                               r.IsActive)
                    .OrderBy(r => r.Priority)
                    .FirstOrDefaultAsync();

                (bool isAllowed, int errorCode, string errorMessage) result;

                if (wildcardRule != null)
                {
                    var wildcardErrorMessage = wildcardRule.ErrorMessage ?? (wildcardRule.IsAllowed ? "Transfer allowed" : "Transfer not allowed");
                    result = (wildcardRule.IsAllowed, wildcardRule.ErrorCode ?? 0, wildcardErrorMessage);
                    activity?.SetTag("rule.type", "wildcard");
                    activity?.SetTag("rule.id", wildcardRule.Id);
                }
                else
                {
                    // Then check for specific source-destination rules
                    var specificRule = await _context.TransferRules
                        .Where(r => r.Country == country && 
                                   r.SourceSubscriptionType == sourceType && 
                                   r.DestinationSubscriptionType == destType && 
                                   r.IsActive)
                        .OrderBy(r => r.Priority)
                        .FirstOrDefaultAsync();

                    if (specificRule != null)
                    {
                        var specificErrorMessage = specificRule.ErrorMessage ?? (specificRule.IsAllowed ? "Transfer allowed" : "Transfer not allowed");
                        result = (specificRule.IsAllowed, specificRule.ErrorCode ?? 0, specificErrorMessage);
                        activity?.SetTag("rule.type", "specific");
                        activity?.SetTag("rule.id", specificRule.Id);
                    }
                    else
                    {
                        // Default allow if no rules found (for backward compatibility)
                        result = (true, 0, "Transfer allowed (no specific rule found)");
                        activity?.SetTag("rule.type", "default");
                    }
                }

                // Cache the evaluation result
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES)
                };
                var evaluationResult = new EvaluationResult
                {
                    IsAllowed = result.isAllowed,
                    ErrorCode = result.errorCode,
                    ErrorMessage = result.errorMessage
                };
                var serializedResult = JsonSerializer.Serialize(evaluationResult);
                await _cache.SetStringAsync(evaluationCacheKey, serializedResult, cacheOptions);

                activity?.SetTag("result.allowed", result.isAllowed);
                activity?.SetTag("result.error.code", result.errorCode);
                activity?.SetTag("result.success", true);
                
                return result;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("error.type", ex.GetType().Name);
                
                // Default to deny on error for safety
                return (false, 14, "Transfer denied due to rule evaluation error");
            }
        }

        /// <summary>
        /// Gets all active transfer rules for a specific country using Entity Framework with caching
        /// Enhanced with OpenTelemetry tracing and Redis caching
        /// </summary>
        public async Task<List<TransferRule>> GetActiveRulesAsync(string country)
        {
            using var activity = _activitySource.StartActivity("TransferRulesRepository.GetActiveRules");
            activity?.SetTag("operation", "GetActiveRules");
            activity?.SetTag("repository", "TransferRulesRepository");
            activity?.SetTag("country", country);
            
            try
            {
                var cacheKey = $"{CACHE_KEY_COUNTRY_PREFIX}{country}";
                
                // Try to get from cache first
                var cachedValue = await _cache.GetStringAsync(cacheKey);
                if (cachedValue != null)
                {
                    var cachedRules = JsonSerializer.Deserialize<List<TransferRule>>(cachedValue);
                    if (cachedRules != null)
                    {
                        activity?.SetTag("cache.hit", true);
                        activity?.SetTag("rules.count", cachedRules.Count);
                        return cachedRules;
                    }
                }

                activity?.SetTag("cache.hit", false);

                // Get from database
                var rules = await _context.TransferRules
                    .Where(r => r.Country == country && r.IsActive)
                    .OrderBy(r => r.Priority)
                    .ThenBy(r => r.SourceSubscriptionType)
                    .ThenBy(r => r.DestinationSubscriptionType)
                    .ToListAsync();

                // Cache the result
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES)
                };
                var serializedRules = JsonSerializer.Serialize(rules);
                await _cache.SetStringAsync(cacheKey, serializedRules, cacheOptions);

                activity?.SetTag("rules.count", rules.Count);
                activity?.SetTag("result.success", true);
                
                return rules;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("error.type", ex.GetType().Name);
                return new List<TransferRule>();
            }
        }

        /// <summary>
        /// Adds or updates a transfer rule using Entity Framework
        /// Enhanced with OpenTelemetry tracing and cache invalidation
        /// </summary>
        public async Task<bool> UpsertRuleAsync(TransferRule rule)
        {
            using var activity = _activitySource.StartActivity("TransferRulesRepository.UpsertRule");
            activity?.SetTag("operation", "UpsertRule");
            activity?.SetTag("repository", "TransferRulesRepository");
            activity?.SetTag("rule.id", rule.Id);
            activity?.SetTag("country", rule.Country);
            activity?.SetTag("source.subscription.type", rule.SourceSubscriptionType);
            activity?.SetTag("destination.subscription.type", rule.DestinationSubscriptionType);
            
            try
            {
                var existingRule = await _context.TransferRules
                    .FirstOrDefaultAsync(r => r.Id == rule.Id);

                bool isUpdate = existingRule != null;
                activity?.SetTag("is.update", isUpdate);

                if (existingRule != null)
                {
                    // Update existing rule
                    existingRule.Country = rule.Country;
                    existingRule.SourceSubscriptionType = rule.SourceSubscriptionType;
                    existingRule.DestinationSubscriptionType = rule.DestinationSubscriptionType;
                    existingRule.IsAllowed = rule.IsAllowed;
                    existingRule.ErrorCode = rule.ErrorCode;
                    existingRule.ErrorMessage = rule.ErrorMessage;
                    existingRule.RequiresConfiguration = rule.RequiresConfiguration;
                    existingRule.ConfigurationKey = rule.ConfigurationKey;
                    existingRule.ConfigurationValue = rule.ConfigurationValue;
                    existingRule.Priority = rule.Priority;
                    existingRule.Description = rule.Description;
                    existingRule.IsActive = rule.IsActive;
                    existingRule.ModifiedDate = DateTime.UtcNow;
                    existingRule.ModifiedBy = rule.ModifiedBy;

                    _context.TransferRules.Update(existingRule);
                }
                else
                {
                    // Add new rule
                    rule.CreatedDate = DateTime.UtcNow;
                    rule.ModifiedDate = DateTime.UtcNow;
                    _context.TransferRules.Add(rule);
                }

                var rowsAffected = await _context.SaveChangesAsync();
                bool success = rowsAffected > 0;

                if (success)
                {
                    // Clear related caches
                    await ClearCacheAsync(rule.Country, rule.SourceSubscriptionType, rule.DestinationSubscriptionType);
                }

                activity?.SetTag("rows.affected", rowsAffected);
                activity?.SetTag("result.success", success);
                
                return success;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("error.type", ex.GetType().Name);
                return false;
            }
        }

        /// <summary>
        /// Deactivates a transfer rule using Entity Framework
        /// Enhanced with OpenTelemetry tracing and cache invalidation
        /// </summary>
        public async Task<bool> DeactivateRuleAsync(int ruleId)
        {
            using var activity = _activitySource.StartActivity("TransferRulesRepository.DeactivateRule");
            activity?.SetTag("operation", "DeactivateRule");
            activity?.SetTag("repository", "TransferRulesRepository");
            activity?.SetTag("rule.id", ruleId);
            
            try
            {
                var rule = await _context.TransferRules.FindAsync(ruleId);
                if (rule != null)
                {
                    rule.IsActive = false;
                    rule.ModifiedDate = DateTime.UtcNow;
                    rule.ModifiedBy = "System";
                    
                    _context.TransferRules.Update(rule);
                    var rowsAffected = await _context.SaveChangesAsync();
                    bool success = rowsAffected > 0;

                    if (success)
                    {
                        // Clear related caches
                        await ClearCacheAsync(rule.Country, rule.SourceSubscriptionType, rule.DestinationSubscriptionType);
                    }

                    activity?.SetTag("rows.affected", rowsAffected);
                    activity?.SetTag("result.success", success);
                    return success;
                }
                
                activity?.SetTag("rule.found", false);
                return false;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("error.type", ex.GetType().Name);
                return false;
            }
        }

        /// <summary>
        /// Clears caches related to a specific rule
        /// </summary>
        private async Task ClearCacheAsync(string country, string sourceType, string destType)
        {
            try
            {
                // Clear country-specific cache
                var countryCacheKey = $"{CACHE_KEY_COUNTRY_PREFIX}{country}";
                await _cache.RemoveAsync(countryCacheKey);

                // Clear evaluation cache for this specific combination
                var evaluationCacheKey = $"{CACHE_KEY_EVALUATION_PREFIX}{country}:{sourceType}:{destType}";
                await _cache.RemoveAsync(evaluationCacheKey);

                // Also clear wildcard evaluation cache if this might affect it
                if (destType != "*")
                {
                    var wildcardEvaluationCacheKey = $"{CACHE_KEY_EVALUATION_PREFIX}{country}:{sourceType}:*";
                    await _cache.RemoveAsync(wildcardEvaluationCacheKey);
                }
            }
            catch (Exception)
            {
                // Cache clearing errors should not affect the main operation
                // Silently continue - cache will eventually expire
            }
        }

        /// <summary>
        /// Helper class for caching evaluation results
        /// </summary>
        private class EvaluationResult
        {
            public bool IsAllowed { get; set; }
            public int ErrorCode { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
        }
    }
} 
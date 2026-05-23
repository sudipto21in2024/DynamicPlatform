using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Core.Interfaces;

namespace Platform.Engine.Services.Ai;

/// <summary>
/// Resolves the correct AI provider for the current request using the BYOK model.
/// Resolution order: Skill.PreferredProvider → Tenant Default → appsettings fallback (dev only).
/// </summary>
public class TenantAiProviderResolver
{
    private readonly ITenantAiProviderRepository _repo;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<TenantAiProviderResolver> _logger;

    public TenantAiProviderResolver(
        ITenantAiProviderRepository repo,
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<TenantAiProviderResolver> logger)
    {
        _repo = repo;
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Resolves an IAiProvider instance and the model to use for the given context.
    /// </summary>
    /// <param name="tenantId">The calling tenant's ID.</param>
    /// <param name="preferredProviderName">Optional: skill's preferred provider label.</param>
    /// <param name="preferredModel">Optional: skill's preferred model override.</param>
    public async Task<(IAiProvider Provider, string Model)> ResolveAsync(
        Guid tenantId,
        string? preferredProviderName = null,
        string? preferredModel = null,
        CancellationToken ct = default)
    {
        Core.Domain.Entities.TenantAiProvider? dbConfig = null;

        // --- Priority 1: Skill-specified provider name ---
        if (!string.IsNullOrWhiteSpace(preferredProviderName))
        {
            dbConfig = await _repo.GetByNameAsync(tenantId, preferredProviderName, ct);
            if (dbConfig != null)
                _logger.LogDebug("AI: Resolved skill-preferred provider '{Name}'", dbConfig.Name);
        }

        // --- Priority 2: Tenant's default provider ---
        if (dbConfig == null)
        {
            dbConfig = await _repo.GetDefaultAsync(tenantId, ct);
            if (dbConfig != null)
                _logger.LogDebug("AI: Resolved tenant-default provider '{Name}'", dbConfig.Name);
        }

        // --- Priority 3: Dev/test fallback from appsettings ---
        if (dbConfig == null)
        {
            var fallback = _config.GetSection("AiSettings");
            var baseUrl = fallback["BaseUrl"];
            var apiKey = fallback["ApiKey"];
            var defaultModel = fallback["DefaultModel"];

            if (!string.IsNullOrWhiteSpace(baseUrl) && !string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning(
                    "AI: No DB provider configured for tenant {TenantId}. Using appsettings fallback.", tenantId);

                var fallbackSettings = new AiProviderSettings
                {
                    ProviderName = fallback["ProviderName"] ?? "FallbackProvider",
                    BaseUrl = baseUrl,
                    ApiKey = apiKey,
                    DefaultModel = defaultModel ?? "minimaxai/minimax-m2.7",
                    MaxTokens = int.TryParse(fallback["MaxTokens"], out var mt) ? mt : 8192,
                    TimeoutSeconds = 120
                };

                var fallbackProvider = new OpenAiCompatibleProvider(_httpFactory, fallbackSettings, _logger);
                var fallbackModel = !string.IsNullOrEmpty(preferredModel) ? preferredModel : fallbackSettings.DefaultModel;
                return (fallbackProvider, fallbackModel);
            }

            // Hard stop — no provider anywhere
            throw new AiProviderNotConfiguredException(
                "No AI provider is configured. Please add your API key in Settings → AI Providers.");
        }

        // Build provider from DB config (key is stored plain for now; encryption in Phase 2)
        var settings = new AiProviderSettings
        {
            ProviderName = dbConfig.Name,
            BaseUrl = dbConfig.BaseUrl,
            ApiKey = dbConfig.ApiKeyEncrypted, // Phase 2: decrypt here
            DefaultModel = dbConfig.DefaultModel,
            MaxTokens = dbConfig.MaxTokens,
            TimeoutSeconds = dbConfig.TimeoutSeconds
        };

        var provider = new OpenAiCompatibleProvider(_httpFactory, settings, _logger);
        var model = !string.IsNullOrEmpty(preferredModel) ? preferredModel : dbConfig.DefaultModel;

        return (provider, model);
    }
}

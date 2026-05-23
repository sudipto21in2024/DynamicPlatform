using System;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Platform.Core.Interfaces;
using Platform.Engine.Services.Ai;

namespace Platform.API.Controllers;

/// <summary>
/// AI generation endpoints. Uses the AiSkillLibrary for prompts, SchemaContextExtractor
/// for live project context, and TenantAiProviderResolver for BYOK key resolution.
/// </summary>
[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly TenantAiProviderResolver _resolver;
    private readonly ITenantAiProviderRepository _providerRepo;
    private readonly SchemaContextExtractor _contextExtractor;
    private readonly AiSkillLibrary _skillLibrary;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<AiController> _logger;

    // Temporary: hardcoded dev tenant until JWT auth middleware is wired.
    // The resolver falls back to appsettings when no DB record exists for this tenantId.
    private static readonly Guid DevTenantId = Guid.Empty;

    public AiController(
        TenantAiProviderResolver resolver,
        ITenantAiProviderRepository providerRepo,
        SchemaContextExtractor contextExtractor,
        AiSkillLibrary skillLibrary,
        IHttpClientFactory httpFactory,
        ILogger<AiController> logger)
    {
        _resolver = resolver;
        _providerRepo = providerRepo;
        _contextExtractor = contextExtractor;
        _skillLibrary = skillLibrary;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/ai/generate-schema
    // Converts a natural-language domain description → EntityMetadata[] JSON.
    //
    // HOW CONTEXT FLOWS:
    //   1. AiSkillLibrary loads "GenerateEntitySchema" skill (DB or built-in)
    //   2. SchemaContextExtractor reads all Artifacts for projectId from DB
    //      → formats entities, relations, connectors as a markdown table
    //   3. systemPrompt + schemaContext → sent as system message to NVIDIA/OpenAI/Groq
    //   4. User's natural language description → sent as user message
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("generate-schema")]
    public async Task<IActionResult> GenerateSchema(
        [FromBody] AiPromptRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest("Prompt is required.");

        return await ExecuteSkillAsync("GenerateEntitySchema", request, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/ai/design-entity
    // Targeted redesign/modifications of a specific entity with optional new related entities.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("design-entity")]
    public async Task<IActionResult> DesignEntity(
        [FromBody] AiEntityDesignRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest("Prompt is required.");

        try
        {
            var tenantId = GetCurrentTenantId();

            // Load DesignEntity skill
            var skill = await _skillLibrary.GetSkillAsync("DesignEntity", request.ProjectId, ct);
            var systemPrompt = BuildSystemPrompt(skill);

            // Inject live schema context + current selected entity details
            var schemaContext = await _contextExtractor.ExtractAsync(request.ProjectId, ct);
            
            var fullSystemPmt = systemPrompt + "\n\n" + schemaContext;
            if (!string.IsNullOrWhiteSpace(request.CurrentEntityJson))
            {
                fullSystemPmt += $"\n\n## CURRENT SELECTED ENTITY FOR TARGETED ASSISTANCE\n{request.CurrentEntityJson}\n";
            }

            var history = BuildFewShotHistory(skill);

            var (provider, model) = await _resolver.ResolveAsync(
                tenantId,
                skill.PreferredProvider,
                skill.PreferredModel,
                ct);

            var aiRequest = new AiRequest
            {
                SystemPrompt = fullSystemPmt,
                UserPrompt   = request.Prompt,
                History      = history,
                Model        = model,
                Temperature  = skill.DefaultTemperature,
                TopP         = skill.DefaultTopP,
                MaxTokens    = skill.MaxTokens
            };

            _logger.LogInformation(
                "AI entity design skill provider={Provider} model={Model} projectId={ProjectId}",
                provider.ProviderName, model, request.ProjectId);

            var rawOutput = await provider.CompleteAsync(aiRequest, ct);
            var cleaned = CleanOutput(rawOutput, skill.ExpectedOutputFormat);
            return Ok(cleaned);
        }
        catch (AiProviderNotConfiguredException ex)
        {
            return StatusCode(402, new { error = "AI_PROVIDER_REQUIRED", message = ex.Message,
                settingsUrl = "/settings/ai-providers" });
        }
        catch (AiProviderException ex)
        {
            _logger.LogError("AI provider error for DesignEntity skill: {Message}", ex.Message);
            return StatusCode(502, new { error = "AI_PROVIDER_ERROR", message = ex.Message });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/ai/generate-connector
    // Converts a business requirement → ConnectorMetadata JSON with C# logic.
    //
    // HOW CONTEXT FLOWS:
    //   1. AiSkillLibrary loads "GenerateConnectorLogic" (with rules + few-shot examples)
    //   2. SchemaContextExtractor injects LIVE entity field names from DB
    //      → AI knows "Customer.LoyaltyPoints is int" before writing code
    //   3. AI generates businessLogic that uses EXACT field names from the schema
    //   4. Result saved as ArtifactType.Connector → compiled by Scriban at build time
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("generate-connector")]
    public async Task<IActionResult> GenerateConnector(
        [FromBody] AiPromptRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest("Prompt is required.");

        return await ExecuteSkillAsync("GenerateConnectorLogic", request, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/ai/generate-rule
    // Creates a declarative BusinessRuleMetadata from a condition statement.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("generate-rule")]
    public async Task<IActionResult> GenerateRule(
        [FromBody] AiPromptRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest("Prompt is required.");

        return await ExecuteSkillAsync("GenerateBusinessRule", request, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/ai/explain-logic
    // Explains an existing businessLogic C# snippet in plain English.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("explain-logic")]
    public async Task<IActionResult> ExplainLogic(
        [FromBody] AiPromptRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest("Prompt with the code snippet is required.");

        return await ExecuteSkillAsync("ExplainLogic", request, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/ai/complete   (generic streaming endpoint for Studio Playground)
    // Accepts a raw systemPrompt + userPrompt and streams tokens via SSE.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("complete")]
    public async Task Complete([FromBody] AiCompleteRequest request, CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");

        try
        {
            var tenantId = GetCurrentTenantId();
            var (provider, model) = await _resolver.ResolveAsync(tenantId, ct: ct);

            var aiRequest = new AiRequest
            {
                SystemPrompt = request.SystemPrompt,
                UserPrompt   = request.UserPrompt,
                Model        = model,
                Temperature  = request.Temperature,
                TopP         = request.TopP,
                MaxTokens    = request.MaxTokens
            };

            await foreach (var token in provider.CompleteStreamAsync(aiRequest, ct))
            {
                await Response.WriteAsync($"data: {JsonSerializer.Serialize(token)}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }

            await Response.WriteAsync("data: [DONE]\n\n", ct);
        }
        catch (AiProviderNotConfiguredException ex)
        {
            await Response.WriteAsync(
                $"data: {{\"error\":\"AI_PROVIDER_REQUIRED\",\"message\":\"{ex.Message}\"}}\n\n", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Streaming AI completion failed");
            await Response.WriteAsync(
                $"data: {{\"error\":\"STREAM_ERROR\",\"message\":\"{ex.Message}\"}}\n\n", ct);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET  /api/ai/skills
    // Returns the catalog of available built-in skills.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet("skills")]
    public IActionResult GetSkills() =>
        Ok(AiSkillLibrary.GetAllBuiltInSkills()
            .Select(s => new { s.SkillName, s.Description, s.Category, s.DefaultTemperature, s.MaxTokens }));

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/ai/providers/test
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("providers/test")]
    public async Task<IActionResult> TestProvider(
        [FromBody] TestProviderRequest request, CancellationToken ct)
    {
        var settings = new AiProviderSettings
        {
            ProviderName  = "test",
            BaseUrl       = request.BaseUrl.TrimEnd('/'),
            ApiKey        = request.ApiKey,
            DefaultModel  = request.DefaultModel,
            MaxTokens     = 20,
            TimeoutSeconds = 30
        };

        var provider = new OpenAiCompatibleProvider(_httpFactory, settings, _logger);

        try
        {
            var result = await provider.CompleteAsync(new AiRequest
            {
                SystemPrompt = "You are a test agent. Reply with only the word: OK",
                UserPrompt   = "Ping.",
                Temperature  = 0,
                MaxTokens    = 10,
                Model        = request.DefaultModel
            }, ct);

            return Ok(new { status = "OK", response = result.Trim() });
        }
        catch (Exception ex)
        {
            return Ok(new { status = "Error", message = ex.Message });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET  /api/ai/providers
    // POST /api/ai/providers
    // DELETE /api/ai/providers/{id}
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet("providers")]
    public async Task<IActionResult> GetProviders(CancellationToken ct)
    {
        var tenantId  = GetCurrentTenantId();
        var providers = await _providerRepo.GetByTenantAsync(tenantId, ct);
        return Ok(providers.Select(p => new
        {
            p.Id, p.Name, p.BaseUrl, p.ApiKeyPreview,
            p.DefaultModel, p.IsDefault, p.IsEnabled, p.LastTestStatus, p.LastTestedAt
        }));
    }

    [HttpPost("providers")]
    public async Task<IActionResult> AddProvider(
        [FromBody] AddProviderRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
            return BadRequest("API key is required.");

        var tenantId = GetCurrentTenantId();

        if (request.IsDefault)
            await _providerRepo.ClearDefaultAsync(tenantId, ct);

        var preview = request.ApiKey.Length > 12
            ? request.ApiKey[..6] + "..." + request.ApiKey[^4..]
            : "***";

        var entity = new Platform.Core.Domain.Entities.TenantAiProvider
        {
            TenantId           = tenantId,
            Name               = request.Name,
            BaseUrl            = request.BaseUrl.TrimEnd('/'),
            ApiKeyEncrypted    = request.ApiKey, // Phase 2: encrypt before storing
            ApiKeyPreview      = preview,
            DefaultModel       = request.DefaultModel,
            MaxTokens          = request.MaxTokens     > 0 ? request.MaxTokens     : 8192,
            TimeoutSeconds     = request.TimeoutSeconds > 0 ? request.TimeoutSeconds : 120,
            DefaultTemperature = request.DefaultTemperature > 0 ? request.DefaultTemperature : 0.7,
            IsDefault          = request.IsDefault,
            IsEnabled          = true
        };

        await _providerRepo.AddAsync(entity, ct);

        return CreatedAtAction(nameof(GetProviders), new { },
            new { entity.Id, entity.Name, entity.BaseUrl, entity.ApiKeyPreview,
                  entity.DefaultModel, entity.IsDefault });
    }

    [HttpDelete("providers/{id:guid}")]
    public async Task<IActionResult> DeleteProvider(Guid id, CancellationToken ct)
    {
        await _providerRepo.DeleteAsync(id, ct);
        return NoContent();
    }

    // ── Core Execution Helper ─────────────────────────────────────────────────

    /// <summary>
    /// Central method that wires: Skill → Context → Provider → AI → Clean output.
    ///
    /// Data flow:
    ///   1. Load AiSkill (DB override or built-in) for the requested skillName
    ///   2. Build system prompt = skill.SystemPrompt + numbered rules block
    ///   3. Fetch project schema context from DB (entities, connectors, rules)
    ///   4. Append context to system prompt
    ///   5. Optionally add few-shot examples as prior history turns
    ///   6. Resolve provider (preferred → tenant default → appsettings fallback)
    ///   7. Call provider.CompleteAsync
    ///   8. Clean and return output
    /// </summary>
    private async Task<IActionResult> ExecuteSkillAsync(
        string skillName, AiPromptRequest request, CancellationToken ct)
    {
        try
        {
            var tenantId = GetCurrentTenantId();

            // Step 1 & 2: Load skill + build full system prompt
            var skill        = await _skillLibrary.GetSkillAsync(skillName, request.ProjectId, ct);
            var systemPrompt = BuildSystemPrompt(skill);

            // Step 3 & 4: Inject live schema context
            var schemaContext  = await _contextExtractor.ExtractAsync(request.ProjectId, ct);
            var fullSystemPmt  = systemPrompt + schemaContext;

            // Step 5: Build few-shot history
            var history = BuildFewShotHistory(skill);

            // Step 6: Resolve provider
            var (provider, model) = await _resolver.ResolveAsync(
                tenantId,
                skill.PreferredProvider,
                skill.PreferredModel,
                ct);

            // Step 7: Call AI
            var aiRequest = new AiRequest
            {
                SystemPrompt = fullSystemPmt,
                UserPrompt   = request.Prompt,
                History      = history,
                Model        = model,
                Temperature  = skill.DefaultTemperature,
                TopP         = skill.DefaultTopP,
                MaxTokens    = skill.MaxTokens
            };

            _logger.LogInformation(
                "AI skill={Skill} provider={Provider} model={Model} projectId={ProjectId}",
                skillName, provider.ProviderName, model, request.ProjectId);

            var rawOutput = await provider.CompleteAsync(aiRequest, ct);

            // Step 8: Clean output
            var cleaned = CleanOutput(rawOutput, skill.ExpectedOutputFormat);
            return Ok(cleaned);
        }
        catch (AiProviderNotConfiguredException ex)
        {
            return StatusCode(402, new { error = "AI_PROVIDER_REQUIRED", message = ex.Message,
                settingsUrl = "/settings/ai-providers" });
        }
        catch (AiProviderException ex)
        {
            _logger.LogError("AI provider error for skill {Skill}: {Message}", skillName, ex.Message);
            return StatusCode(502, new { error = "AI_PROVIDER_ERROR", message = ex.Message });
        }
    }

    // ── Prompt Assembly Helpers ───────────────────────────────────────────────

    private static string BuildSystemPrompt(Platform.Engine.Models.AiSkillMetadata skill)
    {
        var sb = new StringBuilder();
        sb.AppendLine(skill.SystemPrompt);

        if (skill.Rules.Any())
        {
            sb.AppendLine("\nADDITIONAL RULES:");
            for (int i = 0; i < skill.Rules.Count; i++)
                sb.AppendLine($"{i + 1}. {skill.Rules[i]}");
        }

        return sb.ToString();
    }

    private static System.Collections.Generic.List<AiMessage> BuildFewShotHistory(
        Platform.Engine.Models.AiSkillMetadata skill)
    {
        var history = new System.Collections.Generic.List<AiMessage>();

        // Add guide prompt (assistant primer) first if defined
        if (!string.IsNullOrWhiteSpace(skill.GuidePrompt))
            history.Add(new AiMessage { Role = "assistant", Content = skill.GuidePrompt });

        // Add few-shot examples as alternating user/assistant turns
        foreach (var example in skill.FewShotExamples)
        {
            history.Add(new AiMessage { Role = "user",      Content = example.UserInput });
            history.Add(new AiMessage { Role = "assistant", Content = example.ExpectedOutput });
        }

        return history;
    }

    private static string CleanOutput(string raw, string format)
    {
        // Strip markdown code fences that models add despite instructions
        raw = Regex.Replace(raw, @"```json|```csharp|```", "").Trim();

        if (format == "JSON")
        {
            // Find the first { or [ and last } or ]
            var start = raw.IndexOfAny(new[] { '[', '{' });
            if (start > 0) raw = raw[start..];
            var end = raw.LastIndexOfAny(new[] { ']', '}' });
            if (end >= 0 && end < raw.Length - 1) raw = raw[..(end + 1)];
        }

        return raw;
    }

    // ── Tenant Context ────────────────────────────────────────────────────────

    private Guid GetCurrentTenantId()
    {
        // Phase 2: return Guid.Parse(User.FindFirst("tenant_id")!.Value);
        return DevTenantId; // Triggers appsettings fallback in resolver
    }
}

// ── Request / Response DTOs ───────────────────────────────────────────────────

public class AiPromptRequest
{
    public string Prompt    { get; set; } = string.Empty;
    /// <summary>If provided, the AI receives the project's entity/connector schema as context.</summary>
    public Guid?  ProjectId { get; set; }
}

public class AiEntityDesignRequest
{
    public string Prompt { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public string CurrentEntityJson { get; set; } = string.Empty;
}

public class AiCompleteRequest
{
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPrompt   { get; set; } = string.Empty;
    public double Temperature  { get; set; } = 0.7;
    public double TopP         { get; set; } = 0.95;
    public int    MaxTokens    { get; set; } = 8192;
}

public class TestProviderRequest
{
    public string BaseUrl      { get; set; } = string.Empty;
    public string ApiKey       { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = string.Empty;
}

public class AddProviderRequest
{
    public string Name               { get; set; } = string.Empty;
    public string BaseUrl            { get; set; } = string.Empty;
    public string ApiKey             { get; set; } = string.Empty;
    public string DefaultModel       { get; set; } = string.Empty;
    public int    MaxTokens          { get; set; } = 8192;
    public int    TimeoutSeconds     { get; set; } = 120;
    public double DefaultTemperature { get; set; } = 0.7;
    public bool   IsDefault          { get; set; } = true;
}

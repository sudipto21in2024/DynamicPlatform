using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Platform.API.Services;

namespace Platform.API.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly GeminiService _geminiService;

    public AiController(GeminiService geminiService)
    {
        _geminiService = geminiService;
    }

    [HttpPost("generate-schema")]
    public async Task<IActionResult> GenerateSchema([FromBody] PromptRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest("Prompt is required");

        var systemPrompt = @"
You are a Software Architect. 
Convert the user's description into a JSON array of EntityMetadata objects for a Low-Code Platform.
The Output MUST be valid JSON only. Do not include markdown code blocks (```json).
EntityMetadata Format:
[
  {
    ""Name"": ""EntityName"",
    ""Namespace"": ""GeneratedApp.Entities"",
    ""Fields"": [
       { ""Name"": ""FieldName"", ""Type"": ""string|int|datetime|bool|decimal|guid"", ""IsRequired"": true, ""MaxLength"": 100, ""Rules"": [] }
    ],
    ""Relations"": [
        { ""TargetEntity"": ""TargetName"", ""Type"": ""OneToMany|ManyToOne"", ""NavPropName"": ""target"" }
    ]
  }
]
Ensure you infer appropriate fields (Id is auto-added, audit is auto-added, don't add them).
User Description: 
" + request.Prompt;

        var result = await _geminiService.GenerateContentAsync(systemPrompt);
        
        // Basic cleanup if model ignores instructions
        result = result.Replace("```json", "").Replace("```", "").Trim();
        
        return Ok(result);
    }
}

public class PromptRequest
{
    public string Prompt { get; set; } = string.Empty;
}

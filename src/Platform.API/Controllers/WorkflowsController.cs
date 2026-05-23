using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Platform.Core.Domain.Entities;
using Platform.Core.Interfaces;
using System.Text.Json;

namespace Platform.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/workflows")]
public class WorkflowsController : ControllerBase
{
    private readonly IArtifactRepository _repo;

    public WorkflowsController(IArtifactRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Artifact>>> GetWorkflows(Guid projectId)
    {
        var artifacts = await _repo.GetByProjectIdAsync(projectId);
        return Ok(artifacts.Where(a => a.Type == ArtifactType.Workflow));
    }

    [HttpPost]
    public async Task<ActionResult<Artifact>> CreateWorkflow(Guid projectId, [FromBody] JsonElement metadata)
    {
        var jsonContent = JsonSerializer.Serialize(metadata);
        string name = "New Workflow";
        if (metadata.TryGetProperty("name", out var nameProp)) {
            name = nameProp.GetString() ?? name;
        }

        var artifact = new Artifact
        {
            ProjectId = projectId,
            Name = name,
            Type = ArtifactType.Workflow,
            Content = jsonContent
        };

        await _repo.AddAsync(artifact);
        return Ok(artifact);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateWorkflow(Guid projectId, Guid id, [FromBody] JsonElement metadata)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null || existing.ProjectId != projectId) return NotFound();

        var jsonContent = JsonSerializer.Serialize(metadata);
        if (metadata.TryGetProperty("name", out var nameProp)) {
            existing.Name = nameProp.GetString() ?? existing.Name;
        }
        existing.Content = jsonContent;
        existing.LastModified = DateTime.UtcNow;

        await _repo.UpdateAsync(existing);
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteWorkflow(Guid projectId, Guid id)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null || existing.ProjectId != projectId) return NotFound();

        await _repo.DeleteAsync(id);
        return Ok();
    }

    [HttpPost("{id}/publish")]
    public async Task<ActionResult> PublishWorkflow(Guid projectId, Guid id)
    {
        var artifact = await _repo.GetByIdAsync(id);
        if (artifact == null || artifact.ProjectId != projectId) return NotFound();

        // Simulate Elsa 3.0 Publication
        var metadata = JsonDocument.Parse(artifact.Content).RootElement;
        var nodes = metadata.GetProperty("nodes").EnumerateArray();
        var triggerNode = nodes.FirstOrDefault(n => n.GetProperty("type").GetString() == "http");

        if (triggerNode.ValueKind != JsonValueKind.Undefined)
        {
            var config = triggerNode.GetProperty("config");
            var path = config.GetProperty("path").GetString();
            var method = config.GetProperty("method").GetString();

            Console.WriteLine($"[ENGINE] Registering Elsa HTTP Endpoint: {method} {path} for Workflow {artifact.Name}");
        }

        artifact.LastModified = DateTime.UtcNow;
        // In a real implementation: _elsaPublisher.PublishAsync(...)
        
        return Ok(new { Message = "Workflow published to engine successfully", Endpoint = "/workflows/..." });
    }
}

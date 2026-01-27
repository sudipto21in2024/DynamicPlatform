using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Platform.Core.Domain.Entities;
using Platform.Core.Interfaces;
using System.Text.Json;
using Platform.Engine.Models;
using System.Text.Json.Serialization;

namespace Platform.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/widgets")]
public class WidgetsController : ControllerBase
{
    private readonly IArtifactRepository _repo;

    public WidgetsController(IArtifactRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Artifact>>> GetWidgets(Guid projectId)
    {
        var artifacts = await _repo.GetByProjectIdAsync(projectId);
        return Ok(artifacts.Where(a => a.Type == ArtifactType.Widget));
    }

    [HttpPost]
    public async Task<ActionResult<Artifact>> CreateWidget(Guid projectId, [FromBody] WidgetDefinition definition)
    {
        var jsonContent = JsonSerializer.Serialize(definition);
        
        var artifact = new Artifact
        {
            ProjectId = projectId,
            Name = definition.Name,
            Type = ArtifactType.Widget,
            Content = jsonContent
        };

        await _repo.AddAsync(artifact);
        return Ok(artifact);
    }
    
    [HttpPut("{id}")]
    public async Task<ActionResult<Artifact>> UpdateWidget(Guid projectId, Guid id, [FromBody] WidgetDefinition definition)
    {
        var artifact = await _repo.GetByIdAsync(id);
        if (artifact == null) return NotFound();
        
        artifact.Name = definition.Name;
        artifact.Content = JsonSerializer.Serialize(definition);
        artifact.LastModified = DateTime.UtcNow;
        
        await _repo.UpdateAsync(artifact);
        return Ok(artifact);
    }
    
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteWidget(Guid projectId, Guid id)
    {
        await _repo.DeleteAsync(id);
        return NoContent();
    }
}

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
[Route("api/projects/{projectId}/forms")]
public class FormsController : ControllerBase
{
    private readonly IArtifactRepository _repo;

    public FormsController(IArtifactRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Artifact>>> GetForms(Guid projectId)
    {
        var artifacts = await _repo.GetByProjectIdAsync(projectId);
        return Ok(artifacts.Where(a => a.Type == ArtifactType.Form));
    }

    [HttpPost]
    public async Task<ActionResult<Artifact>> CreateOrUpdateForm(Guid projectId, [FromBody] dynamic metadata)
    {
        var jsonContent = JsonSerializer.Serialize(metadata);
        
        string name = "New Form";
        try 
        {
             // Try to get name from dynamic JsonElement
             name = metadata.GetProperty("Name").GetString() ?? metadata.GetProperty("name").GetString() ?? "New Form";
        } 
        catch {}

        // Basic Upsert logic based on Name for now, or just Create. 
        // The repo doesn't seem to have complicated Upsert exposed here, so I'll just Add.
        // Ideally we should check if one exists with same name or ID.
        // For simplicity (MVP) as per WorkflowController, just Add. 
        // Note: Realistically we need ID-based updates.
        
        var artifact = new Artifact
        {
            ProjectId = projectId,
            Name = name,
            Type = ArtifactType.Form,
            Content = jsonContent
        };

        await _repo.AddAsync(artifact);
        return Ok(artifact);
    }
    
    [HttpPut("{id}")]
    public async Task<ActionResult<Artifact>> UpdateForm(Guid projectId, Guid id, [FromBody] dynamic metadata)
    {
        var artifact = await _repo.GetByIdAsync(id);
        if (artifact == null) return NotFound();
        
        artifact.Content = JsonSerializer.Serialize(metadata);
        try 
        {
             var name = metadata.GetProperty("Name").GetString() ?? metadata.GetProperty("name").GetString();
             if (!string.IsNullOrEmpty(name)) artifact.Name = name;
        } 
        catch {}
        
        artifact.LastModified = DateTime.UtcNow;
        await _repo.UpdateAsync(artifact);
        return Ok(artifact);
    }
}

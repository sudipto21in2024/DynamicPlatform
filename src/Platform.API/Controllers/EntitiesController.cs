using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Platform.Core.Domain.Entities;
using Platform.Core.Interfaces;
using Platform.Engine.Generators;
using Platform.Engine.Models;

namespace Platform.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/entities")]
public class EntitiesController : ControllerBase
{
    private readonly IArtifactRepository _repo;
    private readonly EntityGenerator _generator;

    public EntitiesController(IArtifactRepository repo, EntityGenerator generator)
    {
        _repo = repo;
        _generator = generator;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Artifact>>> GetEntities(Guid projectId)
    {
        var artifacts = await _repo.GetByProjectIdAsync(projectId);
        // Filter only Entities
        var entities = new List<Artifact>();
        foreach (var a in artifacts)
        {
            if (a.Type == ArtifactType.Entity) entities.Add(a);
        }
        return Ok(entities);
    }

    [HttpPost]
    public async Task<ActionResult<Artifact>> CreateEntity(Guid projectId, [FromBody] EntityMetadata metadata)
    {
        try
        {
            // 1. Validate Metadata
            if (string.IsNullOrEmpty(metadata.Name)) return BadRequest("Name is required");

            // 2. Serialize to JSON
            var jsonContent = JsonSerializer.Serialize(metadata);

            // 3. Create Artifact
            var artifact = new Artifact
            {
                ProjectId = projectId,
                Name = metadata.Name,
                Type = ArtifactType.Entity,
                Content = jsonContent
            };

            await _repo.AddAsync(artifact);

            return Ok(artifact);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in CreateEntity: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("{entityId}/preview")]
    public async Task<ActionResult<string>> PreviewCode(Guid projectId, Guid entityId)
    {
        var artifact = await _repo.GetByIdAsync(entityId);
        if (artifact == null) return NotFound();

        var metadata = JsonSerializer.Deserialize<EntityMetadata>(artifact.Content);
        if (metadata == null) return BadRequest("Invalid JSON content");

        // Use the Engine to generate code
        try 
        {
            var code = _generator.Generate(metadata);
            return Ok(code);
        }
        catch (Exception ex)
        {
            return BadRequest($"Generation Failed: {ex.Message}");
        }
    }

    [HttpDelete("{entityId}")]
    public async Task<IActionResult> DeleteEntity(Guid projectId, Guid entityId)
    {
        var artifact = await _repo.GetByIdAsync(entityId);
        if (artifact == null) return NotFound();
        await _repo.DeleteAsync(entityId);
        return NoContent();
    }
}

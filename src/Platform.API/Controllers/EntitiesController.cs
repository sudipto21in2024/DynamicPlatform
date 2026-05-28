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

    [HttpGet("export")]
    public async Task<IActionResult> ExportEntities(Guid projectId)
    {
        var artifacts = await _repo.GetByProjectIdAsync(projectId);
        var entities = new List<EntityMetadata>();
        foreach (var a in artifacts)
        {
            if (a.Type == ArtifactType.Entity)
            {
                try
                {
                    var meta = JsonSerializer.Deserialize<EntityMetadata>(a.Content);
                    if (meta != null)
                    {
                        entities.Add(meta);
                    }
                }
                catch { /* Ignore corrupt entries */ }
            }
        }
        var bytes = JsonSerializer.SerializeToUtf8Bytes(entities, new JsonSerializerOptions { WriteIndented = true });
        return File(bytes, "application/json", $"entities-export-{projectId}.json");
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportEntities(Guid projectId, [FromBody] EntityImportRequest request)
    {
        if (request == null || request.Entities == null || request.Entities.Count == 0)
            return BadRequest("No entities provided for import.");

        var existingArtifacts = await _repo.GetByProjectIdAsync(projectId);
        var existingEntitiesByName = new Dictionary<string, Artifact>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in existingArtifacts)
        {
            if (a.Type == ArtifactType.Entity)
            {
                existingEntitiesByName[a.Name] = a;
            }
        }

        // Detect conflicts (existing entities that are not explicitly approved for overwrite)
        var conflicts = new List<string>();
        foreach (var meta in request.Entities)
        {
            if (string.IsNullOrWhiteSpace(meta.Name)) continue;
            if (existingEntitiesByName.ContainsKey(meta.Name))
            {
                var isConfirmed = request.ConfirmedOverwrites.Contains(meta.Name, StringComparer.OrdinalIgnoreCase);
                if (!isConfirmed)
                {
                    conflicts.Add(meta.Name);
                }
            }
        }

        // If conflicts are found, return confirmation payload
        if (conflicts.Count > 0)
        {
            return Ok(new { RequiresConfirmation = true, Conflicts = conflicts });
        }

        var importedCount = 0;
        var updatedCount = 0;

        foreach (var meta in request.Entities)
        {
            if (string.IsNullOrWhiteSpace(meta.Name)) continue;

            var jsonContent = JsonSerializer.Serialize(meta);

            if (existingEntitiesByName.TryGetValue(meta.Name, out var existingArtifact))
            {
                existingArtifact.Content = jsonContent;
                await _repo.UpdateAsync(existingArtifact);
                updatedCount++;
            }
            else
            {
                var artifact = new Artifact
                {
                    ProjectId = projectId,
                    Name = meta.Name,
                    Type = ArtifactType.Entity,
                    Content = jsonContent
                };
                await _repo.AddAsync(artifact);
                importedCount++;
            }
        }

        return Ok(new { Message = "Import completed successfully.", RequiresConfirmation = false, ImportedCount = importedCount, UpdatedCount = updatedCount });
    }
}

public class EntityImportRequest
{
    public List<EntityMetadata> Entities { get; set; } = new();
    public List<string> ConfirmedOverwrites { get; set; } = new();
}

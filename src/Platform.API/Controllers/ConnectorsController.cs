using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Platform.Core.Domain.Entities;
using Platform.Core.Interfaces;

namespace Platform.API.Controllers;

/// <summary>
/// Manages Connector artifacts for a project.
/// Connectors are stateless execution units with named inputs, typed outputs,
/// and a C# businessLogic snippet compiled at build time via Scriban templates.
/// </summary>
[ApiController]
[Route("api/projects/{projectId}/connectors")]
public class ConnectorsController : ControllerBase
{
    private readonly IArtifactRepository _repo;

    public ConnectorsController(IArtifactRepository repo)
    {
        _repo = repo;
    }

    /// <summary>Returns all Connector artifacts for a project.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Artifact>>> GetConnectors(Guid projectId)
    {
        var artifacts = await _repo.GetByProjectIdAsync(projectId);
        var connectors = new List<Artifact>();
        foreach (var a in artifacts)
            if (a.Type == ArtifactType.Connector) connectors.Add(a);
        return Ok(connectors);
    }

    /// <summary>Creates or updates a Connector artifact by name (upsert).</summary>
    [HttpPost]
    public async Task<ActionResult<Artifact>> UpsertConnector(
        Guid projectId, [FromBody] JsonElement body)
    {
        try
        {
            var name = body.TryGetProperty("name", out var n) ? n.GetString() : null;
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Connector 'name' is required.");

            var content = body.GetRawText();

            // Check for existing artifact with same name (update)
            var all = await _repo.GetByProjectIdAsync(projectId);
            Artifact? existing = null;
            foreach (var a in all)
                if (a.Type == ArtifactType.Connector && a.Name == name) { existing = a; break; }

            if (existing != null)
            {
                existing.Content = content;
                existing.LastModified = DateTime.UtcNow;
                await _repo.UpdateAsync(existing);
                return Ok(existing);
            }

            var artifact = new Artifact
            {
                ProjectId    = projectId,
                Name         = name,
                Type         = ArtifactType.Connector,
                Content      = content,
                LastModified = DateTime.UtcNow
            };

            await _repo.AddAsync(artifact);
            return CreatedAtAction(nameof(GetConnectors), new { projectId }, artifact);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>Returns a single Connector artifact by ID.</summary>
    [HttpGet("{connectorId:guid}")]
    public async Task<IActionResult> GetConnector(Guid projectId, Guid connectorId)
    {
        var artifact = await _repo.GetByIdAsync(connectorId);
        if (artifact == null || artifact.ProjectId != projectId) return NotFound();
        return Ok(artifact);
    }

    /// <summary>Deletes a Connector artifact.</summary>
    [HttpDelete("{connectorId:guid}")]
    public async Task<IActionResult> DeleteConnector(Guid projectId, Guid connectorId)
    {
        var artifact = await _repo.GetByIdAsync(connectorId);
        if (artifact == null || artifact.ProjectId != projectId) return NotFound();
        await _repo.DeleteAsync(connectorId);
        return NoContent();
    }
}

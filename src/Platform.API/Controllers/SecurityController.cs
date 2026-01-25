using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Platform.Core.Domain.Entities;
using Platform.Core.Interfaces;
using Platform.Engine.Models;

namespace Platform.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/security")]
public class SecurityController : ControllerBase
{
    private readonly IArtifactRepository _repo;

    public SecurityController(IArtifactRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<ActionResult<SecurityMetadata>> GetSecurityConfig(Guid projectId)
    {
        var artifacts = await _repo.GetByProjectIdAsync(projectId);
        var securityArtifact = artifacts.FirstOrDefault(a => a.Type == ArtifactType.SecurityConfig);

        if (securityArtifact == null)
        {
            return Ok(new SecurityMetadata()); // Return empty config
        }

        try
        {
            var metadata = JsonSerializer.Deserialize<SecurityMetadata>(securityArtifact.Content);
            return Ok(metadata);
        }
        catch
        {
            return BadRequest("Invalid Security configuration data.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> SaveSecurityConfig(Guid projectId, [FromBody] SecurityMetadata metadata)
    {
        var artifacts = await _repo.GetByProjectIdAsync(projectId);
        var securityArtifact = artifacts.FirstOrDefault(a => a.Type == ArtifactType.SecurityConfig);

        var jsonContent = JsonSerializer.Serialize(metadata);

        if (securityArtifact == null)
        {
            securityArtifact = new Artifact
            {
                ProjectId = projectId,
                Name = "SecurityConfig",
                Type = ArtifactType.SecurityConfig,
                Content = jsonContent,
                LastModified = DateTime.UtcNow
            };
            await _repo.AddAsync(securityArtifact);
        }
        else
        {
            securityArtifact.Content = jsonContent;
            securityArtifact.LastModified = DateTime.UtcNow;
            await _repo.UpdateAsync(securityArtifact);
        }

        return Ok(securityArtifact);
    }
}

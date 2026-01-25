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
[Route("api/projects/{projectId}/users")]
public class UsersController : ControllerBase
{
    private readonly IArtifactRepository _repo;

    public UsersController(IArtifactRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<ActionResult<AppUserMetadata>> GetUsersConfig(Guid projectId)
    {
        var artifacts = await _repo.GetByProjectIdAsync(projectId);
        var usersArtifact = artifacts.FirstOrDefault(a => a.Type == ArtifactType.UsersConfig);

        if (usersArtifact == null)
        {
            return Ok(new AppUserMetadata());
        }

        try
        {
            var metadata = JsonSerializer.Deserialize<AppUserMetadata>(usersArtifact.Content);
            return Ok(metadata);
        }
        catch
        {
            return BadRequest("Invalid Users configuration data.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> SaveUsersConfig(Guid projectId, [FromBody] AppUserMetadata metadata)
    {
        var artifacts = await _repo.GetByProjectIdAsync(projectId);
        var usersArtifact = artifacts.FirstOrDefault(a => a.Type == ArtifactType.UsersConfig);

        var jsonContent = JsonSerializer.Serialize(metadata);

        if (usersArtifact == null)
        {
            usersArtifact = new Artifact
            {
                ProjectId = projectId,
                Name = "UsersConfig",
                Type = ArtifactType.UsersConfig,
                Content = jsonContent,
                LastModified = DateTime.UtcNow
            };
            await _repo.AddAsync(usersArtifact);
        }
        else
        {
            usersArtifact.Content = jsonContent;
            usersArtifact.LastModified = DateTime.UtcNow;
            await _repo.UpdateAsync(usersArtifact);
        }

        return Ok(usersArtifact);
    }
}

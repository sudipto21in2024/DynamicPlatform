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
    public async Task<ActionResult<Artifact>> CreateWorkflow(Guid projectId, [FromBody] dynamic metadata)
    {
        var jsonContent = JsonSerializer.Serialize(metadata);
        var artifact = new Artifact
        {
            ProjectId = projectId,
            Name = metadata.GetProperty("name").GetString() ?? "New Workflow",
            Type = ArtifactType.Workflow,
            Content = jsonContent
        };

        await _repo.AddAsync(artifact);
        return Ok(artifact);
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Platform.Core.Domain.Entities;
using Platform.Core.Interfaces;
using Platform.Engine.Interfaces;
using Platform.Engine.Models.Delta;
using Platform.Infrastructure.Data;

namespace Platform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PublishController : ControllerBase
{
    private readonly IVersioningService _versioningService;
    private readonly IMetadataDiffService _diffService;
    private readonly ISqlSchemaEvolutionService _evolutionService;
    private readonly IRepository<ProjectSnapshot> _snapshotRepository;
    private readonly PlatformDbContext _db;

    public PublishController(
        IVersioningService versioningService,
        IMetadataDiffService diffService,
        ISqlSchemaEvolutionService evolutionService,
        IRepository<ProjectSnapshot> snapshotRepository,
        PlatformDbContext db)
    {
        _versioningService = versioningService;
        _diffService = diffService;
        _evolutionService = evolutionService;
        _snapshotRepository = snapshotRepository;
        _db = db;
    }

    [HttpPost("{projectId}/plan")]
    public async Task<ActionResult<MigrationPlan>> GetMigrationPlan(Guid projectId, [FromQuery] string version)
    {
        var lastPublished = await _versioningService.GetLastPublishedSnapshotAsync(projectId);
        
        // Create a temporary draft snapshot from current artifacts
        var draft = await _versioningService.CreateSnapshotAsync(projectId, version, "System");

        var plan = _diffService.Compare(lastPublished, draft);
        
        // We don't save the snapshot yet as "Published", just return the plan
        return Ok(plan);
    }

    [HttpPost("{projectId}/apply")]
    public async Task<IActionResult> ApplyMigration(Guid projectId, [FromQuery] string version)
    {
        var project = await _db.Projects.FindAsync(projectId);
        if (project == null) return NotFound("Project not found");

        var lastPublished = await _versioningService.GetLastPublishedSnapshotAsync(projectId);
        var draft = await _versioningService.CreateSnapshotAsync(projectId, version, "User");

        var plan = _diffService.Compare(lastPublished, draft);

        try
        {
            // Apply SQL changes to the project's isolated database
            // In MVP, we might use the default connection for everything, 
            // but the architecture supports isolated DBs.
            var connectionString = project.IsolatedConnectionString ?? _db.Database.GetConnectionString()!;
            
            await _evolutionService.ApplyMigrationAsync(plan, connectionString);

            // Mark snapshot as published
            draft.IsPublished = true;
            await _snapshotRepository.UpdateAsync(draft);
            await _snapshotRepository.SaveChangesAsync();

            return Ok(new { Message = "Migration applied successfully", Plan = plan });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = "Migration failed", Error = ex.Message });
        }
    }
}

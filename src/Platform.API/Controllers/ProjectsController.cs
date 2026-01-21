using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Platform.Core.Domain.Entities;
using Platform.Infrastructure.Data;

namespace Platform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly PlatformDbContext _db;

    public ProjectsController(PlatformDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
    {
        return await _db.Projects.Include(p => p.Tenant).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Project>> GetProject(Guid id)
    {
        var project = await _db.Projects.Include(p => p.Tenant)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null) return NotFound();
        return project;
    }

    [HttpPost]
    public async Task<ActionResult<Project>> CreateProject(Project project)
    {
        // Minimal validation for MVP
        if (project.TenantId == Guid.Empty)
        {
            // Auto-create a default tenant if missing for MVP simplicity
            var tenant = await _db.Tenants.FirstOrDefaultAsync();
            if (tenant == null)
            {
                tenant = new Tenant { Name = "Default Tenant" };
                _db.Tenants.Add(tenant);
                await _db.SaveChangesAsync();
            }
            project.TenantId = tenant.Id;
        }

        project.CreatedAt = DateTime.UtcNow;
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
    }
}

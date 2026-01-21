using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Platform.Core.Domain.Entities;
using Platform.Core.Interfaces;

namespace Platform.Infrastructure.Data.Repositories;

public class ArtifactRepository : IArtifactRepository
{
    private readonly PlatformDbContext _db;

    public ArtifactRepository(PlatformDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Artifact>> GetByProjectIdAsync(Guid projectId)
    {
        return await _db.Artifacts
            .Where(a => a.ProjectId == projectId)
            .ToListAsync();
    }

    public async Task<Artifact?> GetByIdAsync(Guid id)
    {
        return await _db.Artifacts.FindAsync(id);
    }

    public async Task AddAsync(Artifact artifact)
    {
        _db.Artifacts.Add(artifact);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Artifact artifact)
    {
        _db.Artifacts.Update(artifact);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var artifact = await GetByIdAsync(id);
        if (artifact != null)
        {
            _db.Artifacts.Remove(artifact);
            await _db.SaveChangesAsync();
        }
    }
}

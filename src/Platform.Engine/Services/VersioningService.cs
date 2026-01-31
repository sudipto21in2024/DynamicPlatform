using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Platform.Core.Domain.Entities;
using Platform.Core.Interfaces;
using Platform.Engine.Interfaces;

namespace Platform.Engine.Services;

/// <summary>
/// Implementation of the versioning service for project snapshots.
/// </summary>
public class VersioningService : IVersioningService
{
    private readonly IArtifactRepository _artifactRepository;
    private readonly IRepository<ProjectSnapshot> _snapshotRepository;

    public VersioningService(
        IArtifactRepository artifactRepository,
        IRepository<ProjectSnapshot> snapshotRepository)
    {
        _artifactRepository = artifactRepository;
        _snapshotRepository = snapshotRepository;
    }

    public async Task<ProjectSnapshot> CreateSnapshotAsync(Guid projectId, string version, string? createdBy = null)
    {
        var artifacts = await _artifactRepository.GetByProjectIdAsync(projectId);
        
        // Serialize artifacts to JSON
        var content = JsonSerializer.Serialize(artifacts, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        });

        var hash = ComputeHash(content);

        var snapshot = new ProjectSnapshot
        {
            ProjectId = projectId,
            Version = version,
            Content = content,
            Hash = hash,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            IsPublished = false
        };

        await _snapshotRepository.AddAsync(snapshot);
        await _snapshotRepository.SaveChangesAsync();

        return snapshot;
    }

    public async Task<ProjectSnapshot?> GetSnapshotAsync(Guid projectId, string version)
    {
        return await _snapshotRepository.FirstOrDefaultAsync(s => s.ProjectId == projectId && s.Version == version);
    }

    public async Task<ProjectSnapshot?> GetLatestSnapshotAsync(Guid projectId)
    {
        var snapshots = await _snapshotRepository.GetAllAsync(
            filter: s => s.ProjectId == projectId,
            orderBy: q => q.OrderByDescending(s => s.CreatedAt),
            take: 1);
            
        return snapshots.FirstOrDefault();
    }

    public async Task<ProjectSnapshot?> GetLastPublishedSnapshotAsync(Guid projectId)
    {
        var snapshots = await _snapshotRepository.GetAllAsync(
            filter: s => s.ProjectId == projectId && s.IsPublished,
            orderBy: q => q.OrderByDescending(s => s.CreatedAt),
            take: 1);

        return snapshots.FirstOrDefault();
    }

    private string ComputeHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes);
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Platform.Core.Domain.Entities;

namespace Platform.Core.Interfaces;

public interface IArtifactRepository
{
    Task<IEnumerable<Artifact>> GetByProjectIdAsync(Guid projectId);
    Task<Artifact?> GetByIdAsync(Guid id);
    Task<Project?> GetProjectByIdAsync(Guid id);
    Task AddAsync(Artifact artifact);
    Task UpdateAsync(Artifact artifact);
    Task DeleteAsync(Guid id);
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Platform.Core.Domain.Entities;

namespace Platform.Core.Interfaces;

/// <summary>
/// Repository for managing tenant-configured AI providers (BYOK model).
/// </summary>
public interface ITenantAiProviderRepository
{
    Task<IEnumerable<TenantAiProvider>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<TenantAiProvider?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TenantAiProvider?> GetByNameAsync(Guid tenantId, string name, CancellationToken ct = default);
    Task<TenantAiProvider?> GetDefaultAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(TenantAiProvider provider, CancellationToken ct = default);
    Task UpdateAsync(TenantAiProvider provider, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task ClearDefaultAsync(Guid tenantId, CancellationToken ct = default);
}

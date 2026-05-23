using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Platform.Core.Domain.Entities;
using Platform.Core.Interfaces;
using Platform.Infrastructure.Data;

namespace Platform.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of ITenantAiProviderRepository.
/// </summary>
public class TenantAiProviderRepository : ITenantAiProviderRepository
{
    private readonly PlatformDbContext _db;

    public TenantAiProviderRepository(PlatformDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<TenantAiProvider>> GetByTenantAsync(
        Guid tenantId, CancellationToken ct = default) =>
        await _db.TenantAiProviders
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);

    public async Task<TenantAiProvider?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.TenantAiProviders.FindAsync(new object[] { id }, ct);

    public async Task<TenantAiProvider?> GetByNameAsync(
        Guid tenantId, string name, CancellationToken ct = default) =>
        await _db.TenantAiProviders
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Name == name, ct);

    public async Task<TenantAiProvider?> GetDefaultAsync(
        Guid tenantId, CancellationToken ct = default) =>
        await _db.TenantAiProviders
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.IsDefault && p.IsEnabled, ct);

    public async Task AddAsync(TenantAiProvider provider, CancellationToken ct = default)
    {
        _db.TenantAiProviders.Add(provider);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(TenantAiProvider provider, CancellationToken ct = default)
    {
        _db.TenantAiProviders.Update(provider);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(id, ct);
        if (entity != null)
        {
            _db.TenantAiProviders.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }

    /// <summary>Clears IsDefault on all providers for a tenant before setting a new default.</summary>
    public async Task ClearDefaultAsync(Guid tenantId, CancellationToken ct = default)
    {
        var providers = await _db.TenantAiProviders
            .Where(p => p.TenantId == tenantId && p.IsDefault)
            .ToListAsync(ct);

        foreach (var p in providers)
            p.IsDefault = false;

        await _db.SaveChangesAsync(ct);
    }
}

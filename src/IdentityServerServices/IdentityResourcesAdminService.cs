using Duende.IdentityServer.EntityFramework.DbContexts;
using IdentityServerServices.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerServices;

public class IdentityResourcesAdminService : IIdentityResourcesAdminService
{
    private readonly ConfigurationDbContext _configurationDbContext;

    public IdentityResourcesAdminService(ConfigurationDbContext configurationDbContext)
    {
        _configurationDbContext = configurationDbContext;
    }

    public async Task<IReadOnlyList<IdentityResourceListItemDto>> GetIdentityResourcesAsync(CancellationToken ct = default)
    {
        return await _configurationDbContext.IdentityResources
            .AsNoTracking()
            .OrderBy(resource => resource.Name)
            .Select(resource => new IdentityResourceListItemDto
            {
                Id = resource.Id,
                Name = resource.Name,
                DisplayName = resource.DisplayName ?? string.Empty,
                Description = resource.Description ?? string.Empty,
                Enabled = resource.Enabled
            })
            .ToListAsync(ct);
    }
}

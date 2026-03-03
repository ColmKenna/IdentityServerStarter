using Duende.IdentityServer.EntityFramework.DbContexts;
using IdentityServerServices.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerServices;

public class ApiScopesAdminService : IApiScopesAdminService
{
    private readonly ConfigurationDbContext _context;

    public ApiScopesAdminService(ConfigurationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ApiScopeListItemDto>> GetApiScopesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ApiScopes
            .AsNoTracking()
            .OrderBy(scope => scope.Name)
            .Select(scope => new ApiScopeListItemDto
            {
                Id = scope.Id,
                Name = scope.Name,
                DisplayName = scope.DisplayName ?? string.Empty,
                Description = scope.Description ?? string.Empty,
                Enabled = scope.Enabled
            })
            .ToListAsync(cancellationToken);
    }
}

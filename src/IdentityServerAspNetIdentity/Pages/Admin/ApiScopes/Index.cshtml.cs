using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerAspNetIdentity.Pages.Admin.ApiScopes;

[Authorize(Roles = "ADMIN")]
public class IndexModel : PageModel
{
    private readonly ConfigurationDbContext _context;

    public IndexModel(ConfigurationDbContext context)
    {
        _context = context;
    }

    public IList<ApiScopeListItem> ApiScopes { get; set; } = new List<ApiScopeListItem>();

    public async Task OnGetAsync()
    {
        ApiScopes = await _context.ApiScopes
            .AsNoTracking()
            .OrderBy(scope => scope.Name)
            .Select(scope => new ApiScopeListItem
            {
                Id = scope.Id,
                Name = scope.Name,
                DisplayName = scope.DisplayName ?? string.Empty,
                Description = scope.Description ?? string.Empty,
                Enabled = scope.Enabled
            })
            .ToListAsync();
    }

    public class ApiScopeListItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public string Description { get; set; } = default!;
        public bool Enabled { get; set; }
    }
}

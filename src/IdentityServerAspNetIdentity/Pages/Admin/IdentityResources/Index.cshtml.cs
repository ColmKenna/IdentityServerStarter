using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerAspNetIdentity.Pages.Admin.IdentityResources;

[Authorize(Roles = "ADMIN")]
public class IndexModel : PageModel
{
    private readonly ConfigurationDbContext _context;

    public IndexModel(ConfigurationDbContext context)
    {
        _context = context;
    }

    public IList<IdentityResourceListItem> IdentityResources { get; set; } = new List<IdentityResourceListItem>();

    public async Task OnGetAsync()
    {
        IdentityResources = await _context.IdentityResources
            .AsNoTracking()
            .OrderBy(resource => resource.Name)
            .Select(resource => new IdentityResourceListItem
            {
                Id = resource.Id,
                Name = resource.Name,
                DisplayName = resource.DisplayName ?? string.Empty,
                Description = resource.Description ?? string.Empty,
                Enabled = resource.Enabled
            })
            .ToListAsync();
    }

    public class IdentityResourceListItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public string Description { get; set; } = default!;
        public bool Enabled { get; set; }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore;
using Duende.IdentityServer.EntityFramework.Entities;

namespace IdentityServerAspNetIdentity.Pages.Admin.Clients;

[Authorize(Roles = "ADMIN")]
public class IndexModel : PageModel
{
    private readonly ConfigurationDbContext _context;

    public IndexModel(ConfigurationDbContext context)
    {
        _context = context;
    }

    public List<Client> Clients { get; set; } = new();

    public async Task OnGetAsync()
    {
        Clients = await _context.Clients
            .AsNoTracking()
            .OrderBy(c => c.ClientName)
            .ToListAsync();
    }
}

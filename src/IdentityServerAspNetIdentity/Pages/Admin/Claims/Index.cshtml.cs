using IdentityServer.EF.DataAccess.DataMigrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerAspNetIdentity.Pages.Admin.Claims;

[Authorize(Roles = "ADMIN")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<ClaimListItem> Claims { get; set; } = new List<ClaimListItem>();

    public async Task OnGetAsync()
    {
        Claims = await _context.UserClaims
            .AsNoTracking()
            .Where(c => c.ClaimType != null && c.ClaimType != string.Empty)
            .Select(c => c.ClaimType!)
            .Distinct()
            .OrderBy(claimType => claimType)
            .Select(claimType => new ClaimListItem
            {
                ClaimType = claimType
            })
            .ToListAsync();
    }

    public class ClaimListItem
    {
        public string ClaimType { get; set; } = default!;
    }
}

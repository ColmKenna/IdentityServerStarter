using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Admin.Roles;

[Authorize(Roles = "ADMIN")]
public class IndexModel : PageModel
{
    private readonly RoleManager<IdentityRole> _roleManager;

    public IndexModel(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public IList<RoleListItem> Roles { get; set; } = new List<RoleListItem>();

    public async Task OnGetAsync()
    {
        var roles = _roleManager.Roles
            .OrderBy(r => r.Name)
            .ToList();

        Roles = roles.Select(r => new RoleListItem
        {
            Id = r.Id,
            Name = r.Name ?? string.Empty
        }).ToList();
    }

    public class RoleListItem
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
    }
}

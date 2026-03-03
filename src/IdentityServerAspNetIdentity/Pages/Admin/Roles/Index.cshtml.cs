using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Admin.Roles;

[Authorize(Roles = "ADMIN")]
public class IndexModel : PageModel
{
    private readonly IRolesAdminService _rolesAdminService;

    public IndexModel(IRolesAdminService rolesAdminService)
    {
        _rolesAdminService = rolesAdminService;
    }

    public IList<RoleListItemDto> Roles { get; set; } = new List<RoleListItemDto>();

    public async Task OnGetAsync()
    {
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        Roles = (await _rolesAdminService.GetRolesAsync(cancellationToken)).ToList();
    }
}

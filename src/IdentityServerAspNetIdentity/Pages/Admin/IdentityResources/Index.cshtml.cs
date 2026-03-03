using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Admin.IdentityResources;

[Authorize(Roles = "ADMIN")]
public class IndexModel : PageModel
{
    private readonly IIdentityResourcesAdminService _identityResourcesAdminService;

    public IndexModel(IIdentityResourcesAdminService identityResourcesAdminService)
    {
        _identityResourcesAdminService = identityResourcesAdminService;
    }

    public IList<IdentityResourceListItemDto> IdentityResources { get; set; } = new List<IdentityResourceListItemDto>();

    public async Task OnGetAsync()
    {
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        IdentityResources = (await _identityResourcesAdminService.GetIdentityResourcesAsync(cancellationToken)).ToList();
    }
}

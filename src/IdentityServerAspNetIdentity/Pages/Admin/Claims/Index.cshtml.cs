using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Admin.Claims;

[Authorize(Roles = "ADMIN")]
public class IndexModel : PageModel
{
    private readonly IClaimsAdminService _claimsAdminService;

    public IndexModel(IClaimsAdminService claimsAdminService)
    {
        _claimsAdminService = claimsAdminService;
    }

    public IList<ClaimTypeListItemDto> Claims { get; set; } = new List<ClaimTypeListItemDto>();

    public async Task OnGetAsync()
    {
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        Claims = (await _claimsAdminService.GetClaimsAsync(cancellationToken)).ToList();
    }
}

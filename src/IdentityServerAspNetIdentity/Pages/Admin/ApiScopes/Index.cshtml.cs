using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Admin.ApiScopes;

[Authorize(Roles = "ADMIN")]
public class IndexModel : PageModel
{
    private readonly IApiScopesAdminService _apiScopesAdminService;

    public IndexModel(IApiScopesAdminService apiScopesAdminService)
    {
        _apiScopesAdminService = apiScopesAdminService;
    }

    public IList<ApiScopeListItemDto> ApiScopes { get; set; } = new List<ApiScopeListItemDto>();

    public async Task OnGetAsync()
    {
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        ApiScopes = (await _apiScopesAdminService.GetApiScopesAsync(cancellationToken)).ToList();
    }
}

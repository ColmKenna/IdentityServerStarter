using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Admin.Clients;

[Authorize(Roles = "ADMIN")]
public class IndexModel : PageModel
{
    private readonly IClientAdminService _clientAdminService;

    public IndexModel(IClientAdminService clientAdminService)
    {
        _clientAdminService = clientAdminService;
    }

    public IList<ClientListItemDto> Clients { get; set; } = new List<ClientListItemDto>();

    public async Task OnGetAsync()
    {
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        Clients = (await _clientAdminService.GetClientsAsync(cancellationToken)).ToList();
    }
}

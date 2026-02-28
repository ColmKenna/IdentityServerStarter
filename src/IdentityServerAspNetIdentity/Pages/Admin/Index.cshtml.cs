using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Admin;

[Authorize(Roles = "ADMIN")]
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}

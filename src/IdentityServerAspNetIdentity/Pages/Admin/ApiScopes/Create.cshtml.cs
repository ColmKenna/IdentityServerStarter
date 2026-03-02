using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Admin.ApiScopes;

[Authorize(Roles = "ADMIN")]
public class CreateModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Admin/ApiScopes/Edit", new { id = 0 });

    public IActionResult OnPost() => RedirectToPage("/Admin/ApiScopes/Edit", new { id = 0 });
}

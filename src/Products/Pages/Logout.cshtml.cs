using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Products.Pages;

public class LogoutModel : PageModel
{
    public IActionResult OnGet()
    {
        return SignOut(
            new AuthenticationProperties
            {
                RedirectUri = "/"
            },
            "Cookies",
            "oidc");
    }
}
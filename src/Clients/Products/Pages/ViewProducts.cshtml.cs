using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Products.Pages;

[Authorize(Policy = "CanViewProducts")]
public class ViewProductsModel : PageModel
{
    public void OnGet()
    {
    }
}
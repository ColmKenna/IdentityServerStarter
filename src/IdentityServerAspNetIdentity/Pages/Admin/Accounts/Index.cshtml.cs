using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerAspNetIdentity.Pages.Admin.Accounts;

[Authorize(Roles = "ADMIN")]
public class Index : PageModel
{
    private UserManager<ApplicationUser> userManager;

    public Index(UserManager<ApplicationUser> userManager)
    {
        this.userManager = userManager;
    }
    
    
    public IList<ApplicationUser> Users { get; set; }
    public async Task OnGetAsync()
    {
         
        Users = await userManager.Users.ToListAsync();
    }

}
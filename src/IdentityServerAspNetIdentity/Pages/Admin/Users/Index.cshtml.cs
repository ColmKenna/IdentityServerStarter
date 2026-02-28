using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerAspNetIdentity.Pages.Admin.Users;

[Authorize(Policy = UserPolicyConstants.UsersRead)]
public class Index : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public Index(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public IList<UserListItem> Users { get; set; } = new List<UserListItem>();

    public async Task OnGetAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        Users = users.Select(u => new UserListItem
        {
            Id = u.Id,
            UserName = u.UserName ?? string.Empty,
            Email = u.Email ?? string.Empty,
            EmailConfirmed = u.EmailConfirmed,
            LockoutEnd = u.LockoutEnd,
            TwoFactorEnabled = u.TwoFactorEnabled
        }).ToList();
    }

    public class UserListItem
    {
        public string Id { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public bool EmailConfirmed { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool TwoFactorEnabled { get; set; }

        public string LockoutStatus
        {
            get
            {
                if (LockoutEnd.HasValue && LockoutEnd.Value == DateTimeOffset.MaxValue)
                    return "Disabled";
                if (LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow)
                    return "Locked Out";
                return "Active";
            }
        }
    }
}

using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Admin.Users;

[Authorize(Policy = UserPolicyConstants.UsersRead)]
public class Index : PageModel
{
    private readonly IUserEditor _userEditor;

    public Index(IUserEditor userEditor)
    {
        _userEditor = userEditor;
    }

    public IList<UserListItemDto> Users { get; set; } = new List<UserListItemDto>();

    public async Task OnGetAsync()
    {
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        Users = (await _userEditor.GetUsersAsync(cancellationToken)).ToList();
    }
}

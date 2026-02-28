#nullable enable
using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Admin.Roles;

[Authorize(Roles = "ADMIN")]
public class EditModel : PageModel
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public EditModel(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public string RoleId { get; set; } = default!;

    public string RoleName { get; set; } = default!;

    public IList<UserListItem> UsersInRole { get; set; } = new List<UserListItem>();

    public IList<UserListItem> AvailableUsers { get; set; } = new List<UserListItem>();

    [BindProperty]
    public string? SelectedUserId { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var role = await _roleManager.FindByIdAsync(RoleId);
        if (role == null)
        {
            return NotFound();
        }

        await PopulatePageData(role);
        return Page();
    }

    public async Task<IActionResult> OnPostAddUserAsync()
    {
        var role = await _roleManager.FindByIdAsync(RoleId);
        if (role == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(SelectedUserId))
        {
            ModelState.AddModelError(nameof(SelectedUserId), "Please select a user");
            await PopulatePageData(role);
            return Page();
        }

        var user = await _userManager.FindByIdAsync(SelectedUserId);
        if (user == null)
        {
            return NotFound();
        }

        var result = await _userManager.AddToRoleAsync(user, role.Name!);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            await PopulatePageData(role);
            return Page();
        }

        TempData["Success"] = $"User '{user.UserName}' added to role '{role.Name}'";
        return RedirectToPage(new { roleId = RoleId });
    }

    public async Task<IActionResult> OnPostRemoveUserAsync()
    {
        var role = await _roleManager.FindByIdAsync(RoleId);
        if (role == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(SelectedUserId))
        {
            ModelState.AddModelError(nameof(SelectedUserId), "Please select a user");
            await PopulatePageData(role);
            return Page();
        }

        var user = await _userManager.FindByIdAsync(SelectedUserId);
        if (user == null)
        {
            return NotFound();
        }

        var result = await _userManager.RemoveFromRoleAsync(user, role.Name!);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            await PopulatePageData(role);
            return Page();
        }

        TempData["Success"] = $"User '{user.UserName}' removed from role '{role.Name}'";
        return RedirectToPage(new { roleId = RoleId });
    }

    private async Task PopulatePageData(IdentityRole role)
    {
        RoleName = role.Name ?? string.Empty;

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
        var usersInRoleIds = usersInRole.Select(u => u.Id).ToHashSet();

        UsersInRole = usersInRole
            .OrderBy(u => u.UserName)
            .Select(u => new UserListItem
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty
            })
            .ToList();

        var allUsers = _userManager.Users.ToList();
        AvailableUsers = allUsers
            .Where(u => !usersInRoleIds.Contains(u.Id))
            .OrderBy(u => u.UserName)
            .Select(u => new UserListItem
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty
            })
            .ToList();
    }

    public class UserListItem
    {
        public string Id { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public string Email { get; set; } = default!;
    }
}

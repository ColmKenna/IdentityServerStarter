#nullable enable
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Admin.Roles;

[Authorize(Roles = "ADMIN")]
public class EditModel : PageModel
{
    private readonly IRolesAdminService _rolesAdminService;

    public EditModel(IRolesAdminService rolesAdminService)
    {
        _rolesAdminService = rolesAdminService;
    }

    [BindProperty(SupportsGet = true)]
    public string RoleId { get; set; } = default!;

    public string RoleName { get; set; } = default!;

    public IList<RoleUserDto> UsersInRole { get; set; } = new List<RoleUserDto>();

    public IList<RoleUserDto> AvailableUsers { get; set; } = new List<RoleUserDto>();

    [BindProperty]
    public string? SelectedUserId { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var ct = HttpContext?.RequestAborted ?? CancellationToken.None;
        var pageData = await _rolesAdminService.GetRoleForEditAsync(RoleId, ct);
        if (pageData == null)
        {
            return NotFound();
        }

        PopulateFromPageData(pageData);
        return Page();
    }

    public async Task<IActionResult> OnPostAddUserAsync()
    {
        var ct = HttpContext?.RequestAborted ?? CancellationToken.None;

        if (string.IsNullOrWhiteSpace(SelectedUserId))
        {
            ModelState.AddModelError(nameof(SelectedUserId), "Please select a user");
            await ReloadPageData(ct);
            return Page();
        }

        var result = await _rolesAdminService.AddUserToRoleAsync(RoleId, SelectedUserId, ct);

        switch (result.Status)
        {
            case AddUserToRoleStatus.RoleNotFound:
            case AddUserToRoleStatus.UserNotFound:
                return NotFound();

            case AddUserToRoleStatus.Failed:
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
                await ReloadPageData(ct);
                return Page();

            default:
                TempData["Success"] = $"User '{result.UserName}' added to role '{result.RoleName}'";
                return RedirectToPage(new { roleId = RoleId });
        }
    }

    public async Task<IActionResult> OnPostRemoveUserAsync()
    {
        var ct = HttpContext?.RequestAborted ?? CancellationToken.None;

        if (string.IsNullOrWhiteSpace(SelectedUserId))
        {
            ModelState.AddModelError(nameof(SelectedUserId), "Please select a user");
            await ReloadPageData(ct);
            return Page();
        }

        var result = await _rolesAdminService.RemoveUserFromRoleAsync(RoleId, SelectedUserId, ct);

        switch (result.Status)
        {
            case RemoveUserFromRoleStatus.RoleNotFound:
            case RemoveUserFromRoleStatus.UserNotFound:
                return NotFound();

            case RemoveUserFromRoleStatus.Failed:
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
                await ReloadPageData(ct);
                return Page();

            default:
                TempData["Success"] = $"User '{result.UserName}' removed from role '{result.RoleName}'";
                return RedirectToPage(new { roleId = RoleId });
        }
    }

    private async Task ReloadPageData(CancellationToken ct)
    {
        var pageData = await _rolesAdminService.GetRoleForEditAsync(RoleId, ct);
        if (pageData != null)
        {
            PopulateFromPageData(pageData);
        }
    }

    private void PopulateFromPageData(RoleEditPageDataDto pageData)
    {
        RoleName = pageData.RoleName;
        UsersInRole = pageData.UsersInRole.ToList();
        AvailableUsers = pageData.AvailableUsers.ToList();
    }
}

#nullable enable
using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Admin.Users;

/// <summary>
/// Base page model for User Detail pages. Provides common user-loading logic,
/// authorization service injection, and self-detection helpers.
/// </summary>
public abstract class UserDetailPageModel : PageModel
{
    protected readonly UserManager<ApplicationUser> UserManager;
    protected readonly IAuthorizationService AuthorizationService;

    protected UserDetailPageModel(
        UserManager<ApplicationUser> userManager,
        IAuthorizationService authorizationService)
    {
        UserManager = userManager;
        AuthorizationService = authorizationService;
    }

    /// <summary>
    /// The user being edited.
    /// </summary>
    public ApplicationUser? TargetUser { get; protected set; }

    /// <summary>
    /// Loads the user by ID. Returns true if found, false otherwise.
    /// Sets TargetUser property when successful.
    /// </summary>
    protected async Task<bool> LoadUserAsync(string userId)
    {
        TargetUser = await UserManager.FindByIdAsync(userId);
        return TargetUser != null;
    }

    /// <summary>
    /// Returns true if the current authenticated user is editing their own account.
    /// </summary>
    protected bool IsSelfEdit()
    {
        if (TargetUser == null || User?.Identity?.Name == null)
        {
            return false;
        }

        return TargetUser.UserName == User.Identity.Name;
    }

    /// <summary>
    /// Checks if the current user is authorized for the specified policy.
    /// </summary>
    protected async Task<bool> IsAuthorizedAsync(string policyName)
    {
        var result = await AuthorizationService.AuthorizeAsync(User, null, policyName);
        return result.Succeeded;
    }
}

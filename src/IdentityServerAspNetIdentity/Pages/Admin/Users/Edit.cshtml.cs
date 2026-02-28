#nullable enable
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using IdentityServerAspNetIdentity.Models;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;

namespace IdentityServerAspNetIdentity.Pages.Admin.Users;

//[Authorize(Policy = UserPolicyConstants.UsersRead)]
public class EditModel : UserDetailPageModel
{
    private readonly IPersistedGrantStore _grantStore;
    private readonly IServerSideSessionStore? _sessionStore;
    private readonly IUserEditor _userEditor;

    public EditModel(
        UserManager<ApplicationUser> userManager,
        IAuthorizationService authorizationService,
        IPersistedGrantStore grantStore,
        IUserEditor userEditor,
        IServerSideSessionStore? sessionStore = null)
        : base(userManager, authorizationService)
    {
        _grantStore = grantStore;
        _userEditor = userEditor;
        _sessionStore = sessionStore;
    }

    [BindProperty(SupportsGet = true)]
    public string UserId { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string? Tab { get; set; }

    [BindProperty]
    public ProfileViewModel Input { get; set; } = default!;

    public UserEditPageDataDto UserData { get; private set; } = new();

    public bool CanReadUsers { get; private set; }
    public bool CanWriteUsers { get; private set; }
    public bool CanDeleteUsers { get; private set; }

    public bool CanReadClaims { get; private set; }
    public bool CanWriteClaims { get; private set; }
    public bool CanDeleteClaims { get; private set; }

    public bool CanReadRoles { get; private set; }
    public bool CanWriteRoles { get; private set; }
    public bool CanDeleteRoles { get; private set; }

    public bool CanReadGrants { get; private set; }
    public bool CanDeleteGrants { get; private set; }
    public bool CanReadSessions { get; private set; }
    public bool CanDeleteSessions { get; private set; }

    [BindProperty]
    public string? NewPassword { get; set; }

    [BindProperty]
    public bool LockoutEnabledInput { get; set; }

    [BindProperty]
    public bool TwoFactorEnabledInput { get; set; }

    [BindProperty]
    public string? NewClaimType { get; set; }

    [BindProperty]
    public string? NewClaimValue { get; set; }

    [BindProperty]
    public List<string> SelectedClaims { get; set; } = new();

    [BindProperty]
    public string? OldClaimType { get; set; }

    [BindProperty]
    public string? OldClaimValue { get; set; }

    [BindProperty]
    public string? ReplacementClaimType { get; set; }

    [BindProperty]
    public string? ReplacementClaimValue { get; set; }

    [BindProperty]
    public List<string> SelectedRolesToAdd { get; set; } = new();

    [BindProperty]
    public List<string> SelectedRolesToRemove { get; set; } = new();

    [BindProperty]
    public string? GrantKey { get; set; }

    [BindProperty]
    public string? SessionKey { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await LoadUserAsync(UserId))
        {
            return NotFound();
        }

        if (!await PopulatePageDataFromServiceAsync())
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string userId)
    {
        if (!await IsAuthorizedAsync(UserPolicyConstants.UsersWrite))
        {
            return Forbid();
        }

        var effectiveUserId = !string.IsNullOrWhiteSpace(userId)
            ? userId
            : (!string.IsNullOrWhiteSpace(UserId) ? UserId : Input.UserId);
        if (string.IsNullOrWhiteSpace(effectiveUserId))
        {
            return BadRequest();
        }

        UserId = effectiveUserId;
        Tab = "profile";

        if (!await LoadUserAsync(UserId))
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return await ReturnPageForTabAsync("profile", preserveProfileInput: true);
        }

        var updateResult = await _userEditor.UpdateUserFromEditPostAsync(BuildPostedUserUpdateRequest(
            includeProfile: true,
            includePassword: !string.IsNullOrWhiteSpace(NewPassword),
            includeLockout: HasPostedField(nameof(LockoutEnabledInput)),
            includeTwoFactor: HasPostedField(nameof(TwoFactorEnabledInput))));
        var failedResult = await HandleUserEditorUpdateFailureAsync(updateResult, "profile", preserveProfileInput: true);
        if (failedResult is not null)
        {
            return failedResult;
        }

        TempData["Success"] = "User updated successfully";
        return RedirectToSelf("profile");
    }

    public async Task<IActionResult> OnPostDeleteAsync(string userId)
    {
        if (!await IsAuthorizedAsync(UserPolicyConstants.UsersDelete))
        {
            return Forbid();
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            UserId = userId;
        }

        if (string.IsNullOrWhiteSpace(UserId))
        {
            return BadRequest();
        }

        if (!await LoadUserAsync(UserId))
        {
            return NotFound();
        }

        if (IsSelfEdit())
        {
            return BadRequest();
        }

        var result = await UserManager.DeleteAsync(TargetUser!);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return await ReturnPageForTabAsync("profile");
        }

        TempData["Success"] = "User deleted successfully";
        return Redirect("/Admin/Users");
    }

    public async Task<IActionResult> OnPostClaimsAddAsync()
    {
        if (!await IsAuthorizedAsync(UserPolicyConstants.UserClaimsWrite))
            return Forbid();

        if (!await LoadUserAsync(UserId))
            return NotFound();

        if (string.IsNullOrWhiteSpace(NewClaimType))
        {
            ModelState.AddModelError(nameof(NewClaimType), "Claim type is required");
            return await ReturnPageForTabAsync("claims");
        }

        var claim = new Claim(NewClaimType, NewClaimValue ?? string.Empty);
        var result = await UserManager.AddClaimAsync(TargetUser!, claim);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return await ReturnPageForTabAsync("claims");
        }

        TempData["Success"] = "Claim added successfully";
        return RedirectToSelf("claims");
    }

    public async Task<IActionResult> OnPostClaimsRemoveAsync()
    {
        if (!await IsAuthorizedAsync(UserPolicyConstants.UserClaimsDelete))
            return Forbid();

        if (!await LoadUserAsync(UserId))
            return NotFound();

        var claimsToRemove = SelectedClaims
            .Select(s =>
            {
                var idx = s.IndexOf(':');
                return idx >= 0
                    ? new Claim(s[..idx], s[(idx + 1)..])
                    : new Claim(s, string.Empty);
            })
            .ToList();

        if (!claimsToRemove.Any())
        {
            ModelState.AddModelError(string.Empty, "Select at least one claim to remove");
            return await ReturnPageForTabAsync("claims");
        }

        var result = await UserManager.RemoveClaimsAsync(TargetUser!, claimsToRemove);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return await ReturnPageForTabAsync("claims");
        }

        TempData["Success"] = $"{claimsToRemove.Count} claim(s) removed";
        return RedirectToSelf("claims");
    }

    public async Task<IActionResult> OnPostClaimsReplaceAsync()
    {
        if (!await IsAuthorizedAsync(UserPolicyConstants.UserClaimsWrite))
            return Forbid();

        if (!await LoadUserAsync(UserId))
            return NotFound();

        if (string.IsNullOrWhiteSpace(OldClaimType) || string.IsNullOrWhiteSpace(ReplacementClaimType))
        {
            ModelState.AddModelError(string.Empty, "Claim type is required");
            return await ReturnPageForTabAsync("claims");
        }

        var oldClaim = new Claim(OldClaimType, OldClaimValue ?? string.Empty);
        var newClaim = new Claim(ReplacementClaimType, ReplacementClaimValue ?? string.Empty);

        var result = await UserManager.ReplaceClaimAsync(TargetUser!, oldClaim, newClaim);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return await ReturnPageForTabAsync("claims");
        }

        TempData["Success"] = "Claim replaced successfully";
        return RedirectToSelf("claims");
    }

    public async Task<IActionResult> OnPostRolesAddAsync()
    {
        if (!await IsAuthorizedAsync(UserPolicyConstants.UserRolesWrite))
            return Forbid();

        if (!await LoadUserAsync(UserId))
            return NotFound();

        if (!SelectedRolesToAdd.Any())
        {
            ModelState.AddModelError(string.Empty, "Select at least one role");
            return await ReturnPageForTabAsync("roles");
        }

        var result = await UserManager.AddToRolesAsync(TargetUser!, SelectedRolesToAdd);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return await ReturnPageForTabAsync("roles");
        }

        TempData["Success"] = $"Added to {SelectedRolesToAdd.Count} role(s)";
        return RedirectToSelf("roles");
    }

    public async Task<IActionResult> OnPostRolesRemoveAsync()
    {
        if (!await IsAuthorizedAsync(UserPolicyConstants.UserRolesDelete))
            return Forbid();

        if (!await LoadUserAsync(UserId))
            return NotFound();

        if (!SelectedRolesToRemove.Any())
        {
            ModelState.AddModelError(string.Empty, "Select at least one role to remove");
            return await ReturnPageForTabAsync("roles");
        }

        var result = await UserManager.RemoveFromRolesAsync(TargetUser!, SelectedRolesToRemove);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return await ReturnPageForTabAsync("roles");
        }

        TempData["Success"] = $"Removed from {SelectedRolesToRemove.Count} role(s)";
        return RedirectToSelf("roles");
    }

    public async Task<IActionResult> OnPostSecurityResetPasswordAsync()
    {
        if (!await IsAuthorizedAsync(UserPolicyConstants.UsersWrite))
            return Forbid();

        if (!await LoadUserAsync(UserId))
            return NotFound();

        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            ModelState.AddModelError(nameof(NewPassword), "New password is required");
            return await ReturnPageForTabAsync("security");
        }

        var updateResult = await _userEditor.UpdateUserFromEditPostAsync(BuildPostedUserUpdateRequest(
            includeProfile: true,
            includePassword: true));
        var failedResult = await HandleUserEditorUpdateFailureAsync(updateResult, "security", preserveProfileInput: true);
        if (failedResult is not null)
        {
            return failedResult;
        }

        TempData["Success"] = "Password reset successfully";
        return RedirectToSelf("security");
    }

    public async Task<IActionResult> OnPostSecurityDisableAccountAsync()
    {
        if (!await IsAuthorizedAsync(UserPolicyConstants.UsersWrite))
            return Forbid();

        if (!await LoadUserAsync(UserId))
            return NotFound();

        var result = await UserManager.SetLockoutEndDateAsync(TargetUser!, DateTimeOffset.MaxValue);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return await ReturnPageForTabAsync("security");
        }

        TempData["Success"] = "Account disabled";
        return RedirectToSelf("security");
    }

    public async Task<IActionResult> OnPostSecurityEnableAccountAsync()
    {
        if (!await IsAuthorizedAsync(UserPolicyConstants.UsersWrite))
            return Forbid();

        if (!await LoadUserAsync(UserId))
            return NotFound();

        var result = await UserManager.SetLockoutEndDateAsync(TargetUser!, null);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return await ReturnPageForTabAsync("security");
        }

        TempData["Success"] = "Account enabled";
        return RedirectToSelf("security");
    }

    public async Task<IActionResult> OnPostSecurityClearLockoutAsync()
    {
        if (!await IsAuthorizedAsync(UserPolicyConstants.UsersWrite))
            return Forbid();

        if (!await LoadUserAsync(UserId))
            return NotFound();

        await UserManager.SetLockoutEndDateAsync(TargetUser!, null);
        await UserManager.ResetAccessFailedCountAsync(TargetUser!);

        TempData["Success"] = "Lockout cleared";
        return RedirectToSelf("security");
    }

    public async Task<IActionResult> OnPostSecurityToggleLockoutEnabledAsync()
    {
        if (!await IsAuthorizedAsync(UserPolicyConstants.UsersWrite))
            return Forbid();

        if (!await LoadUserAsync(UserId))
            return NotFound();

        var updateResult = await _userEditor.UpdateUserFromEditPostAsync(BuildPostedUserUpdateRequest(
            includeProfile: true,
            includeLockout: true));
        var failedResult = await HandleUserEditorUpdateFailureAsync(updateResult, "security", preserveProfileInput: true);
        if (failedResult is not null)
        {
            return failedResult;
        }

        TempData["Success"] = LockoutEnabledInput ? "Lockout enabled" : "Lockout disabled";
        return RedirectToSelf("security");
    }

    public async Task<IActionResult> OnPostSecurityResetFailedCountAsync()
    {
        if (!await IsAuthorizedAsync(UserPolicyConstants.UsersWrite))
            return Forbid();

        if (!await LoadUserAsync(UserId))
            return NotFound();

        var result = await UserManager.ResetAccessFailedCountAsync(TargetUser!);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return await ReturnPageForTabAsync("security");
        }

        TempData["Success"] = "Failed access count reset";
        return RedirectToSelf("security");
    }

    public async Task<IActionResult> OnPostSecurityToggleTwoFactorAsync()
    {
        if (!await IsAuthorizedAsync(UserPolicyConstants.UsersWrite))
            return Forbid();

        if (!await LoadUserAsync(UserId))
            return NotFound();

        var updateResult = await _userEditor.UpdateUserFromEditPostAsync(BuildPostedUserUpdateRequest(
            includeProfile: true,
            includeTwoFactor: true));
        var failedResult = await HandleUserEditorUpdateFailureAsync(updateResult, "security", preserveProfileInput: true);
        if (failedResult is not null)
        {
            return failedResult;
        }

        TempData["Success"] = TwoFactorEnabledInput ? "Two-factor authentication enabled" : "Two-factor authentication disabled";
        return RedirectToSelf("security");
    }

    public async Task<IActionResult> OnPostSecurityResetAuthenticatorAsync()
    {
        if (!await IsAuthorizedAsync(UserPolicyConstants.UsersWrite))
            return Forbid();

        if (!await LoadUserAsync(UserId))
            return NotFound();

        var result = await UserManager.ResetAuthenticatorKeyAsync(TargetUser!);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return await ReturnPageForTabAsync("security");
        }

        TempData["Success"] = "Authenticator key reset";
        return RedirectToSelf("security");
    }

    public async Task<IActionResult> OnPostSecurityForceSignOutAsync()
    {
        if (!await IsAuthorizedAsync(UserPolicyConstants.UsersWrite))
            return Forbid();

        if (!await LoadUserAsync(UserId))
            return NotFound();

        var result = await UserManager.UpdateSecurityStampAsync(TargetUser!);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return await ReturnPageForTabAsync("security");
        }

        TempData["Success"] = "User has been signed out of all sessions";
        return RedirectToSelf("security");
    }

    public async Task<IActionResult> OnPostGrantsSessionsRevokeGrantAsync()
    {
        if (!await IsAuthorizedAsync(UserPolicyConstants.UserGrantsDelete))
            return Forbid();

        if (!await LoadUserAsync(UserId))
            return NotFound();

        if (!string.IsNullOrEmpty(GrantKey))
        {
            await _grantStore.RemoveAsync(GrantKey);
        }

        TempData["Success"] = "Grant revoked";
        return RedirectToSelf("grantssessions");
    }

    public async Task<IActionResult> OnPostGrantsSessionsRevokeAllGrantsAsync()
    {
        if (!await IsAuthorizedAsync(UserPolicyConstants.UserGrantsDelete))
            return Forbid();

        if (!await LoadUserAsync(UserId))
            return NotFound();

        await _grantStore.RemoveAllAsync(new PersistedGrantFilter { SubjectId = UserId });

        TempData["Success"] = "All grants revoked";
        return RedirectToSelf("grantssessions");
    }

    public async Task<IActionResult> OnPostGrantsSessionsEndSessionAsync()
    {
        if (!await IsAuthorizedAsync(UserPolicyConstants.UserSessionsDelete))
            return Forbid();

        if (!await LoadUserAsync(UserId))
            return NotFound();

        if (_sessionStore != null && !string.IsNullOrEmpty(SessionKey))
        {
            await _sessionStore.DeleteSessionAsync(SessionKey);
        }

        TempData["Success"] = "Session ended";
        return RedirectToSelf("grantssessions");
    }

    public async Task<IActionResult> OnPostGrantsSessionsEndAllSessionsAsync()
    {
        if (!await IsAuthorizedAsync(UserPolicyConstants.UserSessionsDelete))
            return Forbid();

        if (!await LoadUserAsync(UserId))
            return NotFound();

        if (_sessionStore != null)
        {
            await _sessionStore.DeleteSessionsAsync(new SessionFilter { SubjectId = UserId });
        }

        TempData["Success"] = "All sessions ended";
        return RedirectToSelf("grantssessions");
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private async Task<IActionResult?> HandleUserEditorUpdateFailureAsync(
        UserProfileUpdateResult updateResult,
        string tab,
        bool preserveProfileInput = false)
    {
        if (!updateResult.UserFound)
        {
            return NotFound();
        }

        if (updateResult.Result.Succeeded)
        {
            return null;
        }

        if (updateResult.Result.Errors.Any(e => e.Code == "ConcurrencyFailure"))
        {
            ModelState.AddModelError(string.Empty, "The user was modified by another administrator. Please reload and try again.");
        }

        AddIdentityErrors(updateResult.Result);
        return await ReturnPageForTabAsync(tab, preserveProfileInput);
    }

    private UserEditPostUpdateRequest BuildPostedUserUpdateRequest(
        bool includeProfile = false,
        bool includePassword = false,
        bool includeLockout = false,
        bool includeTwoFactor = false)
    {
        var request = new UserEditPostUpdateRequest
        {
            UserId = UserId
        };

        if (includeProfile && Input is not null)
        {
            Input.UserId = UserId;
            request.Profile = ToUserProfileEditViewModel(Input);
        }

        if (includePassword)
        {
            request.NewPassword = NewPassword;
        }

        if (includeLockout)
        {
            request.LockoutEnabled = LockoutEnabledInput;
        }

        if (includeTwoFactor)
        {
            request.TwoFactorEnabled = TwoFactorEnabledInput;
        }

        return request;
    }

    private bool HasPostedField(string fieldName)
    {
        return Request.HasFormContentType && Request.Form.ContainsKey(fieldName);
    }

    private async Task<bool> PopulatePageDataFromServiceAsync(bool preserveProfileInput = false)
    {
        var lookupUserId = !string.IsNullOrWhiteSpace(UserId) ? UserId : TargetUser?.Id;
        if (string.IsNullOrWhiteSpace(lookupUserId))
        {
            return false;
        }

        await PopulateAuthorizationFlagsAsync();

        var userEditData = await _userEditor.GetUserEditPageDataAsync(new UserEditPageDataRequest
        {
            UserId = lookupUserId,
            IncludeUserTabData = CanReadUsers,
            IncludeClaims = CanReadClaims,
            IncludeRoles = CanReadRoles,
            IncludeGrants = CanReadGrants,
            IncludeSessions = CanReadSessions
        });
        if (userEditData is null)
        {
            return false;
        }

        ApplyUserEditPageData(userEditData, preserveProfileInput);
        return true;
    }

    private async Task PopulateAuthorizationFlagsAsync()
    {
        CanReadUsers = await IsAuthorizedAsync(UserPolicyConstants.UsersRead);
        CanWriteUsers = await IsAuthorizedAsync(UserPolicyConstants.UsersWrite);
        CanDeleteUsers = await IsAuthorizedAsync(UserPolicyConstants.UsersDelete);

        CanReadClaims = await IsAuthorizedAsync(UserPolicyConstants.UserClaimsRead);
        CanWriteClaims = await IsAuthorizedAsync(UserPolicyConstants.UserClaimsWrite);
        CanDeleteClaims = await IsAuthorizedAsync(UserPolicyConstants.UserClaimsDelete);

        CanReadRoles = await IsAuthorizedAsync(UserPolicyConstants.UserRolesRead);
        CanWriteRoles = await IsAuthorizedAsync(UserPolicyConstants.UserRolesWrite);
        CanDeleteRoles = await IsAuthorizedAsync(UserPolicyConstants.UserRolesDelete);

        CanReadGrants = await IsAuthorizedAsync(UserPolicyConstants.UserGrantsRead);
        CanDeleteGrants = await IsAuthorizedAsync(UserPolicyConstants.UserGrantsDelete);
        CanReadSessions = await IsAuthorizedAsync(UserPolicyConstants.UserSessionsRead);
        CanDeleteSessions = await IsAuthorizedAsync(UserPolicyConstants.UserSessionsDelete);
    }

    private void ApplyUserEditPageData(UserEditPageDataDto data, bool preserveProfileInput)
    {
        UserId = data.Profile.UserId;
        UserData = data;

        if (!preserveProfileInput || Input is null || string.IsNullOrWhiteSpace(Input.UserId))
        {
            Input = ToProfileViewModel(data.Profile);
        }
    }

    private IActionResult RedirectToSelf(string tab)
    {
        Tab = tab;
        return RedirectToPage("/Admin/Users/Edit", new { userId = UserId, tab });
    }

    private async Task<IActionResult> ReturnPageForTabAsync(string tab, bool preserveProfileInput = false)
    {
        Tab = tab;

        if (!await PopulatePageDataFromServiceAsync(preserveProfileInput))
        {
            return NotFound();
        }

        return Page();
    }

    private static ProfileViewModel ToProfileViewModel(UserProfileEditViewModel userProfile)
    {
        return new ProfileViewModel
        {
            UserId = userProfile.UserId,
            Username = userProfile.Username,
            Email = userProfile.Email,
            EmailConfirmed = userProfile.EmailConfirmed,
            PhoneNumber = userProfile.PhoneNumber,
            PhoneNumberConfirmed = userProfile.PhoneNumberConfirmed,
            ConcurrencyStamp = userProfile.ConcurrencyStamp
        };
    }

    private static UserProfileEditViewModel ToUserProfileEditViewModel(ProfileViewModel input)
    {
        return new UserProfileEditViewModel
        {
            UserId = input.UserId,
            Username = input.Username,
            Email = input.Email,
            EmailConfirmed = input.EmailConfirmed,
            PhoneNumber = input.PhoneNumber,
            PhoneNumberConfirmed = input.PhoneNumberConfirmed,
            ConcurrencyStamp = input.ConcurrencyStamp
        };
    }
}

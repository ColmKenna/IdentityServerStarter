#nullable enable
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Admin.Claims;

[Authorize(Roles = "ADMIN")]
public class EditModel : PageModel
{
    private readonly IClaimsAdminService _claimsAdminService;

    public EditModel(IClaimsAdminService claimsAdminService)
    {
        _claimsAdminService = claimsAdminService;
    }

    [BindProperty(SupportsGet = true)]
    public string ClaimType { get; set; } = default!;

    public IList<ClaimUserAssignmentItemDto> UsersInClaim { get; private set; } = new List<ClaimUserAssignmentItemDto>();

    public IList<AvailableClaimUserItemDto> AvailableUsers { get; private set; } = new List<AvailableClaimUserItemDto>();

    [BindProperty]
    public string? SelectedUserId { get; set; }

    [BindProperty]
    public string? NewClaimValue { get; set; }

    [BindProperty]
    public string? RemoveUserId { get; set; }

    [BindProperty]
    public string? RemoveClaimValue { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(ClaimType))
        {
            return BadRequest();
        }

        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        var pageData = await _claimsAdminService.GetForEditAsync(ClaimType, NewClaimValue, cancellationToken);
        if (pageData is null)
        {
            return NotFound();
        }

        ApplyPageData(pageData);
        return Page();
    }

    public async Task<IActionResult> OnPostAddUserAsync()
    {
        if (string.IsNullOrWhiteSpace(ClaimType))
        {
            return BadRequest();
        }

        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        var pageData = await _claimsAdminService.GetForEditAsync(ClaimType, NewClaimValue, cancellationToken);
        if (pageData is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(SelectedUserId))
        {
            ModelState.AddModelError(nameof(SelectedUserId), "Please select a user");
            ApplyPageData(pageData);
            return Page();
        }

        if (string.IsNullOrWhiteSpace(NewClaimValue))
        {
            ModelState.AddModelError(nameof(NewClaimValue), "Claim value is required");
            ApplyPageData(pageData);
            return Page();
        }

        var result = await _claimsAdminService.AddUserToClaimAsync(
            ClaimType,
            SelectedUserId,
            NewClaimValue,
            cancellationToken);

        if (result.Status == AddClaimAssignmentStatus.UserNotFound)
        {
            return NotFound();
        }

        if (result.Status == AddClaimAssignmentStatus.AlreadyAssigned)
        {
            ModelState.AddModelError(string.Empty, "Selected user already has this claim type");
            ApplyPageData(pageData);
            return Page();
        }

        if (result.Status == AddClaimAssignmentStatus.IdentityFailure)
        {
            AddErrors(result.Errors);
            ApplyPageData(pageData);
            return Page();
        }

        TempData["Success"] = $"Claim '{ClaimType}' assigned to user '{result.UserName}'.";
        return RedirectToPage("/Admin/Claims/Edit", new { claimType = ClaimType });
    }

    public async Task<IActionResult> OnPostRemoveUserAsync()
    {
        if (string.IsNullOrWhiteSpace(ClaimType))
        {
            return BadRequest();
        }

        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        var pageData = await _claimsAdminService.GetForEditAsync(ClaimType, NewClaimValue, cancellationToken);
        if (pageData is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(RemoveUserId) || RemoveClaimValue is null)
        {
            ModelState.AddModelError(string.Empty, "Claim assignment details are required");
            ApplyPageData(pageData);
            return Page();
        }

        var result = await _claimsAdminService.RemoveUserFromClaimAsync(
            ClaimType,
            RemoveUserId,
            RemoveClaimValue,
            cancellationToken);

        if (result.Status == RemoveClaimAssignmentStatus.UserNotFound)
        {
            return NotFound();
        }

        if (result.Status == RemoveClaimAssignmentStatus.AssignmentNotFound)
        {
            ModelState.AddModelError(string.Empty, "The selected claim assignment does not exist");
            ApplyPageData(pageData);
            return Page();
        }

        if (result.Status == RemoveClaimAssignmentStatus.IdentityFailure)
        {
            AddErrors(result.Errors);
            ApplyPageData(pageData);
            return Page();
        }

        if (!result.HasRemainingAssignments)
        {
            TempData["Warning"] = $"Claim type '{ClaimType}' no longer has any assigned users and was removed from the system. Assign it to a user with a different value to keep this claim.";
            return RedirectToPage("/Admin/Claims/Index");
        }

        TempData["Success"] = $"Claim '{ClaimType}' removed from user '{result.UserName}'.";
        return RedirectToPage("/Admin/Claims/Edit", new { claimType = ClaimType });
    }

    private void ApplyPageData(ClaimEditPageDataDto pageData)
    {
        UsersInClaim = pageData.UsersInClaim.ToList();
        AvailableUsers = pageData.AvailableUsers.ToList();
        NewClaimValue = pageData.NewClaimValue;
    }

    private void AddErrors(IEnumerable<string> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }
    }
}

#nullable enable
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Admin.IdentityResources;

[Authorize(Roles = "ADMIN")]
public class EditModel : PageModel
{
    private readonly IIdentityResourcesAdminService _identityResourcesAdminService;

    public EditModel(IIdentityResourcesAdminService identityResourcesAdminService)
    {
        _identityResourcesAdminService = identityResourcesAdminService;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public IdentityResourceInputModel Input { get; set; } = new();

    [BindProperty]
    public string? SelectedClaimType { get; set; }

    [BindProperty]
    public string? RemoveClaimType { get; set; }

    public IList<string> AppliedUserClaims { get; private set; } = new List<string>();
    public IList<string> AvailableUserClaims { get; private set; } = new List<string>();

    public async Task<IActionResult> OnGetAsync()
    {
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;

        var editData = await _identityResourcesAdminService.GetForEditAsync(Id, cancellationToken);
        if (editData is null)
            return NotFound();

        Input = new IdentityResourceInputModel
        {
            Name = editData.Input.Name,
            DisplayName = editData.Input.DisplayName,
            Description = editData.Input.Description,
            Enabled = editData.Input.Enabled
        };

        AppliedUserClaims = editData.AppliedUserClaims.ToList();
        AvailableUserClaims = editData.AvailableUserClaims.ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;

        var existingData = await _identityResourcesAdminService.GetForEditAsync(Id, cancellationToken);
        if (existingData is null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            ApplyPageData(existingData, mapInput: false);
            return Page();
        }

        var updateResult = await _identityResourcesAdminService.UpdateAsync(Id, new UpdateIdentityResourceRequest
        {
            Name = Input.Name,
            DisplayName = Input.DisplayName,
            Description = Input.Description,
            Enabled = Input.Enabled
        }, cancellationToken);

        if (updateResult.Status == UpdateIdentityResourceStatus.NotFound)
            return NotFound();

        if (updateResult.Status == UpdateIdentityResourceStatus.DuplicateName)
        {
            ModelState.AddModelError("Input.Name", "An identity resource with this name already exists.");
            ApplyPageData(existingData, mapInput: false);
            return Page();
        }

        TempData["Success"] = "Identity resource updated successfully";
        return RedirectToPage("/Admin/IdentityResources/Edit", new { id = Id });
    }

    public async Task<IActionResult> OnPostAddClaimAsync()
    {
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;

        var existingData = await _identityResourcesAdminService.GetForEditAsync(Id, cancellationToken);
        if (existingData is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(SelectedClaimType))
        {
            ModelState.AddModelError(nameof(SelectedClaimType), "Please select a user claim");
            ApplyPageData(existingData, mapInput: true);
            return Page();
        }

        var addResult = await _identityResourcesAdminService.AddClaimAsync(Id, SelectedClaimType, cancellationToken);
        if (addResult.Status == AddIdentityResourceClaimStatus.NotFound)
        {
            return NotFound();
        }

        if (addResult.Status == AddIdentityResourceClaimStatus.AlreadyApplied)
        {
            ModelState.AddModelError(nameof(SelectedClaimType), "This user claim is already applied to the identity resource.");
            ApplyPageData(existingData, mapInput: true);
            return Page();
        }

        TempData["Success"] = $"User claim '{addResult.ClaimType}' added successfully";
        return RedirectToPage("/Admin/IdentityResources/Edit", new { id = Id });
    }

    public async Task<IActionResult> OnPostRemoveClaimAsync()
    {
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;

        var existingData = await _identityResourcesAdminService.GetForEditAsync(Id, cancellationToken);
        if (existingData is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(RemoveClaimType))
        {
            ModelState.AddModelError(nameof(RemoveClaimType), "Please select a user claim to remove");
            ApplyPageData(existingData, mapInput: true);
            return Page();
        }

        var removeResult = await _identityResourcesAdminService.RemoveClaimAsync(Id, RemoveClaimType, cancellationToken);
        if (removeResult.Status == RemoveIdentityResourceClaimStatus.NotFound)
        {
            return NotFound();
        }

        if (removeResult.Status == RemoveIdentityResourceClaimStatus.NotApplied)
        {
            ModelState.AddModelError(nameof(RemoveClaimType), "The selected user claim is not applied to this identity resource.");
            ApplyPageData(existingData, mapInput: true);
            return Page();
        }

        TempData["Success"] = $"User claim '{removeResult.ClaimType}' removed successfully";
        return RedirectToPage("/Admin/IdentityResources/Edit", new { id = Id });
    }

    private void ApplyPageData(IdentityResourceEditPageDataDto pageData, bool mapInput)
    {
        if (mapInput)
        {
            Input = new IdentityResourceInputModel
            {
                Name = pageData.Input.Name,
                DisplayName = pageData.Input.DisplayName,
                Description = pageData.Input.Description,
                Enabled = pageData.Input.Enabled
            };
        }

        AppliedUserClaims = pageData.AppliedUserClaims.ToList();
        AvailableUserClaims = pageData.AvailableUserClaims.ToList();
    }
}

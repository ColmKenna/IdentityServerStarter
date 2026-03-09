#nullable enable
using System.ComponentModel.DataAnnotations;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Admin.ApiScopes;

[Authorize(Roles = "ADMIN")]
public class EditModel : PageModel
{
    private readonly IApiScopesAdminService _apiScopesAdminService;

    public EditModel(IApiScopesAdminService apiScopesAdminService)
    {
        _apiScopesAdminService = apiScopesAdminService;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public bool IsCreateMode => Id == 0;

    [BindProperty]
    public ApiScopeInputModel Input { get; set; } = new();

    public IList<string> AppliedUserClaims { get; private set; } = new List<string>();
    public IList<string> AvailableUserClaims { get; private set; } = new List<string>();

    [BindProperty]
    public string? SelectedClaimType { get; set; }

    [BindProperty]
    public string? RemoveClaimType { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        if (IsCreateMode)
        {
            var createData = await _apiScopesAdminService.GetForCreateAsync(cancellationToken);
            ApplyPageData(createData, mapInput: true);
            return Page();
        }

        var editData = await _apiScopesAdminService.GetForEditAsync(Id, cancellationToken);
        if (editData is null)
        {
            return NotFound();
        }

        ApplyPageData(editData, mapInput: true);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        if (IsCreateMode)
        {
            if (!ModelState.IsValid)
            {
                var createPageData = await _apiScopesAdminService.GetForCreateAsync(cancellationToken);
                ApplyPageData(createPageData, mapInput: false);
                return Page();
            }

            var createResult = await _apiScopesAdminService.CreateAsync(new CreateApiScopeRequest
            {
                Name = Input.Name,
                DisplayName = Input.DisplayName,
                Description = Input.Description,
                Enabled = Input.Enabled
            }, cancellationToken);

            if (createResult.Status == CreateApiScopeStatus.DuplicateName)
            {
                ModelState.AddModelError("Input.Name", "An API scope with this name already exists.");
                var createPageData = await _apiScopesAdminService.GetForCreateAsync(cancellationToken);
                ApplyPageData(createPageData, mapInput: false);
                return Page();
            }

            TempData["Success"] = "API scope created successfully";
            return RedirectToPage("/Admin/ApiScopes/Edit", new { id = createResult.CreatedId });
        }

        var existingData = await _apiScopesAdminService.GetForEditAsync(Id, cancellationToken);
        if (existingData is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ApplyPageData(existingData, mapInput: false);
            return Page();
        }

        var updateResult = await _apiScopesAdminService.UpdateAsync(Id, new UpdateApiScopeRequest
        {
            Name = Input.Name,
            DisplayName = Input.DisplayName,
            Description = Input.Description,
            Enabled = Input.Enabled
        }, cancellationToken);

        if (updateResult.Status == UpdateApiScopeStatus.NotFound)
        {
            return NotFound();
        }

        if (updateResult.Status == UpdateApiScopeStatus.DuplicateName)
        {
            ModelState.AddModelError("Input.Name", "An API scope with this name already exists.");
            ApplyPageData(existingData, mapInput: false);
            return Page();
        }

        TempData["Success"] = "API scope updated successfully";
        return RedirectToPage("/Admin/ApiScopes/Edit", new { id = Id });
    }

    public async Task<IActionResult> OnPostAddClaimAsync()
    {
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        var existingData = await _apiScopesAdminService.GetForEditAsync(Id, cancellationToken);
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

        var addResult = await _apiScopesAdminService.AddClaimAsync(Id, SelectedClaimType, cancellationToken);
        if (addResult.Status == AddApiScopeClaimStatus.NotFound)
        {
            return NotFound();
        }

        if (addResult.Status == AddApiScopeClaimStatus.AlreadyApplied)
        {
            ModelState.AddModelError(nameof(SelectedClaimType), "This user claim is already applied to the API scope.");
            ApplyPageData(existingData, mapInput: true);
            return Page();
        }

        TempData["Success"] = $"User claim '{addResult.ClaimType}' added successfully";
        return RedirectToPage("/Admin/ApiScopes/Edit", new { id = Id });
    }

    public async Task<IActionResult> OnPostRemoveClaimAsync()
    {
        var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        var existingData = await _apiScopesAdminService.GetForEditAsync(Id, cancellationToken);
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

        var removeResult = await _apiScopesAdminService.RemoveClaimAsync(Id, RemoveClaimType, cancellationToken);
        if (removeResult.Status == RemoveApiScopeClaimStatus.NotFound)
        {
            return NotFound();
        }

        if (removeResult.Status == RemoveApiScopeClaimStatus.NotApplied)
        {
            ModelState.AddModelError(nameof(RemoveClaimType), "The selected user claim is not applied to this API scope.");
            ApplyPageData(existingData, mapInput: true);
            return Page();
        }

        TempData["Success"] = $"User claim '{removeResult.ClaimType}' removed successfully";
        return RedirectToPage("/Admin/ApiScopes/Edit", new { id = Id });
    }

    private void ApplyPageData(ApiScopeEditPageDataDto pageData, bool mapInput)
    {
        if (mapInput)
        {
            Input = new ApiScopeInputModel
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

    public class ApiScopeInputModel
    {
        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; } = default!;

        [Display(Name = "Display Name")]
        public string? DisplayName { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Enabled")]
        public bool Enabled { get; set; } = true;
    }
}

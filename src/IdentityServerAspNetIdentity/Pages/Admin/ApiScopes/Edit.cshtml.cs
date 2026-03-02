#nullable enable
using System.ComponentModel.DataAnnotations;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using IdentityServer.EF.DataAccess.DataMigrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerAspNetIdentity.Pages.Admin.ApiScopes;

[Authorize(Roles = "ADMIN")]
public class EditModel : PageModel
{
    private readonly ConfigurationDbContext _configurationDbContext;
    private readonly ApplicationDbContext _applicationDbContext;

    public EditModel(ConfigurationDbContext configurationDbContext, ApplicationDbContext applicationDbContext)
    {
        _configurationDbContext = configurationDbContext;
        _applicationDbContext = applicationDbContext;
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
        if (IsCreateMode)
        {
            Input = new ApiScopeInputModel
            {
                Name = string.Empty,
                Enabled = true
            };

            AppliedUserClaims = new List<string>();
            AvailableUserClaims = new List<string>();
            return Page();
        }

        var apiScope = await GetApiScopeAsync(trackChanges: false);
        if (apiScope is null)
        {
            return NotFound();
        }

        await PopulatePageDataAsync(apiScope, mapInput: true);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (IsCreateMode)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var createScopeName = Input.Name.Trim();
            var createDuplicateNameExists = await _configurationDbContext.ApiScopes
                .AnyAsync(scope => scope.Name == createScopeName);
            if (createDuplicateNameExists)
            {
                ModelState.AddModelError("Input.Name", "An API scope with this name already exists.");
                return Page();
            }

            var entity = new ApiScope
            {
                Name = createScopeName,
                DisplayName = NormalizeOptional(Input.DisplayName),
                Description = NormalizeOptional(Input.Description),
                Enabled = Input.Enabled,
                ShowInDiscoveryDocument = true,
                Required = false,
                Emphasize = false,
                NonEditable = false,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            };

            _configurationDbContext.ApiScopes.Add(entity);
            await _configurationDbContext.SaveChangesAsync();

            TempData["Success"] = "API scope created successfully";
            return RedirectToPage("/Admin/ApiScopes/Edit", new { id = entity.Id });
        }

        var apiScope = await GetApiScopeAsync(trackChanges: true);
        if (apiScope is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulatePageDataAsync(apiScope, mapInput: false);
            return Page();
        }

        var scopeName = Input.Name.Trim();
        var duplicateNameExists = await _configurationDbContext.ApiScopes
            .AnyAsync(scope => scope.Name == scopeName && scope.Id != Id);
        if (duplicateNameExists)
        {
            ModelState.AddModelError("Input.Name", "An API scope with this name already exists.");
            await PopulatePageDataAsync(apiScope, mapInput: false);
            return Page();
        }

        apiScope.Name = scopeName;
        apiScope.DisplayName = NormalizeOptional(Input.DisplayName);
        apiScope.Description = NormalizeOptional(Input.Description);
        apiScope.Enabled = Input.Enabled;
        apiScope.Updated = DateTime.UtcNow;

        await _configurationDbContext.SaveChangesAsync();

        TempData["Success"] = "API scope updated successfully";
        return RedirectToPage("/Admin/ApiScopes/Edit", new { id = Id });
    }

    public async Task<IActionResult> OnPostAddClaimAsync()
    {
        var apiScope = await GetApiScopeAsync(trackChanges: true);
        if (apiScope is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(SelectedClaimType))
        {
            ModelState.AddModelError(nameof(SelectedClaimType), "Please select a user claim");
            await PopulatePageDataAsync(apiScope, mapInput: true);
            return Page();
        }

        var claimType = SelectedClaimType.Trim();
        if (apiScope.UserClaims.Any(claim => claim.Type == claimType))
        {
            ModelState.AddModelError(nameof(SelectedClaimType), "This user claim is already applied to the API scope.");
            await PopulatePageDataAsync(apiScope, mapInput: true);
            return Page();
        }

        apiScope.UserClaims.Add(new ApiScopeClaim
        {
            Type = claimType
        });
        apiScope.Updated = DateTime.UtcNow;

        await _configurationDbContext.SaveChangesAsync();

        TempData["Success"] = $"User claim '{claimType}' added successfully";
        return RedirectToPage("/Admin/ApiScopes/Edit", new { id = Id });
    }

    public async Task<IActionResult> OnPostRemoveClaimAsync()
    {
        var apiScope = await GetApiScopeAsync(trackChanges: true);
        if (apiScope is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(RemoveClaimType))
        {
            ModelState.AddModelError(nameof(RemoveClaimType), "Please select a user claim to remove");
            await PopulatePageDataAsync(apiScope, mapInput: true);
            return Page();
        }

        var claimType = RemoveClaimType.Trim();
        var claim = apiScope.UserClaims.FirstOrDefault(userClaim => userClaim.Type == claimType);
        if (claim is null)
        {
            ModelState.AddModelError(nameof(RemoveClaimType), "The selected user claim is not applied to this API scope.");
            await PopulatePageDataAsync(apiScope, mapInput: true);
            return Page();
        }

        apiScope.UserClaims.Remove(claim);
        apiScope.Updated = DateTime.UtcNow;

        await _configurationDbContext.SaveChangesAsync();

        TempData["Success"] = $"User claim '{claimType}' removed successfully";
        return RedirectToPage("/Admin/ApiScopes/Edit", new { id = Id });
    }

    private async Task<ApiScope?> GetApiScopeAsync(bool trackChanges)
    {
        var query = _configurationDbContext.ApiScopes
            .Include(scope => scope.UserClaims)
            .Where(scope => scope.Id == Id);

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync();
    }

    private async Task PopulatePageDataAsync(ApiScope apiScope, bool mapInput)
    {
        if (mapInput)
        {
            Input = new ApiScopeInputModel
            {
                Name = apiScope.Name,
                DisplayName = apiScope.DisplayName,
                Description = apiScope.Description,
                Enabled = apiScope.Enabled
            };
        }

        AppliedUserClaims = apiScope.UserClaims
            .Where(claim => !string.IsNullOrWhiteSpace(claim.Type))
            .Select(claim => claim.Type.Trim())
            .Distinct()
            .OrderBy(claimType => claimType)
            .ToList();

        var allUserClaimTypes = await _applicationDbContext.UserClaims
            .AsNoTracking()
            .Where(claim => claim.ClaimType != null && claim.ClaimType != string.Empty)
            .Select(claim => claim.ClaimType!)
            .Distinct()
            .OrderBy(claimType => claimType)
            .ToListAsync();

        AvailableUserClaims = allUserClaimTypes
            .Where(claimType => !AppliedUserClaims.Contains(claimType))
            .ToList();
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
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

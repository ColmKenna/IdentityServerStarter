using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IdentityServerServices;
using IdentityServerServices.ViewModels;

namespace IdentityServerAspNetIdentity.Pages.Admin.Clients;

[Authorize(Roles = "ADMIN")]
public class EditModel : PageModel
{
    private readonly IClientAdminService _clientAdminService;

    public EditModel(IClientAdminService clientAdminService)
    {
        _clientAdminService = clientAdminService;
    }

    [BindProperty]
    public ClientEditViewModel Input { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var client = await _clientAdminService.GetClientForEditAsync(Id);
        if (client == null)
        {
            return NotFound();
        }

        Input = client;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            // Re-populate available options in case of validation failure
            var existingClient = await _clientAdminService.GetClientForEditAsync(Id);
            if (existingClient != null)
            {
                Input.AvailableScopes = existingClient.AvailableScopes;
                Input.AvailableGrantTypes = existingClient.AvailableGrantTypes;
            }
            return Page();
        }

        var success = await _clientAdminService.UpdateClientAsync(Id, Input);
        if (!success)
        {
            return NotFound();
        }

        TempData["Success"] = "Client updated successfully";
        return RedirectToPage("/Admin/Clients/Edit", new { id = Id });
    }

    public IActionResult OnPostAddRedirectUri()
    {
        Input.RedirectUris.Add(string.Empty);
        return Page();
    }

    public IActionResult OnPostRemoveRedirectUri(int index)
    {
        if (index >= 0 && index < Input.RedirectUris.Count)
        {
            Input.RedirectUris.RemoveAt(index);
        }
        return Page();
    }

    public IActionResult OnPostAddPostLogoutRedirectUri()
    {
        Input.PostLogoutRedirectUris.Add(string.Empty);
        return Page();
    }

    public IActionResult OnPostRemovePostLogoutRedirectUri(int index)
    {
        if (index >= 0 && index < Input.PostLogoutRedirectUris.Count)
        {
            Input.PostLogoutRedirectUris.RemoveAt(index);
        }
        return Page();
    }

    public IActionResult OnPostAddAllowedScope()
    {
        Input.AllowedScopes.Add(string.Empty);
        return Page();
    }

    public IActionResult OnPostRemoveAllowedScope(int index)
    {
        if (index >= 0 && index < Input.AllowedScopes.Count)
        {
            Input.AllowedScopes.RemoveAt(index);
        }
        return Page();
    }

    public IActionResult OnPostAddGrantType()
    {
        Input.AllowedGrantTypes.Add(string.Empty);
        return Page();
    }

    public IActionResult OnPostRemoveGrantType(int index)
    {
        if (index >= 0 && index < Input.AllowedGrantTypes.Count)
        {
            Input.AllowedGrantTypes.RemoveAt(index);
        }
        return Page();
    }
}

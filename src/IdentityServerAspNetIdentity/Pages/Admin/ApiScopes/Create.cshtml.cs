#nullable enable
using System.ComponentModel.DataAnnotations;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerAspNetIdentity.Pages.Admin.ApiScopes;

[Authorize(Roles = "ADMIN")]
public class CreateModel : PageModel
{
    private readonly ConfigurationDbContext _context;

    public CreateModel(ConfigurationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ApiScopeInputModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var scopeName = Input.Name.Trim();
        if (await _context.ApiScopes.AnyAsync(scope => scope.Name == scopeName))
        {
            ModelState.AddModelError("Input.Name", "An API scope with this name already exists.");
            return Page();
        }

        var entity = new ApiScope
        {
            Name = scopeName,
            DisplayName = NormalizeOptional(Input.DisplayName),
            Description = NormalizeOptional(Input.Description),
            Enabled = Input.Enabled,
            ShowInDiscoveryDocument = true,
            Required = false,
            Emphasize = false,
            NonEditable = false,
            Created = DateTime.UtcNow
        };

        _context.ApiScopes.Add(entity);
        await _context.SaveChangesAsync();

        TempData["Success"] = "API scope created successfully";
        return RedirectToPage("/Admin/ApiScopes/Edit", new { id = entity.Id });
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

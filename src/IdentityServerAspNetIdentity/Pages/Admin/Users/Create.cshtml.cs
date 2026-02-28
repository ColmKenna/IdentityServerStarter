#nullable enable
using System.ComponentModel.DataAnnotations;
using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.Pages.Admin.Users;

[Authorize(Policy = UserPolicyConstants.UsersWrite)]
public class CreateModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public CreateUserInput Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = new ApplicationUser
        {
            UserName = Input.UserName,
            Email = Input.Email,
            EmailConfirmed = false
        };

        IdentityResult result;
        if (!string.IsNullOrEmpty(Input.Password))
        {
            result = await _userManager.CreateAsync(user, Input.Password);
        }
        else
        {
            result = await _userManager.CreateAsync(user);
        }

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        TempData["Success"] = $"User '{user.UserName}' created successfully";
        return RedirectToPage("/Admin/Users/Edit", new { userId = user.Id });
    }

    public class CreateUserInput
    {
        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; } = default!;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = default!;

        [Display(Name = "Password (optional)")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }
    }
}
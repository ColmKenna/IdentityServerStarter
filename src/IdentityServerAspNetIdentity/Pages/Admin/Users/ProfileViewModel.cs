#nullable enable
using System.ComponentModel.DataAnnotations;

namespace IdentityServerAspNetIdentity.Pages.Admin.Users;

public class ProfileViewModel
{
    [Required]
    public string UserId { get; set; } = default!;

    [Required]
    [StringLength(256)]
    [Display(Name = "Username")]
    public string Username { get; set; } = default!;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = default!;

    [Display(Name = "Email Confirmed")]
    public bool EmailConfirmed { get; set; }

    [Phone]
    [StringLength(50)]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Phone Number Confirmed")]
    public bool PhoneNumberConfirmed { get; set; }

    public string? ConcurrencyStamp { get; set; }
}

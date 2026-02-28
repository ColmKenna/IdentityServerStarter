namespace IdentityServerServices.ViewModels;

public class UserProfileEditViewModel
{
    public string UserId { get; set; } = default!;

    public string Username { get; set; } = default!;

    public string Email { get; set; } = default!;

    public bool EmailConfirmed { get; set; }

    public string? PhoneNumber { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public string? ConcurrencyStamp { get; set; }
}

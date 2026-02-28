namespace IdentityServerServices.ViewModels;

public class UserEditPostUpdateRequest
{
    public string UserId { get; set; } = default!;

    public UserProfileEditViewModel? Profile { get; set; }

    public string? NewPassword { get; set; }

    public bool? LockoutEnabled { get; set; }

    public bool? TwoFactorEnabled { get; set; }
}

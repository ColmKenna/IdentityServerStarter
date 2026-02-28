using Microsoft.AspNetCore.Identity;

namespace IdentityServerServices.ViewModels;

public class UserProfileUpdateResult
{
    public bool UserFound { get; init; }

    public IdentityResult Result { get; init; } = IdentityResult.Success;
}

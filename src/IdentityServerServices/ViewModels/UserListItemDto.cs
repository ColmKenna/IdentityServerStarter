namespace IdentityServerServices.ViewModels;

public sealed class UserListItemDto
{
    public string Id { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool EmailConfirmed { get; init; }
    public DateTimeOffset? LockoutEnd { get; init; }
    public bool TwoFactorEnabled { get; init; }

    public string LockoutStatus
    {
        get
        {
            if (LockoutEnd.HasValue && LockoutEnd.Value == DateTimeOffset.MaxValue)
                return "Disabled";
            if (LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow)
                return "Locked Out";
            return "Active";
        }
    }
}

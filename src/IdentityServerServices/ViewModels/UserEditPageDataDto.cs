using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace IdentityServerServices.ViewModels;

public class UserEditPageDataRequest
{
    public string UserId { get; set; } = default!;

    public bool IncludeUserTabData { get; set; }

    public bool IncludeClaims { get; set; }

    public bool IncludeRoles { get; set; }

    public bool IncludeGrants { get; set; }

    public bool IncludeSessions { get; set; }
}

public class UserEditPageDataDto
{
    public UserProfileEditViewModel Profile { get; init; } = new();

    public IList<Claim> Claims { get; init; } = [];

    public IList<string> AvailableClaims { get; init; } = [];

    public IList<string> Roles { get; init; } = [];

    public IList<string> AvailableRoles { get; init; } = [];

    public IList<UserLoginInfo> ExternalLogins { get; init; } = [];

    public IList<PersistedGrant> Grants { get; init; } = [];

    public IList<ServerSideSession> Sessions { get; init; } = [];

    public bool HasPassword { get; init; }

    public bool LockoutEnabled { get; init; }

    public int AccessFailedCount { get; init; }

    public bool TwoFactorEnabled { get; init; }

    public IList<string> TwoFactorProviders { get; init; } = [];

    public string AccountStatus { get; init; } = "Active";
}

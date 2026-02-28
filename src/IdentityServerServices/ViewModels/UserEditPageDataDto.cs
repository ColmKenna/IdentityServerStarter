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

    public IList<Claim> Claims { get; init; } = new List<Claim>();

    public IList<string> Roles { get; init; } = new List<string>();

    public IList<string> AvailableRoles { get; init; } = new List<string>();

    public IList<UserLoginInfo> ExternalLogins { get; init; } = new List<UserLoginInfo>();

    public IList<PersistedGrant> Grants { get; init; } = new List<PersistedGrant>();

    public IList<ServerSideSession> Sessions { get; init; } = new List<ServerSideSession>();

    public bool HasPassword { get; init; }

    public bool LockoutEnabled { get; init; }

    public int AccessFailedCount { get; init; }

    public bool TwoFactorEnabled { get; init; }

    public IList<string> TwoFactorProviders { get; init; } = new List<string>();

    public string AccountStatus { get; init; } = "Active";
}

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using IdentityServerAspNetIdentity.Models;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IdentityServerServices;

public class UserEditor : IUserEditor
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IPersistedGrantStore _grantStore;
    private readonly IServerSideSessionStore? _sessionStore;

    public UserEditor(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IPersistedGrantStore grantStore,
        IServerSideSessionStore? sessionStore = null)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _grantStore = grantStore;
        _sessionStore = sessionStore;
    }

    public async Task<UserEditPageDataDto?> GetUserEditPageDataAsync(UserEditPageDataRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return null;
        }

        var profile = Map(user);
        var claims = new List<Claim>();
        var roles = new List<string>();
        var availableRoles = new List<string>();
        var externalLogins = new List<UserLoginInfo>();
        var grants = new List<PersistedGrant>();
        var sessions = new List<ServerSideSession>();
        var hasPassword = false;
        var lockoutEnabled = false;
        var accessFailedCount = 0;
        var twoFactorEnabled = false;
        var twoFactorProviders = new List<string>();
        var accountStatus = "Active";

        if (request.IncludeUserTabData)
        {
            externalLogins = (await _userManager.GetLoginsAsync(user)).ToList();
            hasPassword = await _userManager.HasPasswordAsync(user);
            lockoutEnabled = user.LockoutEnabled;
            accessFailedCount = user.AccessFailedCount;
            twoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            twoFactorProviders = (await _userManager.GetValidTwoFactorProvidersAsync(user)).ToList();
            accountStatus = GetAccountStatus(user);
        }

        if (request.IncludeClaims)
        {
            claims = (await _userManager.GetClaimsAsync(user)).ToList();
        }

        if (request.IncludeRoles)
        {
            roles = (await _userManager.GetRolesAsync(user)).ToList();
            var allRoles = await _roleManager.Roles
                .Where(r => r.Name != null)
                .Select(r => r.Name!)
                .ToListAsync();
            availableRoles = allRoles.Except(roles).ToList();
        }

        if (request.IncludeGrants)
        {
            var persistedGrants = await _grantStore.GetAllAsync(new PersistedGrantFilter { SubjectId = user.Id });
            grants = persistedGrants.ToList();
        }

        if (request.IncludeSessions && _sessionStore != null)
        {
            var userSessions = await _sessionStore.GetSessionsAsync(new SessionFilter { SubjectId = user.Id });
            sessions = userSessions.ToList();
        }

        return new UserEditPageDataDto
        {
            Profile = profile,
            Claims = claims,
            Roles = roles,
            AvailableRoles = availableRoles,
            ExternalLogins = externalLogins,
            Grants = grants,
            Sessions = sessions,
            HasPassword = hasPassword,
            LockoutEnabled = lockoutEnabled,
            AccessFailedCount = accessFailedCount,
            TwoFactorEnabled = twoFactorEnabled,
            TwoFactorProviders = twoFactorProviders,
            AccountStatus = accountStatus
        };
    }

    public async Task<UserProfileEditViewModel?> GetUserForEditAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        return Map(user);
    }

    public async Task<UserProfileUpdateResult> UpdateUserFromEditPostAsync(UserEditPostUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return UserMissingResult("UserIdMissing", "User ID is required.");
        }

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return UserMissingResult("UserNotFound", "User not found.");
        }

        if (request.Profile is not null)
        {
            user.UserName = request.Profile.Username;
            user.Email = request.Profile.Email;
            user.EmailConfirmed = request.Profile.EmailConfirmed;
            user.PhoneNumber = request.Profile.PhoneNumber;
            user.PhoneNumberConfirmed = request.Profile.PhoneNumberConfirmed;

            if (!string.IsNullOrWhiteSpace(request.Profile.ConcurrencyStamp))
            {
                user.ConcurrencyStamp = request.Profile.ConcurrencyStamp;
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return new UserProfileUpdateResult
                {
                    UserFound = true,
                    Result = updateResult
                };
            }
        }

        if (request.LockoutEnabled.HasValue)
        {
            var lockoutResult = await _userManager.SetLockoutEnabledAsync(user, request.LockoutEnabled.Value);
            if (!lockoutResult.Succeeded)
            {
                return new UserProfileUpdateResult
                {
                    UserFound = true,
                    Result = lockoutResult
                };
            }
        }

        if (request.TwoFactorEnabled.HasValue)
        {
            var twoFactorResult = await _userManager.SetTwoFactorEnabledAsync(user, request.TwoFactorEnabled.Value);
            if (!twoFactorResult.Succeeded)
            {
                return new UserProfileUpdateResult
                {
                    UserFound = true,
                    Result = twoFactorResult
                };
            }
        }

        if (request.NewPassword is not null)
        {
            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return new UserProfileUpdateResult
                {
                    UserFound = true,
                    Result = IdentityResult.Failed(new IdentityError
                    {
                        Code = "PasswordMissing",
                        Description = "New password is required."
                    })
                };
            }

            if (await _userManager.HasPasswordAsync(user))
            {
                var removePasswordResult = await _userManager.RemovePasswordAsync(user);
                if (!removePasswordResult.Succeeded)
                {
                    return new UserProfileUpdateResult
                    {
                        UserFound = true,
                        Result = removePasswordResult
                    };
                }
            }

            var addPasswordResult = await _userManager.AddPasswordAsync(user, request.NewPassword);
            if (!addPasswordResult.Succeeded)
            {
                return new UserProfileUpdateResult
                {
                    UserFound = true,
                    Result = addPasswordResult
                };
            }
        }

        return new UserProfileUpdateResult
        {
            UserFound = true,
            Result = IdentityResult.Success
        };
    }

    public async Task<UserProfileUpdateResult> UpdateUserProfileAsync(UserProfileEditViewModel viewModel)
    {
        return await UpdateUserFromEditPostAsync(new UserEditPostUpdateRequest
        {
            UserId = viewModel.UserId,
            Profile = viewModel
        });
    }

    private static UserProfileEditViewModel Map(ApplicationUser user)
    {
        return new UserProfileEditViewModel
        {
            UserId = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            ConcurrencyStamp = user.ConcurrencyStamp
        };
    }

    private static string GetAccountStatus(ApplicationUser user)
    {
        var lockoutEnd = user.LockoutEnd;
        if (lockoutEnd.HasValue && lockoutEnd.Value == DateTimeOffset.MaxValue)
        {
            return "Disabled";
        }

        if (lockoutEnd.HasValue && lockoutEnd.Value > DateTimeOffset.UtcNow)
        {
            return $"Locked Out (until {lockoutEnd.Value:g})";
        }

        return "Active";
    }

    private static UserProfileUpdateResult UserMissingResult(string code, string description)
    {
        return new UserProfileUpdateResult
        {
            UserFound = false,
            Result = IdentityResult.Failed(new IdentityError
            {
                Code = code,
                Description = description
            })
        };
    }
}

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using IdentityServerAspNetIdentity.Models;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Security.Claims;
using IdentityServer.EF.DataAccess.DataMigrations;

namespace IdentityServerServices;

public class UserEditor(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IPersistedGrantStore grantStore,
    ApplicationDbContext dbContext,
    IServerSideSessionStore? sessionStore = null) : IUserEditor
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly IPersistedGrantStore _grantStore = grantStore;
    private readonly IServerSideSessionStore? _sessionStore = sessionStore;
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<IReadOnlyList<UserListItemDto>> GetUsersAsync(CancellationToken ct = default)
    {
        return await _userManager.Users
            .AsNoTracking()
            .Select(u => new UserListItemDto
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                EmailConfirmed = u.EmailConfirmed,
                LockoutEnd = u.LockoutEnd,
                TwoFactorEnabled = u.TwoFactorEnabled
            })
            .ToListAsync(ct);
    }

    public async Task<UserEditPageDataDto?> GetUserEditPageDataAsync(UserEditPageDataRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
            return null;

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
            return null;

        var (externalLogins, hasPassword, lockoutEnabled, accessFailedCount, twoFactorEnabled, twoFactorProviders, accountStatus) =
            request.IncludeUserTabData
                ? await FetchUserTabDataAsync(user)
                : (new List<UserLoginInfo>(), false, false, 0, false, new List<string>(), "Active");

        var (claims, availableClaims) =
            request.IncludeClaims
                ? await FetchClaimsDataAsync(user, ct)
                : (new List<Claim>(), new List<string>());

        var (roles, availableRoles) =
            request.IncludeRoles
                ? await FetchRolesDataAsync(user, ct)
                : (new List<string>(), new List<string>());

        var grants = request.IncludeGrants
            ? await FetchGrantsAsync(user)
            : [];

        var sessions = request.IncludeSessions
            ? await FetchSessionsAsync(user)
            : [];

        return new UserEditPageDataDto
        {
            Profile = Map(user),
            Claims = claims,
            AvailableClaims = availableClaims,
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

    private async Task<(List<UserLoginInfo> ExternalLogins, bool HasPassword, bool LockoutEnabled, int AccessFailedCount, bool TwoFactorEnabled, List<string> TwoFactorProviders, string AccountStatus)> FetchUserTabDataAsync(ApplicationUser user)
    {
        var externalLogins = (await _userManager.GetLoginsAsync(user)).ToList();
        var hasPassword = await _userManager.HasPasswordAsync(user);
        var twoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        var twoFactorProviders = (await _userManager.GetValidTwoFactorProvidersAsync(user)).ToList();
        return (externalLogins, hasPassword, user.LockoutEnabled, user.AccessFailedCount, twoFactorEnabled, twoFactorProviders, GetAccountStatus(user));
    }

    private async Task<(List<Claim> Claims, List<string> AvailableClaims)> FetchClaimsDataAsync(ApplicationUser user, CancellationToken ct)
    {
        var claims = (await _userManager.GetClaimsAsync(user)).ToList();
        var claimTypes = claims
            .Select(c => c.Type)
            .Where(type => !string.IsNullOrWhiteSpace(type))
            .ToList();

        var availableClaims = await _dbContext.UserClaims
            .AsNoTracking()
            .Where(c => c.UserId != user.Id && !string.IsNullOrEmpty(c.ClaimType))
            .Select(c => c.ClaimType!)
            .Where(claimType => !claimTypes.Contains(claimType))
            .Distinct()
            .OrderBy(claimType => claimType)
            .ToListAsync(ct);

        return (claims, availableClaims);
    }

    private async Task<(List<string> Roles, List<string> AvailableRoles)> FetchRolesDataAsync(ApplicationUser user, CancellationToken ct)
    {
        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        var allRoles = await _roleManager.Roles
            .Where(r => r.Name != null)
            .Select(r => r.Name!)
            .ToListAsync(ct);
        return (roles, allRoles.Except(roles).ToList());
    }

    private async Task<List<PersistedGrant>> FetchGrantsAsync(ApplicationUser user)
    {
        var persistedGrants = await _grantStore.GetAllAsync(new PersistedGrantFilter { SubjectId = user.Id });
        return [.. persistedGrants];
    }

    private async Task<List<ServerSideSession>> FetchSessionsAsync(ApplicationUser user)
    {
        if (_sessionStore is null)
            return [];
        var userSessions = await _sessionStore.GetSessionsAsync(new SessionFilter { SubjectId = user.Id });
        return [.. userSessions];
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
            return UserMissingResult("UserIdMissing", "User ID is required.");

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
            return UserMissingResult("UserNotFound", "User not found.");

        if (request.NewPassword is not null && string.IsNullOrWhiteSpace(request.NewPassword))
            return new UserProfileUpdateResult
            {
                UserFound = true,
                Result = IdentityResult.Failed(new IdentityError { Code = "PasswordMissing", Description = "New password is required." })
            };

        await using var transaction = await TryBeginTransactionAsync();
        var originalState = CaptureUserState(user);
        var userStateChanged = false;

        if (request.Profile is not null)
        {
            user.UserName = request.Profile.Username;
            user.Email = request.Profile.Email;
            user.EmailConfirmed = request.Profile.EmailConfirmed;
            user.PhoneNumber = request.Profile.PhoneNumber;
            user.PhoneNumberConfirmed = request.Profile.PhoneNumberConfirmed;

            if (!string.IsNullOrWhiteSpace(request.Profile.ConcurrencyStamp))
                user.ConcurrencyStamp = request.Profile.ConcurrencyStamp;

            if (FailIfFailed(await _userManager.UpdateAsync(user)) is { } updateFailure)
                return await RollbackAndReturnFailureAsync(transaction, user, originalState, userStateChanged, updateFailure);

            userStateChanged = true;
        }

        if (request.LockoutEnabled.HasValue)
        {
            if (FailIfFailed(await _userManager.SetLockoutEnabledAsync(user, request.LockoutEnabled.Value)) is { } lockoutFailure)
                return await RollbackAndReturnFailureAsync(transaction, user, originalState, userStateChanged, lockoutFailure);

            userStateChanged = true;
        }

        if (request.TwoFactorEnabled.HasValue)
        {
            if (FailIfFailed(await _userManager.SetTwoFactorEnabledAsync(user, request.TwoFactorEnabled.Value)) is { } twoFactorFailure)
                return await RollbackAndReturnFailureAsync(transaction, user, originalState, userStateChanged, twoFactorFailure);

            userStateChanged = true;
        }

        if (request.NewPassword is not null)
        {
            if (await UpdatePasswordAsync(user, request.NewPassword) is { } passwordFailure)
                return await RollbackAndReturnFailureAsync(transaction, user, originalState, userStateChanged, passwordFailure);
        }

        if (transaction is not null)
            await transaction.CommitAsync();

        return new UserProfileUpdateResult { UserFound = true, Result = IdentityResult.Success };
    }

    public Task<UserProfileUpdateResult> UpdateUserProfileAsync(UserProfileEditViewModel viewModel)
    {
        return UpdateUserFromEditPostAsync(new UserEditPostUpdateRequest
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

    private async Task<UserProfileUpdateResult?> UpdatePasswordAsync(ApplicationUser user, string newPassword)
    {
        if (await _userManager.HasPasswordAsync(user))
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            return FailIfFailed(await _userManager.ResetPasswordAsync(user, resetToken, newPassword));
        }

        return FailIfFailed(await _userManager.AddPasswordAsync(user, newPassword));
    }

    private async Task<UserProfileUpdateResult> RollbackAndReturnFailureAsync(
        IDbContextTransaction? transaction,
        ApplicationUser user,
        UserStateSnapshot originalState,
        bool userStateChanged,
        UserProfileUpdateResult failure)
    {
        if (transaction is not null)
        {
            await transaction.RollbackAsync();
            return failure;
        }

        if (userStateChanged)
            await RestoreUserStateAsync(user, originalState);

        return failure;
    }

    private static UserStateSnapshot CaptureUserState(ApplicationUser user)
    {
        return new UserStateSnapshot(
            user.UserName,
            user.Email,
            user.EmailConfirmed,
            user.PhoneNumber,
            user.PhoneNumberConfirmed,
            user.ConcurrencyStamp,
            user.LockoutEnabled,
            user.TwoFactorEnabled);
    }

    private async Task RestoreUserStateAsync(ApplicationUser user, UserStateSnapshot state)
    {
        user.UserName = state.UserName;
        user.Email = state.Email;
        user.EmailConfirmed = state.EmailConfirmed;
        user.PhoneNumber = state.PhoneNumber;
        user.PhoneNumberConfirmed = state.PhoneNumberConfirmed;
        user.ConcurrencyStamp = state.ConcurrencyStamp;
        user.LockoutEnabled = state.LockoutEnabled;
        user.TwoFactorEnabled = state.TwoFactorEnabled;

        await _userManager.UpdateAsync(user);
    }

    private async Task<IDbContextTransaction?> TryBeginTransactionAsync()
    {
        if (_dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            return null;

        if (_dbContext.Database.CurrentTransaction is not null)
            return null;

        return await _dbContext.Database.BeginTransactionAsync();
    }

    private static UserProfileUpdateResult? FailIfFailed(IdentityResult result)
        => result.Succeeded ? null : new UserProfileUpdateResult { UserFound = true, Result = result };

    private sealed record UserStateSnapshot(
        string? UserName,
        string? Email,
        bool EmailConfirmed,
        string? PhoneNumber,
        bool PhoneNumberConfirmed,
        string? ConcurrencyStamp,
        bool LockoutEnabled,
        bool TwoFactorEnabled);

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

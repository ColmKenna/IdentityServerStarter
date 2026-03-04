using System.Security.Claims;
using IdentityServer.EF.DataAccess.DataMigrations;
using IdentityServerAspNetIdentity.Models;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerServices;

public class ClaimsAdminService : IClaimsAdminService
{
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public ClaimsAdminService(
        ApplicationDbContext applicationDbContext,
        UserManager<ApplicationUser> userManager)
    {
        _applicationDbContext = applicationDbContext;
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<ClaimTypeListItemDto>> GetClaimsAsync(CancellationToken ct = default)
    {
        return await _applicationDbContext.UserClaims
            .AsNoTracking()
            .Where(claim => !string.IsNullOrEmpty(claim.ClaimType))
            .Select(claim => claim.ClaimType!)
            .Distinct()
            .OrderBy(claimType => claimType)
            .Select(claimType => new ClaimTypeListItemDto
            {
                ClaimType = claimType
            })
            .ToListAsync(ct);
    }

    public async Task<ClaimEditPageDataDto?> GetForEditAsync(
        string claimType,
        string? currentNewClaimValue = null,
        CancellationToken ct = default)
    {
        var assignments = await _applicationDbContext.UserClaims
            .AsNoTracking()
            .Where(claim => claim.ClaimType == claimType)
            .Select(claim => new
            {
                claim.UserId,
                ClaimValue = claim.ClaimValue ?? string.Empty
            })
            .ToListAsync(ct);

        if (assignments.Count == 0)
            return null;

        var users = await _userManager.Users.ToListAsync(ct);
        var usersById = users.ToDictionary(user => user.Id, user => user, StringComparer.Ordinal);

        var usersInClaim = assignments
            .Where(assignment => usersById.ContainsKey(assignment.UserId))
            .Select(assignment => new ClaimUserAssignmentItemDto
            {
                UserId = assignment.UserId,
                UserName = usersById[assignment.UserId].UserName ?? string.Empty,
                Email = usersById[assignment.UserId].Email ?? string.Empty,
                ClaimValue = assignment.ClaimValue
            })
            .OrderBy(assignment => assignment.UserName)
            .ThenBy(assignment => assignment.ClaimValue)
            .ToList();

        SetIsLastUserAssignment(usersInClaim);

        var assignedUserIds = usersInClaim
            .Select(assignment => assignment.UserId)
            .ToHashSet(StringComparer.Ordinal);

        var newClaimValue = ShouldDefaultNewClaimValue(currentNewClaimValue, assignments.Select(a => a.ClaimValue))
            ? bool.TrueString.ToLowerInvariant()
            : currentNewClaimValue;

        return new ClaimEditPageDataDto
        {
            UsersInClaim = usersInClaim,
            AvailableUsers = BuildAvailableUsers(users, assignedUserIds),
            NewClaimValue = newClaimValue
        };
    }

    private static void SetIsLastUserAssignment(List<ClaimUserAssignmentItemDto> usersInClaim)
    {
        var assignmentCountsByUser = usersInClaim
            .GroupBy(assignment => assignment.UserId)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        var uniqueUsersCount = assignmentCountsByUser.Count;
        foreach (var assignment in usersInClaim)
        {
            assignment.IsLastUserAssignment =
                uniqueUsersCount == 1 &&
                assignmentCountsByUser[assignment.UserId] == 1;
        }
    }

    private static List<AvailableClaimUserItemDto> BuildAvailableUsers(
        List<ApplicationUser> users,
        HashSet<string> assignedUserIds)
    {
        return users
            .Where(user => !assignedUserIds.Contains(user.Id))
            .OrderBy(user => user.UserName)
            .Select(user => new AvailableClaimUserItemDto
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty
            })
            .ToList();
    }

    public async Task<AddClaimAssignmentResult> AddUserToClaimAsync(
        string claimType,
        string selectedUserId,
        string claimValue,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(selectedUserId);
        if (user is null)
        {
            return new AddClaimAssignmentResult
            {
                Status = AddClaimAssignmentStatus.UserNotFound
            };
        }

        var alreadyAssigned = await _applicationDbContext.UserClaims
            .AnyAsync(claim => claim.UserId == selectedUserId && claim.ClaimType == claimType, ct);
        if (alreadyAssigned)
        {
            return new AddClaimAssignmentResult
            {
                Status = AddClaimAssignmentStatus.AlreadyAssigned
            };
        }

        var result = await _userManager.AddClaimAsync(user, new Claim(claimType, claimValue));
        if (!result.Succeeded)
        {
            return new AddClaimAssignmentResult
            {
                Status = AddClaimAssignmentStatus.IdentityFailure,
                Errors = result.Errors.Select(error => error.Description).ToList()
            };
        }

        return new AddClaimAssignmentResult
        {
            Status = AddClaimAssignmentStatus.Success,
            UserName = user.UserName ?? string.Empty
        };
    }

    public async Task<RemoveClaimAssignmentResult> RemoveUserFromClaimAsync(
        string claimType,
        string userId,
        string claimValue,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return new RemoveClaimAssignmentResult
            {
                Status = RemoveClaimAssignmentStatus.UserNotFound
            };
        }

        var assignmentExists = await _applicationDbContext.UserClaims.AnyAsync(claim =>
            claim.UserId == userId &&
            claim.ClaimType == claimType &&
            (claim.ClaimValue ?? string.Empty) == claimValue, ct);
        if (!assignmentExists)
        {
            return new RemoveClaimAssignmentResult
            {
                Status = RemoveClaimAssignmentStatus.AssignmentNotFound
            };
        }

        var result = await _userManager.RemoveClaimAsync(user, new Claim(claimType, claimValue));
        if (!result.Succeeded)
        {
            return new RemoveClaimAssignmentResult
            {
                Status = RemoveClaimAssignmentStatus.IdentityFailure,
                Errors = result.Errors.Select(error => error.Description).ToList()
            };
        }

        var hasRemainingAssignments = await _applicationDbContext.UserClaims
            .AnyAsync(claim => claim.ClaimType == claimType, ct);

        return new RemoveClaimAssignmentResult
        {
            Status = RemoveClaimAssignmentStatus.Success,
            UserName = user.UserName ?? string.Empty,
            HasRemainingAssignments = hasRemainingAssignments
        };
    }

    private static bool ShouldDefaultNewClaimValue(
        string? currentNewClaimValue,
        IEnumerable<string> claimValues)
    {
        if (!string.IsNullOrWhiteSpace(currentNewClaimValue))
        {
            return false;
        }

        var normalizedClaimValues = claimValues
            .Select(value => (value ?? string.Empty).Trim())
            .ToList();

        if (normalizedClaimValues.Count == 0)
        {
            return false;
        }

        return normalizedClaimValues.All(value => bool.TryParse(value, out _));
    }
}

using IdentityServerServices.ViewModels;

namespace IdentityServerServices;

public interface IClaimsAdminService
{
    Task<IReadOnlyList<ClaimTypeListItemDto>> GetClaimsAsync(CancellationToken ct = default);

    Task<ClaimEditPageDataDto?> GetForEditAsync(
        string claimType,
        string? currentNewClaimValue = null,
        CancellationToken ct = default);

    Task<AddClaimAssignmentResult> AddUserToClaimAsync(
        string claimType,
        string selectedUserId,
        string claimValue,
        CancellationToken ct = default);

    Task<RemoveClaimAssignmentResult> RemoveUserFromClaimAsync(
        string claimType,
        string userId,
        string claimValue,
        CancellationToken ct = default);
}

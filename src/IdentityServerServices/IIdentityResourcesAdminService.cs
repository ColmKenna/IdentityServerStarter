using IdentityServerServices.ViewModels;

namespace IdentityServerServices;

public interface IIdentityResourcesAdminService
{
    Task<IReadOnlyList<IdentityResourceListItemDto>> GetIdentityResourcesAsync(CancellationToken ct = default);
    Task<IdentityResourceEditPageDataDto?> GetForEditAsync(int id, CancellationToken ct = default);
    Task<UpdateIdentityResourceResultDto> UpdateAsync(int id, UpdateIdentityResourceRequest request, CancellationToken ct = default);
    Task<AddIdentityResourceClaimResult> AddClaimAsync(int id, string claimType, CancellationToken ct = default);
    Task<RemoveIdentityResourceClaimResult> RemoveClaimAsync(int id, string claimType, CancellationToken ct = default);
}

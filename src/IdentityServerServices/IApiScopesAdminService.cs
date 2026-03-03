using IdentityServerServices.ViewModels;

namespace IdentityServerServices;

public interface IApiScopesAdminService
{
    Task<IReadOnlyList<ApiScopeListItemDto>> GetApiScopesAsync(CancellationToken cancellationToken = default);
    Task<ApiScopeEditPageDataDto> GetForCreateAsync(CancellationToken ct = default);
    Task<ApiScopeEditPageDataDto?> GetForEditAsync(int id, CancellationToken ct = default);
    Task<CreateApiScopeResult> CreateAsync(CreateApiScopeRequest request, CancellationToken ct = default);
    Task<UpdateApiScopeResult> UpdateAsync(int id, UpdateApiScopeRequest request, CancellationToken ct = default);
    Task<AddApiScopeClaimResult> AddClaimAsync(int id, string claimType, CancellationToken ct = default);
    Task<RemoveApiScopeClaimResult> RemoveClaimAsync(int id, string claimType, CancellationToken ct = default);
}

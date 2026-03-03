using IdentityServerServices.ViewModels;

namespace IdentityServerServices;

public interface IIdentityResourcesAdminService
{
    Task<IReadOnlyList<IdentityResourceListItemDto>> GetIdentityResourcesAsync(CancellationToken ct = default);
}

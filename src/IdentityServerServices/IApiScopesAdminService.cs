using IdentityServerServices.ViewModels;

namespace IdentityServerServices;

public interface IApiScopesAdminService
{
    Task<IReadOnlyList<ApiScopeListItemDto>> GetApiScopesAsync(CancellationToken cancellationToken = default);
}

using IdentityServerServices.ViewModels;

namespace IdentityServerServices;

public interface IClientAdminService
{
    Task<IReadOnlyList<ClientListItemDto>> GetClientsAsync(CancellationToken ct = default);
    Task<ClientEditViewModel?> GetClientForEditAsync(int id, CancellationToken ct = default);
    Task<bool> UpdateClientAsync(int id, ClientEditViewModel viewModel, CancellationToken ct = default);
}
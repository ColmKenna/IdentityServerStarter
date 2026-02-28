using IdentityServerServices.ViewModels;

namespace IdentityServerServices;

public interface IClientAdminService
{
    Task<ClientEditViewModel?> GetClientForEditAsync(int id);
    Task<bool> UpdateClientAsync(int id, ClientEditViewModel viewModel);
}
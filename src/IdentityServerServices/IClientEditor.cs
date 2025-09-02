using IdentityServerServices.ViewModels;

namespace IdentityServerServices;

public interface IClientEditor
{
    Task<ClientEditViewModel?> GetClientForEditAsync(int id);
    Task<bool> UpdateClientAsync(int id, ClientEditViewModel viewModel);
}
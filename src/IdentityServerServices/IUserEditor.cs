using IdentityServerServices.ViewModels;

namespace IdentityServerServices;

public interface IUserEditor
{
    Task<UserEditPageDataDto?> GetUserEditPageDataAsync(UserEditPageDataRequest request);
    Task<UserProfileEditViewModel?> GetUserForEditAsync(string userId);
    Task<UserProfileUpdateResult> UpdateUserFromEditPostAsync(UserEditPostUpdateRequest request);
    Task<UserProfileUpdateResult> UpdateUserProfileAsync(UserProfileEditViewModel viewModel);
}

using IdentityServerServices.ViewModels;

namespace IdentityServerServices;

public interface IUserEditor
{
    Task<IReadOnlyList<UserListItemDto>> GetUsersAsync(CancellationToken ct = default);
    Task<UserEditPageDataDto?> GetUserEditPageDataAsync(UserEditPageDataRequest request, CancellationToken ct = default);
    Task<UserProfileEditViewModel?> GetUserForEditAsync(string userId);
    Task<UserProfileUpdateResult> UpdateUserFromEditPostAsync(UserEditPostUpdateRequest request);
    Task<UserProfileUpdateResult> UpdateUserProfileAsync(UserProfileEditViewModel viewModel);
}

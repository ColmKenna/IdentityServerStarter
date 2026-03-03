using IdentityServerServices.ViewModels;

namespace IdentityServerServices;

public interface IRolesAdminService
{
    Task<IReadOnlyList<RoleListItemDto>> GetRolesAsync(CancellationToken ct = default);
    Task<RoleEditPageDataDto?> GetRoleForEditAsync(string roleId, CancellationToken ct = default);
    Task<AddUserToRoleResult> AddUserToRoleAsync(string roleId, string userId, CancellationToken ct = default);
    Task<RemoveUserFromRoleResult> RemoveUserFromRoleAsync(string roleId, string userId, CancellationToken ct = default);
}

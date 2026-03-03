using IdentityServerAspNetIdentity.Models;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace IdentityServerServices;

public class RolesAdminService : IRolesAdminService
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public RolesAdminService(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public Task<IReadOnlyList<RoleListItemDto>> GetRolesAsync(CancellationToken ct = default)
    {
        var roles = _roleManager.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleListItemDto
            {
                Id = r.Id,
                Name = r.Name ?? string.Empty
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<RoleListItemDto>>(roles);
    }

    public async Task<RoleEditPageDataDto?> GetRoleForEditAsync(string roleId, CancellationToken ct = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
            return null;

        var roleName = role.Name ?? string.Empty;
        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
        var usersInRoleIds = usersInRole.Select(u => u.Id).ToHashSet();

        var usersInRoleList = usersInRole
            .OrderBy(u => u.UserName)
            .Select(u => new RoleUserDto
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty
            })
            .ToList();

        var allUsers = _userManager.Users.ToList();
        var availableUsers = allUsers
            .Where(u => !usersInRoleIds.Contains(u.Id))
            .OrderBy(u => u.UserName)
            .Select(u => new RoleUserDto
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty
            })
            .ToList();

        return new RoleEditPageDataDto
        {
            RoleName = roleName,
            UsersInRole = usersInRoleList,
            AvailableUsers = availableUsers
        };
    }

    public async Task<AddUserToRoleResult> AddUserToRoleAsync(string roleId, string userId, CancellationToken ct = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
            return new AddUserToRoleResult { Status = AddUserToRoleStatus.RoleNotFound };

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return new AddUserToRoleResult { Status = AddUserToRoleStatus.UserNotFound };

        var result = await _userManager.AddToRoleAsync(user, role.Name!);
        if (!result.Succeeded)
        {
            return new AddUserToRoleResult
            {
                Status = AddUserToRoleStatus.Failed,
                Errors = result.Errors.Select(e => e.Description).ToList()
            };
        }

        return new AddUserToRoleResult
        {
            Status = AddUserToRoleStatus.Success,
            UserName = user.UserName,
            RoleName = role.Name
        };
    }

    public async Task<RemoveUserFromRoleResult> RemoveUserFromRoleAsync(string roleId, string userId, CancellationToken ct = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
            return new RemoveUserFromRoleResult { Status = RemoveUserFromRoleStatus.RoleNotFound };

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return new RemoveUserFromRoleResult { Status = RemoveUserFromRoleStatus.UserNotFound };

        var result = await _userManager.RemoveFromRoleAsync(user, role.Name!);
        if (!result.Succeeded)
        {
            return new RemoveUserFromRoleResult
            {
                Status = RemoveUserFromRoleStatus.Failed,
                Errors = result.Errors.Select(e => e.Description).ToList()
            };
        }

        return new RemoveUserFromRoleResult
        {
            Status = RemoveUserFromRoleStatus.Success,
            UserName = user.UserName,
            RoleName = role.Name
        };
    }
}

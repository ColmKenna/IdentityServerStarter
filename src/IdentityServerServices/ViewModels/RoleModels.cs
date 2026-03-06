namespace IdentityServerServices.ViewModels;

public sealed class RoleListItemDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed class RoleEditPageDataDto
{
    public string RoleName { get; init; } = string.Empty;
    public IReadOnlyList<RoleUserDto> UsersInRole { get; init; } = [];
    public IReadOnlyList<RoleUserDto> AvailableUsers { get; init; } = [];
}

public sealed class RoleUserDto
{
    public string Id { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

public enum AddUserToRoleStatus { Success, RoleNotFound, UserNotFound, Failed }

public sealed class AddUserToRoleResult
{
    public AddUserToRoleStatus Status { get; init; }
    public string? UserName { get; init; }
    public string? RoleName { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

public enum RemoveUserFromRoleStatus { Success, RoleNotFound, UserNotFound, Failed }

public sealed class RemoveUserFromRoleResult
{
    public RemoveUserFromRoleStatus Status { get; init; }
    public string? UserName { get; init; }
    public string? RoleName { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

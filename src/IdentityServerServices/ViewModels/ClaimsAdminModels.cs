namespace IdentityServerServices.ViewModels;

public sealed class ClaimTypeListItemDto
{
    public string ClaimType { get; init; } = string.Empty;
}

public sealed class ClaimUserAssignmentItemDto
{
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string ClaimValue { get; init; } = string.Empty;
    public bool IsLastUserAssignment { get; set; }
}

public sealed class AvailableClaimUserItemDto
{
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

public sealed class ClaimEditPageDataDto
{
    public IReadOnlyList<ClaimUserAssignmentItemDto> UsersInClaim { get; init; } = Array.Empty<ClaimUserAssignmentItemDto>();
    public IReadOnlyList<AvailableClaimUserItemDto> AvailableUsers { get; init; } = Array.Empty<AvailableClaimUserItemDto>();
    public string? NewClaimValue { get; init; }
}

public enum AddClaimAssignmentStatus
{
    Success,
    UserNotFound,
    AlreadyAssigned,
    IdentityFailure
}

public sealed class AddClaimAssignmentResult
{
    public AddClaimAssignmentStatus Status { get; init; }
    public string UserName { get; init; } = string.Empty;
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}

public enum RemoveClaimAssignmentStatus
{
    Success,
    UserNotFound,
    AssignmentNotFound,
    IdentityFailure
}

public sealed class RemoveClaimAssignmentResult
{
    public RemoveClaimAssignmentStatus Status { get; init; }
    public string UserName { get; init; } = string.Empty;
    public bool HasRemainingAssignments { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}

namespace IdentityServerServices.ViewModels;

public sealed class ApiScopeEditInputDto
{
    public string Name { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public bool Enabled { get; init; } = true;
}

public sealed class ApiScopeEditPageDataDto
{
    public ApiScopeEditInputDto Input { get; init; } = new();
    public IReadOnlyList<string> AppliedUserClaims { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> AvailableUserClaims { get; init; } = Array.Empty<string>();
}

public sealed class CreateApiScopeRequest
{
    public string Name { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public bool Enabled { get; init; } = true;
}

public sealed class UpdateApiScopeRequest
{
    public string Name { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public bool Enabled { get; init; } = true;
}

public enum CreateApiScopeStatus
{
    Success,
    DuplicateName
}

public sealed class CreateApiScopeResult
{
    public CreateApiScopeStatus Status { get; init; }
    public int CreatedId { get; init; }
}

public enum UpdateApiScopeStatus
{
    Success,
    NotFound,
    DuplicateName
}

public sealed class UpdateApiScopeResult
{
    public UpdateApiScopeStatus Status { get; init; }
}

public enum AddApiScopeClaimStatus
{
    Success,
    NotFound,
    AlreadyApplied
}

public sealed class AddApiScopeClaimResult
{
    public AddApiScopeClaimStatus Status { get; init; }
    public string? ClaimType { get; init; }
}

public enum RemoveApiScopeClaimStatus
{
    Success,
    NotFound,
    NotApplied
}

public sealed class RemoveApiScopeClaimResult
{
    public RemoveApiScopeClaimStatus Status { get; init; }
    public string? ClaimType { get; init; }
}

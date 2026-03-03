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

    public static CreateApiScopeResult DuplicateName () => new() { Status = CreateApiScopeStatus.DuplicateName };
    public static CreateApiScopeResult Success(int createdId) => new() { Status = CreateApiScopeStatus.Success, CreatedId = createdId };
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

    public static UpdateApiScopeResult NotFound() => new() { Status = UpdateApiScopeStatus.NotFound };
    public static UpdateApiScopeResult DuplicateName() => new() { Status = UpdateApiScopeStatus.DuplicateName };
    public static UpdateApiScopeResult Success() => new() { Status = UpdateApiScopeStatus.Success };
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

    public static AddApiScopeClaimResult NotFound() => new() { Status = AddApiScopeClaimStatus.NotFound };
    public static AddApiScopeClaimResult AlreadyApplied(string claimType) => new() { Status = AddApiScopeClaimStatus.AlreadyApplied, ClaimType = claimType };
    public static AddApiScopeClaimResult Success(string claimType) => new() { Status = AddApiScopeClaimStatus.Success, ClaimType = claimType };
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

    public static RemoveApiScopeClaimResult NotFound() => new() { Status = RemoveApiScopeClaimStatus.NotFound };
    public static RemoveApiScopeClaimResult NotApplied(string claimType) => new() { Status = RemoveApiScopeClaimStatus.NotApplied, ClaimType = claimType };
    public static RemoveApiScopeClaimResult Success(string claimType) => new() { Status = RemoveApiScopeClaimStatus.Success, ClaimType = claimType };
}

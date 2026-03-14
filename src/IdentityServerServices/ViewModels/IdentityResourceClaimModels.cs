namespace IdentityServerServices.ViewModels;

public enum AddIdentityResourceClaimStatus
{
    Success,
    NotFound,
    AlreadyApplied
}

public sealed class AddIdentityResourceClaimResult
{
    public AddIdentityResourceClaimStatus Status { get; init; }
    public string ClaimType { get; init; } = string.Empty;

    public static AddIdentityResourceClaimResult NotFound() => new() { Status = AddIdentityResourceClaimStatus.NotFound };
    public static AddIdentityResourceClaimResult AlreadyApplied(string claimType) => new() { Status = AddIdentityResourceClaimStatus.AlreadyApplied, ClaimType = claimType };
    public static AddIdentityResourceClaimResult Success(string claimType) => new() { Status = AddIdentityResourceClaimStatus.Success, ClaimType = claimType };
}

public enum RemoveIdentityResourceClaimStatus
{
    Success,
    NotFound,
    NotApplied
}

public sealed class RemoveIdentityResourceClaimResult
{
    public RemoveIdentityResourceClaimStatus Status { get; init; }
    public string ClaimType { get; init; } = string.Empty;

    public static RemoveIdentityResourceClaimResult NotFound() => new() { Status = RemoveIdentityResourceClaimStatus.NotFound };
    public static RemoveIdentityResourceClaimResult NotApplied(string claimType) => new() { Status = RemoveIdentityResourceClaimStatus.NotApplied, ClaimType = claimType };
    public static RemoveIdentityResourceClaimResult Success(string claimType) => new() { Status = RemoveIdentityResourceClaimStatus.Success, ClaimType = claimType };
}

namespace IdentityServerServices.ViewModels;

public sealed class UpdateIdentityResourceRequest
{
    public string Name { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public bool Enabled { get; init; }
}

public sealed class UpdateIdentityResourceResultDto
{
    public UpdateIdentityResourceStatus Status { get; init; }
}

public enum UpdateIdentityResourceStatus
{
    Success,
    NotFound,
    DuplicateName
}

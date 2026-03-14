namespace IdentityServerServices.ViewModels;

public sealed class IdentityResourceEditPageDataDto
{
    public IdentityResourceInputModel Input { get; init; } = new();
    public IReadOnlyList<string> AppliedUserClaims { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> AvailableUserClaims { get; init; } = Array.Empty<string>();
}

public sealed class IdentityResourceInputModel
{
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public bool Enabled { get; set; } = true;
}

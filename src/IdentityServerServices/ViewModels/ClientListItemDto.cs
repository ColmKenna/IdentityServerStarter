namespace IdentityServerServices.ViewModels;

public sealed class ClientListItemDto
{
    public int Id { get; init; }
    public string ClientId { get; init; } = string.Empty;
    public string ClientName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool Enabled { get; init; }
}

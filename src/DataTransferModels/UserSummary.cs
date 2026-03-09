namespace DataTransferModels;

public class UserSummary
{
    public required string Id { get; set; }
    public required string UserName { get; set; }
    public string? Email { get; set; }
}
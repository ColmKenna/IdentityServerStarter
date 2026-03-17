namespace DataTransferModels
{
    public class ClientDtModel
    {
        public required string ClientId { get; set; }
        public required List<string> AllowedGrantTypes { get; set; }
        public required List<string> RedirectUris { get; set; }
        public required List<string> PostLogoutRedirectUris { get; set; }
        public bool AllowOfflineAccess { get; set; }
        public required List<string> AllowedScopes { get; set; }
        public string? Name { get; set; }
        public required string Description { get; set; }
    }
}
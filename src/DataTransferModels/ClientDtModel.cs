using static System.Formats.Asn1.AsnWriter;

namespace DataTransferModels
{
    public class ClientDtModel
        {
            public string ClientId { get; set; }
            public List<string> AllowedGrantTypes { get; set; }
            public List<string> RedirectUris { get; set; }
            public List<string> PostLogoutRedirectUris { get; set; }
            public bool AllowOfflineAccess { get; set; }
            public List<string> AllowedScopes { get; set; }
            public string? Name { get; set; }
            public string Description { get; set; }
        }
}
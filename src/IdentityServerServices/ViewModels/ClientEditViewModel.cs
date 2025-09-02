using System.ComponentModel.DataAnnotations;

namespace IdentityServerServices.ViewModels;

public class ClientEditViewModel
{
    [Required]
    [Display(Name = "Client ID")]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Client Name")]
    public string ClientName { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Enabled")]
    public bool Enabled { get; set; } = true;

    [Display(Name = "Client URI")]
    [Url]
    public string? ClientUri { get; set; }

    [Display(Name = "Logo URI")]
    [Url]
    public string? LogoUri { get; set; }

    [Display(Name = "Require PKCE")]
    public bool RequirePkce { get; set; }

    [Display(Name = "Require Client Secret")]
    public bool RequireClientSecret { get; set; } = true;

    [Display(Name = "Require Consent")]
    public bool RequireConsent { get; set; } = false;

    [Display(Name = "Allow Offline Access")]
    public bool AllowOfflineAccess { get; set; }

    [Display(Name = "Front Channel Logout URI")]
    [Url]
    public string? FrontChannelLogoutUri { get; set; }

    [Display(Name = "Back Channel Logout URI")]
    [Url]
    public string? BackChannelLogoutUri { get; set; }

    [Display(Name = "Access Token Lifetime (seconds)")]
    [Range(1, int.MaxValue, ErrorMessage = "Access token lifetime must be positive")]
    public int AccessTokenLifetime { get; set; } = 3600;

    [Display(Name = "Identity Token Lifetime (seconds)")]
    [Range(1, int.MaxValue, ErrorMessage = "Identity token lifetime must be positive")]
    public int IdentityTokenLifetime { get; set; } = 300;

    [Display(Name = "Sliding Refresh Token Lifetime (seconds)")]
    [Range(1, int.MaxValue, ErrorMessage = "Sliding refresh token lifetime must be positive")]
    public int SlidingRefreshTokenLifetime { get; set; } = 1296000;

    [Display(Name = "Refresh Token Expiration")]
    public int RefreshTokenExpiration { get; set; } = 1; // Sliding

    [Display(Name = "Refresh Token Usage")]
    public int RefreshTokenUsage { get; set; } = 1; // ReUse

    [Display(Name = "Always Include User Claims in ID Token")]
    public bool AlwaysIncludeUserClaimsInIdToken { get; set; }

    [Display(Name = "Allowed Grant Types")]
    public List<string> AllowedGrantTypes { get; set; } = new();

    [Display(Name = "Redirect URIs")]
    public List<string> RedirectUris { get; set; } = new();

    [Display(Name = "Post Logout Redirect URIs")]
    public List<string> PostLogoutRedirectUris { get; set; } = new();

    [Display(Name = "Allowed Scopes")]
    public List<string> AllowedScopes { get; set; } = new();

    [Display(Name = "New Client Secret")]
    [DataType(DataType.Password)]
    public string? NewSecret { get; set; }

    [Display(Name = "New Secret Description")]
    public string? NewSecretDescription { get; set; }

    // Available options for dropdowns/checkboxes
    public List<string> AvailableScopes { get; set; } = new();
    public List<string> AvailableGrantTypes { get; set; } = new();
}

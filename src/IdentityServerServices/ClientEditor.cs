using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using IdentityModel;
using Duende.IdentityServer.Models;
using IdentityServerServices.ViewModels;

namespace IdentityServerServices;

public class ClientEditor : IClientEditor
{
    private readonly ConfigurationDbContext _context;

    public ClientEditor(ConfigurationDbContext context)
    {
        _context = context;
    }

    public async Task<ClientEditViewModel?> GetClientForEditAsync(int id)
    {
        var client = await _context.Clients
            .Include(c => c.AllowedGrantTypes)
            .Include(c => c.RedirectUris)
            .Include(c => c.PostLogoutRedirectUris)
            .Include(c => c.AllowedScopes)
            .Include(c => c.ClientSecrets)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client == null)
            return null;

        var viewModel = new ClientEditViewModel
        {
            ClientId = client.ClientId,
            ClientName = client.ClientName ?? string.Empty,
            Description = client.Description,
            Enabled = client.Enabled,
            ClientUri = client.ClientUri,
            LogoUri = client.LogoUri,
            RequirePkce = client.RequirePkce,
            RequireClientSecret = client.RequireClientSecret,
            RequireConsent = client.RequireConsent,
            AllowOfflineAccess = client.AllowOfflineAccess,
            FrontChannelLogoutUri = client.FrontChannelLogoutUri,
            BackChannelLogoutUri = client.BackChannelLogoutUri,
            AccessTokenLifetime = client.AccessTokenLifetime,
            IdentityTokenLifetime = client.IdentityTokenLifetime,
            SlidingRefreshTokenLifetime = client.SlidingRefreshTokenLifetime,
            RefreshTokenExpiration = client.RefreshTokenExpiration,
            RefreshTokenUsage = client.RefreshTokenUsage,
            AlwaysIncludeUserClaimsInIdToken = client.AlwaysIncludeUserClaimsInIdToken,
            AllowedGrantTypes = client.AllowedGrantTypes.Select(gt => gt.GrantType).ToList(),
            RedirectUris = client.RedirectUris.Select(ru => ru.RedirectUri).ToList(),
            PostLogoutRedirectUris = client.PostLogoutRedirectUris.Select(pru => pru.PostLogoutRedirectUri).ToList(),
            AllowedScopes = client.AllowedScopes.Select(s => s.Scope).ToList()
        };

        // Populate available options
        await PopulateAvailableOptionsAsync(viewModel);

        return viewModel;
    }

    public async Task<bool> UpdateClientAsync(int id, ClientEditViewModel viewModel)
    {
        var client = await _context.Clients
            .Include(c => c.AllowedGrantTypes)
            .Include(c => c.RedirectUris)
            .Include(c => c.PostLogoutRedirectUris)
            .Include(c => c.AllowedScopes)
            .Include(c => c.ClientSecrets)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client == null)
            return false;

        // Update scalar properties
        client.ClientId = viewModel.ClientId;
        client.ClientName = viewModel.ClientName;
        client.Description = viewModel.Description;
        client.Enabled = viewModel.Enabled;
        client.ClientUri = viewModel.ClientUri;
        client.LogoUri = viewModel.LogoUri;
        client.RequirePkce = viewModel.RequirePkce;
        client.RequireClientSecret = viewModel.RequireClientSecret;
        client.RequireConsent = viewModel.RequireConsent;
        client.AllowOfflineAccess = viewModel.AllowOfflineAccess;
        client.FrontChannelLogoutUri = viewModel.FrontChannelLogoutUri;
        client.BackChannelLogoutUri = viewModel.BackChannelLogoutUri;
        client.AccessTokenLifetime = viewModel.AccessTokenLifetime;
        client.IdentityTokenLifetime = viewModel.IdentityTokenLifetime;
        client.SlidingRefreshTokenLifetime = viewModel.SlidingRefreshTokenLifetime;
        client.RefreshTokenExpiration = viewModel.RefreshTokenExpiration;
        client.RefreshTokenUsage = viewModel.RefreshTokenUsage;
        client.AlwaysIncludeUserClaimsInIdToken = viewModel.AlwaysIncludeUserClaimsInIdToken;

        // Update collections
        UpdateAllowedGrantTypes(client, viewModel.AllowedGrantTypes);
        UpdateRedirectUris(client, viewModel.RedirectUris);
        UpdatePostLogoutRedirectUris(client, viewModel.PostLogoutRedirectUris);
        UpdateAllowedScopes(client, viewModel.AllowedScopes);

        // Add new secret if provided
        if (!string.IsNullOrWhiteSpace(viewModel.NewSecret))
        {
            client.ClientSecrets.Add(new ClientSecret
            {
                Value = viewModel.NewSecret.Sha256(),
                Description = viewModel.NewSecretDescription ?? "Added via admin",
                Created = DateTime.UtcNow,
                Type = "SharedSecret"
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    private async Task PopulateAvailableOptionsAsync(ClientEditViewModel viewModel)
    {
        // Available scopes
        var identityResources = await _context.IdentityResources.Select(ir => ir.Name).ToListAsync();
        var apiScopes = await _context.ApiScopes.Select(aps => aps.Name).ToListAsync();
        
        viewModel.AvailableScopes = identityResources.Concat(apiScopes).ToList();

        // Available grant types
        viewModel.AvailableGrantTypes = new List<string>
        {
            "authorization_code",
            "client_credentials",
            "refresh_token",
            "implicit",
            "password",
            "hybrid",
            "device_flow"
        };
    }

    private void UpdateAllowedGrantTypes(Duende.IdentityServer.EntityFramework.Entities.Client client, List<string> newGrantTypes)
    {
        client.AllowedGrantTypes.Clear();
        foreach (var grantType in newGrantTypes.Where(gt => !string.IsNullOrWhiteSpace(gt)))
        {
            client.AllowedGrantTypes.Add(new ClientGrantType
            {
                GrantType = grantType.Trim()
            });
        }
    }

    private void UpdateRedirectUris(Duende.IdentityServer.EntityFramework.Entities.Client client, List<string> newUris)
    {
        client.RedirectUris.Clear();
        foreach (var uri in newUris.Where(u => !string.IsNullOrWhiteSpace(u)))
        {
            client.RedirectUris.Add(new ClientRedirectUri
            {
                RedirectUri = uri.Trim()
            });
        }
    }

    private void UpdatePostLogoutRedirectUris(Duende.IdentityServer.EntityFramework.Entities.Client client, List<string> newUris)
    {
        client.PostLogoutRedirectUris.Clear();
        foreach (var uri in newUris.Where(u => !string.IsNullOrWhiteSpace(u)))
        {
            client.PostLogoutRedirectUris.Add(new ClientPostLogoutRedirectUri
            {
                PostLogoutRedirectUri = uri.Trim()
            });
        }
    }

    private void UpdateAllowedScopes(Duende.IdentityServer.EntityFramework.Entities.Client client, List<string> newScopes)
    {
        client.AllowedScopes.Clear();
        foreach (var scope in newScopes.Where(s => !string.IsNullOrWhiteSpace(s)))
        {
            client.AllowedScopes.Add(new ClientScope
            {
                Scope = scope.Trim()
            });
        }
    }
}

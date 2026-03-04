using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.Models;
using Microsoft.EntityFrameworkCore;
using IdentityModel;
using IdentityServerServices.ViewModels;
using Client = Duende.IdentityServer.EntityFramework.Entities.Client;

namespace IdentityServerServices;

public class ClientAdminService : IClientAdminService
{
    private readonly ConfigurationDbContext _context;

    public ClientAdminService(ConfigurationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ClientListItemDto>> GetClientsAsync(CancellationToken ct = default)
    {
        return await _context.Clients
            .AsNoTracking()
            .OrderBy(c => c.ClientName)
            .Select(c => new ClientListItemDto
            {
                Id = c.Id,
                ClientId = c.ClientId,
                ClientName = c.ClientName ?? string.Empty,
                Description = c.Description,
                Enabled = c.Enabled
            })
            .ToListAsync(ct);
    }

    public async Task<ClientEditViewModel?> GetClientForEditAsync(int id)
    {
        var client = await ClientsWithIncludes()
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

        await PopulateAvailableOptionsAsync(viewModel);

        return viewModel;
    }

    public async Task<bool> UpdateClientAsync(int id, ClientEditViewModel viewModel)
    {
        var client = await ClientsWithIncludes()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client == null)
            return false;

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

        ReplaceCollection(client.AllowedGrantTypes, viewModel.AllowedGrantTypes, value => new ClientGrantType { GrantType = value });
        ReplaceCollection(client.RedirectUris, viewModel.RedirectUris, value => new ClientRedirectUri { RedirectUri = value });
        ReplaceCollection(client.PostLogoutRedirectUris, viewModel.PostLogoutRedirectUris, value => new ClientPostLogoutRedirectUri { PostLogoutRedirectUri = value });
        ReplaceCollection(client.AllowedScopes, viewModel.AllowedScopes, value => new ClientScope { Scope = value });

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

    private IQueryable<Client> ClientsWithIncludes()
    {
        return _context.Clients
            .Include(c => c.AllowedGrantTypes)
            .Include(c => c.RedirectUris)
            .Include(c => c.PostLogoutRedirectUris)
            .Include(c => c.AllowedScopes)
            .Include(c => c.ClientSecrets);
    }

    private async Task PopulateAvailableOptionsAsync(ClientEditViewModel viewModel)
    {
        var identityResources = await _context.IdentityResources.Select(ir => ir.Name).ToListAsync();
        var apiScopes = await _context.ApiScopes.Select(aps => aps.Name).ToListAsync();
        
        viewModel.AvailableScopes = identityResources.Concat(apiScopes).ToList();

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

    private static void ReplaceCollection<T>(ICollection<T> collection, List<string> newValues, Func<string, T> createEntity)
    {
        collection.Clear();
        foreach (var value in newValues.Where(v => !string.IsNullOrWhiteSpace(v)))
        {
            collection.Add(createEntity(value.Trim()));
        }
    }
}

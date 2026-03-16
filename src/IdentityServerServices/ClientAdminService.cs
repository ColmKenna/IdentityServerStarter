using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.Models;
using Microsoft.EntityFrameworkCore;
using IdentityModel;
using IdentityServerServices.ViewModels;
using Client = Duende.IdentityServer.EntityFramework.Entities.Client;

namespace IdentityServerServices;

public class ClientAdminService(ConfigurationDbContext context) : IClientAdminService
{
    private readonly ConfigurationDbContext _context = context;

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

    public async Task<ClientEditViewModel?> GetClientForEditAsync(int id, CancellationToken ct = default)
    {
        var client = await ClientsForRead()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

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
            AllowedGrantTypes = [.. client.AllowedGrantTypes.Select(gt => gt.GrantType)],
            RedirectUris = [.. client.RedirectUris.Select(ru => ru.RedirectUri)],
            PostLogoutRedirectUris = [.. client.PostLogoutRedirectUris.Select(pru => pru.PostLogoutRedirectUri)],
            AllowedScopes = [.. client.AllowedScopes.Select(s => s.Scope)]
        };

        await PopulateAvailableOptionsAsync(viewModel, ct);

        return viewModel;
    }

    public async Task<bool> UpdateClientAsync(int id, ClientEditViewModel viewModel, CancellationToken ct = default)
    {
        var client = await ClientsForWrite()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

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

        SyncCollection(client.AllowedGrantTypes, viewModel.AllowedGrantTypes, gt => gt.GrantType, value => new ClientGrantType { GrantType = value });
        SyncCollection(client.RedirectUris, viewModel.RedirectUris, ru => ru.RedirectUri, value => new ClientRedirectUri { RedirectUri = value });
        SyncCollection(client.PostLogoutRedirectUris, viewModel.PostLogoutRedirectUris, pru => pru.PostLogoutRedirectUri, value => new ClientPostLogoutRedirectUri { PostLogoutRedirectUri = value });
        SyncCollection(client.AllowedScopes, viewModel.AllowedScopes, s => s.Scope, value => new ClientScope { Scope = value });

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

        await _context.SaveChangesAsync(ct);
        return true;
    }

    private IQueryable<Client> ClientsForRead()
    {
        return _context.Clients
            .AsNoTracking()
            .AsSplitQuery()
            .Include(c => c.AllowedGrantTypes)
            .Include(c => c.RedirectUris)
            .Include(c => c.PostLogoutRedirectUris)
            .Include(c => c.AllowedScopes);
    }

    private IQueryable<Client> ClientsForWrite()
    {
        return _context.Clients
            .AsSplitQuery()
            .Include(c => c.AllowedGrantTypes)
            .Include(c => c.RedirectUris)
            .Include(c => c.PostLogoutRedirectUris)
            .Include(c => c.AllowedScopes)
            .Include(c => c.ClientSecrets);
    }

    private async Task PopulateAvailableOptionsAsync(ClientEditViewModel viewModel, CancellationToken ct)
    {
        var identityResources = await _context.IdentityResources.AsNoTracking().Select(ir => ir.Name).ToListAsync(ct);
        var apiScopes = await _context.ApiScopes.AsNoTracking().Select(aps => aps.Name).ToListAsync(ct);

        viewModel.AvailableScopes = [.. identityResources, .. apiScopes];

        viewModel.AvailableGrantTypes =
        [
            "authorization_code",
            "client_credentials",
            "refresh_token",
            "implicit",
            "password",
            "hybrid",
            "device_flow"
        ];
    }

    private static void SyncCollection<T>(
        ICollection<T> existing,
        List<string> newValues,
        Func<T, string> getValue,
        Func<string, T> createEntity)
    {
        var desired = newValues
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toRemove = existing
            .Where(e => !desired.Contains(getValue(e)))
            .ToList();

        foreach (var item in toRemove)
            existing.Remove(item);

        var current = existing
            .Select(getValue)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var value in desired.Where(v => !current.Contains(v)))
            existing.Add(createEntity(value));
    }
}

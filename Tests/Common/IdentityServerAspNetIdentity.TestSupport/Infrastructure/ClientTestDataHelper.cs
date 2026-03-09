using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Client = Duende.IdentityServer.EntityFramework.Entities.Client;
using ApiScopeEntity = Duende.IdentityServer.EntityFramework.Entities.ApiScope;
using IdentityResourceEntity = Duende.IdentityServer.EntityFramework.Entities.IdentityResource;

namespace IdentityServerAspNetIdentity.TestSupport.Infrastructure;

public static class ClientTestDataHelper
{
    public static async Task<int> SeedClientAsync(
        CustomWebApplicationFactory factory,
        string clientId,
        string? clientName = null,
        string? description = null,
        bool enabled = true,
        IReadOnlyList<string>? allowedGrantTypes = null,
        IReadOnlyList<string>? redirectUris = null,
        IReadOnlyList<string>? postLogoutRedirectUris = null,
        IReadOnlyList<string>? allowedScopes = null,
        string? clientUri = null,
        string? logoUri = null)
    {
        using var scope = factory.Services.CreateScope();
        var configurationDbContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

        var client = new Client
        {
            ClientId = clientId,
            ClientName = clientName ?? clientId,
            Description = description,
            Enabled = enabled,
            ClientUri = clientUri,
            LogoUri = logoUri,
            AllowedGrantTypes = [.. (allowedGrantTypes ?? []).Select(x => new ClientGrantType { GrantType = x })],
            RedirectUris = [.. (redirectUris ?? []).Select(x => new ClientRedirectUri { RedirectUri = x })],
            PostLogoutRedirectUris = [.. (postLogoutRedirectUris ?? []).Select(x => new ClientPostLogoutRedirectUri { PostLogoutRedirectUri = x })],
            AllowedScopes = [.. (allowedScopes ?? []).Select(x => new ClientScope { Scope = x })]
        };

        configurationDbContext.Clients.Add(client);
        await configurationDbContext.SaveChangesAsync();

        return client.Id;
    }

    public static async Task SeedIdentityResourcesAsync(
        CustomWebApplicationFactory factory,
        params string[] names)
    {
        if (names.Length == 0)
        {
            return;
        }

        using var scope = factory.Services.CreateScope();
        var configurationDbContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

        var existingNames = await configurationDbContext.IdentityResources
            .Select(x => x.Name)
            .ToListAsync();

        foreach (var name in names
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Distinct(StringComparer.Ordinal)
                     .Except(existingNames, StringComparer.Ordinal))
        {
            configurationDbContext.IdentityResources.Add(new IdentityResourceEntity { Name = name });
        }

        await configurationDbContext.SaveChangesAsync();
    }

    public static async Task SeedApiScopesAsync(
        CustomWebApplicationFactory factory,
        params string[] names)
    {
        if (names.Length == 0)
        {
            return;
        }

        using var scope = factory.Services.CreateScope();
        var configurationDbContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

        var existingNames = await configurationDbContext.ApiScopes
            .Select(x => x.Name)
            .ToListAsync();

        foreach (var name in names
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Distinct(StringComparer.Ordinal)
                     .Except(existingNames, StringComparer.Ordinal))
        {
            configurationDbContext.ApiScopes.Add(new ApiScopeEntity { Name = name });
        }

        await configurationDbContext.SaveChangesAsync();
    }
}

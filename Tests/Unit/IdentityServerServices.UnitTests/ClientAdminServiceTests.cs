using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.Models;
using FluentAssertions;
using IdentityServerServices.ViewModels;
using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Client = Duende.IdentityServer.EntityFramework.Entities.Client;
using IdentityResource = Duende.IdentityServer.EntityFramework.Entities.IdentityResource;
using ApiScope = Duende.IdentityServer.EntityFramework.Entities.ApiScope;

namespace IdentityServerServices.UnitTests;

public class ClientAdminServiceTests
{
    private static ConfigurationDbContext CreateDbContext()
    {
        var storeOptions = new ConfigurationStoreOptions();
        var options = new DbContextOptionsBuilder<ConfigurationDbContext>()
            .UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared")
            .Options;
            
        var context = new ConfigurationDbContext(options);
        context.StoreOptions = storeOptions;
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task GetClientsAsync_ShouldReturnMappedDtos_SortedByClientName()
    {
        await using var context = CreateDbContext();
        context.Clients.AddRange(
            new Client { ClientId = "C", ClientName = "Zebra", Description = null, Enabled = true },
            new Client { ClientId = "A", ClientName = "Apple", Description = "desc", Enabled = false },
            new Client { ClientId = "B", ClientName = null, Description = null, Enabled = true }
        );
        await context.SaveChangesAsync();
        var sut = new ClientAdminService(context);

        var result = await sut.GetClientsAsync();

        result.Should().HaveCount(3);
        result.Select(c => c.ClientName).Should().ContainInOrder("", "Apple", "Zebra");
        
        var apple = result.Single(c => c.ClientId == "A");
        apple.Description.Should().Be("desc");
        apple.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task GetClientForEditAsync_ShouldReturnNull_WhenClientNotFound()
    {
        await using var context = CreateDbContext();
        var sut = new ClientAdminService(context);

        var result = await sut.GetClientForEditAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetClientForEditAsync_ShouldExtractFlattenedCollections()
    {
        await using var context = CreateDbContext();
        
        var client = new Client
        {
            ClientId = "test-client",
            ClientName = "Test Client",
            AllowedGrantTypes = [new ClientGrantType { GrantType = "code" }],
            RedirectUris = [new ClientRedirectUri { RedirectUri = "https://localhost/callback" }],
            PostLogoutRedirectUris = [new ClientPostLogoutRedirectUri { PostLogoutRedirectUri = "https://localhost/signout" }],
            AllowedScopes = [new ClientScope { Scope = "openid" }, new ClientScope { Scope = "profile" }]
        };
        context.Clients.Add(client);
        
        context.IdentityResources.Add(new IdentityResource { Name = "openid" });
        context.ApiScopes.Add(new ApiScope { Name = "api1" });
        
        await context.SaveChangesAsync();
        
        var sut = new ClientAdminService(context);

        var result = await sut.GetClientForEditAsync(client.Id);

        result.Should().NotBeNull();
        result!.ClientId.Should().Be("test-client");
        result.AllowedGrantTypes.Should().ContainSingle().Which.Should().Be("code");
        result.RedirectUris.Should().ContainSingle().Which.Should().Be("https://localhost/callback");
        result.PostLogoutRedirectUris.Should().ContainSingle().Which.Should().Be("https://localhost/signout");
        result.AllowedScopes.Should().HaveCount(2).And.Contain("openid", "profile");
        
        result.AvailableScopes.Should().HaveCount(2).And.Contain("openid", "api1");
        result.AvailableGrantTypes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdateClientAsync_ShouldReturnFalse_WhenClientNotFound()
    {
        await using var context = CreateDbContext();
        var sut = new ClientAdminService(context);

        var result = await sut.UpdateClientAsync(999, new ClientEditViewModel());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateClientAsync_ShouldSyncCollectionsProperly_AndAddHashedSecret()
    {
        await using var context = CreateDbContext();
        var client = new Client
        {
            ClientId = "test-client",
            AllowedScopes = [new ClientScope { Scope = "openid" }, new ClientScope { Scope = "profile" }]
        };
        context.Clients.Add(client);
        await context.SaveChangesAsync();
        
        var sut = new ClientAdminService(context);
        
        var updateModel = new ClientEditViewModel
        {
            ClientId = "test-client-updated",
            ClientName = "Updated Name",
            AllowedScopes = ["openid", "api1"], // Removed 'profile', added 'api1'
            NewSecret = "MySuperSecretValue123",
            NewSecretDescription = "Primary Secret"
        };

        var result = await sut.UpdateClientAsync(client.Id, updateModel);

        result.Should().BeTrue();
        
        var updatedClient = await context.Clients
            .Include(c => c.AllowedScopes)
            .Include(c => c.ClientSecrets)
            .SingleAsync(c => c.Id == client.Id);
            
        updatedClient.ClientId.Should().Be("test-client-updated");
        updatedClient.ClientName.Should().Be("Updated Name");
        
        updatedClient.AllowedScopes.Select(s => s.Scope).Should().HaveCount(2).And.Contain("openid", "api1");
        
        updatedClient.ClientSecrets.Should().ContainSingle();
        var secret = updatedClient.ClientSecrets.Single();
        secret.Description.Should().Be("Primary Secret");
        secret.Type.Should().Be("SharedSecret");
        secret.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        secret.Value.Should().Be("MySuperSecretValue123".Sha256());
    }

    [Fact]
    public async Task UpdateClientAsync_ShouldSanitizeInput_AndHandleDuplicatesCaseInsensitively()
    {
        await using var context = CreateDbContext();
        var client = new Client
        {
            ClientId = "test-client",
            AllowedScopes = []
        };
        context.Clients.Add(client);
        await context.SaveChangesAsync();
        
        var sut = new ClientAdminService(context);
        
        var updateModel = new ClientEditViewModel
        {
            ClientId = "test-client",
            // "openid" and "  OPENID  " should be treated as the same
            AllowedScopes = ["openid", "  OPENID  ", "", "  "] 
        };

        await sut.UpdateClientAsync(client.Id, updateModel);

        var updatedClient = await context.Clients
            .Include(c => c.AllowedScopes)
            .SingleAsync(c => c.Id == client.Id);
            
        // This is expected to FAIL currently because StringComparer.Ordinal is used
        // and there is no .ToLowerInvariant() call
        updatedClient.AllowedScopes.Should().ContainSingle()
            .Which.Scope.Should().Be("openid");
    }
}

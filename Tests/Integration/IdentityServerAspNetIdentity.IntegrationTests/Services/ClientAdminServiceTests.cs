using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using FluentAssertions;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.IntegrationTests.Services;

public class ClientAdminServiceTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private IServiceScope _scope = default!;
    private ConfigurationDbContext _configDbContext = default!;
    private IClientAdminService _sut = default!;

    public ClientAdminServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync()
    {
        _scope = _factory.Services.CreateScope();
        _configDbContext = _scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        _sut = _scope.ServiceProvider.GetRequiredService<IClientAdminService>();

        _configDbContext.Clients.RemoveRange(_configDbContext.Clients);
        return _configDbContext.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task UpdateClientAsync_ShouldAddNewSecret_WhenNewSecretProvided()
    {
        // Arrange
        var client = new Client { ClientId = "test_client", ClientName = "Test Client" };
        _configDbContext.Clients.Add(client);
        await _configDbContext.SaveChangesAsync();

        var viewModel = new ClientEditViewModel
        {
            ClientId = "test_client",
            ClientName = "Test Client",
            NewSecret = "MySecret123!",
            NewSecretDescription = "Test Secret",
            AllowedGrantTypes = [],
            RedirectUris = [],
            PostLogoutRedirectUris = [],
            AllowedScopes = []
        };

        // Act
        var result = await _sut.UpdateClientAsync(client.Id, viewModel);

        // Assert
        result.Should().BeTrue();

        var updatedClient = await _configDbContext.Clients
            .Include(c => c.ClientSecrets)
            .FirstOrDefaultAsync(c => c.Id == client.Id);
            
        updatedClient.Should().NotBeNull();
        updatedClient!.ClientSecrets.Should().ContainSingle();
        var secret = updatedClient.ClientSecrets.First();

        // Value should be hashed, not raw
        secret.Value.Should().NotBe("MySecret123!");
        secret.Description.Should().Be("Test Secret");
    }

    [Fact]
    public async Task UpdateClientAsync_ShouldUpdateAllScalarPropertiesAndSyncCollections()
    {
        // Arrange
        var client = new Client 
        { 
            ClientId = "original_id", 
            ClientName = "Original Name",
            Enabled = false,
            RequirePkce = false,
            AccessTokenLifetime = 3600,
            AllowedScopes = [new ClientScope { Scope = "openid" }]
        };
        _configDbContext.Clients.Add(client);
        await _configDbContext.SaveChangesAsync();

        var viewModel = new ClientEditViewModel
        {
            ClientId = "updated_id",
            ClientName = "Updated Name",
            Description = "Updated Description",
            Enabled = true,
            ClientUri = "https://client.com",
            LogoUri = "https://client.com/logo.png",
            RequirePkce = true,
            RequireClientSecret = true,
            RequireConsent = true,
            AllowOfflineAccess = true,
            FrontChannelLogoutUri = "https://client.com/front",
            BackChannelLogoutUri = "https://client.com/back",
            AccessTokenLifetime = 7200,
            IdentityTokenLifetime = 600,
            SlidingRefreshTokenLifetime = 1200,
            RefreshTokenExpiration = 1, // Absolute
            RefreshTokenUsage = 1, // ReUse
            AlwaysIncludeUserClaimsInIdToken = true,
            AllowedGrantTypes = ["authorization_code", "client_credentials"],
            RedirectUris = ["https://client.com/callback"],
            PostLogoutRedirectUris = ["https://client.com/signout"],
            AllowedScopes = ["openid", "profile", "api1"],
            NewSecret = "NewSecret123",
            NewSecretDescription = "Admin Added"
        };

        // Act
        var result = await _sut.UpdateClientAsync(client.Id, viewModel);

        // Assert
        result.Should().BeTrue();

        // Clear tracker to ensure we fetch fresh from DB
        _configDbContext.ChangeTracker.Clear();

        var updatedClient = await _configDbContext.Clients
            .AsNoTracking()
            .Include(c => c.AllowedGrantTypes)
            .Include(c => c.RedirectUris)
            .Include(c => c.PostLogoutRedirectUris)
            .Include(c => c.AllowedScopes)
            .Include(c => c.ClientSecrets)
            .SingleAsync(c => c.Id == client.Id);

        updatedClient.ClientId.Should().Be(viewModel.ClientId);
        updatedClient.ClientName.Should().Be(viewModel.ClientName);
        updatedClient.Description.Should().Be(viewModel.Description);
        updatedClient.Enabled.Should().Be(viewModel.Enabled);
        updatedClient.ClientUri.Should().Be(viewModel.ClientUri);
        updatedClient.LogoUri.Should().Be(viewModel.LogoUri);
        updatedClient.RequirePkce.Should().Be(viewModel.RequirePkce);
        updatedClient.RequireClientSecret.Should().Be(viewModel.RequireClientSecret);
        updatedClient.RequireConsent.Should().Be(viewModel.RequireConsent);
        updatedClient.AllowOfflineAccess.Should().Be(viewModel.AllowOfflineAccess);
        updatedClient.FrontChannelLogoutUri.Should().Be(viewModel.FrontChannelLogoutUri);
        updatedClient.BackChannelLogoutUri.Should().Be(viewModel.BackChannelLogoutUri);
        updatedClient.AccessTokenLifetime.Should().Be(viewModel.AccessTokenLifetime);
        updatedClient.IdentityTokenLifetime.Should().Be(viewModel.IdentityTokenLifetime);
        updatedClient.SlidingRefreshTokenLifetime.Should().Be(viewModel.SlidingRefreshTokenLifetime);
        updatedClient.RefreshTokenExpiration.Should().Be(viewModel.RefreshTokenExpiration);
        updatedClient.RefreshTokenUsage.Should().Be(viewModel.RefreshTokenUsage);
        updatedClient.AlwaysIncludeUserClaimsInIdToken.Should().Be(viewModel.AlwaysIncludeUserClaimsInIdToken);

        updatedClient.AllowedGrantTypes.Select(x => x.GrantType).Should().BeEquivalentTo(viewModel.AllowedGrantTypes);
        updatedClient.RedirectUris.Select(x => x.RedirectUri).Should().BeEquivalentTo(viewModel.RedirectUris);
        updatedClient.PostLogoutRedirectUris.Select(x => x.PostLogoutRedirectUri).Should().BeEquivalentTo(viewModel.PostLogoutRedirectUris);
        updatedClient.AllowedScopes.Select(x => x.Scope).Should().BeEquivalentTo(viewModel.AllowedScopes);

        updatedClient.ClientSecrets.Should().ContainSingle(s => s.Description == "Admin Added");
    }

    [Fact]
    public async Task UpdateClientAsync_ShouldSyncCollections_WhenListsAreModified()
    {
        // Arrange
        var client = new Client 
        { 
            ClientId = "test_client2", 
            ClientName = "Test Client 2",
            RedirectUris = 
            [
                new ClientRedirectUri { RedirectUri = "https://old.com" },
                new ClientRedirectUri { RedirectUri = "https://keep.com" }
            ]
        };
        _configDbContext.Clients.Add(client);
        await _configDbContext.SaveChangesAsync();

        var viewModel = new ClientEditViewModel
        {
            ClientId = "test_client2",
            ClientName = "Test Client 2",
            RedirectUris = ["https://keep.com", "https://new.com"],
            AllowedGrantTypes = [],
            PostLogoutRedirectUris = [],
            AllowedScopes = []
        };

        // Act
        var result = await _sut.UpdateClientAsync(client.Id, viewModel);

        // Assert
        result.Should().BeTrue();

        var updatedClient = await _configDbContext.Clients
            .Include(c => c.RedirectUris)
            .FirstOrDefaultAsync(c => c.Id == client.Id);

        updatedClient.Should().NotBeNull();
        var uris = updatedClient!.RedirectUris.Select(r => r.RedirectUri).ToList();
        uris.Should().HaveCount(2);
        uris.Should().Contain("https://keep.com");
        uris.Should().Contain("https://new.com");
        uris.Should().NotContain("https://old.com");
    }
}

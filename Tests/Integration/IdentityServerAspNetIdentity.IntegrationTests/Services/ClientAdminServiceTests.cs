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

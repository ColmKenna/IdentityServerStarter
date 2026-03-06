using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Options;
using FluentAssertions;
using IdentityServerServices.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityServerServices.UnitTests;

public class ClientAdminServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ConfigurationDbContext> _options;

    public ClientAdminServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var storeOptions = new ConfigurationStoreOptions();
        _options = new DbContextOptionsBuilder<ConfigurationDbContext>()
            .UseSqlite(_connection)
            .UseApplicationServiceProvider(
                new ServiceCollection()
                    .AddSingleton(storeOptions)
                    .BuildServiceProvider())
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private ConfigurationDbContext CreateContext()
    {
        return new ConfigurationDbContext(_options);
    }

    private static Client CreateTestClient(string clientId = "test-client", string clientName = "Test Client")
    {
        return new Client
        {
            ClientId = clientId,
            ClientName = clientName,
            Enabled = true,
            RequirePkce = true,
            RequireClientSecret = true,
            AccessTokenLifetime = 3600,
            IdentityTokenLifetime = 300,
            SlidingRefreshTokenLifetime = 1296000,
            RefreshTokenExpiration = 1,
            RefreshTokenUsage = 1,
            AllowedGrantTypes =
            [
                new() { GrantType = "authorization_code" }
            ],
            RedirectUris =
            [
                new() { RedirectUri = "https://localhost/callback" }
            ],
            PostLogoutRedirectUris =
            [
                new() { PostLogoutRedirectUri = "https://localhost/signout" }
            ],
            AllowedScopes =
            [
                new() { Scope = "openid" },
                new() { Scope = "profile" }
            ]
        };
    }

    private async Task<int> SeedClientAsync(Client? client = null)
    {
        client ??= CreateTestClient();
        using var context = CreateContext();
        context.Clients.Add(client);
        await context.SaveChangesAsync();
        return client.Id;
    }

    private static ClientAdminService CreateService(ConfigurationDbContext context)
    {
        return new ClientAdminService(context);
    }

    private static ClientEditViewModel CreateDefaultEditViewModel()
    {
        return new ClientEditViewModel
        {
            ClientId = "test-client",
            ClientName = "Test Client",
            AllowedGrantTypes = [],
            RedirectUris = [],
            PostLogoutRedirectUris = [],
            AllowedScopes = []
        };
    }


    [Fact]
    public async Task GetClientForEditAsync_ReturnsNull_WhenClientDoesNotExist()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetClientForEditAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetClientForEditAsync_ReturnsViewModel_WhenClientExists()
    {
        var clientId = await SeedClientAsync();

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetClientForEditAsync(clientId);

        result.Should().NotBeNull();
        result!.ClientId.Should().Be("test-client");
        result.ClientName.Should().Be("Test Client");
        result.Description.Should().BeNull();
        result.Enabled.Should().BeTrue();
        result.RequirePkce.Should().BeTrue();
        result.AccessTokenLifetime.Should().Be(3600);
        result.IdentityTokenLifetime.Should().Be(300);
    }

    [Fact]
    public async Task GetClientForEditAsync_MapsGrantTypes()
    {
        var clientId = await SeedClientAsync();

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetClientForEditAsync(clientId);

        result!.AllowedGrantTypes.Should().ContainSingle("authorization_code");
    }

    [Fact]
    public async Task GetClientForEditAsync_MapsRedirectUris()
    {
        var clientId = await SeedClientAsync();

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetClientForEditAsync(clientId);

        result!.RedirectUris.Should().ContainSingle("https://localhost/callback");
    }

    [Fact]
    public async Task GetClientForEditAsync_MapsPostLogoutRedirectUris()
    {
        var clientId = await SeedClientAsync();

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetClientForEditAsync(clientId);

        result!.PostLogoutRedirectUris.Should().ContainSingle("https://localhost/signout");
    }

    [Fact]
    public async Task GetClientForEditAsync_MapsScopes()
    {
        var clientId = await SeedClientAsync();

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetClientForEditAsync(clientId);

        result!.AllowedScopes.Should().BeEquivalentTo("openid", "profile");
    }

    [Fact]
    public async Task GetClientForEditAsync_PopulatesAvailableGrantTypes()
    {
        var clientId = await SeedClientAsync();

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetClientForEditAsync(clientId);

        result!.AvailableGrantTypes.Should().Contain("authorization_code");
        result.AvailableGrantTypes.Should().Contain("client_credentials");
    }

    [Fact]
    public async Task GetClientForEditAsync_PopulatesAvailableScopes_FromIdentityResourcesAndApiScopes()
    {
        using (var context = CreateContext())
        {
            context.IdentityResources.Add(new IdentityResource { Name = "openid" });
            context.ApiScopes.Add(new ApiScope { Name = "api1" });
            await context.SaveChangesAsync();
        }

        var clientId = await SeedClientAsync();

        using (var context = CreateContext())
        {
            var service = CreateService(context);

            var result = await service.GetClientForEditAsync(clientId);

            result!.AvailableScopes.Should().Contain("openid");
            result.AvailableScopes.Should().Contain("api1");
        }
    }

    [Fact]
    public async Task GetClientForEditAsync_MapsNullClientName_ToEmptyString()
    {
        var client = CreateTestClient();
        client.ClientName = null;
        var clientId = await SeedClientAsync(client);

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetClientForEditAsync(clientId);

        result!.ClientName.Should().BeEmpty();
    }


    [Fact]
    public async Task UpdateClientAsync_ReturnsFalse_WhenClientDoesNotExist()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.UpdateClientAsync(999, new ClientEditViewModel());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateClientAsync_UpdatesScalarProperties()
    {
        var clientId = await SeedClientAsync();

        using (var context = CreateContext())
        {
            var service = CreateService(context);
            var viewModel = CreateDefaultEditViewModel();
            viewModel.ClientId = "updated-client";
            viewModel.ClientName = "Updated Client";
            viewModel.Description = "Updated description";
            viewModel.Enabled = false;
            viewModel.RequirePkce = false;
            viewModel.RequireClientSecret = false;
            viewModel.AccessTokenLifetime = 7200;
            viewModel.AllowedGrantTypes = ["client_credentials"];

            var result = await service.UpdateClientAsync(clientId, viewModel);
            result.Should().BeTrue();
        }

        using (var context = CreateContext())
        {
            var updated = await context.Clients.FirstAsync(c => c.Id == clientId);
            updated.ClientId.Should().Be("updated-client");
            updated.ClientName.Should().Be("Updated Client");
            updated.Description.Should().Be("Updated description");
            updated.Enabled.Should().BeFalse();
            updated.RequirePkce.Should().BeFalse();
            updated.RequireClientSecret.Should().BeFalse();
            updated.AccessTokenLifetime.Should().Be(7200);
        }
    }

    [Fact]
    public async Task UpdateClientAsync_ReplacesGrantTypes()
    {
        var clientId = await SeedClientAsync();

        using (var context = CreateContext())
        {
            var service = CreateService(context);
            var viewModel = CreateDefaultEditViewModel();
            viewModel.AllowedGrantTypes = ["client_credentials", "refresh_token"];

            await service.UpdateClientAsync(clientId, viewModel);
        }

        using (var context = CreateContext())
        {
            var updated = await context.Clients
                .Include(c => c.AllowedGrantTypes)
                .FirstAsync(c => c.Id == clientId);

            updated.AllowedGrantTypes.Select(g => g.GrantType)
                .Should().BeEquivalentTo("client_credentials", "refresh_token");
        }
    }

    [Fact]
    public async Task UpdateClientAsync_ReplacesRedirectUris()
    {
        var clientId = await SeedClientAsync();

        using (var context = CreateContext())
        {
            var service = CreateService(context);
            var viewModel = CreateDefaultEditViewModel();
            viewModel.RedirectUris = ["https://new/callback"];

            await service.UpdateClientAsync(clientId, viewModel);
        }

        using (var context = CreateContext())
        {
            var updated = await context.Clients
                .Include(c => c.RedirectUris)
                .FirstAsync(c => c.Id == clientId);

            updated.RedirectUris.Select(r => r.RedirectUri)
                .Should().ContainSingle("https://new/callback");
        }
    }

    [Fact]
    public async Task UpdateClientAsync_ReplacesPostLogoutRedirectUris()
    {
        var clientId = await SeedClientAsync();

        using (var context = CreateContext())
        {
            var service = CreateService(context);
            var viewModel = CreateDefaultEditViewModel();
            viewModel.PostLogoutRedirectUris = ["https://new/signout"];

            await service.UpdateClientAsync(clientId, viewModel);
        }

        using (var context = CreateContext())
        {
            var updated = await context.Clients
                .Include(c => c.PostLogoutRedirectUris)
                .FirstAsync(c => c.Id == clientId);

            updated.PostLogoutRedirectUris.Select(p => p.PostLogoutRedirectUri)
                .Should().ContainSingle("https://new/signout");
        }
    }

    [Fact]
    public async Task UpdateClientAsync_ReplacesScopes()
    {
        var clientId = await SeedClientAsync();

        using (var context = CreateContext())
        {
            var service = CreateService(context);
            var viewModel = CreateDefaultEditViewModel();
            viewModel.AllowedScopes = ["api1", "api2"];

            await service.UpdateClientAsync(clientId, viewModel);
        }

        using (var context = CreateContext())
        {
            var updated = await context.Clients
                .Include(c => c.AllowedScopes)
                .FirstAsync(c => c.Id == clientId);

            updated.AllowedScopes.Select(s => s.Scope)
                .Should().BeEquivalentTo("api1", "api2");
        }
    }

    [Fact]
    public async Task UpdateClientAsync_AddsNewSecret_WhenProvided()
    {
        var clientId = await SeedClientAsync();

        using (var context = CreateContext())
        {
            var service = CreateService(context);
            var viewModel = CreateDefaultEditViewModel();
            viewModel.NewSecret = "my-secret";
            viewModel.NewSecretDescription = "My test secret";

            await service.UpdateClientAsync(clientId, viewModel);
        }

        using (var context = CreateContext())
        {
            var updated = await context.Clients
                .Include(c => c.ClientSecrets)
                .FirstAsync(c => c.Id == clientId);

            updated.ClientSecrets.Should().ContainSingle();
            var secret = updated.ClientSecrets.First();
            secret.Description.Should().Be("My test secret");
            secret.Type.Should().Be("SharedSecret");
            secret.Value.Should().NotBe("my-secret");
        }
    }

    [Fact]
    public async Task UpdateClientAsync_UsesDefaultDescription_WhenSecretDescriptionIsNull()
    {
        var clientId = await SeedClientAsync();

        using (var context = CreateContext())
        {
            var service = CreateService(context);
            var viewModel = CreateDefaultEditViewModel();
            viewModel.NewSecret = "my-secret";
            viewModel.NewSecretDescription = null;

            await service.UpdateClientAsync(clientId, viewModel);
        }

        using (var context = CreateContext())
        {
            var updated = await context.Clients
                .Include(c => c.ClientSecrets)
                .FirstAsync(c => c.Id == clientId);

            updated.ClientSecrets.First().Description.Should().Be("Added via admin");
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateClientAsync_DoesNotAddSecret_WhenNewSecretIsNullOrWhitespace(string? newSecret)
    {
        var clientId = await SeedClientAsync();

        using (var context = CreateContext())
        {
            var service = CreateService(context);
            var viewModel = CreateDefaultEditViewModel();
            viewModel.NewSecret = newSecret;

            await service.UpdateClientAsync(clientId, viewModel);
        }

        using (var context = CreateContext())
        {
            var updated = await context.Clients
                .Include(c => c.ClientSecrets)
                .FirstAsync(c => c.Id == clientId);

            updated.ClientSecrets.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task UpdateClientAsync_PreservesExistingSecrets_WhenNewSecretAdded()
    {
        var client = CreateTestClient();
        client.ClientSecrets =
        [
            new()
            {
                Value = "existing-hashed-value",
                Description = "Existing secret",
                Type = "SharedSecret"
            }
        ];
        var clientId = await SeedClientAsync(client);

        using (var context = CreateContext())
        {
            var service = CreateService(context);
            var viewModel = CreateDefaultEditViewModel();
            viewModel.NewSecret = "new-secret";
            viewModel.NewSecretDescription = "New secret";

            await service.UpdateClientAsync(clientId, viewModel);
        }

        using (var context = CreateContext())
        {
            var updated = await context.Clients
                .Include(c => c.ClientSecrets)
                .FirstAsync(c => c.Id == clientId);

            updated.ClientSecrets.Should().HaveCount(2);
            updated.ClientSecrets.Should().Contain(s => s.Description == "Existing secret");
            updated.ClientSecrets.Should().Contain(s => s.Description == "New secret");
        }
    }

    [Fact]
    public async Task UpdateClientAsync_IgnoresWhitespaceOnlyGrantTypes()
    {
        var clientId = await SeedClientAsync();

        using (var context = CreateContext())
        {
            var service = CreateService(context);
            var viewModel = CreateDefaultEditViewModel();
            viewModel.AllowedGrantTypes = ["authorization_code", "", "  "];

            await service.UpdateClientAsync(clientId, viewModel);
        }

        using (var context = CreateContext())
        {
            var updated = await context.Clients
                .Include(c => c.AllowedGrantTypes)
                .FirstAsync(c => c.Id == clientId);

            updated.AllowedGrantTypes.Should().ContainSingle();
            updated.AllowedGrantTypes.First().GrantType.Should().Be("authorization_code");
        }
    }

    [Fact]
    public async Task UpdateClientAsync_TrimsGrantTypeValues()
    {
        var clientId = await SeedClientAsync();

        using (var context = CreateContext())
        {
            var service = CreateService(context);
            var viewModel = CreateDefaultEditViewModel();
            viewModel.AllowedGrantTypes = ["  authorization_code  "];

            await service.UpdateClientAsync(clientId, viewModel);
        }

        using (var context = CreateContext())
        {
            var updated = await context.Clients
                .Include(c => c.AllowedGrantTypes)
                .FirstAsync(c => c.Id == clientId);

            updated.AllowedGrantTypes.First().GrantType.Should().Be("authorization_code");
        }
    }

    [Fact]
    public async Task UpdateClientAsync_IgnoresWhitespaceOnlyRedirectUris()
    {
        var clientId = await SeedClientAsync();

        using (var context = CreateContext())
        {
            var service = CreateService(context);
            var viewModel = CreateDefaultEditViewModel();
            viewModel.RedirectUris = ["https://valid/callback", "", "  "];

            await service.UpdateClientAsync(clientId, viewModel);
        }

        using (var context = CreateContext())
        {
            var updated = await context.Clients
                .Include(c => c.RedirectUris)
                .FirstAsync(c => c.Id == clientId);

            updated.RedirectUris.Should().ContainSingle();
            updated.RedirectUris.First().RedirectUri.Should().Be("https://valid/callback");
        }
    }

    [Fact]
    public async Task UpdateClientAsync_TrimsRedirectUriValues()
    {
        var clientId = await SeedClientAsync();

        using (var context = CreateContext())
        {
            var service = CreateService(context);
            var viewModel = CreateDefaultEditViewModel();
            viewModel.RedirectUris = ["  https://valid/callback  "];

            await service.UpdateClientAsync(clientId, viewModel);
        }

        using (var context = CreateContext())
        {
            var updated = await context.Clients
                .Include(c => c.RedirectUris)
                .FirstAsync(c => c.Id == clientId);

            updated.RedirectUris.First().RedirectUri.Should().Be("https://valid/callback");
        }
    }

    [Fact]
    public async Task UpdateClientAsync_IgnoresWhitespaceOnlyPostLogoutRedirectUris()
    {
        var clientId = await SeedClientAsync();

        using (var context = CreateContext())
        {
            var service = CreateService(context);
            var viewModel = CreateDefaultEditViewModel();
            viewModel.PostLogoutRedirectUris = ["https://valid/signout", "", "  "];

            await service.UpdateClientAsync(clientId, viewModel);
        }

        using (var context = CreateContext())
        {
            var updated = await context.Clients
                .Include(c => c.PostLogoutRedirectUris)
                .FirstAsync(c => c.Id == clientId);

            updated.PostLogoutRedirectUris.Should().ContainSingle();
            updated.PostLogoutRedirectUris.First().PostLogoutRedirectUri.Should().Be("https://valid/signout");
        }
    }

    [Fact]
    public async Task UpdateClientAsync_TrimsPostLogoutRedirectUriValues()
    {
        var clientId = await SeedClientAsync();

        using (var context = CreateContext())
        {
            var service = CreateService(context);
            var viewModel = CreateDefaultEditViewModel();
            viewModel.PostLogoutRedirectUris = ["  https://valid/signout  "];

            await service.UpdateClientAsync(clientId, viewModel);
        }

        using (var context = CreateContext())
        {
            var updated = await context.Clients
                .Include(c => c.PostLogoutRedirectUris)
                .FirstAsync(c => c.Id == clientId);

            updated.PostLogoutRedirectUris.First().PostLogoutRedirectUri.Should().Be("https://valid/signout");
        }
    }

    [Fact]
    public async Task UpdateClientAsync_IgnoresWhitespaceOnlyScopes()
    {
        var clientId = await SeedClientAsync();

        using (var context = CreateContext())
        {
            var service = CreateService(context);
            var viewModel = CreateDefaultEditViewModel();
            viewModel.AllowedScopes = ["openid", "", "  "];

            await service.UpdateClientAsync(clientId, viewModel);
        }

        using (var context = CreateContext())
        {
            var updated = await context.Clients
                .Include(c => c.AllowedScopes)
                .FirstAsync(c => c.Id == clientId);

            updated.AllowedScopes.Should().ContainSingle();
            updated.AllowedScopes.First().Scope.Should().Be("openid");
        }
    }

    [Fact]
    public async Task UpdateClientAsync_TrimsScopeValues()
    {
        var clientId = await SeedClientAsync();

        using (var context = CreateContext())
        {
            var service = CreateService(context);
            var viewModel = CreateDefaultEditViewModel();
            viewModel.AllowedScopes = ["  openid  "];

            await service.UpdateClientAsync(clientId, viewModel);
        }

        using (var context = CreateContext())
        {
            var updated = await context.Clients
                .Include(c => c.AllowedScopes)
                .FirstAsync(c => c.Id == clientId);

            updated.AllowedScopes.First().Scope.Should().Be("openid");
        }
    }


    [Fact]
    public async Task GetClientsAsync_NoClients_ReturnsEmptyList()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetClientsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetClientsAsync_ClientsExist_ReturnsAllClients()
    {
        using (var seedContext = CreateContext())
        {
            seedContext.Clients.AddRange(
                new Client { ClientId = "client-a", ClientName = "Client A", Enabled = true },
                new Client { ClientId = "client-b", ClientName = "Client B", Enabled = false });
            await seedContext.SaveChangesAsync();
        }

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetClientsAsync();

        result.Should().HaveCount(2);
        result.Select(c => c.ClientId).Should().Contain(["client-a", "client-b"]);
    }

    [Fact]
    public async Task GetClientsAsync_ClientsExist_ReturnedInAscendingOrderByClientName()
    {
        using (var seedContext = CreateContext())
        {
            seedContext.Clients.AddRange(
                new Client { ClientId = "c", ClientName = "Zeta App" },
                new Client { ClientId = "a", ClientName = "Alpha App" },
                new Client { ClientId = "b", ClientName = "Middle App" });
            await seedContext.SaveChangesAsync();
        }

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetClientsAsync();

        result.Select(c => c.ClientName).Should().Equal("Alpha App", "Middle App", "Zeta App");
    }

    [Fact]
    public async Task GetClientsAsync_MapsAllFields()
    {
        using (var seedContext = CreateContext())
        {
            seedContext.Clients.Add(new Client
            {
                ClientId = "web-app",
                ClientName = "Web Application",
                Description = "The main web app",
                Enabled = true
            });
            await seedContext.SaveChangesAsync();
        }

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetClientsAsync();

        var item = result.Should().ContainSingle().Subject;
        item.ClientId.Should().Be("web-app");
        item.ClientName.Should().Be("Web Application");
        item.Description.Should().Be("The main web app");
        item.Enabled.Should().BeTrue();
        item.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetClientsAsync_NullClientName_ReturnsEmptyString()
    {
        using (var seedContext = CreateContext())
        {
            seedContext.Clients.Add(new Client
            {
                ClientId = "web-app",
                ClientName = null,
                Enabled = true
            });
            await seedContext.SaveChangesAsync();
        }

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetClientsAsync();

        var item = result.Should().ContainSingle().Subject;
        item.ClientName.Should().Be(string.Empty);
    }

    [Fact]
    public async Task GetClientsAsync_NullDescription_ReturnsNull()
    {
        using (var seedContext = CreateContext())
        {
            seedContext.Clients.Add(new Client
            {
                ClientId = "web-app",
                ClientName = "Web App",
                Description = null,
                Enabled = true
            });
            await seedContext.SaveChangesAsync();
        }

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetClientsAsync();

        var item = result.Should().ContainSingle().Subject;
        item.Description.Should().BeNull();
    }


    [Fact]
    public async Task GetClientForEditAsync_WithNoIdentityResourcesOrApiScopes_ReturnsEmptyAvailableScopes()
    {
        var clientId = await SeedClientAsync();

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetClientForEditAsync(clientId);

        result!.AvailableScopes.Should().BeEmpty();
    }

    [Fact]
    public async Task GetClientForEditAsync_WithOnlyIdentityResources_AvailableScopesContainsOnlyIdentityResourceNames()
    {
        using (var seedContext = CreateContext())
        {
            seedContext.IdentityResources.AddRange(
                new IdentityResource { Name = "openid" },
                new IdentityResource { Name = "profile" });
            await seedContext.SaveChangesAsync();
        }

        var clientId = await SeedClientAsync();

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetClientForEditAsync(clientId);

        result!.AvailableScopes.Should().BeEquivalentTo("openid", "profile");
    }

    [Fact]
    public async Task GetClientForEditAsync_WithOnlyApiScopes_AvailableScopesContainsOnlyApiScopeNames()
    {
        using (var seedContext = CreateContext())
        {
            seedContext.ApiScopes.AddRange(
                new ApiScope { Name = "api1" },
                new ApiScope { Name = "api2" });
            await seedContext.SaveChangesAsync();
        }

        var clientId = await SeedClientAsync();

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetClientForEditAsync(clientId);

        result!.AvailableScopes.Should().BeEquivalentTo("api1", "api2");
    }

    [Fact]
    public async Task GetClientForEditAsync_AvailableScopesListsIdentityResourcesBeforeApiScopes()
    {
        using (var seedContext = CreateContext())
        {
            seedContext.IdentityResources.Add(new IdentityResource { Name = "openid" });
            seedContext.ApiScopes.Add(new ApiScope { Name = "api1" });
            await seedContext.SaveChangesAsync();
        }

        var clientId = await SeedClientAsync();

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetClientForEditAsync(clientId);

        result!.AvailableScopes.Should().HaveCount(2);
        result.AvailableScopes[0].Should().Be("openid");
        result.AvailableScopes[1].Should().Be("api1");
    }

    [Fact]
    public async Task GetClientForEditAsync_WhenSameNameExistsInIdentityResourcesAndApiScopes_AppearsInAvailableScopesTwice()
    {
        using (var seedContext = CreateContext())
        {
            seedContext.IdentityResources.Add(new IdentityResource { Name = "shared-scope" });
            seedContext.ApiScopes.Add(new ApiScope { Name = "shared-scope" });
            await seedContext.SaveChangesAsync();
        }

        var clientId = await SeedClientAsync();

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetClientForEditAsync(clientId);

        result!.AvailableScopes.Count(s => s == "shared-scope").Should().Be(2);
    }
}

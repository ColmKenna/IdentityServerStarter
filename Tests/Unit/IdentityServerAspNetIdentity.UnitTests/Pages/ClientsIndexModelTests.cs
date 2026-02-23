using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Options;
using IdentityServerAspNetIdentity.Pages.Admin.Clients;
using Microsoft.AspNetCore.Http;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class ClientsIndexModelTests : IDisposable
{
    private readonly ConfigurationDbContext _context;
    private readonly IndexModel _pageModel;
    private readonly ServiceProvider _serviceProvider;

    public ClientsIndexModelTests()
    {
        var dbName = $"TestConfigDb-{Guid.NewGuid()}";
        var services = new ServiceCollection();
        services.AddSingleton(new ConfigurationStoreOptions());
        services.AddDbContext<ConfigurationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ConfigurationDbContext>();
        _context.Database.EnsureCreated();

        _pageModel = new IndexModel(_context)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    public void Dispose()
    {
        _context.Dispose();
        _serviceProvider.Dispose();
    }

    // Phase 1 — Page loads
    [Fact]
    public async Task should_populate_clients_when_get()
    {
        // Arrange
        _context.Clients.AddRange(
            new Client { ClientId = "client1", ClientName = "Client One" },
            new Client { ClientId = "client2", ClientName = "Client Two" }
        );
        await _context.SaveChangesAsync();

        // Act
        await _pageModel.OnGetAsync();

        // Assert
        _pageModel.Clients.Should().HaveCount(2);
    }

    // Phase 2 — Empty state
    [Fact]
    public async Task should_return_empty_list_when_no_clients_exist()
    {
        // Act
        await _pageModel.OnGetAsync();

        // Assert
        _pageModel.Clients.Should().BeEmpty();
    }

    // Phase 2 — Data ordering
    [Fact]
    public async Task should_order_clients_by_name_when_get()
    {
        // Arrange
        _context.Clients.AddRange(
            new Client { ClientId = "z-client", ClientName = "Zebra Client" },
            new Client { ClientId = "a-client", ClientName = "Alpha Client" },
            new Client { ClientId = "m-client", ClientName = "Middle Client" }
        );
        await _context.SaveChangesAsync();

        // Act
        await _pageModel.OnGetAsync();

        // Assert
        _pageModel.Clients.Should().HaveCount(3);
        _pageModel.Clients[0].ClientName.Should().Be("Alpha Client");
        _pageModel.Clients[1].ClientName.Should().Be("Middle Client");
        _pageModel.Clients[2].ClientName.Should().Be("Zebra Client");
    }

    // Phase 2 — Data properties
    [Fact]
    public async Task should_include_client_details_when_get()
    {
        // Arrange
        _context.Clients.Add(new Client
        {
            ClientId = "test-client",
            ClientName = "Test Client",
            Description = "A test client",
            Enabled = true
        });
        await _context.SaveChangesAsync();

        // Act
        await _pageModel.OnGetAsync();

        // Assert
        var client = _pageModel.Clients.Should().ContainSingle().Subject;
        client.ClientId.Should().Be("test-client");
        client.ClientName.Should().Be("Test Client");
        client.Description.Should().Be("A test client");
        client.Enabled.Should().BeTrue();
    }
}

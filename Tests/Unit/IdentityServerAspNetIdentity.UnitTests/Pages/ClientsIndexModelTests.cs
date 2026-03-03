using IdentityServerServices;
using IdentityServerServices.ViewModels;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class ClientsIndexModelTests
{
    private readonly Mock<IClientAdminService> _mockService;
    private readonly IdentityServerAspNetIdentity.Pages.Admin.Clients.IndexModel _pageModel;

    public ClientsIndexModelTests()
    {
        _mockService = new Mock<IClientAdminService>();
        _pageModel = new IdentityServerAspNetIdentity.Pages.Admin.Clients.IndexModel(_mockService.Object);
    }

    [Fact]
    public async Task OnGetAsync_InitializesClientsCollection()
    {
        _mockService
            .Setup(service => service.GetClientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ClientListItemDto>());

        await _pageModel.OnGetAsync();

        _pageModel.Clients.Should().NotBeNull();
    }

    [Fact]
    public async Task OnGetAsync_ServiceReturnsClients_PopulatesClients()
    {
        _mockService
            .Setup(service => service.GetClientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ClientListItemDto>
            {
                new()
                {
                    Id = 1,
                    ClientId = "web-app",
                    ClientName = "Web Application",
                    Description = "Main web app",
                    Enabled = true
                },
                new()
                {
                    Id = 2,
                    ClientId = "api-client",
                    ClientName = "API Client",
                    Description = null,
                    Enabled = false
                }
            });

        await _pageModel.OnGetAsync();

        _pageModel.Clients.Should().HaveCount(2);
        _pageModel.Clients.Select(c => c.ClientId).Should().Contain(new[] { "web-app", "api-client" });
    }

    [Fact]
    public async Task OnGetAsync_ServiceReturnsOrderedData_PreservesOrder()
    {
        _mockService
            .Setup(service => service.GetClientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ClientListItemDto>
            {
                new() { Id = 1, ClientId = "a", ClientName = "Alpha", Enabled = true },
                new() { Id = 2, ClientId = "m", ClientName = "Middle", Enabled = true },
                new() { Id = 3, ClientId = "z", ClientName = "Zeta", Enabled = true }
            });

        await _pageModel.OnGetAsync();

        _pageModel.Clients.Select(c => c.ClientName).Should().Equal("Alpha", "Middle", "Zeta");
    }

    [Fact]
    public async Task OnGetAsync_NoClients_ReturnsEmptyList()
    {
        _mockService
            .Setup(service => service.GetClientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ClientListItemDto>());

        await _pageModel.OnGetAsync();

        _pageModel.Clients.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGetAsync_CallsServiceOnce()
    {
        _mockService
            .Setup(service => service.GetClientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ClientListItemDto>());

        await _pageModel.OnGetAsync();

        _mockService.Verify(
            service => service.GetClientsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

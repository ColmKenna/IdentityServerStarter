using IdentityServerAspNetIdentity.Pages.Admin.Clients;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using FluentAssertions;

namespace IdentityServerAspNetIdentity.UnitTests.Pages.Admin.Clients;

public class IndexModelTests
{
    private readonly Mock<IClientAdminService> _mockClientAdminService;
    private readonly IndexModel _sut;

    public IndexModelTests()
    {
        _mockClientAdminService = new Mock<IClientAdminService>();

        _sut = new IndexModel(_mockClientAdminService.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task OnGetAsync_ShouldPopulateClients_WhenServiceReturnsData()
    {
        // Arrange
        var expectedClients = new List<ClientListItemDto>
        {
            new() { Id = 1, ClientId = "spa-client", ClientName = "SPA", Description = "Single Page App", Enabled = true },
            new() { Id = 2, ClientId = "api-client", ClientName = "API", Description = "API Client", Enabled = false }
        };

        _mockClientAdminService
            .Setup(x => x.GetClientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedClients);

        // Act
        await _sut.OnGetAsync();

        // Assert
        _sut.Clients.Should().HaveCount(2);
        _sut.Clients.Should().BeEquivalentTo(expectedClients);
    }

    [Fact]
    public async Task OnGetAsync_ShouldReturnEmptyList_WhenServiceReturnsNoClients()
    {
        // Arrange
        _mockClientAdminService
            .Setup(x => x.GetClientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ClientListItemDto>());

        // Act
        await _sut.OnGetAsync();

        // Assert
        _sut.Clients.Should().BeEmpty();
    }
}

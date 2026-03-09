using IdentityServerAspNetIdentity.Pages.Admin.Clients;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading;

namespace IdentityServerAspNetIdentity.UnitTests.Pages.Admin.Clients;

public class IndexModelTests
{
    private readonly Mock<IClientAdminService> _clientAdminService = new();

    [Fact]
    public async Task OnGetAsync_ShouldPopulateClients_WhenServiceReturnsResults()
    {
        var expectedClients = new List<ClientListItemDto>
        {
            new() { Id = 3, ClientId = "web-app", ClientName = "Web App", Description = "Interactive client", Enabled = true },
            new() { Id = 7, ClientId = "worker", ClientName = "Worker", Description = null, Enabled = false }
        };

        _clientAdminService.Setup(x => x.GetClientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedClients);

        var sut = CreateSut();

        await sut.OnGetAsync();

        sut.Clients.Should().BeEquivalentTo(expectedClients);
    }

    [Fact]
    public async Task OnGetAsync_ShouldPassRequestAbortedToken_WhenHttpContextHasCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var sut = CreateSut();
        sut.PageContext.HttpContext.RequestAborted = cts.Token;

        _clientAdminService.Setup(x => x.GetClientsAsync(cts.Token))
            .ReturnsAsync([]);

        await sut.OnGetAsync();

        _clientAdminService.Verify(x => x.GetClientsAsync(cts.Token), Times.Once);
    }

    private IndexModel CreateSut()
    {
        return new IndexModel(_clientAdminService.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }
}
